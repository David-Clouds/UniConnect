namespace UniConnect.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string CodigoQR { get; set; } = string.Empty;
        public DateTime FechaCompra { get; set; }

        public int EventoId { get; set; }
        public Evento Evento { get; set; } = null!;

        public int EstudianteId { get; set; }
        public Estudiante Estudiante { get; set; } = null!;
    }
}