using Microsoft.Extensions.AI;
using Step07Mcp;

namespace Talk.Steps.Tests;

public class Step07McpAppTests
{
    [Fact]
    public async Task RunAsync_PassesTheDiscoveredToolsAndPrintsTheResponseUntilExit()
    {
        using var chatClient = new FakeChatClient("It's sunny.");
        var tools = Array.Empty<AITool>();
        var input = new StringReader("what's the weather?\nexit\n");
        var output = new StringWriter();

        await Step07McpApp.RunAsync(chatClient, tools, input, output);

        Assert.Contains("LLM: It's sunny.", output.ToString());
        Assert.NotNull(chatClient.LastOptions?.Tools);
    }
}
