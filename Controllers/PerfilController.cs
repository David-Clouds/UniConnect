using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.ViewModels;

namespace UniConnect.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PerfilController : Controller
{
    private readonly AppDbContext _context;

    public PerfilController(AppDbContext context) => _context = context;

    [HttpGet("/Perfil")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Active = "profile";
        await HttpContext.Session.LoadAsync(cancellationToken);
        var estudianteId = HttpContext.Session.GetInt32(CuentaController.SesionEstudianteId);
        if (estudianteId is not > 0)
            return RedirigirAlLogin();

        var estudiante = await _context.Estudiantes.AsNoTracking()
            .Where(e => e.Id == estudianteId.Value)
            .Select(e => new
            {
                e.Nombre, e.Correo, e.Codigo, e.EsAdministradorGeneral,
                Tickets = e.Tickets.Select(t => new PerfilActividadViewModel
                {
                    EventoId = t.EventoId,
                    Titulo = t.Evento.Titulo,
                    Organizador = t.Evento.Organizador.Nombre,
                    Fecha = t.Evento.Fecha,
                    ImagenUrl = t.Evento.ImagenUrl
                })
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (estudiante is null)
            return RedirigirAlLogin();

        var ahora = DateTime.Now;
        var actividades = estudiante.Tickets
            .OrderByDescending(t => t.Fecha).ThenBy(t => t.EventoId).Take(6).ToList();

        return View(new PerfilViewModel
        {
            Nombre = estudiante.Nombre,
            Correo = estudiante.Correo,
            Codigo = estudiante.Codigo,
            EsAdministrador = estudiante.EsAdministradorGeneral,
            EventosAsistidos = estudiante.Tickets.Where(t => t.Fecha < ahora)
                .Select(t => t.EventoId).Distinct().Count(),
            EventosProximos = estudiante.Tickets.Where(t => t.Fecha >= ahora)
                .Select(t => t.EventoId).Distinct().Count(),
            Comunidades = 0,
            Voluntariados = 0,
            Actividades = actividades
        });
    }

    private IActionResult RedirigirAlLogin()
    {
        var retorno = Url.Action(nameof(Index), "Perfil");
        if (retorno is null || !Url.IsLocalUrl(retorno)) retorno = Url.Content("~/Perfil");
        return RedirectToAction(nameof(CuentaController.Login), "Cuenta", new { returnUrl = retorno });
    }
}
