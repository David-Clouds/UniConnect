using Microsoft.AspNetCore.Mvc;
using UniConnect.Data;
using UniConnect.Services;
using UniConnect.Services.ML;

namespace UniConnect.Controllers.Api;

/// <summary>Endpoints de los modelos de ML.NET (clasificación y recomendación).</summary>
[ApiController]
[Route("api/ml")]
[Produces("application/json")]
public class MlApiController : ControllerBase
{
    private const int MaxTitulo = 200;
    private const int MaxDescripcion = 2000;

    private readonly ServicioMl _ml;
    private readonly AppDbContext _context;

    public MlApiController(ServicioMl ml, AppDbContext context)
    {
        _ml = ml;
        _context = context;
    }

    // GET /api/ml/estado — algoritmos, cantidad de datos y métricas de cada modelo
    [HttpGet("estado")]
    public ActionResult<EstadoMlDto> Estado() => Ok(_ml.Estado);

    public sealed record SolicitudClasificacion(string? Titulo, string? Descripcion);

    // POST /api/ml/clasificar   { "titulo": "...", "descripcion": "..." }
    [HttpPost("clasificar")]
    public ActionResult<ResultadoClasificacionDto> ClasificarPost([FromBody] SolicitudClasificacion solicitud) =>
        Clasificar(solicitud.Titulo, solicitud.Descripcion);

    // GET /api/ml/clasificar?titulo=...&descripcion=...   (cómodo para probar desde el navegador)
    [HttpGet("clasificar")]
    public ActionResult<ResultadoClasificacionDto> ClasificarGet([FromQuery] string? titulo, [FromQuery] string? descripcion) =>
        Clasificar(titulo, descripcion);

    private ActionResult<ResultadoClasificacionDto> Clasificar(string? titulo, string? descripcion)
    {
        titulo = titulo?.Trim();
        descripcion = descripcion?.Trim();

        if (string.IsNullOrEmpty(titulo))
            return BadRequest(new ProblemDetails { Status = 400, Title = "El título es obligatorio." });
        if (titulo.Length > MaxTitulo || (descripcion?.Length ?? 0) > MaxDescripcion)
            return BadRequest(new ProblemDetails { Status = 400, Title = $"Máximo {MaxTitulo} caracteres de título y {MaxDescripcion} de descripción." });

        var resultado = _ml.Clasificar(titulo, descripcion);
        return resultado is null
            ? StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails { Status = 503, Title = "El modelo todavía se está entrenando. Intenta en unos segundos." })
            : Ok(resultado);
    }

    // GET /api/ml/recomendaciones?limite=4&incluirPasados=false
    // Con sesión iniciada devuelve recomendaciones personalizadas; sin sesión, los eventos más populares.
    [HttpGet("recomendaciones")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<RecomendacionesDto>> Recomendaciones(
        int limite = 4, bool incluirPasados = false, CancellationToken cancellationToken = default)
    {
        var estudianteId = await SesionEstudiante.ObtenerIdAsync(HttpContext, _context, cancellationToken);
        return Ok(await _ml.RecomendarAsync(_context, estudianteId, limite, incluirPasados, cancellationToken));
    }
}
