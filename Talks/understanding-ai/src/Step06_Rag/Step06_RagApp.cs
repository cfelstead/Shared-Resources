using Microsoft.Extensions.AI;
using Talk.Core;
using Talk.Core.Rag;

namespace Step06_Rag;

public static class Step06_RagApp
{
    private const int TopK = 2;

    public static async Task RunAsync(
        IChatClient chatClient,
        ILocalEmbeddingGenerator embeddingGenerator,
        InMemoryVectorStore vectorStore,
        TextReader input,
        TextWriter output)
    {
        while (true)
        {
            output.Write("You: ");
            var line = input.ReadLine();
            if (ConsoleHelpers.IsExit(line))
            {
                break;
            }

            var question = line ?? string.Empty;

            var withoutRetrieval = await chatClient.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, question) });
            output.WriteLine($"LLM (no retrieval): {withoutRetrieval.Text}");

            var queryEmbedding = embeddingGenerator.GenerateEmbedding(question);
            var retrieved = vectorStore.Search(queryEmbedding, TopK);

            output.WriteLine("Retrieved context:");
            foreach (var scored in retrieved)
            {
                output.WriteLine($"  [{scored.Chunk.Id}] ({scored.Score:F2}) {scored.Chunk.Text}");
            }

            var context = string.Join("\n\n", retrieved.Select(r => r.Chunk.Text));
            var augmentedPrompt =
                $"Answer the question using only the following context. If the context doesn't contain the answer, say so.\n\n" +
                $"Context:\n{context}\n\nQuestion: {question}";

            var withRetrieval = await chatClient.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, augmentedPrompt) });
            output.WriteLine($"LLM (with retrieval): {withRetrieval.Text}");
        }
    }
}
