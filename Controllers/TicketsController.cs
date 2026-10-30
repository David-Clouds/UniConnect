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

    private async Task<int?> ObtenerEstudianteVerificadoAsync(CancellationToken cancellationToken)
    {
        await HttpContext.Session.LoadAsync(cancellationToken);
        // CuentaController stores this server-side value with SetInt32.
        // Check the encoded length before GetInt32 to reject malformed values.
        if (!HttpContext.Session.TryGetValue(CuentaController.SesionEstudianteId, out var valor)
            || valor.Length != sizeof(int))
            return null;

        var estudianteId = HttpContext.Session.GetInt32(CuentaController.SesionEstudianteId);
        if (estudianteId is not > 0) return null;

        return await _context.Estudiantes.AsNoTracking()
            .AnyAsync(e => e.Id == estudianteId.Value, cancellationToken)
            ? estudianteId : null;
    }

    private IActionResult IrAlLogin(int? ticketId = null)
    {
        // Generate the return path from our own routes, never from browser input.
        var retorno = ticketId.HasValue
            ? Url.Action(nameof(Detalle), "Tickets", new { id = ticketId.Value })
            : Url.Action(nameof(Index), "Tickets");
        if (retorno is null || !Url.IsLocalUrl(retorno))
            retorno = Url.Content("~/Eventos/MisTickets");
        return RedirectToAction(nameof(CuentaController.Login), "Cuenta", new { returnUrl = retorno });
    }

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
        var estudianteId = await ObtenerEstudianteVerificadoAsync(cancellationToken);
        if (estudianteId is null) return IrAlLogin();

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
        if (!ModelState.IsValid || id <= 0) return NotFound();
        var estudianteId = await ObtenerEstudianteVerificadoAsync(cancellationToken);
        if (estudianteId is null) return IrAlLogin(id);

        var ticket = await TicketsDelEstudiante(estudianteId.Value)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket is null) return NotFound();
        return View(ticket);
    }
}
