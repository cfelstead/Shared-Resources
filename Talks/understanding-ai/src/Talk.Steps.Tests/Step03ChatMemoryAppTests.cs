using Step03ChatMemory;

namespace Talk.Steps.Tests;

public class Step03ChatMemoryAppTests
{
    [Fact]
    public async Task RunAsync_ReplaysGrowingHistoryOnEachTurn()
    {
        using var chatClient = new FakeChatClient("ack");
        var input = new StringReader("first\nsecond\nexit\n");
        var output = new StringWriter();

        await Step03ChatMemoryApp.RunAsync(chatClient, input, output);

        Assert.Equal(2, chatClient.Requests.Count);
        Assert.Single(chatClient.Requests[0]);
        Assert.Equal(3, chatClient.Requests[1].Count);
    }
}
