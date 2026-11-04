using Microsoft.EntityFrameworkCore;
using UniConnect.Controllers;
using UniConnect.Data;

namespace UniConnect.Services;

/// <summary>Lee el estudiante de la sesión con las mismas comprobaciones que usan los controladores.</summary>
public static class SesionEstudiante
{
    public static async Task<int?> ObtenerIdAsync(HttpContext http, AppDbContext db, CancellationToken ct)
    {
        await http.Session.LoadAsync(ct);
        if (!http.Session.TryGetValue(CuentaController.SesionEstudianteId, out var valor) || valor.Length != sizeof(int))
            return null;

        var id = http.Session.GetInt32(CuentaController.SesionEstudianteId);
        if (id is not > 0) return null;

        return await db.Estudiantes.AsNoTracking().AnyAsync(e => e.Id == id.Value, ct) ? id : null;
    }
}
