namespace UniConnect.Models
{
    public class Organizador
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }

        public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
    }
}