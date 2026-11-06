using Microsoft.AspNetCore.Mvc;
using UniConnect.Data;
using UniConnect.Services.Busqueda;

namespace UniConnect.Controllers.Api;

/// <summary>Buscador de la barra superior: eventos e iniciativas de Join Us.</summary>
[ApiController]
[Route("api/buscar")]
[Produces("application/json")]
public class BusquedaApiController : ControllerBase
{
    private const int MaxLargo = 80;

    private readonly ServicioBusqueda _busqueda;
    private readonly AppDbContext _context;

    public BusquedaApiController(ServicioBusqueda busqueda, AppDbContext context)
    {
        _busqueda = busqueda;
        _context = context;
    }

    // GET /api/buscar?q=hackaton
    [HttpGet]
    public async Task<ActionResult<ResultadoBusquedaDto>> Buscar([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var texto = q?.Trim() ?? string.Empty;
        if (texto.Length < 2)
            return Ok(new ResultadoBusquedaDto(_busqueda.BusquedaConfigurada ? "Algolia" : "Base de datos", []));
        if (texto.Length > MaxLargo)
            return BadRequest(new ProblemDetails { Status = 400, Title = $"La búsqueda admite hasta {MaxLargo} caracteres." });

        return Ok(await _busqueda.BuscarAsync(_context, texto, cancellationToken));
    }
}
