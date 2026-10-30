namespace UniConnect.ViewModels;

public sealed class MisTicketsViewModel
{
    public bool AccesoDisponible { get; init; }
    public IReadOnlyList<TicketDetalleViewModel> Tickets { get; init; } = [];
    public int TotalTickets => Tickets.Count;
    public int EventosProximos { get; init; }
    public int EventosPasados { get; init; }
}
