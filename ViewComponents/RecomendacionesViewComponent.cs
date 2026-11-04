using Microsoft.AspNetCore.Mvc;
using UniConnect.Data;
using UniConnect.Services;
using UniConnect.Services.ML;

namespace UniConnect.ViewComponents;

/// <summary>Sección "Recomendados para ti" de la página de inicio.</summary>
public class RecomendacionesViewComponent : ViewComponent
{
    private readonly AppDbContext _context;
    private readonly ServicioMl _ml;
    private readonly ILogger<RecomendacionesViewComponent> _logger;

    public RecomendacionesViewComponent(AppDbContext context, ServicioMl ml, ILogger<RecomendacionesViewComponent> logger)
    {
        _context = context;
        _ml = ml;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var ct = HttpContext.RequestAborted;
            var estudianteId = await SesionEstudiante.ObtenerIdAsync(HttpContext, _context, ct);
            var recomendaciones = await _ml.RecomendarAsync(_context, estudianteId, 4, incluirPasados: false, ct);
            return recomendaciones.Eventos.Count == 0 ? Content(string.Empty) : View(recomendaciones);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // La página de inicio nunca debe fallar por las recomendaciones.
            _logger.LogWarning(ex, "No se pudieron generar las recomendaciones.");
            return Content(string.Empty);
        }
    }
}
