using System;
using System.Threading.Tasks;
using InfraAutomatorCLI.Interfaces;
using InfraAutomatorCLI.Models;
using Microsoft.Extensions.Logging;

namespace InfraAutomatorCLI.Services
{
    public class TaskRunner
    {
        private readonly ITaskParser _taskParser;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly IApplicationRunner _applicationRunner;
        private readonly ILogger<TaskRunner> _logger;
        
        public TaskRunner(
            ITaskParser taskParser, 
            IScriptExecutor scriptExecutor,
            IApplicationRunner applicationRunner,
            ILogger<TaskRunner> logger)
        {
            _taskParser = taskParser;
            _scriptExecutor = scriptExecutor;
            _applicationRunner = applicationRunner;
            _logger = logger;
        }
        
        public async Task ExecuteTaskFromFileAsync(string filePath)
        {
            var task = await _taskParser.ParseTaskFromFileAsync(filePath);
            
            _logger.LogInformation($"Starting execution of task: {task.Name}");
            Console.WriteLine($"Executing task: {task.Name}");
            
            foreach (var step in task.Steps)
            {
                _logger.LogInformation($"Executing step: {step.Name} ({step.Type})");
                Console.WriteLine($"Step: {step.Name}");
                
                try
                {
                    switch (step.Type.ToLowerInvariant())
                    {
                        case "application":
                            await _applicationRunner.RunApplicationAsync(step.Name, step.Parameters);
                            break;
                            
                        case "pythonscript":
                        case "csharpscript":
                            await _scriptExecutor.ExecuteAsync(step.Parameters["scriptPath"], step.Parameters);
                            break;
                            
                        default:
                            _logger.LogWarning($"Unknown step type: {step.Type}");
                            break;
                    }
                    
                    _logger.LogInformation($"Step {step.Name} completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing step: {step.Name}");
                    throw;
                }
            }
        }
    }
}