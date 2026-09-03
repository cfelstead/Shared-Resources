namespace Talk.Core;

public sealed class AppConfig
{
    public AiProvider AiProvider { get; init; }
    public string AiEndpoint { get; init; } = "";
    public string AiModel { get; init; } = "";
    public string AiApiKey { get; init; } = "";
    public string McpServerCommand { get; init; } = "dotnet";
    public string McpServerArguments { get; init; } = "run --project ../McpSidecarServer";
}
