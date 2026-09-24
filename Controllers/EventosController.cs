using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.Models;

namespace UniConnect.Controllers
{
    public class EventosController : Controller
    {
        private readonly AppDbContext _context;

        public EventosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Eventos  (listado + filtro)
        public async Task<IActionResult> Index(string? categoria, DateTime? fecha, decimal? precioMax)
        {
            var query = _context.Eventos
                .Include(e => e.Organizador)
                .AsQueryable();

            if (!string.IsNullOrEmpty(categoria) && categoria != "Todos")
                query = query.Where(e => e.Categoria == categoria);

            if (fecha.HasValue)
                query = query.Where(e => e.Fecha.Date == fecha.Value.Date);

            if (precioMax.HasValue)
                query = query.Where(e => e.Precio <= precioMax.Value);

            var eventos = await query.OrderBy(e => e.Fecha).ToListAsync();

            ViewBag.Categorias = new[] { "Todos", "Emprendimiento", "Académico", "Cultura", "Deportes", "Música", "Networking" };
            ViewBag.CategoriaSeleccionada = categoria ?? "Todos";
            ViewBag.Active = "home";

            return View(eventos);
        }

        // GET: /Eventos/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var evento = await _context.Eventos
                .Include(e => e.Organizador)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento == null)
                return NotFound();

            // Guardar en sesión los últimos eventos vistos (lo usaremos en la entrega de la semana 8)
            var vistos = HttpContext.Session.GetString("UltimosVistos");
            var lista = string.IsNullOrEmpty(vistos)
                ? new List<int>()
                : System.Text.Json.JsonSerializer.Deserialize<List<int>>(vistos)!;

            lista.Remove(id);
            lista.Insert(0, id);
            if (lista.Count > 5) lista = lista.Take(5).ToList();

            HttpContext.Session.SetString("UltimosVistos", System.Text.Json.JsonSerializer.Serialize(lista));

            ViewBag.Active = "home";
            return View(evento);
        }
    }
}