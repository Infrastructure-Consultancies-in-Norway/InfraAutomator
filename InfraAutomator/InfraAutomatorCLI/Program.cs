using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using InfraAutomatorCLI.Extensions;
using InfraAutomator.Interfaces;
using InfraAutomator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InfraAutomatorCLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a basic crash log directory for early failures
            try
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"));
            }
            catch { }

            // Setup dependency injection
            var serviceProvider = ConfigureServices();

            // Get the task runner from DI
            var taskRunner = serviceProvider.GetRequiredService<TaskRunner>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("InfraAutomator CLI started");

                // Run Python diagnostics before any initialization
                var pythonRuntime = serviceProvider.GetRequiredService<PythonRuntime>();
                pythonRuntime.DiagnosePythonEnvironment();

                if (args.Length == 0)
                {
                    Console.WriteLine("Please specify a YAML task file to execute");
                    return;
                }

                string yamlFilePath = args[0];
                if (!File.Exists(yamlFilePath))
                {
                    logger.LogError($"Task file not found: {yamlFilePath}");
                    Console.WriteLine($"Task file not found: {yamlFilePath}");
                    return;
                }

                await taskRunner.ExecuteTaskFromFileAsync(yamlFilePath);

                logger.LogInformation("Task execution completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during execution");
                Console.WriteLine($"Error: {ex.Message}");

                // Save any unhandled exception to a file
                try
                {
                    File.WriteAllText(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "crash-report.log"),
                        $"Time: {DateTime.Now}\nException: {ex.GetType().FullName}\nMessage: {ex.Message}\nStack Trace: {ex.StackTrace}");
                }
                catch { }
            }
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();  // Add Debug logging provider
                builder.AddFile("logs/infraautomator-{Date}.log");
            });

            // Register services
            services.AddSingleton<ITaskParser, YamlTaskParser>();
            services.AddSingleton<TaskRunner>();
            services.AddSingleton<IScriptExecutor, ScriptExecutorFactory>();
            services.AddSingleton<IApplicationRunner, ApplicationRunner>();
            services.AddSingleton<PythonRuntime>();

            return services.BuildServiceProvider();
        }
    }
}
