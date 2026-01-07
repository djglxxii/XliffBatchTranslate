using System.Collections.Concurrent;
using System.Xml.Linq;

namespace XliffBatchTranslate;

public sealed class XliffTranslator
{
    private static readonly XNamespace XliffNs = "urn:oasis:names:tc:xliff:document:1.2";

    private readonly LmStudioClient _lm;
    private readonly XliffTranslateOptions _options;

    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);

    public XliffTranslator(LmStudioClient lm, XliffTranslateOptions options)
    {
        _lm = lm ?? throw new ArgumentNullException(nameof(lm));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public int CountTransUnits(string inputPath)
    {
        var doc = XDocument.Load(inputPath, LoadOptions.None);
        return doc.Descendants(XliffNs + "trans-unit").Count();
    }

    public async Task<XliffTranslateStats> TranslateFileAsync(string inputPath, string outputPath, CancellationToken ct = default)
    {
        var doc = XDocument.Load(inputPath, LoadOptions.PreserveWhitespace);

        var fileEl = doc.Descendants(XliffNs + "file").FirstOrDefault();
        if (fileEl is not null)
            fileEl.SetAttributeValue("target-language", _options.TargetLanguage);
        
        var transUnits = doc.Descendants(XliffNs + "trans-unit").ToList();

        var stats = new XliffTranslateStats { TotalTransUnits = transUnits.Count };

        foreach (var tu in transUnits)
        {
            ct.ThrowIfCancellationRequested();

            var sourceEl = tu.Element(XliffNs + "source");
            if (sourceEl is null)
            {
                stats.SkippedCount++;
                continue;
            }

            var targetEl = tu.Element(XliffNs + "target");
            if (targetEl is null)
            {
                targetEl = new XElement(XliffNs + "target");
                sourceEl.AddAfterSelf(targetEl);
            }

            var sourcePlain = sourceEl.Value ?? string.Empty;
            var targetPlain = targetEl.Value ?? string.Empty;

            var targetMissing = string.IsNullOrWhiteSpace(targetPlain);
            var targetSameAsSource = string.Equals(NormalizeWs(targetPlain), NormalizeWs(sourcePlain), StringComparison.Ordinal);

            var shouldTranslate =
                targetMissing ||
                (_options.TranslateIfTargetMissingOrSameAsSource && targetSameAsSource);

            if (!shouldTranslate || string.IsNullOrWhiteSpace(sourcePlain))
            {
                stats.SkippedCount++;
                continue;
            }

            // 1) Tokenize inline XLIFF child nodes -> __XLF_TAG_i__
            var (textWithTagTokens, tagNodes) = XliffTokenization.ExtractTextWithTokens(sourceEl);

            // 2) Protect placeholders like #[Shared.Filters], {0}, etc -> __XLF_PH_i__
            var (textWithAllTokens, phOriginals) = PlaceholderProtection.Protect(textWithTagTokens);

            // 3) Translate with cache + strict prompt
            string translated;
            if (_options.UseCache && _cache.TryGetValue(textWithAllTokens, out var cached))
            {
                translated = cached;
                stats.CacheHitCount++;
            }
            else
            {
                translated = await TranslateStrictWithValidationAsync(
                                source: textWithAllTokens,
                                targetLanguage: _options.TargetLanguage,
                                phTokenCount: phOriginals.Count,
                                tagTokenCount: tagNodes.Count,
                                ct: ct).ConfigureAwait(false)
                             ?? textWithAllTokens;

                if (_options.UseCache)
                    _cache[textWithAllTokens] = translated;
            }

            // 4) Restore placeholders
            translated = PlaceholderProtection.Restore(translated, phOriginals);

            // 5) Write target, rehydrating inline XLIFF nodes
            targetEl.RemoveNodes();
            foreach (var node in XliffTokenization.RehydrateNodesFromTokens(translated, tagNodes))
                targetEl.Add(node);

            stats.TranslatedCount++;
        }

        doc.Save(outputPath, SaveOptions.DisableFormatting);
        return stats;
    }

    private async Task<string?> TranslateStrictWithValidationAsync(string source, string targetLanguage, int phTokenCount, int tagTokenCount, CancellationToken ct)
    {
        // Attempt 1
        var first = await SafeLmCallAsync(source, targetLanguage, maxTokens: _options.MaxTokens, ct).ConfigureAwait(false);
        if (!LooksBad(source, first, phTokenCount, tagTokenCount))
            return first;

        // Attempt 2 (tighter)
        var second = await SafeLmCallAsync(source, targetLanguage, maxTokens: 64, ct).ConfigureAwait(false);
        if (!LooksBad(source, second, phTokenCount, tagTokenCount))
            return second;

        return null;
    }

    private async Task<string?> SafeLmCallAsync(string source, string targetLanguage, int maxTokens, CancellationToken ct)
    {
        try
        {
            return await _lm.TranslateStrictAsync(source, targetLanguage, maxTokens, ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksBad(string source, string? candidate, int phTokenCount, int tagTokenCount)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return true;

        var c = candidate.Trim();

        // Token survival checks
        if (!PlaceholderProtection.AllTokensPresent(c, phTokenCount))
            return true;

        for (int i = 0; i < tagTokenCount; i++)
        {
            if (!c.Contains($"__XLF_TAG_{i}__", StringComparison.Ordinal))
                return true;
        }

        // Prompt leakage markers (keep short and specific)
        string[] badMarkers =
        {
            "Translate ONLY", "Output ONLY", "Preserve tokens", "You translate",
            "point-of-care", "POC", "interfaz de usuario", "Devuelve", "sin comillas"
        };

        foreach (var m in badMarkers)
        {
            if (c.Contains(m, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Explosion on tiny inputs
        if (source.Length <= 20 && c.Length > 80)
            return true;

        return false;
    }

    private static string NormalizeWs(string s) =>
        string.Join(" ", (s ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

public sealed record XliffTranslateStats
{
    public int TotalTransUnits { get; set; }
    public int TranslatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int CacheHitCount { get; set; }
    public int FailureCount { get; set; }
}
