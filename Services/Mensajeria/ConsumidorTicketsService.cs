using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UniConnect.Services.Integraciones;

namespace UniConnect.Services.Mensajeria;

/// <summary>
/// Escucha la cola "uniconnect.tickets.emitidos" de CloudAMQP y procesa cada mensaje
/// (reentrenar ML.NET y avisar por PieHost). Confirma cada mensaje (ack) solo después de procesarlo.
/// </summary>
public sealed class ConsumidorTicketsService : BackgroundService
{
    private readonly Uri? _url;
    private readonly ProcesadorTicketEmitido _procesador;
    private readonly EstadoIntegraciones _estado;
    private readonly ILogger<ConsumidorTicketsService> _logger;

    public ConsumidorTicketsService(IConfiguration configuracion, ProcesadorTicketEmitido procesador,
        EstadoIntegraciones estado, ILogger<ConsumidorTicketsService> logger)
    {
        _url = PublicadorTickets.LeerUrl(configuracion, logger);
        _procesador = procesador;
        _estado = estado;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_url is null)
        {
            _logger.LogInformation("CloudAMQP no está configurado: los tickets se procesan dentro de la aplicación.");
            return;
        }

        await Task.Yield(); // no retrasar el arranque de la web
        var espera = TimeSpan.FromSeconds(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // La recuperación automática de conexión viene activada por defecto en RabbitMQ.Client 7.
                var fabrica = new ConnectionFactory { Uri = _url };
                await using var conexion = await fabrica.CreateConnectionAsync("uniconnect-consumidor", stoppingToken);
                await using var canal = await conexion.CreateChannelAsync(cancellationToken: stoppingToken);

                await canal.QueueDeclareAsync(queue: PublicadorTickets.Cola, durable: true, exclusive: false,
                    autoDelete: false, cancellationToken: stoppingToken);
                await canal.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

                var consumidor = new AsyncEventingBasicConsumer(canal);
                consumidor.ReceivedAsync += async (_, entrega) =>
                {
                    try
                    {
                        var mensaje = JsonSerializer.Deserialize<TicketEmitidoMensaje>(entrega.Body.Span, JsonSerializerOptions.Web);
                        if (mensaje is not null)
                            await _procesador.ProcesarAsync(mensaje, stoppingToken);

                        _estado.MensajeConsumido();
                        await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Mensaje inválido: se descarta para que no se repita en bucle.
                        _logger.LogWarning(ex, "Mensaje de ticket inválido; se descarta.");
                        await canal.BasicNackAsync(entrega.DeliveryTag, multiple: false, requeue: false,
                            cancellationToken: stoppingToken);
                    }
                };

                await canal.BasicConsumeAsync(PublicadorTickets.Cola, autoAck: false, consumer: consumidor,
                    cancellationToken: stoppingToken);

                _estado.RabbitConectado(true);
                _estado.ErrorRabbit(null);
                _logger.LogInformation("Consumidor de CloudAMQP escuchando la cola {Cola}.", PublicadorTickets.Cola);
                espera = TimeSpan.FromSeconds(5);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _estado.RabbitConectado(false);
                _estado.ErrorRabbit(ex.Message);
                _logger.LogWarning(ex, "No se pudo conectar a CloudAMQP; nuevo intento en {Segundos} s.", espera.TotalSeconds);

                try { await Task.Delay(espera, stoppingToken); }
                catch (OperationCanceledException) { break; }

                espera = TimeSpan.FromSeconds(Math.Min(espera.TotalSeconds * 2, 120));
            }
        }

        _estado.RabbitConectado(false);
    }
}
