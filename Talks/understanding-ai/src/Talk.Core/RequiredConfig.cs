namespace Talk.Core;

internal static class RequiredConfig
{
    public static string Require(string value, string variableName, AiProvider provider)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{variableName} must be set when AI_PROVIDER={provider}.");
        }

        return value;
    }
}
