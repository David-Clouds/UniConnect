using System.Globalization;
using System.Text.Json.Serialization;
using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.Services.Integraciones;

namespace UniConnect.Services.Busqueda;

/// <summary>
/// Buscador de eventos y de iniciativas de Join Us con Algolia.
/// Configuración: Algolia:AppId, Algolia:SearchKey (búsqueda), Algolia:AdminKey (solo para indexar)
/// y Algolia:IndexName (opcional, "uniconnect" por defecto).
/// Sin Algolia configurado, o si Algolia falla, busca directamente en la base de datos.
/// </summary>
public sealed class ServicioBusqueda : IDisposable
{
    private const int MaxResultados = 6;

    private readonly SearchClient? _busqueda;
    private readonly SearchClient? _indexacion;
    private readonly string _indice;
    private readonly EstadoIntegraciones _estado;
    private readonly ILogger<ServicioBusqueda> _logger;

    public ServicioBusqueda(IConfiguration configuracion, EstadoIntegraciones estado, ILogger<ServicioBusqueda> logger)
    {
        _estado = estado;
        _logger = logger;

        var appId = Limpiar(configuracion["Algolia:AppId"]);
        var claveBusqueda = Limpiar(configuracion["Algolia:SearchKey"]);
        var claveAdmin = Limpiar(configuracion["Algolia:AdminKey"]);
        _indice = Limpiar(configuracion["Algolia:IndexName"]) ?? "uniconnect";

        if (appId is not null && claveBusqueda is not null)
            _busqueda = new SearchClient(appId, claveBusqueda);
        if (appId is not null && claveAdmin is not null)
            _indexacion = new SearchClient(appId, claveAdmin);
    }

    public bool BusquedaConfigurada => _busqueda is not null;
    public bool IndexacionConfigurada => _indexacion is not null;

    // ═══════════════════════════ Indexación ═══════════════════════════

    /// <summary>Sube a Algolia todos los eventos e iniciativas. Devuelve la cantidad de registros enviados.</summary>
    public async Task<int> IndexarAsync(AppDbContext db, CancellationToken ct)
    {
        if (_indexacion is null) return 0;

        var cultura = CultureInfo.GetCultureInfo("es-PE");

        var eventos = await db.Eventos.AsNoTracking()
            .Select(e => new { e.Id, e.Titulo, e.Descripcion, e.Categoria, e.Fecha, e.Precio, e.ImagenUrl, Organizador = e.Organizador.Nombre })
            .ToListAsync(ct);

        var comunidades = await db.Comunidades.AsNoTracking()
            .Select(c => new { c.Id, c.Nombre, c.Descripcion, c.TipoApoyo, c.Organizacion, c.Ubicacion, c.Tags, c.ImagenUrl })
            .ToListAsync(ct);

        var registros = eventos.Select(e => new RegistroBusqueda
            {
                ObjectId = $"evento-{e.Id}",
                Tipo = "Evento",
                Titulo = e.Titulo,
                Categoria = e.Categoria,
                Descripcion = Recortar(e.Descripcion),
                Detalle = $"{e.Fecha.ToString("d MMM yyyy", cultura)} · {(e.Precio == 0 ? "Gratis" : "S/ " + e.Precio.ToString("0.00", cultura))} · {e.Organizador}",
                ImagenUrl = e.ImagenUrl,
                // Segundos desde 1970: número seguro para el ranking de Algolia.
                Orden = (long)(e.Fecha - DateTime.UnixEpoch).TotalSeconds
            })
            .Concat(comunidades.Select(c => new RegistroBusqueda
            {
                ObjectId = $"comunidad-{c.Id}",
                Tipo = "Join Us",
                Titulo = c.Nombre,
                Categoria = c.TipoApoyo,
                Descripcion = Recortar($"{c.Descripcion} {c.Tags}"),
                Detalle = $"{c.TipoApoyo} · {c.Organizacion} · {c.Ubicacion}",
                ImagenUrl = c.ImagenUrl,
                Orden = 0
            }))
            .ToList();

        await _indexacion.SetSettingsAsync(_indice, new IndexSettings
        {
            SearchableAttributes = new List<string> { "titulo", "categoria", "descripcion", "detalle" },
            AttributesForFaceting = new List<string> { "tipo" },
            CustomRanking = new List<string> { "desc(orden)" },
            IndexLanguages = new List<SupportedLanguage> { SupportedLanguage.Es },
            QueryLanguages = new List<SupportedLanguage> { SupportedLanguage.Es }
        }, cancellationToken: ct);

        await _indexacion.SaveObjectsAsync(_indice, registros, waitForTasks: false, cancellationToken: ct);

        _estado.IndexacionCompletada(registros.Count);
        _logger.LogInformation("Algolia: {Cantidad} registros indexados en '{Indice}'.", registros.Count, _indice);
        return registros.Count;
    }

    // ═══════════════════════════ Búsqueda ═══════════════════════════

    public async Task<ResultadoBusquedaDto> BuscarAsync(AppDbContext db, string texto, CancellationToken ct)
    {
        if (_busqueda is not null)
        {
            try
            {
                var respuesta = await _busqueda.SearchSingleIndexAsync<RegistroBusqueda>(_indice,
                    new SearchParams(new SearchParamsObject { Query = texto, HitsPerPage = MaxResultados }),
                    cancellationToken: ct);

                var resultados = (respuesta.Hits ?? new List<RegistroBusqueda>())
                    .Select(ADto)
                    .Where(r => r is not null)
                    .Select(r => r!)
                    .ToList();

                return new ResultadoBusquedaDto("Algolia", resultados);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _estado.ErrorAlgolia(ex.Message);
                _logger.LogWarning(ex, "Algolia no respondió; se busca en la base de datos.");
            }
        }

        return new ResultadoBusquedaDto("Base de datos", await BuscarEnBaseAsync(db, texto, ct));
    }

    private static async Task<List<ResultadoItemDto>> BuscarEnBaseAsync(AppDbContext db, string texto, CancellationToken ct)
    {
        // LIKE no distingue mayúsculas en SQLite; se escapan los comodines que escriba el usuario.
        var patron = "%" + texto.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var cultura = CultureInfo.GetCultureInfo("es-PE");

        var eventos = await db.Eventos.AsNoTracking()
            .Where(e => EF.Functions.Like(e.Titulo, patron, "\\")
                     || EF.Functions.Like(e.Categoria, patron, "\\")
                     || (e.Descripcion != null && EF.Functions.Like(e.Descripcion, patron, "\\")))
            .OrderByDescending(e => e.Fecha)
            .Take(MaxResultados)
            .Select(e => new { e.Id, e.Titulo, e.Categoria, e.Fecha, e.Precio, e.ImagenUrl })
            .ToListAsync(ct);

        var comunidades = await db.Comunidades.AsNoTracking()
            .Where(c => EF.Functions.Like(c.Nombre, patron, "\\")
                     || EF.Functions.Like(c.TipoApoyo, patron, "\\")
                     || EF.Functions.Like(c.Descripcion, patron, "\\"))
            .Take(MaxResultados)
            .Select(c => new { c.Id, c.Nombre, c.TipoApoyo, c.Organizacion, c.ImagenUrl })
            .ToListAsync(ct);

        return eventos
            .Select(e => new ResultadoItemDto("Evento", e.Titulo,
                $"{e.Categoria} · {e.Fecha.ToString("d MMM yyyy", cultura)} · {(e.Precio == 0 ? "Gratis" : "S/ " + e.Precio.ToString("0.00", cultura))}",
                $"/Eventos/Detalle/{e.Id}", ImagenSegura(e.ImagenUrl)))
            .Concat(comunidades.Select(c => new ResultadoItemDto("Join Us", c.Nombre,
                $"{c.TipoApoyo} · {c.Organizacion}", $"/Comunidad/Detalle/{c.Id}", ImagenSegura(c.ImagenUrl))))
            .Take(MaxResultados)
            .ToList();
    }

    /// <summary>La URL se arma desde el objectID, nunca desde un texto guardado en el índice.</summary>
    private static ResultadoItemDto? ADto(RegistroBusqueda registro)
    {
        var partes = (registro.ObjectId ?? string.Empty).Split('-', 2);
        if (partes.Length != 2 || !int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            return null;

        var url = partes[0] switch
        {
            "evento" => $"/Eventos/Detalle/{id}",
            "comunidad" => $"/Comunidad/Detalle/{id}",
            _ => null
        };
        if (url is null) return null;

        return new ResultadoItemDto(registro.Tipo ?? "Evento", registro.Titulo ?? string.Empty,
            registro.Detalle ?? string.Empty, url, ImagenSegura(registro.ImagenUrl));
    }

    private static string? ImagenSegura(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri.AbsoluteUri : null;

    private static string? Recortar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Length <= 500 ? texto.Trim() : texto.Substring(0, 500).Trim();

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    public void Dispose()
    {
        _busqueda?.Dispose();
        _indexacion?.Dispose();
    }
}

/// <summary>Registro guardado en el índice de Algolia.</summary>
public sealed class RegistroBusqueda
{
    [JsonPropertyName("objectID")] public string? ObjectId { get; set; }
    [JsonPropertyName("tipo")] public string? Tipo { get; set; }
    [JsonPropertyName("titulo")] public string? Titulo { get; set; }
    [JsonPropertyName("categoria")] public string? Categoria { get; set; }
    [JsonPropertyName("descripcion")] public string? Descripcion { get; set; }
    [JsonPropertyName("detalle")] public string? Detalle { get; set; }
    [JsonPropertyName("imagenUrl")] public string? ImagenUrl { get; set; }
    [JsonPropertyName("orden")] public long Orden { get; set; }
}

public sealed record ResultadoItemDto(string Tipo, string Titulo, string Detalle, string Url, string? ImagenUrl);

public sealed record ResultadoBusquedaDto(string Fuente, IReadOnlyList<ResultadoItemDto> Resultados);
