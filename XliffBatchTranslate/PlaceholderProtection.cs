using System.Text.RegularExpressions;

namespace XliffBatchTranslate;

public static class PlaceholderProtection
{
    // Preserve placeholders like #[Shared.Filters]
    private static readonly Regex HashBracket = new Regex(@"#\[[^\]]+\]", RegexOptions.Compiled);

    // Additional common placeholder formats (optional but usually worth preserving)
    private static readonly Regex CurlyIndex = new Regex(@"\{\d+\}", RegexOptions.Compiled); // {0}
    private static readonly Regex CurlyName  = new Regex(@"\{[A-Za-z_][A-Za-z0-9_]*\}", RegexOptions.Compiled); // {User}
    private static readonly Regex PercentFmt = new Regex(@"%(\d+\$)?[sdif]", RegexOptions.Compiled); // %s %1$s %d
    private static readonly Regex DollarVar  = new Regex(@"\$\{[^\}]+\}", RegexOptions.Compiled); // ${VAR}

    public static (string textWithTokens, List<string> originals) Protect(string input)
    {
        var originals = new List<string>();
        var s = input ?? string.Empty;

        s = ReplaceAll(s, HashBracket, originals);
        s = ReplaceAll(s, CurlyIndex, originals);
        s = ReplaceAll(s, CurlyName, originals);
        s = ReplaceAll(s, PercentFmt, originals);
        s = ReplaceAll(s, DollarVar, originals);

        return (s, originals);
    }

    public static string Restore(string translated, IReadOnlyList<string> originals)
    {
        var s = translated ?? string.Empty;
        for (int i = 0; i < originals.Count; i++)
        {
            s = s.Replace(Token(i), originals[i], StringComparison.Ordinal);
        }
        return s;
    }

    public static bool AllTokensPresent(string translated, int count)
    {
        var s = translated ?? string.Empty;
        for (int i = 0; i < count; i++)
        {
            if (!s.Contains(Token(i), StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static string ReplaceAll(string s, Regex rx, List<string> originals)
    {
        return rx.Replace(s, m =>
        {
            var token = Token(originals.Count);
            originals.Add(m.Value);
            return token;
        });
    }

    private static string Token(int i) => $"__XLF_PH_{i}__";
}
