using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InfraAutomator.Interfaces;
using Microsoft.Extensions.Logging;

namespace InfraAutomator.Services
{
    public class ScriptExecutorFactory : IScriptExecutor
    {
        private readonly ILogger<ScriptExecutorFactory> _logger;
        private readonly PythonRuntime _pythonRuntime;
        
        public ScriptExecutorFactory(ILogger<ScriptExecutorFactory> logger, PythonRuntime pythonRuntime)
        {
            _logger = logger;
            _pythonRuntime = pythonRuntime;
        }
        
        public async Task<object> ExecuteAsync(string scriptPath, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(scriptPath))
            {
                throw new ArgumentException("Script path cannot be empty", nameof(scriptPath));
            }
            
            if (scriptPath.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecutePythonScriptAsync(scriptPath, parameters);
            }
            else if (scriptPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteCSharpScriptAsync(scriptPath, parameters);
            }
            else
            {
                throw new NotSupportedException($"Unsupported script type: {scriptPath}");
            }
        }
        
        private Task<object> ExecutePythonScriptAsync(string scriptPath, Dictionary<string, string> parameters)
        {
            _logger.LogInformation($"Executing Python script: {scriptPath}");
            
            // Execute using embedded Python runtime
            _pythonRuntime.ExecutePythonScript(scriptPath, parameters);
            
            return Task.FromResult<object>("Python script executed successfully");
        }
        
        private Task<object> ExecuteCSharpScriptAsync(string scriptPath, Dictionary<string, string> parameters)
        {
            _logger.LogInformation($"Executing C# script: {scriptPath}");
            
            // C# script execution is not yet implemented
            _logger.LogWarning("C# script execution is not yet implemented");
            
            return Task.FromResult<object>("C# script execution not implemented");
        }
    }
}
