using System.Text.Json;
using RabbitMQ.Client;
using UniConnect.Services.Integraciones;

namespace UniConnect.Services.Mensajeria;

/// <summary>
/// Publica el mensaje "ticket.emitido" en una cola de RabbitMQ alojada en CloudAMQP.
/// Configuración: CloudAmqp:Url (la AMQP URL de CloudAMQP, empieza con amqps://).
/// Si la cola no está configurada o falla, el mensaje se procesa dentro de la app,
/// así la compra y las notificaciones funcionan igual.
/// </summary>
public sealed class PublicadorTickets : IAsyncDisposable
{
    public const string Cola = "uniconnect.tickets.emitidos";

    private readonly Uri? _url;
    private readonly ProcesadorTicketEmitido _procesador;
    private readonly EstadoIntegraciones _estado;
    private readonly ILogger<PublicadorTickets> _logger;
    private readonly SemaphoreSlim _bloqueo = new(1, 1); // IChannel no admite publicaciones simultáneas

    private IConnection? _conexion;
    private IChannel? _canal;

    public PublicadorTickets(IConfiguration configuracion, ProcesadorTicketEmitido procesador,
        EstadoIntegraciones estado, ILogger<PublicadorTickets> logger)
    {
        _url = LeerUrl(configuracion, logger);
        _procesador = procesador;
        _estado = estado;
        _logger = logger;
    }

    public bool Configurado => _url is not null;

    /// <summary>Lee y valida la URL de CloudAMQP. La comparten el publicador y el consumidor.</summary>
    internal static Uri? LeerUrl(IConfiguration configuracion, ILogger logger)
    {
        var valor = configuracion["CloudAmqp:Url"]?.Trim();
        if (string.IsNullOrEmpty(valor)) return null;

        if (Uri.TryCreate(valor, UriKind.Absolute, out var url) && (url.Scheme == "amqps" || url.Scheme == "amqp"))
            return url;

        logger.LogWarning("CloudAmqp:Url no es una URL amqps:// válida; la mensajería con RabbitMQ está desactivada.");
        return null;
    }

    /// <summary>Publica el mensaje. Nunca lanza excepciones.</summary>
    public async Task PublicarAsync(TicketEmitidoMensaje mensaje)
    {
        if (_url is null)
        {
            ProcesarSinCola(mensaje);
            return;
        }

        try
        {
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var cuerpo = JsonSerializer.SerializeToUtf8Bytes(mensaje, JsonSerializerOptions.Web);
            var propiedades = new BasicProperties
            {
                ContentType = "application/json",
                Persistent = true,
                Type = "ticket.emitido",
                MessageId = mensaje.MensajeId.ToString("N")
            };

            await _bloqueo.WaitAsync(limite.Token);
            try
            {
                var canal = await ObtenerCanalAsync(limite.Token);
                await canal.BasicPublishAsync(exchange: string.Empty, routingKey: Cola, mandatory: false,
                    basicProperties: propiedades, body: cuerpo, cancellationToken: limite.Token);
            }
            finally
            {
                _bloqueo.Release();
            }

            _estado.MensajePublicado();
        }
        catch (Exception ex)
        {
            _estado.ErrorRabbit(ex.Message);
            _logger.LogWarning(ex, "No se pudo publicar el ticket {TicketId} en CloudAMQP; se procesa sin cola.", mensaje.TicketId);
            ProcesarSinCola(mensaje);
        }
    }

    private async Task<IChannel> ObtenerCanalAsync(CancellationToken cancellationToken)
    {
        if (_canal is { IsOpen: true }) return _canal;

        if (_canal is not null)
        {
            await _canal.DisposeAsync();
            _canal = null;
        }

        if (_conexion is null || !_conexion.IsOpen)
        {
            if (_conexion is not null)
            {
                await _conexion.DisposeAsync();
                _conexion = null;
            }
            var fabrica = new ConnectionFactory { Uri = _url! };
            _conexion = await fabrica.CreateConnectionAsync("uniconnect-publicador", cancellationToken);
        }

        var canal = await _conexion.CreateChannelAsync(cancellationToken: cancellationToken);
        // Cola durable: los mensajes sobreviven si el consumidor o RabbitMQ se reinician.
        await canal.QueueDeclareAsync(queue: Cola, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: cancellationToken);
        _canal = canal;
        return canal;
    }

    private void ProcesarSinCola(TicketEmitidoMensaje mensaje)
    {
        _estado.ProcesadoLocalmente();
        // En segundo plano para no hacer esperar al estudiante.
        _ = Task.Run(() => _procesador.ProcesarAsync(mensaje, CancellationToken.None));
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_canal is not null) await _canal.DisposeAsync();
            if (_conexion is not null) await _conexion.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error al cerrar la conexión de RabbitMQ.");
        }
        _bloqueo.Dispose();
    }
}
