using System.Net.Sockets;
using Microsoft.Extensions.AI;
using Talk.Core;

namespace Step02StatelessLlm;

public static class Step02StatelessLlmApp
{
    public static async Task RunAsync(IChatClient chatClient, AppConfig config, TextReader input, TextWriter output)
    {
        while (true)
        {
            output.Write("You: ");
            var line = input.ReadLine();
            if (ConsoleHelpers.IsExit(line))
            {
                break;
            }

            ChatResponse response;
            try
            {
                response = await chatClient.GetResponseAsync(
                    new[] { new ChatMessage(ChatRole.User, line ?? string.Empty) });
            }
            catch (HttpRequestException ex) when (ex.InnerException is SocketException)
            {
                output.WriteLine();
                output.WriteLine($"Unable to reach the {config.AiProvider} endpoint.");
                output.WriteLine($"Configured endpoint: {config.AiEndpoint}");
                output.WriteLine("Fix: confirm the provider is running/reachable and AI_ENDPOINT/AI_MODEL are correct, then retry this step.");
                output.WriteLine();
                break;
            }

            output.WriteLine($"LLM: {response.Text}");
        }
    }
}
