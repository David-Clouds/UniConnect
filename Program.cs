using Microsoft.EntityFrameworkCore;
using UniConnect.Data;
using UniConnect.Services;
using UniConnect.Services.Busqueda;
using UniConnect.Services.Integraciones;
using UniConnect.Services.Mensajeria;
using UniConnect.Services.TiempoReal;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Caché distribuida: memoria en desarrollo, Redis en producción
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration["Redis:ConnectionString"];
    });
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
});

// Machine Learning (ML.NET): clasificación de eventos y recomendaciones personalizadas.
// Los modelos se entrenan en segundo plano al iniciar y se reentrenan cada 15 minutos.
builder.Services.AddSingleton<UniConnect.Services.ML.ServicioMl>();
builder.Services.AddHostedService<UniConnect.Services.ML.EntrenamientoMlService>();

// Usuario con sesión y sus permisos (layout: menú y notificaciones).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ContextoUsuario>();

// Integraciones externas. Todas son opcionales: si faltan sus claves, la app funciona igual.
//  • CloudAMQP (RabbitMQ): cola "ticket.emitido"            → CloudAmqp__Url
//  • PieHost (WebSocket): avisos en vivo al organizador     → PieHost__ClusterId, PieHost__ApiKey, PieHost__ApiSecret
//  • Algolia: buscador de la barra superior                 → Algolia__AppId, Algolia__SearchKey, Algolia__AdminKey
builder.Services.AddSingleton<EstadoIntegraciones>();
builder.Services.AddSingleton<NotificadorPieHost>();
builder.Services.AddSingleton<ProcesadorTicketEmitido>();
builder.Services.AddSingleton<PublicadorTickets>();
builder.Services.AddHostedService<ConsumidorTicketsService>();
builder.Services.AddSingleton<ServicioBusqueda>();
builder.Services.AddHostedService<IndexacionBusquedaService>();

// Chatbot "Uni" (Claude). Opcional: sin Anthropic__ApiKey responde con reglas simples.
builder.Services.AddSingleton<UniConnect.Services.Chatbot.ServicioChatbot>();
// Límite por IP para que el chatbot no se pueda usar de forma abusiva (cuida el costo de la API).
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.AddPolicy(UniConnect.Controllers.Api.ChatbotApiController.PoliticaLimite, contexto =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            // Render está detrás de un proxy: la IP real llega en X-Forwarded-For.
            contexto.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim() is { Length: > 0 } ip
                ? ip : contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    // "/" abre Inicio: login si no hay sesión, eventos si ya la hay.
    pattern: "{controller=Inicio}/{action=Index}/{id?}");

// Aplicar migraciones y sembrar datos de prueba automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    UniConnect.Data.SeedData.Inicializar(db);
}

app.Run();
