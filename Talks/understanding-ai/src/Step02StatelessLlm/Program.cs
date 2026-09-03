using Step02StatelessLlm;
using Talk.Core;

ConsoleHelpers.WriteHeader(
    "Step 02 - The Stateless LLM",
    "Each call is independent. The model only sees the latest message.");

using var factory = TalkHost.CreateChatClientFactory();
using var chatClient = factory.CreateBaseClient();

await Step02StatelessLlmApp.RunAsync(chatClient, factory.Config, Console.In, Console.Out);
