using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.Models;
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
                Organizador = t.Evento.Organizador.Nombre,
                CodigoQR = t.CodigoQR
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

    // ───────────────────────── Registro y compra (simulada) ─────────────────────────

    // Métodos de pago que ofrece el prototipo. No se cobra nada: es una compra simulada.
    private static readonly string[] MetodosPagoSimulado = ["Tarjeta", "Yape", "Plin"];

    private IActionResult IrAlLoginConRetorno(string? retorno)
    {
        // El retorno se genera siempre con rutas propias, nunca con datos del navegador.
        if (retorno is null || !Url.IsLocalUrl(retorno))
            retorno = Url.Content("~/Eventos/MisTickets");
        return RedirectToAction(nameof(CuentaController.Login), "Cuenta", new { returnUrl = retorno });
    }

    // Reglas comunes de emisión. Devuelve una redirección si no se puede emitir el ticket.
    private async Task<IActionResult?> ValidarEmisionAsync(int estudianteId, Evento evento, CancellationToken cancellationToken)
    {
        if (evento.Fecha < DateTime.Now)
        {
            TempData["TicketError"] = "Este evento ya finalizó y no admite nuevos registros.";
            return RedirectToAction("Detalle", "Eventos", new { id = evento.Id });
        }

        var existente = await _context.Tickets.AsNoTracking()
            .Where(t => t.EstudianteId == estudianteId && t.EventoId == evento.Id)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existente is not null)
        {
            TempData["TicketMensaje"] = "Ya tienes un ticket para este evento.";
            return RedirectToAction(nameof(Detalle), new { id = existente.Value });
        }

        return null;
    }

    private async Task<int> EmitirTicketAsync(int estudianteId, int eventoId, CancellationToken cancellationToken)
    {
        var ticket = new Ticket
        {
            EstudianteId = estudianteId,
            EventoId = eventoId,
            FechaCompra = DateTime.Now,
            // Valor aleatorio e impredecible; es lo que se codifica en la imagen QR.
            CodigoQR = "UC-" + Guid.NewGuid().ToString("N")
        };
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync(cancellationToken);
        return ticket.Id;
    }

    // GET /Eventos/Checkout/5 — resumen y pago simulado de un evento de pago
    [HttpGet("/Eventos/Checkout/{id:int}")]
    public async Task<IActionResult> Checkout(int id, CancellationToken cancellationToken)
    {
        ViewBag.Active = "home";
        if (id <= 0) return NotFound();

        var estudianteId = await ObtenerEstudianteVerificadoAsync(cancellationToken);
        if (estudianteId is null)
            return IrAlLoginConRetorno(Url.Action(nameof(Checkout), "Tickets", new { id }));

        var evento = await _context.Eventos.AsNoTracking()
            .Include(e => e.Organizador)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (evento is null) return NotFound();

        // Un evento gratuito no pasa por checkout: se registra desde su detalle.
        if (evento.Precio <= 0)
            return RedirectToAction("Detalle", "Eventos", new { id = evento.Id });

        var rechazo = await ValidarEmisionAsync(estudianteId.Value, evento, cancellationToken);
        if (rechazo is not null) return rechazo;

        return View(new CheckoutViewModel
        {
            EventoId = evento.Id,
            Titulo = evento.Titulo,
            ImagenUrl = evento.ImagenUrl,
            Fecha = evento.Fecha,
            Organizador = evento.Organizador.Nombre,
            Precio = evento.Precio,
            MetodosPago = MetodosPagoSimulado
        });
    }

    // POST /Eventos/Checkout/5 — confirma la compra simulada y emite el ticket
    [HttpPost("/Eventos/Checkout/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarCompra(int id, string? metodoPago, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();

        var estudianteId = await ObtenerEstudianteVerificadoAsync(cancellationToken);
        if (estudianteId is null)
            return IrAlLoginConRetorno(Url.Action(nameof(Checkout), "Tickets", new { id }));

        // El precio nunca viene del navegador: se lee siempre de la base de datos.
        var evento = await _context.Eventos.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (evento is null) return NotFound();

        if (evento.Precio <= 0)
            return RedirectToAction("Detalle", "Eventos", new { id = evento.Id });

        var rechazo = await ValidarEmisionAsync(estudianteId.Value, evento, cancellationToken);
        if (rechazo is not null) return rechazo;

        if (metodoPago is null || !MetodosPagoSimulado.Contains(metodoPago))
        {
            TempData["TicketError"] = "Selecciona un método de pago válido.";
            return RedirectToAction(nameof(Checkout), new { id });
        }

        var ticketId = await EmitirTicketAsync(estudianteId.Value, evento.Id, cancellationToken);
        TempData["TicketMensaje"] = $"¡Compra simulada con {metodoPago} exitosa! Este es tu ticket con código QR.";
        return RedirectToAction(nameof(Detalle), new { id = ticketId });
    }

    // POST /Eventos/Registrar/5 — registro directo a un evento gratuito
    [HttpPost("/Eventos/Registrar/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();

        var estudianteId = await ObtenerEstudianteVerificadoAsync(cancellationToken);
        if (estudianteId is null)
            return IrAlLoginConRetorno(Url.Action("Detalle", "Eventos", new { id }));

        var evento = await _context.Eventos.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (evento is null) return NotFound();

        // Un evento de pago no se puede "registrar" gratis: debe pasar por checkout.
        if (evento.Precio > 0)
            return RedirectToAction(nameof(Checkout), new { id = evento.Id });

        var rechazo = await ValidarEmisionAsync(estudianteId.Value, evento, cancellationToken);
        if (rechazo is not null) return rechazo;

        var ticketId = await EmitirTicketAsync(estudianteId.Value, evento.Id, cancellationToken);
        TempData["TicketMensaje"] = "¡Registro exitoso! Este es tu ticket con código QR.";
        return RedirectToAction(nameof(Detalle), new { id = ticketId });
    }
}
