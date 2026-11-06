using Microsoft.EntityFrameworkCore;
using UniConnect.Data;

namespace UniConnect.Services;

/// <summary>
/// Usuario con sesión y sus permisos, calculado una sola vez por petición.
/// Lo usa el layout para mostrar u ocultar el Panel Organizador y las notificaciones.
/// </summary>
public sealed class ContextoUsuario
{
    private readonly IHttpContextAccessor _accesor;
    private readonly AppDbContext _context;
    private readonly ILogger<ContextoUsuario> _logger;
    private Task<UsuarioActual?>? _usuario;

    public ContextoUsuario(IHttpContextAccessor accesor, AppDbContext context, ILogger<ContextoUsuario> logger)
    {
        _accesor = accesor;
        _context = context;
        _logger = logger;
    }

    public Task<UsuarioActual?> ObtenerAsync() => _usuario ??= CargarSeguroAsync();

    // El layout nunca debe fallar por esto (por ejemplo, si la sesión no se puede leer).
    private async Task<UsuarioActual?> CargarSeguroAsync()
    {
        try
        {
            return await CargarAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "No se pudo obtener el usuario de la sesión.");
            return null;
        }
    }

    private async Task<UsuarioActual?> CargarAsync()
    {
        var http = _accesor.HttpContext;
        if (http is null) return null;

        var ct = http.RequestAborted;
        var id = await SesionEstudiante.ObtenerIdAsync(http, _context, ct);
        if (id is null) return null;

        var esAdministrador = await _context.Estudiantes.AsNoTracking()
            .Where(e => e.Id == id.Value)
            .Select(e => e.EsAdministradorGeneral)
            .FirstOrDefaultAsync(ct);

        var organizadores = await _context.Organizadores.AsNoTracking()
            .Where(o => o.EstudianteId == id.Value)
            .Select(o => o.Id)
            .ToListAsync(ct);

        return new UsuarioActual(id.Value, esAdministrador, organizadores);
    }
}

public sealed record UsuarioActual(int Id, bool EsAdministrador, IReadOnlyList<int> OrganizadorIds)
{
    /// <summary>Misma regla que OrganizadorController: administrador o responsable de algún organizador.</summary>
    public bool PuedeVerPanel => EsAdministrador || OrganizadorIds.Count > 0;
}
