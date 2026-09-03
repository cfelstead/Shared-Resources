using Step05KnowledgeCutoff;

namespace Talk.Steps.Tests;

public class Step05KnowledgeCutoffAppTests
{
    [Fact]
    public async Task RunAsync_PrintsTheResponseAndTheNoticeUntilExit()
    {
        using var chatClient = new FakeChatClient("I can't be sure.");
        var input = new StringReader("what's today's weather?\nexit\n");
        var output = new StringWriter();

        await Step05KnowledgeCutoffApp.RunAsync(chatClient, input, output);

        var text = output.ToString();
        Assert.Contains("LLM: I can't be sure.", text);
        Assert.Contains("no real-time data tool was available", text);
    }
}
