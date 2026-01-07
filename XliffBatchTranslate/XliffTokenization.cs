using System.Xml.Linq;

namespace XliffBatchTranslate;

/// <summary>
/// Handles mixed-content XLIFF source nodes by replacing non-text nodes with stable tokens
/// before translation, then reinserting the original nodes into the target.
/// </summary>
public static class XliffTokenization
{
    public static (string textWithTokens, List<XNode> tokenNodes) ExtractTextWithTokens(XElement element)
    {
        var tokenNodes = new List<XNode>();
        var parts = new List<string>();

        foreach (var node in element.Nodes())
        {
            if (node is XText xt)
            {
                parts.Add(xt.Value);
            }
            else
            {
                var token = $"__XLF_TAG_{tokenNodes.Count}__";
                tokenNodes.Add(CloneNode(node));
                parts.Add(token);
            }
        }

        return (string.Concat(parts), tokenNodes);
    }

    public static IEnumerable<XNode> RehydrateNodesFromTokens(string translatedText, IReadOnlyList<XNode> tokenNodes)
    {
        if (tokenNodes.Count == 0)
        {
            return new XNode[] { new XText(translatedText) };
        }

        var nodes = new List<XNode>();
        var remaining = translatedText;

        for (int i = 0; i < tokenNodes.Count; i++)
        {
            var token = $"__XLF_TAG_{i}__";
            var idx = remaining.IndexOf(token, StringComparison.Ordinal);
            if (idx < 0)
            {
                // Token was lost/changed by the model; safest fallback is plain text.
                return new XNode[] { new XText(translatedText) };
            }

            var before = remaining.Substring(0, idx);
            if (before.Length > 0)
            {
                nodes.Add(new XText(before));
            }

            nodes.Add(CloneNode(tokenNodes[i]));
            remaining = remaining.Substring(idx + token.Length);
        }

        if (remaining.Length > 0)
        {
            nodes.Add(new XText(remaining));
        }

        return nodes;
    }

    private static XNode CloneNode(XNode node)
    {
        // Round-trip clone via ToString + Parse is simple and reliable for XNode cloning.
        // Avoids reusing node instances across the document.
        return node switch
        {
            XElement xe => XElement.Parse(xe.ToString(SaveOptions.DisableFormatting)),
            XCData cd => new XCData(cd.Value),
            XText xt => new XText(xt.Value),
            XComment xc => new XComment(xc.Value),
            XProcessingInstruction pi => new XProcessingInstruction(pi.Target, pi.Data),
            XDocumentType dt => new XDocumentType(dt.Name, dt.PublicId, dt.SystemId, dt.InternalSubset),
            _ => throw new NotSupportedException($"Cloning node of type {node.GetType()} is not supported.")
        };
    }
}
