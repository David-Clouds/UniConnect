namespace UniConnect.Models
{
    public class Comunidad
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string TipoApoyo { get; set; } = string.Empty; // Voluntariado, Comunidad, Causa solidaria
        public string? ImagenUrl { get; set; }

        public string Organizacion { get; set; } = string.Empty;
        public string? DescripcionLarga { get; set; }
        public string Ubicacion { get; set; } = string.Empty;
        public string? Frecuencia { get; set; } // texto libre, ej. "Todos los sábados"
        public string? Duracion { get; set; }
        public int Miembros { get; set; }
        public string? Impacto { get; set; }
        public string? Tags { get; set; } // separados por comas
    }
}
