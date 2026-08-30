#nullable enable
using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core.Services
{
    public sealed class StructuredLogEntry
    {
        public string TimestampUtc { get; set; } = DateTime.UtcNow.ToString("O");
        public string Level { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
    }

    public sealed class StructuredJsonFileLogger : ILogger
    {
        private readonly string _category;
        private readonly string _logFilePath;
        private readonly object _gate = new();

        public StructuredJsonFileLogger(string category, string logFilePath)
        {
            _category = category;
            _logFilePath = logFilePath;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var entry = new StructuredLogEntry
            {
                Level = logLevel.ToString(),
                Category = _category,
                Message = formatter(state, exception),
                Exception = exception?.ToString()
            };

            lock (_gate)
            {
                try
                {
                    string? dir = Path.GetDirectoryName(_logFilePath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    string line = JsonSerializer.Serialize(entry);
                    File.AppendAllText(_logFilePath, line + Environment.NewLine);
                }
                catch
                {
                    // Fail soft
                }
            }
        }
    }
}
