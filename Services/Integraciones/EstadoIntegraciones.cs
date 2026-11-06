using System.Text.RegularExpressions;

namespace UniConnect.Services.Integraciones;

/// <summary>
/// Contadores y últimos errores de los servicios externos (CloudAMQP, PieHost y Algolia).
/// Se consultan en /api/integraciones para verificar en Render que todo esté conectado.
/// Nunca guarda claves ni datos personales: los errores se resumen antes de guardarse,
/// porque /api/integraciones es público. El detalle completo queda solo en los logs del servidor.
/// </summary>
public sealed partial class EstadoIntegraciones
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
    public void ErrorRabbit(string? error) => _errorRabbit = ResumirRabbit(error);
    public void ErrorPieHost(string? error) => _errorPieHost = Limpiar(error);
    public void ErrorAlgolia(string? error) => _errorAlgolia = Limpiar(error);

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

    // Los mensajes del broker pueden incluir el vhost u otros datos de la URL: solo se conserva el código.
    private static string? ResumirRabbit(string? error)
    {
        if (error is null) return null;

        var codigo = CodigoAmqp().Match(error);
        if (!codigo.Success)
            return "No se pudo conectar o publicar en CloudAMQP. Revisa CloudAmqp:Url (detalle en los logs del servidor).";

        var motivo = MotivoAmqp().Match(error);
        var pista = codigo.Groups[1].Value switch
        {
            "530" => "el vhost (la parte final de la URL, después de la última /) no es correcto",
            "403" => "el usuario o la contraseña de la URL no son correctos",
            _ => "revisa la URL de CloudAMQP"
        };
        return $"RabbitMQ rechazó la conexión (código {codigo.Groups[1].Value}"
            + (motivo.Success ? $", {motivo.Groups[1].Value}" : string.Empty)
            + $"): {pista}. Detalle en los logs del servidor.";
    }

    // Quita cualquier URL (podría llevar usuario y contraseña) y limita el largo.
    private static string? Limpiar(string? texto)
    {
        if (texto is null) return null;
        texto = UrlConCredenciales().Replace(texto, "[url]");
        return texto.Length <= 300 ? texto : texto.Substring(0, 300);
    }

    [GeneratedRegex(@"code=(\d{3})")]
    private static partial Regex CodigoAmqp();

    [GeneratedRegex(@"text='([A-Z_]+)")]
    private static partial Regex MotivoAmqp();

    [GeneratedRegex(@"[a-zA-Z][a-zA-Z0-9+.-]*://[^\s'""]+")]
    private static partial Regex UrlConCredenciales();
}

public sealed record EstadoIntegracionesDto(EstadoCloudAmqpDto CloudAmqp, EstadoPieHostDto PieHost, EstadoAlgoliaDto Algolia);

public sealed record EstadoCloudAmqpDto(
    bool Configurado, bool ConsumidorConectado, long MensajesPublicados, long MensajesConsumidos,
    long ProcesadosSinCola, string? UltimoError);

public sealed record EstadoPieHostDto(bool Configurado, long NotificacionesEnviadas, string? UltimoError);

public sealed record EstadoAlgoliaDto(
    bool BusquedaConfigurada, bool IndexacionConfigurada, int RegistrosIndexados,
    DateTime? UltimaIndexacion, string? UltimoError);
