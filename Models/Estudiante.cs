namespace UniConnect.Models
{
    public class Estudiante
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool EsAdministradorGeneral { get; set; } = false;

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
