using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.Dtos;
using UniConnect.Models;

namespace UniConnect.Controllers.Api;

/// <summary>API REST de solo lectura para "Join Us" (voluntariados, comunidades y causas).</summary>
[ApiController]
[Route("api/comunidades")]
[Produces("application/json")]
public class ComunidadesApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public ComunidadesApiController(AppDbContext context) => _context = context;

    // GET /api/comunidades?tipo=Voluntariado
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ComunidadDto>>> Listar(string? tipo, CancellationToken cancellationToken)
    {
        var query = _context.Comunidades.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todos")
            query = query.Where(c => c.TipoApoyo == tipo);

        var comunidades = await query.OrderByDescending(c => c.Miembros).ToListAsync(cancellationToken);
        return Ok(comunidades.Select(ADto));
    }

    // GET /api/comunidades/2
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ComunidadDto>> Obtener(int id, CancellationToken cancellationToken)
    {
        var comunidad = await _context.Comunidades.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return comunidad is null ? NotFound() : Ok(ADto(comunidad));
    }

    private static ComunidadDto ADto(Comunidad c) => new(
        c.Id, c.Nombre, c.TipoApoyo, c.Organizacion, c.Descripcion, c.DescripcionLarga,
        c.Ubicacion, c.Frecuencia, c.Duracion, c.Miembros, c.Impacto,
        string.IsNullOrWhiteSpace(c.Tags)
            ? []
            : c.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        c.ImagenUrl);
}
