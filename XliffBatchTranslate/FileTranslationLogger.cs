namespace XliffBatchTranslate;

public sealed class FileTranslationLogger : ITranslationLogger
{
    private readonly string _path;
    private readonly object _lock = new();

    public FileTranslationLogger(string path)
    {
        _path = path;
    }

    public void Skipped(string file, string? unitId, string reason, string source)
        => Write("SKIPPED", file, unitId, reason, source);

    public void Failed(string file, string? unitId, string reason, string source)
        => Write("FAILED", file, unitId, reason, source);

    public void Cached(string file, string? unitId, string source)
        => Write("CACHED", file, unitId, "Cache hit", source);

    private void Write(string level, string file, string? unitId, string reason, string source)
    {
        var line =
            $"{DateTime.UtcNow:o} | {level} | {Path.GetFileName(file)} | {unitId ?? "-"} | {reason} | {Truncate(source)}";

        lock (_lock)
        {
            File.AppendAllLines(_path, new[] { line });
        }
    }

    private static string Truncate(string s, int max = 120)
        => string.IsNullOrEmpty(s) ? "" :
            s.Length <= max ? s : s.Substring(0, max) + "…";
}
