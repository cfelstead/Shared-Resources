using System.Text.RegularExpressions;

namespace Talk.Core.Rag;

/// <summary>
/// Splits a bundled corpus document into paragraph-sized chunks for embedding.
/// </summary>
public static partial class CorpusChunker
{
    public static IReadOnlyList<UnembeddedChunk> SplitIntoChunks(string documentId, string text)
    {
        // Blank-line boundaries only, matched tolerant of \r\n - git's line-ending
        // normalization (core.autocrlf) can rewrite bundled corpus files to CRLF.
        var paragraphs = BlankLine()
            .Split(text)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0);

        return paragraphs
            .Select((paragraph, index) => new UnembeddedChunk($"{documentId}#{index}", paragraph))
            .ToList();
    }

    [GeneratedRegex(@"(?:\r?\n){2,}")]
    private static partial Regex BlankLine();
}
