using System.Text.Json.Serialization;

namespace XliffBatchTranslate;

public sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("messages")]
    public required List<ChatMessage> Messages { get; init; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; init; }
}

public sealed class ChatMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

public sealed class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<Choice>? Choices { get; init; }
}

public sealed class Choice
{
    // Standard chat schema: choices[0].message.content
    [JsonPropertyName("message")]
    public ChatMessage? Message { get; init; }

    // Some backends: choices[0].text
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

public sealed record XliffTranslateOptions
{
    public required string TargetLanguage { get; init; }

    /// <summary>
    /// If true: translate when target is missing/empty OR equals source.
    /// If false: translate only when target missing/empty.
    /// </summary>
    public bool TranslateIfTargetMissingOrSameAsSource { get; init; } = true;

    public double Temperature { get; init; } = 0.2;
    public int MaxTokens { get; init; } = 512;

    public bool UseCache { get; init; } = true;
    public string TargetLanguageCode { get; set; }
}
