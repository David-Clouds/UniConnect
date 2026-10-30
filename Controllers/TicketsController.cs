using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.ViewModels;

namespace UniConnect.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TicketsController : Controller
{
    private readonly AppDbContext _context;

    public TicketsController(AppDbContext context) => _context = context;

    // Integration point for the authentication module. Do not infer database IDs
    // from an arbitrary claim, session value, URL or form. Until the trusted
    // identity-to-Estudiante mapping is agreed and implemented, access stays closed.
    private int? ObtenerEstudianteVerificado() => null;

    private IQueryable<TicketDetalleViewModel> TicketsDelEstudiante(int estudianteId) =>
        _context.Tickets.AsNoTracking()
            .Where(t => t.EstudianteId == estudianteId)
            .Select(t => new TicketDetalleViewModel
            {
                Id = t.Id,
                EventoId = t.EventoId,
                Titulo = t.Evento.Titulo,
                ImagenUrl = t.Evento.ImagenUrl,
                FechaEvento = t.Evento.Fecha,
                FechaCompra = t.FechaCompra,
                Organizador = t.Evento.Organizador.Nombre
            });

    [HttpGet("/Eventos/MisTickets")]
    [HttpGet("/Tickets")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Active = "ticket";
        var estudianteId = ObtenerEstudianteVerificado();
        if (User.Identity?.IsAuthenticated != true || estudianteId is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return View(new MisTicketsViewModel());
        }

        var tickets = await TicketsDelEstudiante(estudianteId.Value)
            .OrderByDescending(t => t.FechaEvento).ThenByDescending(t => t.Id)
            .ToListAsync(cancellationToken);
        var ahora = DateTime.Now;
        return View(new MisTicketsViewModel
        {
            AccesoDisponible = true,
            Tickets = tickets,
            EventosProximos = tickets.Where(t => t.FechaEvento >= ahora)
                .Select(t => t.EventoId).Distinct().Count(),
            EventosPasados = tickets.Where(t => t.FechaEvento < ahora)
                .Select(t => t.EventoId).Distinct().Count()
        });
    }

    [HttpGet("/Tickets/Detalle/{id:int}")]
    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        ViewBag.Active = "ticket";
        var estudianteId = ObtenerEstudianteVerificado();
        if (User.Identity?.IsAuthenticated != true || estudianteId is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return View("Index", new MisTicketsViewModel());
        }

        var ticket = await TicketsDelEstudiante(estudianteId.Value)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket is null) return NotFound();
        return View(ticket);
    }
}
