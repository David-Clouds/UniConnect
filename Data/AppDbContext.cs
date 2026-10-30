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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Estudiante>().Property(e => e.EsAdministradorGeneral)
                .HasDefaultValue(false);
            modelBuilder.Entity<Organizador>().HasOne(o => o.Estudiante).WithMany()
                .HasForeignKey(o => o.EstudianteId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Organizador>().Property(o => o.EstudianteId).IsConcurrencyToken();
        }
    }
}
