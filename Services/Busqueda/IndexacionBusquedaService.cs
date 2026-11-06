using UniConnect.Data;
using UniConnect.Services.Integraciones;

namespace UniConnect.Services.Busqueda;

/// <summary>Sube los eventos e iniciativas a Algolia al iniciar la app y luego cada 30 minutos.</summary>
public sealed class IndexacionBusquedaService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopes;
    private readonly ServicioBusqueda _busqueda;
    private readonly EstadoIntegraciones _estado;
    private readonly ILogger<IndexacionBusquedaService> _logger;

    public IndexacionBusquedaService(IServiceScopeFactory scopes, ServicioBusqueda busqueda,
        EstadoIntegraciones estado, ILogger<IndexacionBusquedaService> logger)
    {
        _scopes = scopes;
        _busqueda = busqueda;
        _estado = estado;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_busqueda.IndexacionConfigurada)
        {
            _logger.LogInformation("Algolia:AdminKey no está configurada: no se indexa (la búsqueda usa la base de datos o el índice existente).");
            return;
        }

        try
        {
            // Dejar que Program.cs aplique migraciones y datos iniciales antes de leer la base.
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            using var temporizador = new PeriodicTimer(Intervalo);
            do
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await _busqueda.IndexarAsync(db, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _estado.ErrorAlgolia(ex.Message);
                    _logger.LogWarning(ex, "No se pudo indexar en Algolia.");
                }
            }
            while (await temporizador.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La aplicación se está deteniendo.
        }
    }
}
