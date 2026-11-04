using Microsoft.EntityFrameworkCore;
using UniConnect.Data;

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
