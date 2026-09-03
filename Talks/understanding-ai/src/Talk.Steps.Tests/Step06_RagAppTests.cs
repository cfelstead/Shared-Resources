using Step06_Rag;
using Talk.Core.Rag;

namespace Talk.Steps.Tests;

public class Step06_RagAppTests
{
    [Fact]
    public async Task RunAsync_AsksTwiceAndAugmentsTheSecondCallWithRetrievedContext()
    {
        using var chatClient = new FakeChatClient("some response");
        var embeddingGenerator = new FakeEmbeddingGenerator(new[] { 1f, 0f });
        var chunk = new DocumentChunk("facilities#1", "The staff car park entry code is 4471.", new[] { 1f, 0f });
        var vectorStore = new InMemoryVectorStore(new[] { chunk });
        var input = new StringReader("What is the car park code?\nexit\n");
        var output = new StringWriter();

        await Step06_RagApp.RunAsync(chatClient, embeddingGenerator, vectorStore, input, output);

        Assert.Equal(2, chatClient.Requests.Count);
        Assert.Contains("LLM (no retrieval): some response", output.ToString());
        Assert.Contains("LLM (with retrieval): some response", output.ToString());
        Assert.DoesNotContain("4471", chatClient.Requests[0][0].Text);
        Assert.Contains("4471", chatClient.Requests[1][0].Text);
    }

    [Fact]
    public async Task RunAsync_PrintsTheRetrievedChunkIdsAndScores()
    {
        using var chatClient = new FakeChatClient("some response");
        var embeddingGenerator = new FakeEmbeddingGenerator(new[] { 1f, 0f });
        var chunk = new DocumentChunk("facilities#1", "The staff car park entry code is 4471.", new[] { 1f, 0f });
        var vectorStore = new InMemoryVectorStore(new[] { chunk });
        var input = new StringReader("What is the car park code?\nexit\n");
        var output = new StringWriter();

        await Step06_RagApp.RunAsync(chatClient, embeddingGenerator, vectorStore, input, output);

        Assert.Contains("[facilities#1]", output.ToString());
    }
}
