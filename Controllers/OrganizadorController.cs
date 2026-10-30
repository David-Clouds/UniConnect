using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.ViewModels;

namespace UniConnect.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class OrganizadorController : Controller
{
    private readonly AppDbContext _context;

    public OrganizadorController(AppDbContext context) => _context = context;

    // Authentication integration point: the authentication module must verify
    // organizer permission and map its trusted identity to an existing Organizador.
    // Never substitute a browser parameter, fixed ID or unverified claim here.
    // Keep access closed until that contract is implemented.
    private int? ObtenerOrganizadorVerificado() => null;

    [HttpGet("/Organizador/Panel")]
    public async Task<IActionResult> Panel(CancellationToken cancellationToken)
    {
        ViewBag.Active = "organizer";
        var organizadorId = ObtenerOrganizadorVerificado();
        if (User.Identity?.IsAuthenticated != true || organizadorId is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return View(new PanelOrganizadorViewModel());
        }

        var eventosPropios = _context.Eventos.AsNoTracking()
            .Where(e => e.OrganizadorId == organizadorId.Value);
        var ticketsPropios = _context.Tickets.AsNoTracking()
            .Where(t => t.Evento.OrganizadorId == organizadorId.Value);

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
            ParticipantesUnicos = participantesUnicos, EventosProximos = proximos,
            Eventos = eventos, Participantes = participantes,
            EmisionesDiarias = Enumerable.Range(0, 7).Select(i => new EmisionDiariaViewModel
            {
                Fecha = inicio.AddDays(i), Cantidad = porDia.GetValueOrDefault(inicio.AddDays(i))
            }).ToList()
        });
    }
}
