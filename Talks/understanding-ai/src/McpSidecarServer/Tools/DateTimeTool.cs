using ModelContextProtocol.Server;

namespace McpSidecarServer.Tools;

public sealed class DateTimeTool
{
    [McpServerTool]
    public string GetCurrentDateTime()
    {
        return DateTimeOffset.Now.ToString("F");
    }
}
