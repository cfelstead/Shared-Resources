using Talk.Core.Rag;

namespace Talk.Steps.Tests;

/// <summary>
/// A canned <see cref="ILocalEmbeddingGenerator"/> that always returns the same vector,
/// used to prove a step's retrieval wiring without loading the real ONNX model.
/// </summary>
public sealed class FakeEmbeddingGenerator(float[] embedding) : ILocalEmbeddingGenerator
{
    public float[] GenerateEmbedding(string text) => embedding;
}
