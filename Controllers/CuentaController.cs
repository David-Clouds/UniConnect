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

        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Estudiante> _hasher = new PasswordHasher<Estudiante>();

        public CuentaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Cuenta/Login
        // El enlace "Cerrar sesión" del layout apunta aquí, por eso al entrar se limpia la sesión.
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            HttpContext.Session.Remove(SesionEstudianteId);
            HttpContext.Session.Remove(SesionEstudianteNombre);
            HttpContext.Session.Remove(SesionEstudianteCorreo);

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

        // Como aún no hay registro, se crea una cuenta de prueba si no existe ningún estudiante.
        // Correo: demo@usmp.pe  ·  Contraseña: Usmp2026!
        private async Task AsegurarCuentaDemoAsync()
        {
            if (await _context.Estudiantes.AnyAsync())
                return;

            var demo = new Estudiante
            {
                Nombre = "Estudiante Demo",
                Correo = "demo@usmp.pe",
                Codigo = "2026000001"
            };
            demo.PasswordHash = _hasher.HashPassword(demo, "Usmp2026!");

            _context.Estudiantes.Add(demo);
            await _context.SaveChangesAsync();
        }
    }
}
