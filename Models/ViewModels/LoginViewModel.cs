using System.ComponentModel.DataAnnotations;

namespace UniConnect.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingresa tu correo universitario.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [RegularExpression(@"^[^@\s]+@usmp\.pe$", ErrorMessage = "Usa tu correo institucional (nombre@usmp.pe).")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa tu contraseña.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
