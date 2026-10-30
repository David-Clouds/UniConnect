namespace UniConnect.ViewModels;

public sealed class EventoOrganizadorViewModel
{
    public int Id { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string Categoria { get; init; } = string.Empty;
    public string? ImagenUrl { get; init; }
    public DateTime Fecha { get; init; }
    public decimal PrecioActual { get; init; }
    public int TicketsEmitidos { get; init; }
    public int ParticipantesDistintos { get; init; }
}
