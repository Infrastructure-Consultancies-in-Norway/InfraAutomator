using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

namespace InfraAutomatorCLI.Extensions
{
    public static class LoggingExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePattern)
        {
            builder.Services.AddSingleton<ILoggerProvider>(new FileLoggerProvider(filePattern));
            return builder;
        }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePattern;

        public FileLoggerProvider(string filePattern)
        {
            _filePattern = filePattern;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_filePattern, categoryName);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}