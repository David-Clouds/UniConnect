using System.Globalization;
using System.Text;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;

namespace UniConnect.Services.Chatbot;

/// <summary>
/// Asistente virtual de UniConnect ("Uni"). Responde preguntas sobre eventos, tickets y Join Us
/// usando Claude con los datos reales de la base. Es opcional, como las demás integraciones:
/// sin la clave <c>Anthropic__ApiKey</c> responde con reglas simples a partir de la base.
/// </summary>
public sealed class ServicioChatbot
{
    public const int MaxMensajes = 12;
    public const int MaxCaracteres = 1000;

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");
    private static readonly TimeSpan VigenciaCatalogo = TimeSpan.FromMinutes(2);

    private readonly AnthropicClient? _cliente;
    private readonly string _modelo;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ServicioChatbot> _logger;

    private readonly SemaphoreSlim _candado = new(1, 1);
    private Catalogo? _catalogo;

    public ServicioChatbot(IConfiguration configuracion, IServiceScopeFactory scopes, ILogger<ServicioChatbot> logger)
    {
        _scopes = scopes;
        _logger = logger;
        _modelo = configuracion["Anthropic:Model"] is { Length: > 0 } m ? m : "claude-opus-5-5";

        var clave = configuracion["Anthropic:ApiKey"];
        if (!string.IsNullOrWhiteSpace(clave))
            _cliente = new AnthropicClient { ApiKey = clave.Trim() };
        else
            _logger.LogInformation("Chatbot: sin Anthropic__ApiKey, se usarán respuestas por reglas.");
    }

    public bool Configurado => _cliente is not null;

    /// <summary>Fuerza a releer eventos e iniciativas en la próxima pregunta (por ejemplo, tras crear un evento).</summary>
    public void InvalidarCatalogo() => _catalogo = null;

    public async Task<RespuestaChatbotDto> ResponderAsync(IReadOnlyList<MensajeChatbotDto> mensajes, CancellationToken ct)
    {
        var catalogo = await ObtenerCatalogoAsync(ct);
        var pregunta = mensajes[^1].Texto;

        if (_cliente is null)
            return new RespuestaChatbotDto(ResponderPorReglas(pregunta, catalogo), "reglas");

        try
        {
            var texto = await ResponderConClaudeAsync(_cliente, mensajes, catalogo, ct);
            if (!string.IsNullOrWhiteSpace(texto))
                return new RespuestaChatbotDto(texto.Trim(), "claude");
        }
        catch (AnthropicRateLimitException ex)
        {
            _logger.LogWarning(ex, "Chatbot: límite de uso de la API de Claude.");
        }
        catch (AnthropicApiException ex)
        {
            _logger.LogWarning(ex, "Chatbot: la API de Claude devolvió un error.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Chatbot: no se pudo contactar a la API de Claude.");
        }

        return new RespuestaChatbotDto(ResponderPorReglas(pregunta, catalogo), "reglas");
    }

    // ═══════════════════════════ Claude ═══════════════════════════

    private async Task<string?> ResponderConClaudeAsync(AnthropicClient cliente,
        IReadOnlyList<MensajeChatbotDto> mensajes, Catalogo catalogo, CancellationToken ct)
    {
        var historial = new List<BetaMessageParam>();
        foreach (var m in mensajes)
        {
            if (m.Rol == "assistant") historial.Add(new BetaAssistantMessageParam { Content = m.Texto });
            else historial.Add(new BetaUserMessageParam { Content = m.Texto });
        }

        var respuesta = await cliente.Beta.Messages.Create(new MessageCreateParams
        {
            Model = _modelo,
            MaxTokens = 2048,
            // Chat de respuestas cortas: esfuerzo bajo = más rápido y barato.
            OutputConfig = new BetaOutputConfig { Effort = Effort.Low },
            // Si un filtro de seguridad rechaza la consulta, la API la reintenta con otro modelo.
            Betas = ["server-side-fallback-2026-07-01"],
            Fallbacks = new Default(),
            System = new List<BetaTextBlockParam>
            {
                new() { Text = InstruccionesSistema, CacheControl = new BetaCacheControlEphemeral() },
                new() { Text = catalogo.Texto }
            },
            Messages = historial
        }, cancellationToken: ct);

        if (respuesta.StopReason == "refusal")
            return "Lo siento, no puedo ayudarte con eso. Puedo contarte sobre eventos, tickets o iniciativas de Join Us. 🙂";

        var texto = new StringBuilder();
        foreach (var bloque in respuesta.Content)
        {
            if (bloque.TryPickText(out var t)) texto.Append(t.Text);
        }
        return texto.ToString();
    }

    private const string InstruccionesSistema = """
        Eres "Uni", el asistente virtual de UniConnect, la plataforma de eventos universitarios de la
        Universidad de San Martín de Porres (USMP). Hablas en español, con un tono cercano y amable
        para estudiantes universitarios.

        Ayudas con:
        - Encontrar eventos (por categoría, fecha, precio o tema) y recomendar los que encajen con lo que pide el estudiante.
        - Explicar cómo usar la plataforma:
          • Inicio (/Eventos): lista de eventos con filtros por categoría, fecha y precio.
          • Detalle de evento: los eventos gratuitos tienen el botón "Registrarme"; los de pago, "Comprar ticket" (pago simulado).
          • Mis Tickets (/Eventos/MisTickets): tickets del estudiante con su código QR.
          • Join Us (/Comunidad): voluntariados, comunidades y causas solidarias; se puede unir desde el detalle.
          • Mi Perfil (/Perfil): eventos, tickets y voluntariados del estudiante.
          • Panel Organizador (/Organizador/Panel): solo para responsables de organizadores y el administrador;
            el administrador puede crear eventos con el botón "+ Crear evento".
        - Responder dudas sobre las iniciativas de Join Us.

        Reglas:
        - Usa SOLO los datos del catálogo que viene a continuación para hablar de eventos e iniciativas.
          Nunca inventes eventos, fechas, precios, lugares ni organizadores. Si algo no está en el catálogo, dilo.
        - Cuando menciones un evento o iniciativa, incluye su enlace tal como aparece en el catálogo
          (por ejemplo /Eventos/Detalle/12 o /Comunidad/Detalle/3), en texto plano.
        - Los precios están en soles (S/). Precio 0 significa gratuito.
        - Respuestas breves: 2 a 6 oraciones o una lista corta. Texto plano, sin tablas ni encabezados markdown.
        - No pidas ni muestres datos personales (contraseñas, correos, códigos de alumno). No puedes comprar
          tickets ni registrar a nadie: indica los pasos para hacerlo en la plataforma.
        - Si la pregunta no tiene relación con la universidad o la plataforma, responde con amabilidad que solo
          puedes ayudar con UniConnect.
        """;

    // ═══════════════════════════ Catálogo (datos reales) ═══════════════════════════

    private sealed record EventoCatalogo(int Id, string Titulo, string Categoria, DateTime Fecha, decimal Precio, string Organizador, string? Descripcion);
    private sealed record ComunidadCatalogo(int Id, string Nombre, string TipoApoyo, string Organizacion, string Ubicacion, string? Frecuencia, string Descripcion);
    private sealed record Catalogo(DateTime Generado, string Texto, IReadOnlyList<EventoCatalogo> Eventos, IReadOnlyList<ComunidadCatalogo> Comunidades);

    private async Task<Catalogo> ObtenerCatalogoAsync(CancellationToken ct)
    {
        var actual = _catalogo;
        if (actual is not null && DateTime.UtcNow - actual.Generado < VigenciaCatalogo) return actual;

        await _candado.WaitAsync(ct);
        try
        {
            if (_catalogo is not null && DateTime.UtcNow - _catalogo.Generado < VigenciaCatalogo) return _catalogo;

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ahora = DateTime.Now;

            var eventos = await db.Eventos.AsNoTracking()
                .Where(e => e.Fecha >= ahora.Date)
                .OrderBy(e => e.Fecha)
                .Take(60)
                .Select(e => new EventoCatalogo(e.Id, e.Titulo, e.Categoria, e.Fecha, e.Precio, e.Organizador.Nombre, e.Descripcion))
                .ToListAsync(ct);

            var comunidades = await db.Comunidades.AsNoTracking()
                .OrderBy(c => c.Nombre)
                .Take(40)
                .Select(c => new ComunidadCatalogo(c.Id, c.Nombre, c.TipoApoyo, c.Organizacion, c.Ubicacion, c.Frecuencia, c.Descripcion))
                .ToListAsync(ct);

            var sb = new StringBuilder();
            sb.AppendLine($"CATÁLOGO ACTUAL (fecha y hora de hoy: {ahora.ToString("dddd d 'de' MMMM 'de' yyyy, HH:mm", Cultura)})");
            sb.AppendLine();
            sb.AppendLine($"Próximos eventos ({eventos.Count}):");
            if (eventos.Count == 0) sb.AppendLine("- No hay eventos próximos publicados.");
            foreach (var e in eventos)
            {
                sb.Append($"- [{e.Titulo}] categoría {e.Categoria}; {e.Fecha.ToString("ddd d MMM yyyy, HH:mm", Cultura)}; ");
                sb.Append(e.Precio == 0 ? "gratuito" : $"S/ {e.Precio.ToString("0.00", Cultura)}");
                sb.Append($"; organiza {e.Organizador}; enlace /Eventos/Detalle/{e.Id}");
                if (!string.IsNullOrWhiteSpace(e.Descripcion)) sb.Append($". {Recortar(e.Descripcion, 220)}");
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine($"Iniciativas de Join Us ({comunidades.Count}):");
            if (comunidades.Count == 0) sb.AppendLine("- No hay iniciativas publicadas.");
            foreach (var c in comunidades)
            {
                sb.Append($"- [{c.Nombre}] {c.TipoApoyo}; {c.Organizacion}; {c.Ubicacion}");
                if (!string.IsNullOrWhiteSpace(c.Frecuencia)) sb.Append($"; {c.Frecuencia}");
                sb.Append($"; enlace /Comunidad/Detalle/{c.Id}. {Recortar(c.Descripcion, 180)}");
                sb.AppendLine();
            }

            _catalogo = new Catalogo(DateTime.UtcNow, sb.ToString(), eventos, comunidades);
            return _catalogo;
        }
        finally
        {
            _candado.Release();
        }
    }

    private static string Recortar(string texto, int max)
    {
        texto = texto.ReplaceLineEndings(" ").Trim();
        return texto.Length <= max ? texto : texto[..max].TrimEnd() + "…";
    }

    // ═══════════════════════════ Respaldo por reglas ═══════════════════════════

    private static string ResponderPorReglas(string pregunta, Catalogo catalogo)
    {
        var q = SinTildes(pregunta.ToLowerInvariant());
        bool Contiene(params string[] palabras) => palabras.Any(q.Contains);

        string ListarEventos(IEnumerable<EventoCatalogo> eventos, string titulo)
        {
            var lista = eventos.Take(5).ToList();
            if (lista.Count == 0) return "No encontré eventos próximos con ese criterio. Revisa Inicio (/Eventos) para ver todos.";
            var sb = new StringBuilder(titulo).AppendLine();
            foreach (var e in lista)
                sb.AppendLine($"• {e.Titulo} — {e.Fecha.ToString("d MMM, HH:mm", Cultura)} — {(e.Precio == 0 ? "Gratis" : $"S/ {e.Precio:0.00}")} → /Eventos/Detalle/{e.Id}");
            return sb.ToString().TrimEnd();
        }

        if (Contiene("hola", "buenas", "buenos", "hey"))
            return "¡Hola! Soy Uni, el asistente de UniConnect 👋 Pregúntame por eventos (por ejemplo: \"eventos gratis\" o \"eventos de música\"), tickets o voluntariados de Join Us.";

        var categoria = catalogo.Eventos.Select(e => e.Categoria).Distinct()
            .FirstOrDefault(c => q.Contains(SinTildes(c.ToLowerInvariant())));
        if (categoria is not null)
            return ListarEventos(catalogo.Eventos.Where(e => e.Categoria == categoria), $"Próximos eventos de {categoria}:");

        if (Contiene("gratis", "gratuito", "free", "sin costo"))
            return ListarEventos(catalogo.Eventos.Where(e => e.Precio == 0), "Próximos eventos gratuitos:");

        if (Contiene("ticket", "entrada", "qr", "comprar", "registr", "inscrib"))
            return "Para inscribirte abre el evento desde Inicio (/Eventos): si es gratuito pulsa \"Registrarme\"; si es de pago, \"Comprar ticket\". Tus tickets con su código QR quedan en Mis Tickets (/Eventos/MisTickets).";

        if (Contiene("voluntari", "join", "comunidad", "donacion", "colecta", "solidari", "ayudar"))
        {
            var lista = catalogo.Comunidades.Take(5).ToList();
            if (lista.Count == 0) return "Por ahora no hay iniciativas publicadas en Join Us (/Comunidad).";
            var sb = new StringBuilder("Algunas iniciativas de Join Us:").AppendLine();
            foreach (var c in lista) sb.AppendLine($"• {c.Nombre} ({c.TipoApoyo}) → /Comunidad/Detalle/{c.Id}");
            return sb.ToString().TrimEnd();
        }

        if (Contiene("crear evento", "publicar evento", "organizador", "panel"))
            return "El Panel Organizador (/Organizador/Panel) muestra tus eventos, tickets y participantes. Solo el administrador puede crear eventos con el botón \"+ Crear evento\".";

        if (Contiene("evento", "proximo", "semana", "hoy", "que hay"))
            return ListarEventos(catalogo.Eventos, "Estos son los próximos eventos:");

        return "Puedo ayudarte con eventos, tickets y Join Us. Prueba con: \"eventos gratis\", \"eventos de deportes\", \"¿cómo compro un ticket?\" o \"voluntariados\".";
    }

    private static string SinTildes(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);
        foreach (var c in normalizado)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

public sealed record MensajeChatbotDto(string Rol, string Texto);
public sealed record SolicitudChatbotDto(List<MensajeChatbotDto>? Mensajes);
public sealed record RespuestaChatbotDto(string Respuesta, string Fuente);
