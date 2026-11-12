using System.ComponentModel.DataAnnotations;

namespace UniConnect.ViewModels;

/// <summary>Formulario con el que el Administrador General publica un evento nuevo.</summary>
public sealed class CrearEventoViewModel : IValidatableObject
{
    public static readonly string[] Categorias =
        ["Emprendimiento", "Académico", "Cultura", "Deportes", "Música", "Networking"];

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 200 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elige una categoría.")]
    [Display(Name = "Categoría")]
    public string Categoria { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [Display(Name = "Fecha y hora")]
    public DateTime? Fecha { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio (0 si es gratuito).")]
    [Range(typeof(decimal), "0", "10000", ErrorMessage = "El precio debe estar entre S/ 0 y S/ 10 000.")]
    [Display(Name = "Precio (S/)")]
    public decimal? Precio { get; set; } = 0;

    [StringLength(2000, ErrorMessage = "La descripción admite hasta 2000 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [StringLength(500, ErrorMessage = "La URL admite hasta 500 caracteres.")]
    [Display(Name = "URL de la imagen")]
    public string? ImagenUrl { get; set; }

    [Required(ErrorMessage = "Elige el organizador del evento.")]
    [Range(1, int.MaxValue, ErrorMessage = "Elige el organizador del evento.")]
    [Display(Name = "Organizador")]
    public int? OrganizadorId { get; set; }

    // Se llena en el controlador para el <select>; no viene del formulario.
    public IReadOnlyList<OrganizadorOpcionViewModel> Organizadores { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Categoria) && !Categorias.Contains(Categoria))
            yield return new ValidationResult("Elige una categoría de la lista.", [nameof(Categoria)]);

        if (Fecha.HasValue && Fecha.Value < DateTime.Now.AddMinutes(-5))
            yield return new ValidationResult("La fecha del evento debe ser futura.", [nameof(Fecha)]);

        if (!string.IsNullOrWhiteSpace(ImagenUrl)
            && !(Uri.TryCreate(ImagenUrl.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == "https" || uri.Scheme == "http")))
            yield return new ValidationResult("La imagen debe ser una URL que empiece con http:// o https://.", [nameof(ImagenUrl)]);
    }
}
