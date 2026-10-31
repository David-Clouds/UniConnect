using QRCoder;

namespace UniConnect.Services;

/// <summary>
/// Genera la imagen QR de un ticket en el servidor (PNG embebido como data URI),
/// sin depender de scripts externos ni de servicios de terceros.
/// </summary>
public static class QrTicketRenderer
{
    public static string ToDataUri(string? contenido, int pixelsPorModulo = 8)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return string.Empty;

        using var generador = new QRCodeGenerator();
        using var datos = generador.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(datos).GetGraphic(pixelsPorModulo);
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }
}
