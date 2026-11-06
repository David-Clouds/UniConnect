using UniConnect.Services.ML;
using UniConnect.Services.TiempoReal;

namespace UniConnect.Services.Mensajeria;

/// <summary>
/// Lo que ocurre después de emitir un ticket. Lo ejecuta el consumidor de CloudAMQP
/// (o la propia app, si la cola no está disponible):
///  1. Pide a ML.NET reentrenar las recomendaciones con el nuevo ticket.
///  2. Avisa en tiempo real al organizador y a los administradores por PieHost.
/// </summary>
public sealed class ProcesadorTicketEmitido
{
    private readonly ServicioMl _ml;
    private readonly NotificadorPieHost _notificador;
    private readonly ILogger<ProcesadorTicketEmitido> _logger;

    public ProcesadorTicketEmitido(ServicioMl ml, NotificadorPieHost notificador, ILogger<ProcesadorTicketEmitido> logger)
    {
        _ml = ml;
        _notificador = notificador;
        _logger = logger;
    }

    /// <summary>Nunca lanza excepciones: un fallo aquí no debe afectar la compra ya realizada.</summary>
    public async Task ProcesarAsync(TicketEmitidoMensaje mensaje, CancellationToken cancellationToken)
    {
        try
        {
            _ml.SolicitarReentrenamiento();
            await _notificador.NotificarTicketAsync(mensaje, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "No se pudo procesar el ticket {TicketId}.", mensaje.TicketId);
        }
    }
}
