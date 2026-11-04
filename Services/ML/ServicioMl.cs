using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using UniConnect.Data;

namespace UniConnect.Services.ML;

/// <summary>
/// Entrena y sirve los dos modelos de ML.NET de UniConnect:
///  • Clasificación multiclase (SdcaMaximumEntropy): sugiere la categoría de un evento a partir de su texto.
///  • Recomendación (Matrix Factorization): estima el interés de cada estudiante por cada categoría.
/// Se registra como singleton; <see cref="EntrenamientoMlService"/> lo reentrena periódicamente.
/// Si un modelo falla, el otro sigue funcionando y la web nunca se cae por ML.
/// </summary>
public sealed class ServicioMl
{
    private const int Semilla = 2026;

    private readonly ILogger<ServicioMl> _logger;
    private readonly SemaphoreSlim _entrenando = new(1, 1);
    private readonly SemaphoreSlim _solicitudes = new(0, 1);
    private static readonly TimeSpan EsperaMinimaEntreEntrenamientos = TimeSpan.FromSeconds(30);

    private volatile ModeloClasificacion? _clasificador;
    private volatile ModeloRecomendacion? _recomendador;
    private volatile EstadoMlDto _estado = EstadoMlDto.SinEntrenar;

    public ServicioMl(ILogger<ServicioMl> logger) => _logger = logger;

    public EstadoMlDto Estado => _estado;
    public IReadOnlyList<string> Categorias => DatosMl.Categorias;

    // ═══════════════════════════ Entrenamiento ═══════════════════════════

    /// <summary>Pide un reentrenamiento anticipado (como máximo uno cada 30 segundos).</summary>
    public void SolicitarReentrenamiento()
    {
        var ultimo = _estado.EntrenadoEn;
        if (ultimo is not null && DateTime.UtcNow - ultimo < EsperaMinimaEntreEntrenamientos) return;
        if (_solicitudes.CurrentCount > 0) return;
        try { _solicitudes.Release(); } catch (SemaphoreFullException) { /* ya había una solicitud pendiente */ }
    }

    /// <summary>Espera hasta que pase el intervalo o alguien pida un reentrenamiento.</summary>
    internal Task EsperarSiguienteEntrenamientoAsync(TimeSpan intervalo, CancellationToken ct) =>
        _solicitudes.WaitAsync(intervalo, ct);

    public async Task EntrenarAsync(AppDbContext db, CancellationToken ct)
    {
        await _entrenando.WaitAsync(ct);
        try
        {
            var reloj = Stopwatch.StartNew();

            // Datos reales de la base: eventos publicados y tickets emitidos.
            // (List<string>.Contains se traduce siempre a SQL; con arreglos C# 14 puede elegir Span.)
            var categoriasValidas = DatosMl.Categorias.ToList();
            var eventosReales = await db.Eventos.AsNoTracking()
                .Where(e => categoriasValidas.Contains(e.Categoria))
                .Select(e => new EventoTexto { Titulo = e.Titulo, Descripcion = e.Descripcion ?? "", Categoria = e.Categoria })
                .ToListAsync(ct);

            var ticketsReales = await db.Tickets.AsNoTracking()
                .Select(t => new { t.EstudianteId, t.Evento.Categoria })
                .ToListAsync(ct);

            var interesesReales = ticketsReales
                .Where(t => categoriasValidas.Contains(t.Categoria))
                .GroupBy(t => (t.EstudianteId, t.Categoria))
                // Un ticket indica interés alto; dos o más, interés máximo.
                .Select(g => new InteresCategoria
                {
                    UsuarioId = ClaveEstudiante(g.Key.EstudianteId),
                    Categoria = g.Key.Categoria,
                    Interes = g.Count() >= 2 ? 5f : 4.5f
                })
                .ToList();

            // El entrenamiento usa CPU: se hace fuera del hilo de la petición.
            var (clasificacion, recomendacion) = await Task.Run(() =>
                (EntrenarClasificador(eventosReales), EntrenarRecomendador(interesesReales)), ct);

            if (clasificacion.Modelo is not null) _clasificador = clasificacion.Modelo;
            if (recomendacion.Modelo is not null) _recomendador = recomendacion.Modelo;

            _estado = new EstadoMlDto(
                EntrenadoEn: DateTime.UtcNow,
                DuracionMs: reloj.ElapsedMilliseconds,
                Clasificacion: clasificacion.Estado,
                Recomendacion: recomendacion.Estado);

            _logger.LogInformation("Modelos ML entrenados en {Ms} ms (clasificación: {C}, recomendación: {R}).",
                reloj.ElapsedMilliseconds, clasificacion.Estado.Disponible, recomendacion.Estado.Disponible);
        }
        finally
        {
            _entrenando.Release();
        }
    }

    private (ModeloClasificacion? Modelo, EstadoModeloDto Estado) EntrenarClasificador(List<EventoTexto> reales)
    {
        var datos = DatosMl.EventosEtiquetados().Concat(reales).ToList();
        try
        {
            var ml = new MLContext(seed: Semilla);
            var vista = ml.Data.LoadFromEnumerable(datos);

            var pipeline = ml.Transforms.Conversion.MapValueToKey("Label", nameof(EventoTexto.Categoria))
                .Append(ml.Transforms.Text.FeaturizeText("TituloFeat", nameof(EventoTexto.Titulo)))
                .Append(ml.Transforms.Text.FeaturizeText("DescripcionFeat", nameof(EventoTexto.Descripcion)))
                .Append(ml.Transforms.Concatenate("Features", "TituloFeat", "DescripcionFeat"))
                .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"));

            // Métricas con 80 % para entrenar y 20 % para evaluar.
            double? exactitud = null, exactitudMacro = null;
            try
            {
                var division = ml.Data.TrainTestSplit(vista, testFraction: 0.2, seed: Semilla);
                var metricas = ml.MulticlassClassification.Evaluate(
                    pipeline.Fit(division.TrainSet).Transform(division.TestSet), labelColumnName: "Label");
                exactitud = Math.Round(metricas.MicroAccuracy, 3);
                exactitudMacro = Math.Round(metricas.MacroAccuracy, 3);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron calcular las métricas del clasificador.");
            }

            // Modelo final con todos los datos.
            var modelo = pipeline.Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel")).Fit(vista);
            var motor = ml.Model.CreatePredictionEngine<EventoTexto, PrediccionCategoria>(modelo);

            return (new ModeloClasificacion(motor, LeerEtiquetas(motor.OutputSchema)),
                new EstadoModeloDto(true, "SdcaMaximumEntropy (clasificación multiclase)", datos.Count, reales.Count,
                    new Dictionary<string, double?> { ["microAccuracy"] = exactitud, ["macroAccuracy"] = exactitudMacro }, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el entrenamiento del clasificador de eventos.");
            return (null, new EstadoModeloDto(false, "SdcaMaximumEntropy", datos.Count, reales.Count, null, ex.Message));
        }
    }

    private (ModeloRecomendacion? Modelo, EstadoModeloDto Estado) EntrenarRecomendador(List<InteresCategoria> reales)
    {
        var datos = DatosMl.InteresesSimulados().Concat(reales).ToList();
        try
        {
            var ml = new MLContext(seed: Semilla);
            var vista = ml.Data.LoadFromEnumerable(datos);

            var opciones = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "UsuarioKey",
                MatrixRowIndexColumnName = "CategoriaKey",
                LabelColumnName = nameof(InteresCategoria.Interes),
                NumberOfIterations = 60,
                ApproximationRank = 6,
                Quiet = true
            };

            var pipeline = ml.Transforms.Conversion.MapValueToKey("UsuarioKey", nameof(InteresCategoria.UsuarioId))
                .Append(ml.Transforms.Conversion.MapValueToKey("CategoriaKey", nameof(InteresCategoria.Categoria)))
                .Append(ml.Recommendation().Trainers.MatrixFactorization(opciones));

            double? rmse = null;
            try
            {
                var division = ml.Data.TrainTestSplit(vista, testFraction: 0.2, seed: Semilla);
                var metricas = ml.Regression.Evaluate(
                    pipeline.Fit(division.TrainSet).Transform(division.TestSet),
                    labelColumnName: nameof(InteresCategoria.Interes), scoreColumnName: "Score");
                rmse = Math.Round(metricas.RootMeanSquaredError, 3);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron calcular las métricas del recomendador.");
            }

            var modelo = pipeline.Fit(vista);
            var motor = ml.Model.CreatePredictionEngine<InteresCategoria, PrediccionInteres>(modelo);
            var estudiantesReales = reales.Select(r => r.UsuarioId).Distinct().Count();

            return (new ModeloRecomendacion(motor),
                new EstadoModeloDto(true, "Matrix Factorization (recomendación)", datos.Count, reales.Count,
                    new Dictionary<string, double?> { ["rmse"] = rmse, ["estudiantesReales"] = estudiantesReales }, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el entrenamiento del recomendador de eventos.");
            return (null, new EstadoModeloDto(false, "Matrix Factorization", datos.Count, reales.Count, null, ex.Message));
        }
    }

    private static string[] LeerEtiquetas(DataViewSchema esquema)
    {
        try
        {
            VBuffer<ReadOnlyMemory<char>> nombres = default;
            esquema["Score"].Annotations.GetValue("SlotNames", ref nombres);
            return nombres.DenseValues().Select(n => n.ToString()).ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static string ClaveEstudiante(int estudianteId) => $"est-{estudianteId}";

    // ═══════════════════════════ Clasificación ═══════════════════════════

    public ResultadoClasificacionDto? Clasificar(string titulo, string? descripcion)
    {
        var modelo = _clasificador;
        if (modelo is null) return null;

        PrediccionCategoria prediccion;
        lock (modelo.Bloqueo) // PredictionEngine no es seguro entre hilos
        {
            prediccion = modelo.Motor.Predict(new EventoTexto { Titulo = titulo, Descripcion = descripcion ?? "" });
        }

        var alternativas = modelo.Etiquetas.Length == prediccion.Score.Length
            ? modelo.Etiquetas.Zip(prediccion.Score, (c, p) => new CategoriaProbableDto(c, Math.Round(p, 3)))
                .OrderByDescending(a => a.Probabilidad).ToList()
            : new List<CategoriaProbableDto> { new(prediccion.Categoria, Math.Round(prediccion.Score.DefaultIfEmpty(0f).Max(), 3)) };

        return new ResultadoClasificacionDto(prediccion.Categoria, alternativas[0].Probabilidad, alternativas);
    }

    // ═══════════════════════════ Recomendación ═══════════════════════════

    /// <summary>Interés estimado (1 a 5) del estudiante por cada categoría; null si el modelo no lo conoce.</summary>
    private Dictionary<string, float>? EstimarIntereses(int estudianteId)
    {
        var modelo = _recomendador;
        if (modelo is null) return null;

        var clave = ClaveEstudiante(estudianteId);
        var intereses = new Dictionary<string, float>();
        lock (modelo.Bloqueo)
        {
            foreach (var categoria in DatosMl.Categorias)
            {
                var score = modelo.Motor.Predict(new InteresCategoria { UsuarioId = clave, Categoria = categoria }).Score;
                if (!float.IsNaN(score)) intereses[categoria] = Math.Clamp(score, 1f, 5f);
            }
        }
        // Un estudiante sin tickets no está en el modelo (sus predicciones salen NaN).
        return intereses.Count == 0 ? null : intereses;
    }

    public async Task<RecomendacionesDto> RecomendarAsync(
        AppDbContext db, int? estudianteId, int limite, bool incluirPasados, CancellationToken ct)
    {
        limite = Math.Clamp(limite, 1, 12);
        var ahora = DateTime.Now;

        var consulta = db.Eventos.AsNoTracking().AsQueryable();
        if (!incluirPasados) consulta = consulta.Where(e => e.Fecha >= ahora);

        var categoriasPropias = new HashSet<string>();
        if (estudianteId is int id)
        {
            consulta = consulta.Where(e => !e.Tickets.Any(t => t.EstudianteId == id));
            categoriasPropias = (await db.Tickets.AsNoTracking()
                .Where(t => t.EstudianteId == id).Select(t => t.Evento.Categoria).ToListAsync(ct)).ToHashSet();
        }

        var candidatos = await consulta
            .Select(e => new
            {
                e.Id, e.Titulo, e.Categoria, e.Fecha, e.Precio, e.ImagenUrl,
                Organizador = e.Organizador.Nombre,
                Popularidad = e.Tickets.Count
            })
            .ToListAsync(ct);

        var intereses = estudianteId is int idEst ? EstimarIntereses(idEst) : null;

        // Tiene tickets pero el modelo todavía no lo conoce (se inscribió hace poco): reentrenar ya.
        if (intereses is null && categoriasPropias.Overlaps(DatosMl.Categorias))
            SolicitarReentrenamiento();

        if (intereses is not null)
        {
            var eventos = candidatos
                .Select(c => new { c, Interes = intereses.TryGetValue(c.Categoria, out var v) ? v : 1f })
                .OrderByDescending(x => x.Interes).ThenByDescending(x => x.c.Popularidad).ThenBy(x => x.c.Fecha)
                .Take(limite)
                .Select(x => new EventoRecomendadoDto(
                    x.c.Id, x.c.Titulo, x.c.Categoria, x.c.Fecha, x.c.Precio, x.c.ImagenUrl, x.c.Organizador,
                    Afinidad: (int)Math.Round((x.Interes - 1f) / 4f * 100),
                    Motivo: categoriasPropias.Contains(x.c.Categoria)
                        ? $"Porque te interesan eventos de {x.c.Categoria}"
                        : $"Estudiantes con tus intereses también eligen {x.c.Categoria}"))
                .ToList();
            return new RecomendacionesDto(true, "ML.NET · Matrix Factorization", eventos);
        }

        // Sin sesión, sin tickets o sin modelo: los eventos más populares.
        var populares = candidatos
            .OrderByDescending(c => c.Popularidad).ThenBy(c => c.Fecha)
            .Take(limite)
            .Select(c => new EventoRecomendadoDto(
                c.Id, c.Titulo, c.Categoria, c.Fecha, c.Precio, c.ImagenUrl, c.Organizador,
                Afinidad: null,
                Motivo: c.Popularidad > 0 ? $"Popular: {c.Popularidad} inscrito(s)" : "Próximo evento"))
            .ToList();
        return new RecomendacionesDto(false, "Popularidad", populares);
    }

    // ═══════════════════════════ Tipos internos ═══════════════════════════

    private sealed record ModeloClasificacion(PredictionEngine<EventoTexto, PrediccionCategoria> Motor, string[] Etiquetas)
    {
        public object Bloqueo { get; } = new();
    }

    private sealed record ModeloRecomendacion(PredictionEngine<InteresCategoria, PrediccionInteres> Motor)
    {
        public object Bloqueo { get; } = new();
    }
}

// ═══════════════════════════ DTOs públicos ═══════════════════════════

public sealed record CategoriaProbableDto(string Categoria, double Probabilidad);

public sealed record ResultadoClasificacionDto(
    string Categoria, double Confianza, IReadOnlyList<CategoriaProbableDto> Alternativas);

public sealed record EventoRecomendadoDto(
    int Id, string Titulo, string Categoria, DateTime Fecha, decimal Precio, string? ImagenUrl,
    string Organizador, int? Afinidad, string Motivo);

public sealed record RecomendacionesDto(bool Personalizadas, string Modelo, IReadOnlyList<EventoRecomendadoDto> Eventos);

public sealed record EstadoModeloDto(
    bool Disponible, string Algoritmo, int Ejemplos, int EjemplosReales,
    IReadOnlyDictionary<string, double?>? Metricas, string? Error);

public sealed record EstadoMlDto(
    DateTime? EntrenadoEn, long DuracionMs, EstadoModeloDto? Clasificacion, EstadoModeloDto? Recomendacion)
{
    public static readonly EstadoMlDto SinEntrenar = new(null, 0, null, null);
}
