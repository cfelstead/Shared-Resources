using Talk.Core.Rag;

namespace Talk.Core.Tests;

public class InMemoryVectorStoreTests
{
    [Fact]
    public void Search_ReturnsTheChunkClosestToTheQueryEmbedding()
    {
        var weather = new DocumentChunk("weather", "It rains a lot in Manchester.", new[] { 1f, 0f, 0f });
        var parking = new DocumentChunk("parking", "The staff car park code is 4471.", new[] { 0f, 1f, 0f });
        var store = new InMemoryVectorStore(new[] { weather, parking });

        var results = store.Search(queryEmbedding: new[] { 0f, 1f, 0f }, topK: 1);

        Assert.Single(results);
        Assert.Equal("parking", results[0].Chunk.Id);
    }

    [Fact]
    public void Search_RanksByDescendingCosineSimilarity()
    {
        var exact = new DocumentChunk("exact", "exact match", new[] { 1f, 0f });
        var close = new DocumentChunk("close", "close match", new[] { 0.9f, 0.1f });
        var opposite = new DocumentChunk("opposite", "opposite", new[] { -1f, 0f });
        var store = new InMemoryVectorStore(new[] { opposite, close, exact });

        var results = store.Search(queryEmbedding: new[] { 1f, 0f }, topK: 3);

        Assert.Equal(new[] { "exact", "close", "opposite" }, results.Select(r => r.Chunk.Id));
        Assert.True(results[0].Score > results[1].Score);
        Assert.True(results[1].Score > results[2].Score);
    }

    [Fact]
    public void Search_ClampsTopKToTheAvailableChunkCount()
    {
        var only = new DocumentChunk("only", "the only chunk", new[] { 1f, 0f });
        var store = new InMemoryVectorStore(new[] { only });

        var results = store.Search(queryEmbedding: new[] { 1f, 0f }, topK: 5);

        Assert.Single(results);
    }

    [Fact]
    public void Search_ThrowsWhenQueryAndChunkEmbeddingDimensionsDiffer()
    {
        var chunk = new DocumentChunk("mismatched", "wrong dimension", new[] { 1f, 0f, 0f });
        var store = new InMemoryVectorStore(new[] { chunk });

        Assert.Throws<ArgumentException>(() => store.Search(queryEmbedding: new[] { 1f, 0f }, topK: 1));
    }
}
