namespace UniConnect.ViewModels;

public sealed class TicketDetalleViewModel
{
    public int Id { get; init; }
    public int EventoId { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string? ImagenUrl { get; init; }
    public DateTime FechaEvento { get; init; }
    public DateTime FechaCompra { get; init; }
    public string Organizador { get; init; } = string.Empty;
}
