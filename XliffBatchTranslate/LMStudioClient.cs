using System.Text;
using System.Text.Json;

namespace XliffBatchTranslate;

public sealed class LmStudioClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly string _endpoint;
    private readonly string _model;
    private readonly string _systemMessage;

    public LmStudioClient(HttpClient http, string endpoint, string model, string systemMessage)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _systemMessage = systemMessage ?? throw new ArgumentNullException(nameof(systemMessage));
    }

    public async Task<string?> TranslateStrictAsync(string text, string targetLanguage, int maxTokens, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        
        text = (text ?? string.Empty).Trim();
        
        var userPrompt =
            $"Translate the following text from English into {targetLanguage}.\n\n" +
            text + $"\n\n{targetLanguage}:";
        
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrEmpty(_systemMessage))
        {
            messages.Add(new ChatMessage { Role = "system", Content = _systemMessage });
        }

        messages.Add(new ChatMessage { Role = "user", Content = userPrompt });
        var request = new ChatCompletionRequest
        {
            Model = _model,
            Messages = messages,
            Temperature = 0.0,
            MaxTokens = maxTokens
        };
        
        var body = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var resp = await _http.PostAsync(_endpoint, content, ct).ConfigureAwait(false);
        var payload = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        ChatCompletionResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(payload, JsonOptions);
        }
        catch
        {
            return null;
        }

        var choice = parsed?.Choices?.FirstOrDefault();
        if (choice is null)
        {
            return null;
        }

        var msg = choice.Message?.Content;
        if (!string.IsNullOrWhiteSpace(msg))
        {
            return msg.Trim();
        }

        var txt = choice.Text;
        if (!string.IsNullOrWhiteSpace(txt))
        {
            return txt.Trim();
        }

        return null;
    }
}