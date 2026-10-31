namespace UniConnect.ViewModels;

public sealed class PerfilViewModel
{
    public string Nombre { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public bool EsAdministrador { get; init; }
    public int EventosAsistidos { get; init; }
    public int EventosProximos { get; init; }
    public int Comunidades { get; init; }
    public int Voluntariados { get; init; }
    public IReadOnlyList<PerfilActividadViewModel> Actividades { get; init; } = [];
}

public sealed class PerfilActividadViewModel
{
    public int EventoId { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string Organizador { get; init; } = string.Empty;
    public DateTime Fecha { get; init; }
    public string? ImagenUrl { get; init; }
}
