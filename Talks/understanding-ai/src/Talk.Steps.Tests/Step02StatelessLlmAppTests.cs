using Step02StatelessLlm;
using Talk.Core;

namespace Talk.Steps.Tests;

public class Step02StatelessLlmAppTests
{
    [Fact]
    public async Task RunAsync_SendsInputAndPrintsTheFakeResponseUntilExit()
    {
        using var chatClient = new FakeChatClient("Hello back!");
        var config = new AppConfig { AiProvider = AiProvider.Ollama, AiEndpoint = "http://example:11434" };
        var input = new StringReader("hi\nexit\n");
        var output = new StringWriter();

        await Step02StatelessLlmApp.RunAsync(chatClient, config, input, output);

        Assert.Contains("LLM: Hello back!", output.ToString());
        Assert.Single(chatClient.Requests);
    }
}
