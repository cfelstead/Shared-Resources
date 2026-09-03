using Microsoft.Extensions.AI;
using Talk.Core;

namespace Step03ChatMemory;

public static class Step03ChatMemoryApp
{
    public static async Task RunAsync(IChatClient chatClient, TextReader input, TextWriter output)
    {
        var history = new List<ChatMessage>();

        while (true)
        {
            output.Write("You: ");
            var line = input.ReadLine();
            if (ConsoleHelpers.IsExit(line))
            {
                break;
            }

            history.Add(new ChatMessage(ChatRole.User, line ?? string.Empty));
            var response = await chatClient.GetResponseAsync(history);

            var assistantMessage = response.Messages.LastOrDefault() ?? new ChatMessage(ChatRole.Assistant, response.Text ?? string.Empty);
            history.Add(assistantMessage);

            output.WriteLine($"LLM: {response.Text}");
        }
    }
}
