namespace UniConnect.Models
{
    public class Comunidad
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string TipoApoyo { get; set; } = string.Empty; // Voluntariado, Donación, Colecta
        public string? ImagenUrl { get; set; }
    }
}