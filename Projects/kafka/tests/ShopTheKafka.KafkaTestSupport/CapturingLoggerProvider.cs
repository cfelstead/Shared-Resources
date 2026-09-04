using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ShopTheKafka.KafkaTestSupport;

/// <summary>Captures formatted log messages at Error level or above, so a test can assert something was logged.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public readonly ConcurrentQueue<string> Messages = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                messages.Enqueue(formatter(state, exception));
            }
        }
    }
}
