namespace Talk.Core.Rag;

/// <summary>
/// Produces embedding vectors for text. Used uniformly for retrieval regardless of
/// which <c>AI_PROVIDER</c> is selected for chat.
/// </summary>
public interface ILocalEmbeddingGenerator
{
    float[] GenerateEmbedding(string text);
}
