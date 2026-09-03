using Step04NativeSkills;
using Talk.Core;

ConsoleHelpers.WriteHeader(
    "Step 04 - Native Skills (Unit Conversion)",
    "The model can call deterministic C# functions for reliable math.");

using var factory = TalkHost.CreateChatClientFactory();
using var chatClient = factory.CreateFunctionInvokingClient();

await Step04NativeSkillsApp.RunAsync(chatClient, Console.In, Console.Out);
