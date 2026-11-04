namespace UniConnect.Dtos;

public sealed record EventoDto(
    int Id,
    string Titulo,
    string Categoria,
    DateTime Fecha,
    decimal Precio,
    bool EsGratuito,
    string? Descripcion,
    string? ImagenUrl,
    int OrganizadorId,
    string Organizador);

public sealed record ComunidadDto(
    int Id,
    string Nombre,
    string TipoApoyo,
    string Organizacion,
    string Descripcion,
    string? DescripcionLarga,
    string Ubicacion,
    string? Frecuencia,
    string? Duracion,
    int Miembros,
    string? Impacto,
    IReadOnlyList<string> Tags,
    string? ImagenUrl);

public sealed record EstadisticasDto(
    int TotalEventos,
    int EventosGratuitos,
    int EventosDePago,
    int TicketsEmitidos,
    decimal IngresosSimulados,
    int TotalComunidades,
    int TotalMiembros,
    IReadOnlyDictionary<string, int> EventosPorCategoria,
    DateTime GeneradoEn);
