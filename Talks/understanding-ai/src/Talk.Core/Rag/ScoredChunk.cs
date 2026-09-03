namespace Talk.Core.Rag;

/// <summary>
/// A chunk paired with its cosine-similarity score against a query embedding.
/// </summary>
public sealed record ScoredChunk(DocumentChunk Chunk, float Score);
