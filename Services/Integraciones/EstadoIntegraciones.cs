namespace UniConnect.Services.Integraciones;

/// <summary>
/// Contadores y últimos errores de los servicios externos (CloudAMQP, PieHost y Algolia).
/// Se consultan en /api/integraciones para verificar en Render que todo esté conectado.
/// Nunca guarda claves ni datos personales.
/// </summary>
public sealed class EstadoIntegraciones
{
    private long _publicados, _consumidos, _procesadosLocal, _notificaciones;
    private volatile bool _rabbitConectado;
    private volatile string? _errorRabbit, _errorPieHost, _errorAlgolia;
    private volatile int _registrosIndexados;
    private DateTime? _ultimaIndexacion;
    private readonly object _bloqueo = new();

    public void MensajePublicado() => Interlocked.Increment(ref _publicados);
    public void MensajeConsumido() => Interlocked.Increment(ref _consumidos);
    public void ProcesadoLocalmente() => Interlocked.Increment(ref _procesadosLocal);
    public void NotificacionEnviada() => Interlocked.Increment(ref _notificaciones);

    public void RabbitConectado(bool conectado) => _rabbitConectado = conectado;
    public void ErrorRabbit(string? error) => _errorRabbit = Recortar(error);
    public void ErrorPieHost(string? error) => _errorPieHost = Recortar(error);
    public void ErrorAlgolia(string? error) => _errorAlgolia = Recortar(error);

    public void IndexacionCompletada(int registros)
    {
        lock (_bloqueo)
        {
            _registrosIndexados = registros;
            _ultimaIndexacion = DateTime.UtcNow;
        }
        _errorAlgolia = null;
    }

    public EstadoIntegracionesDto Instantanea(bool rabbitConfigurado, bool pieHostConfigurado,
        bool algoliaBusqueda, bool algoliaIndexacion)
    {
        DateTime? ultimaIndexacion;
        lock (_bloqueo) ultimaIndexacion = _ultimaIndexacion;

        return new EstadoIntegracionesDto(
            new EstadoCloudAmqpDto(rabbitConfigurado, _rabbitConectado,
                Interlocked.Read(ref _publicados), Interlocked.Read(ref _consumidos),
                Interlocked.Read(ref _procesadosLocal), _errorRabbit),
            new EstadoPieHostDto(pieHostConfigurado, Interlocked.Read(ref _notificaciones), _errorPieHost),
            new EstadoAlgoliaDto(algoliaBusqueda, algoliaIndexacion, _registrosIndexados, ultimaIndexacion, _errorAlgolia));
    }

    private static string? Recortar(string? texto) =>
        texto is null ? null : texto.Length <= 300 ? texto : texto[..300];
}

public sealed record EstadoIntegracionesDto(EstadoCloudAmqpDto CloudAmqp, EstadoPieHostDto PieHost, EstadoAlgoliaDto Algolia);

public sealed record EstadoCloudAmqpDto(
    bool Configurado, bool ConsumidorConectado, long MensajesPublicados, long MensajesConsumidos,
    long ProcesadosSinCola, string? UltimoError);

public sealed record EstadoPieHostDto(bool Configurado, long NotificacionesEnviadas, string? UltimoError);

public sealed record EstadoAlgoliaDto(
    bool BusquedaConfigurada, bool IndexacionConfigurada, int RegistrosIndexados,
    DateTime? UltimaIndexacion, string? UltimoError);
