using Talk.Core.Rag;

namespace Talk.Core.Tests;

public class CorpusChunkerTests
{
    [Fact]
    public void SplitIntoChunks_SplitsOnBlankLinesAndTrimsWhitespace()
    {
        var text = "First paragraph.\n\n  Second paragraph.  \n\n\nThird paragraph.";

        var chunks = CorpusChunker.SplitIntoChunks("doc", text);

        Assert.Equal(
            new[] { "First paragraph.", "Second paragraph.", "Third paragraph." },
            chunks.Select(c => c.Text));
    }

    [Fact]
    public void SplitIntoChunks_IdsEachChunkWithTheDocumentIdAndItsIndex()
    {
        var chunks = CorpusChunker.SplitIntoChunks("facilities", "One.\n\nTwo.");

        Assert.Equal(new[] { "facilities#0", "facilities#1" }, chunks.Select(c => c.Id));
    }

    [Fact]
    public void SplitIntoChunks_SkipsBlankParagraphs()
    {
        var chunks = CorpusChunker.SplitIntoChunks("doc", "One.\n\n   \n\nTwo.");

        Assert.Equal(2, chunks.Count);
    }

    [Fact]
    public void SplitIntoChunks_SplitsOnCrlfBlankLines()
    {
        var chunks = CorpusChunker.SplitIntoChunks("doc", "First paragraph.\r\n\r\nSecond paragraph.");

        Assert.Equal(new[] { "First paragraph.", "Second paragraph." }, chunks.Select(c => c.Text));
    }
}
