using Step05KnowledgeCutoff;
using Talk.Core;

ConsoleHelpers.WriteHeader(
    "Step 05 - Ground Truth Failure",
    "Without live tools, an LLM cannot guarantee current date/weather truth.");

using var factory = TalkHost.CreateChatClientFactory();
using var chatClient = factory.CreateBaseClient();

await Step05KnowledgeCutoffApp.RunAsync(chatClient, Console.In, Console.Out);
