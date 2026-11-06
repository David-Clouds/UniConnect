using Microsoft.AspNetCore.Mvc;
using UniConnect.Services.Busqueda;
using UniConnect.Services.Integraciones;
using UniConnect.Services.Mensajeria;
using UniConnect.Services.TiempoReal;

namespace UniConnect.Controllers.Api;

/// <summary>Estado de los servicios externos. No muestra claves ni datos personales.</summary>
[ApiController]
[Route("api/integraciones")]
[Produces("application/json")]
public class IntegracionesApiController : ControllerBase
{
    private readonly EstadoIntegraciones _estado;
    private readonly PublicadorTickets _publicador;
    private readonly NotificadorPieHost _pieHost;
    private readonly ServicioBusqueda _busqueda;

    public IntegracionesApiController(EstadoIntegraciones estado, PublicadorTickets publicador,
        NotificadorPieHost pieHost, ServicioBusqueda busqueda)
    {
        _estado = estado;
        _publicador = publicador;
        _pieHost = pieHost;
        _busqueda = busqueda;
    }

    // GET /api/integraciones
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult<EstadoIntegracionesDto> Estado() =>
        Ok(_estado.Instantanea(_publicador.Configurado, _pieHost.Configurado,
            _busqueda.BusquedaConfigurada, _busqueda.IndexacionConfigurada));
}
