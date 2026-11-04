using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using UniConnect.Data;
using UniConnect.Dtos;

namespace UniConnect.Controllers.Api;

/// <summary>
/// Estadísticas generales de la plataforma. El resultado se guarda 60 segundos en la caché
/// distribuida: Redis en producción (Render) y memoria en desarrollo.
/// </summary>
[ApiController]
[Route("api/estadisticas")]
[Produces("application/json")]
public class EstadisticasApiController : ControllerBase
{
    private const string ClaveCache = "api:estadisticas";

    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<EstadisticasApiController> _logger;

    public EstadisticasApiController(AppDbContext context, IDistributedCache cache, ILogger<EstadisticasApiController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    // GET /api/estadisticas   (la cabecera X-Cache indica HIT o MISS)
    [HttpGet]
    public async Task<ActionResult<EstadisticasDto>> Obtener(CancellationToken cancellationToken)
    {
        var enCache = await LeerCacheAsync(cancellationToken);
        if (enCache is not null)
        {
            Response.Headers["X-Cache"] = "HIT";
            return Ok(enCache);
        }

        var estadisticas = await CalcularAsync(cancellationToken);
        await GuardarCacheAsync(estadisticas, cancellationToken);

        Response.Headers["X-Cache"] = "MISS";
        return Ok(estadisticas);
    }

    private async Task<EstadisticasDto> CalcularAsync(CancellationToken cancellationToken)
    {
        // SQLite no agrega columnas decimal en la base de datos; se suma en memoria.
        var preciosEventos = await _context.Eventos.AsNoTracking()
            .Select(e => e.Precio).ToListAsync(cancellationToken);
        var preciosVendidos = await _context.Tickets.AsNoTracking()
            .Select(t => t.Evento.Precio).ToListAsync(cancellationToken);
        var porCategoria = await _context.Eventos.AsNoTracking()
            .GroupBy(e => e.Categoria)
            .Select(g => new { Categoria = g.Key, Total = g.Count() })
            .ToListAsync(cancellationToken);

        return new EstadisticasDto(
            TotalEventos: preciosEventos.Count,
            EventosGratuitos: preciosEventos.Count(p => p == 0),
            EventosDePago: preciosEventos.Count(p => p > 0),
            TicketsEmitidos: preciosVendidos.Count,
            IngresosSimulados: preciosVendidos.Sum(),
            TotalComunidades: await _context.Comunidades.CountAsync(cancellationToken),
            TotalMiembros: await _context.Comunidades.SumAsync(c => c.Miembros, cancellationToken),
            EventosPorCategoria: porCategoria.ToDictionary(x => x.Categoria, x => x.Total),
            GeneradoEn: DateTime.UtcNow);
    }

    // Si la caché falla (por ejemplo Redis sin conexión) la API sigue respondiendo sin ella.
    private async Task<EstadisticasDto?> LeerCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            var json = await _cache.GetStringAsync(ClaveCache, cancellationToken);
            return json is null ? null : JsonSerializer.Deserialize<EstadisticasDto>(json, JsonSerializerOptions.Web);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "No se pudo leer la caché de estadísticas.");
            return null;
        }
    }

    private async Task GuardarCacheAsync(EstadisticasDto estadisticas, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.SetStringAsync(
                ClaveCache,
                JsonSerializer.Serialize(estadisticas, JsonSerializerOptions.Web),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "No se pudo guardar la caché de estadísticas.");
        }
    }
}
