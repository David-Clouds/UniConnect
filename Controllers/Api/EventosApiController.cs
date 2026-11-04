using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.Dtos;
using UniConnect.Models;

namespace UniConnect.Controllers.Api;

/// <summary>API REST de solo lectura para los eventos universitarios.</summary>
[ApiController]
[Route("api/eventos")]
[Produces("application/json")]
public class EventosApiController : ControllerBase
{
    private static readonly string[] Categorias =
        ["Emprendimiento", "Académico", "Cultura", "Deportes", "Música", "Networking"];

    private readonly AppDbContext _context;

    public EventosApiController(AppDbContext context) => _context = context;

    // GET /api/eventos?categoria=Cultura&fecha=2026-11-05&precioMax=20&q=hackathon&limite=10
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventoDto>>> Listar(
        string? categoria, DateTime? fecha, decimal? precioMax, string? q,
        int limite = 50, CancellationToken cancellationToken = default)
    {
        if (precioMax < 0)
            return BadRequest(new ProblemDetails { Status = 400, Title = "precioMax no puede ser negativo." });

        limite = Math.Clamp(limite, 1, 100);

        var query = _context.Eventos.AsNoTracking().Include(e => e.Organizador).AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todos")
            query = query.Where(e => e.Categoria == categoria);

        if (fecha.HasValue)
            query = query.Where(e => e.Fecha.Date == fecha.Value.Date);

        if (precioMax.HasValue)
            query = query.Where(e => e.Precio <= precioMax.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            // LIKE no distingue mayúsculas; se escapan los comodines que escriba el usuario.
            var patron = "%" + q.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            query = query.Where(e => EF.Functions.Like(e.Titulo, patron, "\\")
                                  || (e.Descripcion != null && EF.Functions.Like(e.Descripcion, patron, "\\")));
        }

        var eventos = await query.OrderBy(e => e.Fecha).Take(limite).ToListAsync(cancellationToken);
        return Ok(eventos.Select(ADto));
    }

    // GET /api/eventos/3
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventoDto>> Obtener(int id, CancellationToken cancellationToken)
    {
        var evento = await _context.Eventos.AsNoTracking()
            .Include(e => e.Organizador)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return evento is null ? NotFound() : Ok(ADto(evento));
    }

    // GET /api/eventos/categorias
    [HttpGet("categorias")]
    public ActionResult<IEnumerable<string>> ListarCategorias() => Ok(Categorias);

    private static EventoDto ADto(Evento e) => new(
        e.Id, e.Titulo, e.Categoria, e.Fecha, e.Precio, e.Precio == 0,
        e.Descripcion, e.ImagenUrl, e.OrganizadorId, e.Organizador.Nombre);
}
