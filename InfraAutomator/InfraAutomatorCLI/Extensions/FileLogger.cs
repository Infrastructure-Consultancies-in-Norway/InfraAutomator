using Microsoft.Extensions.Logging;

namespace InfraAutomatorCLI.Extensions
{
    public class FileLogger : ILogger
    {
        private readonly string _filePattern;
        private readonly string _categoryName;

        public FileLogger(string filePattern, string categoryName)
        {
            _filePattern = filePattern;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var logMessage = formatter(state, exception);
            
            var logFile = _filePattern.Replace("{Date}", DateTime.Now.ToString("yyyy-MM-dd"));
            
            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(logFile);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_categoryName}: {logMessage}";
            if (exception != null)
            {
                logEntry += Environment.NewLine + exception.ToString();
            }
            
            System.IO.File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }
    }
}