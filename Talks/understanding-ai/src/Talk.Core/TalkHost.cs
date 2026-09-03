using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Talk.Core;

/// <summary>
/// Wires up config loading, logging, and provider client construction so each
/// step project can go straight to its demo loop.
/// </summary>
public static class TalkHost
{
    public static IChatClientFactory CreateChatClientFactory()
    {
        LoadDotEnvFile();

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var config = new AppConfigLoader().Load(configuration);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        return ChatClientFactoryResolver.Create(config, loggerFactory);
    }

    /// <summary>
    /// Walks up from the working directory looking for a `.env` file and sets
    /// its KEY=VALUE entries as process environment variables, so every step
    /// project picks up one shared config regardless of where `dotnet run` is
    /// invoked from. Real environment variables always win over `.env` values.
    /// </summary>
    private static void LoadDotEnvFile()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                foreach (var line in File.ReadAllLines(candidate))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                    {
                        continue;
                    }

                    var separatorIndex = trimmed.IndexOf('=');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    var key = trimmed[..separatorIndex].Trim();
                    var value = trimmed[(separatorIndex + 1)..].Trim();

                    if (Environment.GetEnvironmentVariable(key) is null)
                    {
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }

                return;
            }

            directory = directory.Parent;
        }
    }
}
