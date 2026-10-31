using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.Models;
using UniConnect.Models.ViewModels;

namespace UniConnect.Controllers
{
    public class CuentaController : Controller
    {
        // Claves de sesión que pueden usar los demás módulos para saber quién inició sesión
        public const string SesionEstudianteId = "EstudianteId";
        public const string SesionEstudianteNombre = "EstudianteNombre";
        public const string SesionEstudianteCorreo = "EstudianteCorreo";
        public const string SesionRol = "Rol";

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IPasswordHasher<Estudiante> _hasher = new PasswordHasher<Estudiante>();

        public CuentaController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Cuenta/Login
        // El enlace "Cerrar sesión" del layout apunta aquí, por eso al entrar se limpia la sesión.
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            HttpContext.Session.Remove(SesionEstudianteId);
            HttpContext.Session.Remove(SesionEstudianteNombre);
            HttpContext.Session.Remove(SesionEstudianteCorreo);
            HttpContext.Session.Remove(SesionRol);

            await AsegurarCuentasDeAccesoAsync();
            await AsegurarCuentaDemoAsync();

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Cuenta/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var correo = model.Correo.Trim().ToLowerInvariant();
            var estudiante = await _context.Estudiantes
                .FirstOrDefaultAsync(e => e.Correo.ToLower() == correo);

            var credencialesValidas = estudiante != null
                && !string.IsNullOrEmpty(estudiante.PasswordHash)
                && _hasher.VerifyHashedPassword(estudiante, estudiante.PasswordHash, model.Password)
                    != PasswordVerificationResult.Failed;

            if (!credencialesValidas)
            {
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                model.Password = string.Empty;
                return View(model);
            }

            HttpContext.Session.SetInt32(SesionEstudianteId, estudiante!.Id);
            HttpContext.Session.SetString(SesionEstudianteNombre, estudiante.Nombre);
            HttpContext.Session.SetString(SesionEstudianteCorreo, estudiante.Correo);
            HttpContext.Session.SetString(SesionRol,
                estudiante.EsAdministradorGeneral ? "Administrador" : "Estudiante");

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Eventos");
        }

        // GET: /Cuenta/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // Cuentas de acceso para la presentación. Se crean una sola vez, sin
        // modificar usuarios que ya existen ni sus contraseñas.
        private async Task AsegurarCuentasDeAccesoAsync()
        {
            var cuentas = new[]
            {
                new { Nombre = "Administrador UniConnect", Correo = "admin@usmp.pe", Codigo = "ADMIN2026", EsAdministrador = true },
                new { Nombre = "Estudiante Demo", Correo = "demo@usmp.pe", Codigo = "2026000001", EsAdministrador = false }
            };

            foreach (var cuenta in cuentas)
            {
                var existe = await _context.Estudiantes.AnyAsync(e => e.Correo.ToLower() == cuenta.Correo);
                if (existe) continue;

                var estudiante = new Estudiante
                {
                    Nombre = cuenta.Nombre,
                    Correo = cuenta.Correo,
                    Codigo = cuenta.Codigo,
                    EsAdministradorGeneral = cuenta.EsAdministrador
                };
                estudiante.PasswordHash = _hasher.HashPassword(estudiante, "Usmp2026!");
                _context.Estudiantes.Add(estudiante);
            }

            await _context.SaveChangesAsync();
        }

        // Solo una instalación Development sin estudiantes puede inicializar la demo.
        // Correo: demo@usmp.pe  ·  Contraseña: Usmp2026!
        private async Task AsegurarCuentaDemoAsync()
        {
            if (!_environment.IsDevelopment())
                return;

            // Program.cs aplica migraciones al arrancar. Aquí solo se comprueba
            // su historial; nunca se modifica el esquema desde el login.
            var aplicadas = await _context.Database.GetAppliedMigrationsAsync();
            if (!aplicadas.Contains("20261030202330_PermisosOrganizadores")
                || (await _context.Database.GetPendingMigrationsAsync()).Any())
                return;

            await using var transaccion = await _context.Database.BeginTransactionAsync();
            if (await _context.Estudiantes.AnyAsync())
                return;

            var demo = new Estudiante
            {
                Nombre = "Estudiante Demo",
                Correo = "demo@usmp.pe",
                Codigo = "2026000001",
                EsAdministradorGeneral = false
            };
            demo.PasswordHash = _hasher.HashPassword(demo, "Usmp2026!");

            _context.Estudiantes.Add(demo);

            // La búsqueda por nombre solo identifica los datos iniciales de esta
            // demo vacía; no concede permisos a cuentas que ya existían.
            var organizaciones = await _context.Organizadores
                .Where(o => o.Nombre == "DevClub USMP").Take(2).ToListAsync();
            Organizador? organizadorDemo = null;
            if (organizaciones.Count == 0)
            {
                organizadorDemo = new Organizador
                {
                    Nombre = "DevClub USMP",
                    Correo = "devclub@usmp.pe",
                    Descripcion = "Comunidad de desarrollo de software",
                    Estudiante = demo
                };
                _context.Organizadores.Add(organizadorDemo);
            }
            else if (organizaciones.Count == 1 && organizaciones[0].EstudianteId is null)
            {
                organizaciones[0].Estudiante = demo;
                organizadorDemo = organizaciones[0];
            }
            // No se elige arbitrariamente entre nombres duplicados ni se reemplazan responsables.
            if (organizadorDemo is not null)
                await SeedData.PrepararTicketsDemoAsync(_context, organizadorDemo, demo);
            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
    }
}
