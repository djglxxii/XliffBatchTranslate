namespace XliffBatchTranslate;

public sealed class NullTranslationLogger : ITranslationLogger
{
    public static readonly ITranslationLogger Instance = new NullTranslationLogger();

    public void Skipped(string file, string? unitId, string reason, string source) { }
    public void Failed(string file, string? unitId, string reason, string source) { }
    public void Cached(string file, string? unitId, string source) { }
}