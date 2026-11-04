using Microsoft.ML.Data;

namespace UniConnect.Services.ML;

// ───────────── Clasificación: texto del evento → categoría ─────────────

/// <summary>Ejemplo de entrenamiento (y entrada de predicción) del clasificador.</summary>
public sealed class EventoTexto
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
}

public sealed class PrediccionCategoria
{
    [ColumnName("PredictedLabel")]
    public string Categoria { get; set; } = string.Empty;

    // Probabilidad de cada categoría (SdcaMaximumEntropy devuelve valores que suman 1).
    public float[] Score { get; set; } = [];
}

// ───────────── Recomendación: estudiante × categoría → interés (1 a 5) ─────────────

public sealed class InteresCategoria
{
    public string UsuarioId { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public float Interes { get; set; }
}

public sealed class PrediccionInteres
{
    public float Score { get; set; }
}

/// <summary>
/// Datos base con los que arrancan los modelos. Al entrenar se les suman los datos reales
/// de la base (eventos publicados y tickets emitidos), así los modelos mejoran con el uso.
/// </summary>
internal static class DatosMl
{
    public static readonly string[] Categorias =
        ["Emprendimiento", "Académico", "Cultura", "Deportes", "Música", "Networking"];

    public static IEnumerable<EventoTexto> EventosEtiquetados()
    {
        static EventoTexto E(string categoria, string titulo, string descripcion) =>
            new() { Categoria = categoria, Titulo = titulo, Descripcion = descripcion };

        return
        [
            // Emprendimiento
            E("Emprendimiento", "Pitch Day USMP", "Presenta tu startup ante inversionistas y mentores y recibe retroalimentación de tu modelo de negocio."),
            E("Emprendimiento", "Taller de modelo Canvas", "Aprende a validar tu idea de negocio con el lienzo Canvas y entrevistas a clientes."),
            E("Emprendimiento", "Feria de emprendedores universitarios", "Stands de negocios creados por estudiantes: venta de productos, servicios y marcas propias."),
            E("Emprendimiento", "Cómo financiar tu startup", "Fondos concursables, capital semilla, inversionistas ángeles y crowdfunding para emprendedores."),
            E("Emprendimiento", "Bootcamp de innovación y negocios", "Tres días para convertir una idea en un prototipo de negocio con mentores de la incubadora."),
            E("Emprendimiento", "Marketing digital para emprendedores", "Redes sociales, ventas en línea y estrategias de crecimiento para negocios pequeños."),
            E("Emprendimiento", "Concurso de planes de negocio", "Equipos presentan su plan de negocio y compiten por premios en efectivo y asesoría."),
            E("Emprendimiento", "Finanzas para tu emprendimiento", "Flujo de caja, costos, precios y punto de equilibrio para tu primera empresa."),
            E("Emprendimiento", "Charla: de estudiante a fundador", "Egresados cuentan cómo crearon su empresa y los errores que cometieron al emprender."),
            E("Emprendimiento", "Incubadora USMP: convocatoria abierta", "Postula tu emprendimiento a la incubadora y accede a mentoría, oficina y red de contactos."),
            E("Emprendimiento", "Taller de ventas y negociación", "Técnicas para cerrar ventas, negociar con proveedores y conseguir tus primeros clientes."),
            E("Emprendimiento", "Startup Weekend Lima", "Forma un equipo, desarrolla una propuesta de valor y presenta tu startup en 54 horas."),

            // Académico
            E("Académico", "Congreso de Ingeniería de Sistemas", "Ponencias sobre inteligencia artificial, ciberseguridad y desarrollo de software."),
            E("Académico", "Seminario de investigación científica", "Metodología de la investigación, redacción de artículos y publicación en revistas indexadas."),
            E("Académico", "Hackathon de soluciones tecnológicas", "Equipos programan durante 48 horas soluciones de software para problemas reales."),
            E("Académico", "Conferencia de inteligencia artificial", "Aprendizaje automático, redes neuronales y aplicaciones de la IA en la industria."),
            E("Académico", "Taller de programación en Python", "Fundamentos de programación, análisis de datos y automatización con Python."),
            E("Académico", "Jornada de tesis y titulación", "Asesoría para elaborar el proyecto de tesis, el marco teórico y la sustentación."),
            E("Académico", "Simposio de medicina humana", "Avances clínicos, casos médicos y ponencias de docentes de la facultad de medicina."),
            E("Académico", "Curso de Excel avanzado", "Tablas dinámicas, fórmulas y dashboards para el análisis de información."),
            E("Académico", "Olimpiada de matemáticas", "Competencia de resolución de problemas de cálculo, álgebra y estadística."),
            E("Académico", "Semana de la ciencia y tecnología", "Exposición de proyectos de investigación, laboratorios abiertos y charlas de ciencia."),
            E("Académico", "Taller de ciberseguridad", "Hacking ético, protección de datos y buenas prácticas de seguridad informática."),
            E("Académico", "Conversatorio de derecho constitucional", "Docentes y juristas analizan sentencias y reformas del sistema legal peruano."),

            // Cultura
            E("Cultura", "Noche cultural de danzas peruanas", "Marinera, huaylas y danzas de la costa, sierra y selva con elencos universitarios."),
            E("Cultura", "Muestra de teatro universitario", "Obras de teatro creadas y actuadas por el elenco de artes escénicas de la universidad."),
            E("Cultura", "Exposición de arte y fotografía", "Galería con pinturas, ilustraciones y fotografías de estudiantes y artistas invitados."),
            E("Cultura", "Ciclo de cine latinoamericano", "Proyección de películas latinoamericanas seguida de un cineforo con críticos."),
            E("Cultura", "Feria del libro y la lectura", "Presentación de libros, firma de autores y talleres de escritura creativa."),
            E("Cultura", "Festival gastronómico de las regiones", "Platos típicos, tradiciones culinarias y cultura de las regiones del Perú."),
            E("Cultura", "Recital de poesía", "Lectura de poemas de autores peruanos y micrófono abierto para estudiantes."),
            E("Cultura", "Visita guiada al centro histórico", "Recorrido por museos, casonas y la historia colonial de Lima."),
            E("Cultura", "Taller de cerámica y artesanía", "Técnicas tradicionales de cerámica, tejido y arte popular andino."),
            E("Cultura", "Día de la identidad cultural", "Muestra de costumbres, vestimenta típica, folclore y tradiciones del Perú."),
            E("Cultura", "Concurso de pintura al aire libre", "Artistas estudiantes pintan el campus y un jurado premia las mejores obras."),
            E("Cultura", "Muestra de artes escénicas y mimo", "Espectáculo de expresión corporal, mimo y actuación con el elenco cultural."),

            // Deportes
            E("Deportes", "Campeonato interfacultades de fútbol", "Torneo de fútbol entre facultades con premiación para los equipos ganadores."),
            E("Deportes", "Maratón universitaria 5K", "Carrera atlética de cinco kilómetros por el campus abierta a estudiantes y docentes."),
            E("Deportes", "Torneo de vóley mixto", "Equipos mixtos compiten en el coliseo universitario durante todo el fin de semana."),
            E("Deportes", "Clase abierta de crossfit", "Entrenamiento funcional de alta intensidad guiado por instructores del gimnasio."),
            E("Deportes", "Liga de básquet universitario", "Partidos de básquet entre facultades con fase de grupos y playoffs."),
            E("Deportes", "Torneo de ajedrez", "Competencia de ajedrez por rondas con modalidad rápida y clásica."),
            E("Deportes", "Ciclopaseo por la ciudad", "Recorrido en bicicleta por Lima promoviendo la actividad física y la movilidad sostenible."),
            E("Deportes", "Taller de yoga y bienestar", "Sesión de yoga, estiramientos y respiración para reducir el estrés."),
            E("Deportes", "Copa de natación universitaria", "Competencias de natación en estilo libre, espalda y relevos."),
            E("Deportes", "Torneo de eSports", "Competencia de videojuegos en equipos con transmisión en vivo y premios."),
            E("Deportes", "Clínica de tenis de mesa", "Entrenamiento de técnica y partidos amistosos de tenis de mesa."),
            E("Deportes", "Selectivo de atletismo", "Pruebas de velocidad y resistencia para integrar el equipo de atletismo."),

            // Música
            E("Música", "Concierto de la orquesta sinfónica", "La orquesta universitaria interpreta obras clásicas de Beethoven y Mozart."),
            E("Música", "Festival de bandas universitarias", "Bandas de rock, pop e indie formadas por estudiantes tocan en vivo en el campus."),
            E("Música", "Noche de jazz", "Ensamble de jazz con improvisación, saxofón y piano en el auditorio."),
            E("Música", "Taller de guitarra para principiantes", "Aprende acordes básicos, ritmos y tus primeras canciones en guitarra."),
            E("Música", "Concurso de canto", "Estudiantes interpretan canciones en vivo y un jurado elige las mejores voces."),
            E("Música", "Coro universitario: concierto de temporada", "Presentación del coro con repertorio coral clásico y popular."),
            E("Música", "Peña criolla", "Música criolla, cajón y valses peruanos en vivo con artistas invitados."),
            E("Música", "Taller de producción musical", "Grabación, mezcla y producción de canciones en un estudio digital."),
            E("Música", "Recital de piano", "Estudiantes del taller de piano presentan piezas clásicas y contemporáneas."),
            E("Música", "Festival de música electrónica", "DJs universitarios presentan sets de música electrónica en vivo."),
            E("Música", "Concierto acústico al atardecer", "Cantautores interpretan versiones acústicas en los jardines del campus."),
            E("Música", "Clase abierta de percusión", "Ritmos afroperuanos con cajón, congas y percusión latina."),

            // Networking
            E("Networking", "Feria laboral USMP", "Empresas reclutan practicantes y egresados: lleva tu CV y conversa con reclutadores."),
            E("Networking", "Encuentro de egresados", "Reunión de exalumnos para compartir experiencias profesionales y ampliar contactos."),
            E("Networking", "Taller de LinkedIn y marca personal", "Optimiza tu perfil profesional, tu CV y tu presencia en redes laborales."),
            E("Networking", "Speed networking con empresas", "Rondas de conversaciones de cinco minutos con gerentes y profesionales del sector."),
            E("Networking", "Mentoría profesional", "Profesionales con experiencia orientan a estudiantes sobre su carrera y empleabilidad."),
            E("Networking", "Desayuno de líderes", "Conversatorio con directivos de empresas sobre liderazgo y desarrollo profesional."),
            E("Networking", "Simulacro de entrevistas laborales", "Practica entrevistas de trabajo con reclutadores y recibe retroalimentación."),
            E("Networking", "Meetup de desarrolladores", "Comunidad de programadores comparte proyectos y oportunidades laborales en tecnología."),
            E("Networking", "Convocatoria de prácticas preprofesionales", "Empresas presentan sus programas de prácticas y procesos de selección."),
            E("Networking", "Afterwork de comunidades profesionales", "Espacio informal para conocer profesionales, compartir contactos y oportunidades."),
            E("Networking", "Conversatorio con reclutadores", "Qué buscan las empresas al contratar y cómo destacar en un proceso de selección."),
            E("Networking", "Rueda de contactos internacionales", "Conecta con estudiantes y profesionales de universidades aliadas del extranjero.")
        ];
    }

    /// <summary>
    /// Interacciones simuladas de estudiantes ficticios (semilla fija, siempre las mismas).
    /// Sirven para que el modelo de recomendación aprenda qué categorías suelen ir juntas
    /// (por ejemplo, Emprendimiento con Networking) aunque la base real todavía tenga pocos tickets.
    /// </summary>
    public static IEnumerable<InteresCategoria> InteresesSimulados()
    {
        var perfiles = new (string[] Favoritas, string[] Afines)[]
        {
            (new[] { "Académico" }, new[] { "Networking", "Emprendimiento" }),
            (new[] { "Emprendimiento" }, new[] { "Networking", "Académico" }),
            (new[] { "Cultura" }, new[] { "Música" }),
            (new[] { "Música" }, new[] { "Cultura" }),
            (new[] { "Deportes" }, new[] { "Música" }),
            (new[] { "Networking" }, new[] { "Emprendimiento", "Académico" }),
            (new[] { "Académico", "Deportes" }, new[] { "Networking" }),
            (new[] { "Cultura", "Emprendimiento" }, new[] { "Música", "Networking" })
        };

        var azar = new Random(2026);
        for (var i = 1; i <= 96; i++)
        {
            var perfil = perfiles[i % perfiles.Length];
            foreach (var categoria in Categorias)
            {
                // Datos incompletos a propósito: nadie califica todas las categorías.
                if (azar.NextDouble() < 0.25) continue;

                var interes = perfil.Favoritas.Contains(categoria) ? 4.5f + (float)azar.NextDouble() * 0.5f
                    : perfil.Afines.Contains(categoria) ? 3.2f + (float)azar.NextDouble() * 0.8f
                    : 1.0f + (float)azar.NextDouble() * 1.2f;

                yield return new InteresCategoria { UsuarioId = $"sim-{i}", Categoria = categoria, Interes = interes };
            }
        }
    }
}
