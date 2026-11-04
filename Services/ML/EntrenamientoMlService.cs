using UniConnect.Data;

namespace UniConnect.Services.ML;

/// <summary>
/// Entrena los modelos al iniciar la aplicación y los reentrena cada 15 minutos
/// (o antes, si se solicita), para que incorporen los eventos y tickets nuevos.
/// </summary>
public sealed class EntrenamientoMlService : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopes;
    private readonly ServicioMl _ml;
    private readonly ILogger<EntrenamientoMlService> _logger;

    public EntrenamientoMlService(IServiceScopeFactory scopes, ServicioMl ml, ILogger<EntrenamientoMlService> logger)
    {
        _scopes = scopes;
        _ml = ml;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // no retrasar el arranque de la web

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await _ml.EntrenarAsync(db, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "No se pudieron entrenar los modelos de ML.");
                }

                // Cada 15 minutos, o antes si un estudiante nuevo necesita recomendaciones.
                await _ml.EsperarSiguienteEntrenamientoAsync(Intervalo, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // La aplicación se está deteniendo.
        }
    }
}
