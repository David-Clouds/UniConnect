using Microsoft.AspNetCore.Mvc;
using UniConnect.Data;
using UniConnect.Services;

namespace UniConnect.Controllers;

/// <summary>
/// Página raíz ("/"): muestra primero el login, como en el prototipo de Figma.
/// Si el estudiante ya inició sesión, lo lleva directo al inicio de eventos.
/// Los eventos siguen siendo públicos en /Eventos (funcionalidades sin login).
/// </summary>
public class InicioController : Controller
{
    private readonly AppDbContext _context;

    public InicioController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var estudianteId = await SesionEstudiante.ObtenerIdAsync(HttpContext, _context, cancellationToken);
        return estudianteId is null
            ? RedirectToAction("Login", "Cuenta")
            : RedirectToAction("Index", "Eventos");
    }
}
