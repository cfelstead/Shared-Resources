using Step03ChatMemory;
using Talk.Core;

ConsoleHelpers.WriteHeader(
    "Step 03 - Adding Chat Memory",
    "Memory is client-managed by replaying conversation history on every turn.");

using var factory = TalkHost.CreateChatClientFactory();
using var chatClient = factory.CreateBaseClient();

await Step03ChatMemoryApp.RunAsync(chatClient, Console.In, Console.Out);
