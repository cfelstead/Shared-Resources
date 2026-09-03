using Microsoft.Extensions.AI;
using Talk.Core;

namespace Step05KnowledgeCutoff;

public static class Step05KnowledgeCutoffApp
{
    public static async Task RunAsync(IChatClient chatClient, TextReader input, TextWriter output)
    {
        while (true)
        {
            output.Write("You: ");
            var line = input.ReadLine();
            if (ConsoleHelpers.IsExit(line))
            {
                break;
            }

            var response = await chatClient.GetResponseAsync(
                new[] { new ChatMessage(ChatRole.User, line ?? string.Empty) });

            output.WriteLine($"LLM: {response.Text}");
            output.WriteLine("(Notice: no real-time data tool was available in this step.)");
        }
    }
}
