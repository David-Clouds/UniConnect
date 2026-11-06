using System.Net.Http.Json;
using System.Text.RegularExpressions;
using UniConnect.Services.Integraciones;
using UniConnect.Services.Mensajeria;

namespace UniConnect.Services.TiempoReal;

/// <summary>
/// Envía notificaciones en tiempo real con PieHost (antes PieSocket) usando su API REST de publicación.
/// Los navegadores reciben los mensajes por WebSocket (ver wwwroot/js/notificaciones.js).
/// Configuración: PieHost:ClusterId, PieHost:ApiKey y PieHost:ApiSecret (el secreto solo vive en el servidor).
/// </summary>
public sealed partial class NotificadorPieHost
{
    public const string CanalAdministracion = "uniconnect-admin";
    public const string EventoTicketEmitido = "ticket-emitido";

    public static string CanalOrganizador(int organizadorId) => $"uniconnect-org-{organizadorId}";

    private static readonly HttpClient Http = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly string? _cluster;
    private readonly string? _apiKey;
    private readonly string? _secreto;
    private readonly EstadoIntegraciones _estado;
    private readonly ILogger<NotificadorPieHost> _logger;

    public NotificadorPieHost(IConfiguration configuracion, EstadoIntegraciones estado, ILogger<NotificadorPieHost> logger)
    {
        _estado = estado;
        _logger = logger;

        var cluster = configuracion["PieHost:ClusterId"]?.Trim().ToLowerInvariant();
        // El cluster forma parte del dominio (ej. "s1234.nyc1"): solo letras, números, guiones y puntos.
        _cluster = !string.IsNullOrEmpty(cluster) && ClusterValido().IsMatch(cluster) ? cluster : null;
        _apiKey = Limpiar(configuracion["PieHost:ApiKey"]);
        _secreto = Limpiar(configuracion["PieHost:ApiSecret"]);

        if (!string.IsNullOrEmpty(cluster) && _cluster is null)
            _logger.LogWarning("PieHost:ClusterId tiene un formato inválido; las notificaciones en tiempo real están desactivadas.");
    }

    public bool Configurado => _cluster is not null && _apiKey is not null && _secreto is not null;

    /// <summary>Datos que el navegador necesita para conectarse (la API key de PieHost es pública por diseño).</summary>
    public string? ClusterPublico => Configurado ? _cluster : null;
    public string? ApiKeyPublica => Configurado ? _apiKey : null;

    /// <summary>Avisa al organizador del evento y a los administradores. Nunca lanza excepciones.</summary>
    public async Task NotificarTicketAsync(TicketEmitidoMensaje mensaje, CancellationToken cancellationToken)
    {
        if (!Configurado) return;

        var datos = new
        {
            mensaje.TicketId,
            mensaje.EventoId,
            mensaje.OrganizadorId,
            Titulo = mensaje.TituloEvento,
            mensaje.Categoria,
            mensaje.Precio,
            mensaje.TotalTicketsEvento,
            EmitidoEn = mensaje.EmitidoEnUtc
        };

        await PublicarAsync(CanalOrganizador(mensaje.OrganizadorId), EventoTicketEmitido, datos, cancellationToken);
        await PublicarAsync(CanalAdministracion, EventoTicketEmitido, datos, cancellationToken);
    }

    private async Task PublicarAsync(string canal, string evento, object datos, CancellationToken cancellationToken)
    {
        try
        {
            // API de publicación de PieHost: key, secret, canal y message { event, data }.
            // El SDK oficial envía el canal como "channelId" y la documentación nueva como "roomId": se envían ambos.
            var cuerpo = new
            {
                key = _apiKey,
                secret = _secreto,
                channelId = canal,
                roomId = canal,
                message = new { @event = evento, data = datos }
            };

            using var respuesta = await Http.PostAsJsonAsync(
                $"https://{_cluster}.piesocket.com/api/publish", cuerpo, cancellationToken);

            if (respuesta.IsSuccessStatusCode)
            {
                _estado.NotificacionEnviada();
                _estado.ErrorPieHost(null);
            }
            else
            {
                var error = $"PieHost respondió {(int)respuesta.StatusCode} al publicar en {canal}.";
                _estado.ErrorPieHost(error);
                _logger.LogWarning("{Error}", error);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _estado.ErrorPieHost(ex.Message);
            _logger.LogWarning(ex, "No se pudo enviar la notificación de PieHost al canal {Canal}.", canal);
        }
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    [GeneratedRegex(@"^[a-z0-9-]{1,63}(\.[a-z0-9-]{1,63}){0,3}$")]
    private static partial Regex ClusterValido();
}
