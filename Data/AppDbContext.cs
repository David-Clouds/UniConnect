using Microsoft.EntityFrameworkCore;
using UniConnect.Models;

namespace UniConnect.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Estudiante> Estudiantes { get; set; }
        public DbSet<Organizador> Organizadores { get; set; }
        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Comunidad> Comunidades { get; set; }
    }
}