namespace Talk.Core.Rag;

/// <summary>
/// Holds embedded chunks in memory and ranks them against a query embedding by cosine
/// similarity. Recomputed from the bundled corpus at process startup - no persistence.
/// </summary>
public sealed class InMemoryVectorStore(IReadOnlyList<DocumentChunk> chunks)
{
    public IReadOnlyList<ScoredChunk> Search(float[] queryEmbedding, int topK)
    {
        return chunks
            .Select(chunk => new ScoredChunk(chunk, CosineSimilarity(queryEmbedding, chunk.Embedding)))
            .OrderByDescending(scored => scored.Score)
            .Take(topK)
            .ToList();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException(
                $"Embedding dimensions must match: query has {a.Length}, chunk has {b.Length}.");
        }

        float dot = 0f, magnitudeA = 0f, magnitudeB = 0f;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        if (magnitudeA == 0f || magnitudeB == 0f)
        {
            return 0f;
        }

        return dot / (MathF.Sqrt(magnitudeA) * MathF.Sqrt(magnitudeB));
    }
}
