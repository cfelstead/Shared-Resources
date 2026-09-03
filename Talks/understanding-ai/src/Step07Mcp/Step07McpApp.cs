using Microsoft.Extensions.AI;
using Talk.Core;

namespace Step07Mcp;

public static class Step07McpApp
{
    public static async Task RunAsync(IChatClient chatClient, IReadOnlyList<AITool> tools, TextReader input, TextWriter output)
    {
        while (true)
        {
            output.Write("You: ");
            var line = input.ReadLine();
            if (ConsoleHelpers.IsExit(line))
            {
                break;
            }

            var options = new ChatOptions
            {
                Tools = tools.ToList()
            };

            var response = await chatClient.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, line ?? string.Empty) },
                options);

            output.WriteLine($"LLM: {response.Text}");
        }
    }
}
