using UniConnect.Models;

namespace UniConnect.Data
{
    public static class SeedData
    {
        public static void Inicializar(AppDbContext context)
        {
            if (context.Eventos.Any()) return; // ya hay datos, no dupliques

            var organizador = new Organizador
            {
                Nombre = "DevClub USMP",
                Correo = "devclub@usmp.pe",
                Descripcion = "Comunidad de desarrollo de software"
            };
            context.Organizadores.Add(organizador);
            context.SaveChanges();

            context.Eventos.AddRange(
                new Evento
                {
                    Titulo = "Tech Summit USMP: Inteligencia Artificial",
                    Categoria = "Académico",
                    Fecha = DateTime.Now.AddDays(7),
                    Precio = 0,
                    Descripcion = "Conferencia sobre el impacto de la IA en el mundo empresarial, con talleres prácticos de prompting y automatización.",
                    ImagenUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?w=900&h=500&fit=crop",
                    OrganizadorId = organizador.Id
                },
                new Evento
                {
                    Titulo = "Noche Cultural USMP: Arte y Tradición",
                    Categoria = "Cultura",
                    Fecha = DateTime.Now.AddDays(14),
                    Precio = 15,
                    Descripcion = "Danza contemporánea, música en vivo y teatro experimental celebrando la diversidad cultural universitaria.",
                    ImagenUrl = "https://images.unsplash.com/photo-1514320291840-2e0a9bf2a9ae?w=900&h=500&fit=crop",
                    OrganizadorId = organizador.Id
                },
                new Evento
                {
                    Titulo = "Hackathon 48H: Ciudad Inteligente Lima",
                    Categoria = "Académico",
                    Fecha = DateTime.Now.AddDays(21),
                    Precio = 0,
                    Descripcion = "48 horas creando soluciones tecnológicas a problemas urbanos reales, en equipos de 3 a 5 personas.",
                    ImagenUrl = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?w=900&h=500&fit=crop",
                    OrganizadorId = organizador.Id
                }
            );

            context.SaveChanges();
        }
    }
}