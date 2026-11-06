namespace UniConnect.Services.Mensajeria;

/// <summary>
/// Mensaje que viaja por la cola de CloudAMQP cada vez que se emite un ticket.
/// No incluye datos personales del estudiante: solo información del evento.
/// </summary>
public sealed record TicketEmitidoMensaje(
    Guid MensajeId,
    int TicketId,
    int EventoId,
    int OrganizadorId,
    string TituloEvento,
    string Categoria,
    decimal Precio,
    int TotalTicketsEvento,
    DateTime EmitidoEnUtc);
