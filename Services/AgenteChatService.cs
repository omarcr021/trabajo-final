using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace trabfinal.Services;

public class AgenteChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chat;

    public AgenteChatService(IConfiguration config)
    {
        var apiKey = config["Groq:ApiKey"]!;
        var modelId = config["Groq:ModelId"]!;

        _kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey,
                httpClient: new HttpClient
                {
                    BaseAddress = new Uri("https://api.groq.com/openai/v1/")
                })
            .Build();

        _chat = _kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<string> ChatAsync(string mensaje, string nivelRiesgo)
    {
        var historial = new ChatHistory();

        historial.AddSystemMessage($"""
            Eres un asistente académico inteligente para estudiantes universitarios.
            El estudiante tiene un nivel de riesgo académico: {nivelRiesgo}.
            Responde siempre en español, de forma amigable y concisa.
            Da consejos prácticos y motivadores sobre estudio, organización y bienestar.
            Máximo 3 oraciones por respuesta.
            """);

        historial.AddUserMessage(mensaje);

        var respuesta = await _chat.GetChatMessageContentAsync(historial);
        return respuesta.Content ?? "No pude generar una respuesta.";
    }
}