namespace UniConnect.ViewModels;

public sealed class CheckoutViewModel
{
    public int EventoId { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string? ImagenUrl { get; init; }
    public DateTime Fecha { get; init; }
    public string Organizador { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public IReadOnlyList<string> MetodosPago { get; init; } = [];
}
