namespace XliffBatchTranslate;

public interface ITranslationLogger
{
    void Skipped(string file, string? unitId, string reason, string source);
    void Failed(string file, string? unitId, string reason, string source);
    void Cached(string file, string? unitId, string source);
}