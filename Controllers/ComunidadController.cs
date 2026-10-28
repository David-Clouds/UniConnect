using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.Models;

namespace UniConnect.Controllers
{
    public class ComunidadController : Controller
    {
        private const string ClaveSesion = "ComunidadesUnidas";
        private readonly AppDbContext _context;

        public ComunidadController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Comunidad  (listado + filtro por tipo)
        public async Task<IActionResult> Index(string? tipo)
        {
            var query = _context.Comunidades.AsQueryable();

            if (!string.IsNullOrEmpty(tipo) && tipo != "Todos")
                query = query.Where(c => c.TipoApoyo == tipo);

            var comunidades = await query.OrderByDescending(c => c.Miembros).ToListAsync();

            // Estadísticas generales (sin filtro)
            ViewBag.TotalIniciativas = await _context.Comunidades.CountAsync();
            ViewBag.TotalMiembros = await _context.Comunidades.SumAsync(c => c.Miembros);
            ViewBag.TotalVoluntariados = await _context.Comunidades.CountAsync(c => c.TipoApoyo == "Voluntariado");

            ViewBag.Tipos = new[] { "Todos", "Voluntariado", "Comunidad", "Causa solidaria" };
            ViewBag.TipoSeleccionado = tipo ?? "Todos";
            ViewBag.Active = "joinus";

            return View(comunidades);
        }

        // GET: /Comunidad/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var comunidad = await _context.Comunidades.FirstOrDefaultAsync(c => c.Id == id);

            if (comunidad == null)
                return NotFound();

            ViewBag.Inscrito = ObtenerUnidas().Contains(id);
            ViewBag.Active = "joinus";
            return View(comunidad);
        }

        // POST: /Comunidad/Unirse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unirse(int id)
        {
            if (!await _context.Comunidades.AnyAsync(c => c.Id == id))
                return NotFound();

            var lista = ObtenerUnidas();
            if (!lista.Contains(id))
            {
                lista.Add(id);
                HttpContext.Session.SetString(ClaveSesion, System.Text.Json.JsonSerializer.Serialize(lista));
            }

            return RedirectToAction(nameof(Detalle), new { id });
        }

        private List<int> ObtenerUnidas()
        {
            var unidas = HttpContext.Session.GetString(ClaveSesion);
            return string.IsNullOrEmpty(unidas)
                ? new List<int>()
                : System.Text.Json.JsonSerializer.Deserialize<List<int>>(unidas)!;
        }
    }
}
