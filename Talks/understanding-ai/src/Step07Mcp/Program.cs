using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using Step07Mcp;
using Talk.Core;

ConsoleHelpers.WriteHeader(
    "Step 07 - Enter MCP",
    "The LLM can discover and invoke decoupled protocol tools over MCP.");

using var factory = TalkHost.CreateChatClientFactory();
using var chatClient = factory.CreateFunctionInvokingClient();

var config = factory.Config;
var mcpServerArgs = config.McpServerArguments
    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Command = config.McpServerCommand,
    Arguments = mcpServerArgs
});

await using var mcpClient = await McpClient.CreateAsync(transport);
var mcpTools = await mcpClient.ListToolsAsync();

Console.WriteLine($"Connected to MCP server. Tools discovered: {mcpTools.Count}");

await Step07McpApp.RunAsync(chatClient, mcpTools.Cast<AITool>().ToList(), Console.In, Console.Out);
