namespace UniConnect.ViewModels;

public sealed class PanelOrganizadorViewModel
{
    public bool AccesoDisponible { get; init; }
    public bool EsAdministradorGeneral { get; init; }
    public int? OrganizadorSeleccionadoId { get; init; }
    public IReadOnlyList<OrganizadorOpcionViewModel> Organizadores { get; init; } = [];
    public int TotalEventos { get; init; }
    public int TotalTickets { get; init; }
    public int ParticipantesUnicos { get; init; }
    public int EventosProximos { get; init; }
    public IReadOnlyList<EventoOrganizadorViewModel> Eventos { get; init; } = [];
    public IReadOnlyList<ParticipanteOrganizadorViewModel> Participantes { get; init; } = [];
    public IReadOnlyList<EmisionDiariaViewModel> EmisionesDiarias { get; init; } = [];
}

public sealed class EmisionDiariaViewModel
{
    public DateTime Fecha { get; init; }
    public int Cantidad { get; init; }
}
