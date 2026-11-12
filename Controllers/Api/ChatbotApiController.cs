using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UniConnect.Services.Chatbot;

namespace UniConnect.Controllers.Api;

/// <summary>Asistente virtual "Uni" (Google Gemini, o reglas simples si no hay clave configurada).</summary>
[ApiController]
[Route("api/chatbot")]
[Produces("application/json")]
public class ChatbotApiController : ControllerBase
{
    public const string PoliticaLimite = "chatbot";

    private readonly ServicioChatbot _chatbot;

    public ChatbotApiController(ServicioChatbot chatbot) => _chatbot = chatbot;

    // GET /api/chatbot/estado — indica si responde con Gemini o con reglas
    [HttpGet("estado")]
    public IActionResult Estado() => Ok(new { motor = _chatbot.Configurado ? "gemini" : "reglas" });

    // POST /api/chatbot   { "mensajes": [ { "rol": "user", "texto": "¿Qué eventos gratis hay?" } ] }
    [HttpPost]
    [EnableRateLimiting(PoliticaLimite)]
    public async Task<ActionResult<RespuestaChatbotDto>> Enviar([FromBody] SolicitudChatbotDto solicitud, CancellationToken ct)
    {
        var mensajes = (solicitud.Mensajes ?? [])
            .Where(m => m is not null && !string.IsNullOrWhiteSpace(m.Texto))
            .Select(m => new MensajeChatbotDto(m.Rol == "assistant" ? "assistant" : "user", m.Texto.Trim()))
            .TakeLast(ServicioChatbot.MaxMensajes)
            .ToList();

        // La conversación debe empezar con el estudiante y terminar con su pregunta.
        while (mensajes.Count > 0 && mensajes[0].Rol != "user") mensajes.RemoveAt(0);

        if (mensajes.Count == 0 || mensajes[^1].Rol != "user")
            return BadRequest(new ProblemDetails { Status = 400, Title = "Escribe un mensaje." });
        if (mensajes.Any(m => m.Texto.Length > ServicioChatbot.MaxCaracteres))
            return BadRequest(new ProblemDetails { Status = 400, Title = $"Cada mensaje admite hasta {ServicioChatbot.MaxCaracteres} caracteres." });

        // Une turnos consecutivos del mismo rol (la API exige que se alternen).
        var alternados = new List<MensajeChatbotDto>();
        foreach (var m in mensajes)
        {
            if (alternados.Count > 0 && alternados[^1].Rol == m.Rol)
                alternados[^1] = alternados[^1] with { Texto = alternados[^1].Texto + "\n" + m.Texto };
            else
                alternados.Add(m);
        }

        return Ok(await _chatbot.ResponderAsync(alternados, ct));
    }
}
