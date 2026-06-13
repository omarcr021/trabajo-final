using Microsoft.AspNetCore.Mvc;
using trabfinal.Services;

namespace trabfinal.Controllers;

[ApiController]
[Route("api/agente")]
public class AgenteController : ControllerBase
{
    private readonly AgenteChatService _agente;

    public AgenteController(AgenteChatService agente)
    {
        _agente = agente;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Mensaje))
            return BadRequest(new { error = "El mensaje no puede estar vacío." });

        var respuesta = await _agente.ChatAsync(request.Mensaje, request.NivelRiesgo ?? "Medio");

        return Ok(new { respuesta });
    }
}

public record ChatRequest(string Mensaje, string? NivelRiesgo);