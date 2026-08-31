namespace UniConnect.Models
{
    public class Evento
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Precio { get; set; }
        public string? Descripcion { get; set; }
        public string? ImagenUrl { get; set; }

        public int OrganizadorId { get; set; }
        public Organizador Organizador { get; set; } = null!;

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}