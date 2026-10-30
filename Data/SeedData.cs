using UniConnect.Models;

namespace UniConnect.Data
{
    public static class SeedData
    {
        public static void Inicializar(AppDbContext context)
        {
            if (!context.Eventos.Any()) // ya hay datos, no dupliques
            {
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

            if (!context.Comunidades.Any())
            {
                context.Comunidades.AddRange(
                    new Comunidad
                    {
                        Nombre = "Lima Verde",
                        Organizacion = "Colectivo Lima Verde",
                        TipoApoyo = "Voluntariado",
                        Descripcion = "Reforestamos parques y riberas de Lima con especies nativas junto a vecinos y estudiantes.",
                        DescripcionLarga = "Lima Verde nació en 2021 con un grupo de estudiantes que quería devolverle áreas verdes a la ciudad. Cada semana plantamos árboles nativos, damos mantenimiento a los ya sembrados y organizamos talleres de compostaje con la comunidad. No necesitas experiencia: te damos herramientas, capacitación y refrigerio.",
                        Ubicacion = "Parque Ecológico de Surco, Lima",
                        Frecuencia = "Todos los sábados",
                        Duracion = "4 horas",
                        Miembros = 128,
                        Impacto = "Más de 2,300 árboles nativos plantados en 14 parques de Lima",
                        Tags = "Medio ambiente,Reforestación,Aire libre",
                        ImagenUrl = "https://images.unsplash.com/photo-1542601906990-b4d3fb778b09?w=900&h=500&fit=crop"
                    },
                    new Comunidad
                    {
                        Nombre = "Apoyo Escolar en Comas",
                        Organizacion = "Voluntariado USMP",
                        TipoApoyo = "Voluntariado",
                        Descripcion = "Refuerzo escolar en matemática y comunicación para niños de primaria en Comas.",
                        DescripcionLarga = "Acompañamos a niños de 3.º a 6.º de primaria de colegios públicos de Comas con sesiones de refuerzo en matemática, lectura y escritura. Cada voluntario tutoriza a un grupo pequeño durante el ciclo, lo que permite dar seguimiento real a su progreso. Recibirás una capacitación pedagógica breve antes de empezar.",
                        Ubicacion = "Colegio público aliado, Comas",
                        Frecuencia = "Martes y jueves",
                        Duracion = "2 horas por sesión",
                        Miembros = 64,
                        Impacto = "180 niños acompañados; 85 % mejoró sus notas en matemática",
                        Tags = "Educación,Niñez,Tutoría",
                        ImagenUrl = "https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=900&h=500&fit=crop"
                    },
                    new Comunidad
                    {
                        Nombre = "Banco de Alimentos USMP",
                        Organizacion = "Bienestar Universitario USMP",
                        TipoApoyo = "Causa solidaria",
                        Descripcion = "Recolectamos y distribuimos alimentos no perecibles para estudiantes y familias en situación vulnerable.",
                        DescripcionLarga = "El Banco de Alimentos USMP recibe donaciones en los puntos de acopio del campus y las distribuye cada mes entre estudiantes con dificultades económicas y comedores populares aliados. Puedes ayudar donando, clasificando productos o apoyando en las jornadas de entrega.",
                        Ubicacion = "Campus Santa Anita, USMP",
                        Frecuencia = "Campaña permanente",
                        Duracion = "Flexible",
                        Miembros = 215,
                        Impacto = "12 toneladas de alimentos entregadas a 9 comedores populares",
                        Tags = "Alimentación,Donaciones,Solidaridad",
                        ImagenUrl = "https://images.unsplash.com/photo-1488521787991-ed7bbaae773c?w=900&h=500&fit=crop"
                    },
                    new Comunidad
                    {
                        Nombre = "DevClub USMP",
                        Organizacion = "DevClub USMP",
                        TipoApoyo = "Comunidad",
                        Descripcion = "Comunidad de desarrollo de software: proyectos reales, workshops y preparación para hackathons.",
                        DescripcionLarga = "En DevClub aprendemos construyendo. Formamos equipos para proyectos open source y soluciones para ONGs, organizamos workshops de desarrollo web, cloud e IA, y nos preparamos juntos para hackathons. Todos los niveles son bienvenidos: los miembros con más experiencia mentorean a los que recién empiezan.",
                        Ubicacion = "Facultad de Ingeniería y Arquitectura, USMP",
                        Frecuencia = "Todos los miércoles",
                        Duracion = "2 horas",
                        Miembros = 342,
                        Impacto = "27 proyectos publicados y 6 hackathons ganados",
                        Tags = "Tecnología,Programación,Networking",
                        ImagenUrl = "https://images.unsplash.com/photo-1522071820081-009f0129c71c?w=900&h=500&fit=crop"
                    },
                    new Comunidad
                    {
                        Nombre = "Feria de Salud Comunitaria",
                        Organizacion = "Facultad de Medicina Humana USMP",
                        TipoApoyo = "Voluntariado",
                        Descripcion = "Campañas gratuitas de despistaje y orientación en salud para comunidades de Lima Este.",
                        DescripcionLarga = "Junto a docentes de la Facultad de Medicina organizamos ferias de salud con toma de presión, glucosa, orientación nutricional y charlas preventivas. Estudiantes de todas las carreras pueden participar: se necesitan voluntarios para logística, registro, difusión y apoyo a los equipos de salud.",
                        Ubicacion = "San Juan de Lurigancho, Lima",
                        Frecuencia = "Un domingo al mes",
                        Duracion = "6 horas",
                        Miembros = 97,
                        Impacto = "Más de 3,500 atenciones gratuitas en 2025",
                        Tags = "Salud,Prevención,Comunidad",
                        ImagenUrl = "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=900&h=500&fit=crop"
                    },
                    new Comunidad
                    {
                        Nombre = "EcoClub USMP",
                        Organizacion = "EcoClub USMP",
                        TipoApoyo = "Comunidad",
                        Descripcion = "Estudiantes impulsando un campus sostenible: reciclaje, huertos urbanos y consumo responsable.",
                        DescripcionLarga = "EcoClub promueve la sostenibilidad dentro del campus. Gestionamos los puntos de reciclaje, cuidamos el huerto universitario y organizamos campañas de cero plástico y cine-foros ambientales. Es un espacio para conocer gente con tus mismos intereses y liderar iniciativas propias.",
                        Ubicacion = "Campus Santa Anita, USMP",
                        Frecuencia = "Cada 15 días",
                        Duracion = "1.5 horas",
                        Miembros = 156,
                        Impacto = "4 toneladas de residuos reciclados y 1 huerto universitario activo",
                        Tags = "Sostenibilidad,Reciclaje,Campus",
                        ImagenUrl = "https://images.unsplash.com/photo-1532996122724-e3c354a0b15b?w=900&h=500&fit=crop"
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
