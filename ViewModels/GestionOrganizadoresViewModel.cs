using System.ComponentModel.DataAnnotations;

namespace UniConnect.ViewModels;

public sealed class OrganizadorOpcionViewModel
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public int? EstudianteId { get; init; }
    public string? Responsable { get; init; }
}

public sealed class GestionOrganizadoresViewModel
{
    public IReadOnlyList<OrganizadorOpcionViewModel> Organizadores { get; init; } = [];
    public string? Mensaje { get; init; }
}

public sealed class AsignacionOrganizadorViewModel
{
    [Range(1, int.MaxValue)]
    public int OrganizadorId { get; set; }
    [Range(1, int.MaxValue)]
    public int? EstudianteId { get; set; }
    [Range(1, int.MaxValue)]
    public int? ResponsableAnteriorId { get; set; }
}
