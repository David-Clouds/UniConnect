using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.ViewModels;
using UniConnect.Models;

namespace UniConnect.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OrganizadorController : Controller
{
    private readonly AppDbContext _context;

    public OrganizadorController(AppDbContext context) => _context = context;

    private async Task<Estudiante?> ObtenerEstudianteAsync(CancellationToken ct)
    {
        await HttpContext.Session.LoadAsync(ct);
        if (!HttpContext.Session.TryGetValue(CuentaController.SesionEstudianteId, out var valor)
            || valor.Length != sizeof(int)) return null;
        var id = HttpContext.Session.GetInt32(CuentaController.SesionEstudianteId);
        if (id is not > 0) return null;
        // Project only the current identity and permission, never credentials.
        return await _context.Estudiantes.AsNoTracking().Where(e => e.Id == id.Value)
            .Select(e => new Estudiante { Id = e.Id, EsAdministradorGeneral = e.EsAdministradorGeneral })
            .SingleOrDefaultAsync(ct);
    }

    private IActionResult Login(string destino)
    {
        var retorno = Url.Action(destino, "Organizador");
        if (retorno is null || !Url.IsLocalUrl(retorno)) retorno = Url.Content("~/Organizador/Panel");
        return RedirectToAction(nameof(CuentaController.Login), "Cuenta", new { returnUrl = retorno });
    }

    private IQueryable<Organizador> Permitidos(Estudiante estudiante) =>
        _context.Organizadores.AsNoTracking().Where(o => estudiante.EsAdministradorGeneral
            || o.EstudianteId == estudiante.Id);

    private static IQueryable<OrganizadorOpcionViewModel> Opciones(IQueryable<Organizador> consulta) =>
        consulta.OrderBy(o => o.Nombre).ThenBy(o => o.Id).Select(o => new OrganizadorOpcionViewModel
        {
            Id = o.Id, Nombre = o.Nombre, EstudianteId = o.EstudianteId,
            Responsable = o.Estudiante == null ? null : o.Estudiante.Nombre
        });

    [HttpGet("/Organizador/Panel")]
    public async Task<IActionResult> Panel(int? organizadorId, CancellationToken cancellationToken)
    {
        ViewBag.Active = "organizer";
        var estudiante = await ObtenerEstudianteAsync(cancellationToken);
        if (estudiante is null) return Login(nameof(Panel));
        if (!ModelState.IsValid || organizadorId is <= 0) return BadRequest();
        var permitidos = Permitidos(estudiante);
        var opciones = await Opciones(permitidos).ToListAsync(cancellationToken);
        if (organizadorId.HasValue && !opciones.Any(o => o.Id == organizadorId.Value))
            return StatusCode(StatusCodes.Status403Forbidden);
        if (!estudiante.EsAdministradorGeneral && opciones.Count == 0)
            return StatusCode(StatusCodes.Status403Forbidden);
        organizadorId ??= opciones.Select(o => (int?)o.Id).FirstOrDefault();

        var eventosPropios = _context.Eventos.AsNoTracking()
            .Where(e => e.OrganizadorId == organizadorId && permitidos.Any(o => o.Id == e.OrganizadorId));
        var ticketsPropios = _context.Tickets.AsNoTracking()
            .Where(t => t.Evento.OrganizadorId == organizadorId && permitidos.Any(o => o.Id == t.Evento.OrganizadorId));

        // Existing dates have no timezone metadata. Use the application's local
        // calendar consistently; agree on a timezone with the ticket issuer later.
        var ahora = DateTime.Now;
        var inicio = ahora.Date.AddDays(-6);
        var fin = ahora.Date.AddDays(1);
        var totalEventos = await eventosPropios.CountAsync(cancellationToken);
        var totalTickets = await ticketsPropios.CountAsync(cancellationToken);
        var participantesUnicos = await ticketsPropios.Select(t => t.EstudianteId)
            .Distinct().CountAsync(cancellationToken);
        var proximos = await eventosPropios.CountAsync(e => e.Fecha >= ahora, cancellationToken);

        var emisiones = await ticketsPropios
            .Where(t => t.FechaCompra >= inicio && t.FechaCompra < fin)
            .GroupBy(t => t.FechaCompra.Date)
            .Select(g => new EmisionDiariaViewModel { Fecha = g.Key, Cantidad = g.Count() })
            .ToListAsync(cancellationToken);
        var porDia = emisiones.ToDictionary(e => e.Fecha, e => e.Cantidad);

        var eventos = await eventosPropios.OrderByDescending(e => e.Fecha).ThenByDescending(e => e.Id)
            .Take(20)
            .Select(e => new EventoOrganizadorViewModel
            {
                Id = e.Id, Titulo = e.Titulo, Categoria = e.Categoria,
                ImagenUrl = e.ImagenUrl, Fecha = e.Fecha, PrecioActual = e.Precio,
                TicketsEmitidos = e.Tickets.Count(),
                ParticipantesDistintos = e.Tickets.Select(t => t.EstudianteId).Distinct().Count()
            }).ToListAsync(cancellationToken);
        var participantes = await ticketsPropios.OrderByDescending(t => t.FechaCompra)
            .ThenByDescending(t => t.Id).Take(10)
            .Select(t => new ParticipanteOrganizadorViewModel
            {
                Nombre = t.Estudiante.Nombre, Evento = t.Evento.Titulo, FechaCompra = t.FechaCompra
            }).ToListAsync(cancellationToken);

        return View(new PanelOrganizadorViewModel
        {
            AccesoDisponible = true, TotalEventos = totalEventos, TotalTickets = totalTickets,
            EsAdministradorGeneral = estudiante.EsAdministradorGeneral,
            OrganizadorSeleccionadoId = organizadorId, Organizadores = opciones,
            ParticipantesUnicos = participantesUnicos, EventosProximos = proximos,
            Eventos = eventos, Participantes = participantes,
            EmisionesDiarias = Enumerable.Range(0, 7).Select(i => new EmisionDiariaViewModel
            {
                Fecha = inicio.AddDays(i), Cantidad = porDia.GetValueOrDefault(inicio.AddDays(i))
            }).ToList()
        });
    }

    [HttpGet("/Organizador/Responsables")]
    public async Task<IActionResult> Responsables(CancellationToken cancellationToken)
    {
        var estudiante = await ObtenerEstudianteAsync(cancellationToken);
        if (estudiante is null) return Login(nameof(Responsables));
        if (!estudiante.EsAdministradorGeneral) return StatusCode(StatusCodes.Status403Forbidden);
        ViewBag.Active = "organizer";
        return View(new GestionOrganizadoresViewModel
        {
            Organizadores = await Opciones(_context.Organizadores.AsNoTracking()).ToListAsync(cancellationToken),
            Mensaje = TempData["AsignacionMensaje"] as string
        });
    }

    [HttpPost("/Organizador/AsignarResponsable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarResponsable(AsignacionOrganizadorViewModel model, CancellationToken cancellationToken)
    {
        var estudiante = await ObtenerEstudianteAsync(cancellationToken);
        if (estudiante is null) return Login(nameof(Responsables));
        if (!estudiante.EsAdministradorGeneral) return StatusCode(StatusCodes.Status403Forbidden);
        if (!ModelState.IsValid) return BadRequest();

        // Recheck permission in the write transaction. Serializable isolation and
        // the concurrency token prevent silently overwriting a concurrent reassignment.
        await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (!await _context.Estudiantes.AsNoTracking().AnyAsync(e => e.Id == estudiante.Id
            && e.EsAdministradorGeneral, cancellationToken)) return StatusCode(StatusCodes.Status403Forbidden);
        var organizador = await _context.Organizadores.SingleOrDefaultAsync(o => o.Id == model.OrganizadorId, cancellationToken);
        if (organizador is null) return NotFound();
        if (organizador.EstudianteId != model.ResponsableAnteriorId)
            return Conflict("La asignación cambió. Recarga la página antes de continuar.");
        if (model.EstudianteId.HasValue && !await _context.Estudiantes.AsNoTracking()
            .AnyAsync(e => e.Id == model.EstudianteId.Value, cancellationToken))
            return BadRequest("El estudiante indicado no existe.");
        organizador.EstudianteId = model.EstudianteId;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("La asignación cambió. Recarga la página antes de continuar.");
        }
        TempData["AsignacionMensaje"] = model.EstudianteId.HasValue ? "Responsable actualizado." : "Asignación revocada.";
        return RedirectToAction(nameof(Responsables));
    }
}
