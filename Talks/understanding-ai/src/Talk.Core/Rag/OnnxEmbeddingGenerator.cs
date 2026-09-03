using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace Talk.Core.Rag;

/// <summary>
/// Generates sentence embeddings by running a bundled ONNX sentence-transformer
/// (all-MiniLM-L6-v2) in-process, uniformly regardless of which chat provider is
/// selected. Pools the model's token-level output via attention-masked mean pooling
/// and L2-normalizes the result, matching the model's original sentence-transformers
/// pipeline.
/// </summary>
public sealed class OnnxEmbeddingGenerator : ILocalEmbeddingGenerator, IDisposable
{
    private readonly InferenceSession session;
    private readonly BertTokenizer tokenizer;

    public OnnxEmbeddingGenerator(string modelPath, string vocabPath)
    {
        session = new InferenceSession(modelPath);
        tokenizer = BertTokenizer.Create(vocabPath);
    }

    public float[] GenerateEmbedding(string text)
    {
        var ids = tokenizer.EncodeToIds(text, addSpecialTokens: true, considerPreTokenization: true);
        var inputIds = ids.Select(id => (long)id).ToArray();
        var attentionMask = new long[inputIds.Length];
        Array.Fill(attentionMask, 1L);
        var tokenTypeIds = new long[inputIds.Length];

        var dimensions = new[] { 1, inputIds.Length };
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(inputIds, dimensions)),
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, dimensions)),
            NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(tokenTypeIds, dimensions)),
        };

        using var results = session.Run(inputs);
        var lastHiddenState = results.First(r => r.Name == "last_hidden_state").AsTensor<float>();

        return MeanPoolAndNormalize(lastHiddenState, attentionMask);
    }

    private static float[] MeanPoolAndNormalize(Tensor<float> lastHiddenState, long[] attentionMask)
    {
        var tokenCount = lastHiddenState.Dimensions[1];
        var hiddenSize = lastHiddenState.Dimensions[2];

        var pooled = new float[hiddenSize];
        var validTokenCount = 0f;
        for (var t = 0; t < tokenCount; t++)
        {
            if (attentionMask[t] == 0)
            {
                continue;
            }

            validTokenCount++;
            for (var h = 0; h < hiddenSize; h++)
            {
                pooled[h] += lastHiddenState[0, t, h];
            }
        }

        var magnitude = 0f;
        for (var h = 0; h < hiddenSize; h++)
        {
            pooled[h] /= validTokenCount;
            magnitude += pooled[h] * pooled[h];
        }

        magnitude = MathF.Sqrt(magnitude);
        if (magnitude > 0f)
        {
            for (var h = 0; h < hiddenSize; h++)
            {
                pooled[h] /= magnitude;
            }
        }

        return pooled;
    }

    public void Dispose()
    {
        session.Dispose();
    }
}
