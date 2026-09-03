namespace Talk.Core.Rag;

/// <summary>
/// A retrievable unit of the bundled corpus: some source text plus its embedding vector.
/// </summary>
public sealed record DocumentChunk(string Id, string Text, float[] Embedding);
