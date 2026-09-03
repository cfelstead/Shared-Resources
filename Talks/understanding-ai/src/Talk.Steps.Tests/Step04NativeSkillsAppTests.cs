using Step04NativeSkills;

namespace Talk.Steps.Tests;

public class Step04NativeSkillsAppTests
{
    [Fact]
    public async Task RunAsync_OffersTheConversionToolsAndPrintsTheResponseUntilExit()
    {
        using var chatClient = new FakeChatClient("It's 20C.");
        var input = new StringReader("what's 68F in celsius?\nexit\n");
        var output = new StringWriter();

        await Step04NativeSkillsApp.RunAsync(chatClient, input, output);

        Assert.Contains("LLM: It's 20C.", output.ToString());
        Assert.Equal(2, chatClient.LastOptions?.Tools?.Count);
    }
}
