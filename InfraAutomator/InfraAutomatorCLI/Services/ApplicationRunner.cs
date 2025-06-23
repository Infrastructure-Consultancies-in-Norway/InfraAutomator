using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using InfraAutomatorCLI.Interfaces;
using Microsoft.Extensions.Logging;

namespace InfraAutomatorCLI.Services
{
    public class ApplicationRunner : IApplicationRunner
    {
        private readonly ILogger<ApplicationRunner> _logger;
        
        public ApplicationRunner(ILogger<ApplicationRunner> logger)
        {
            _logger = logger;
        }
        
        public async Task<bool> RunApplicationAsync(string applicationType, Dictionary<string, string> parameters)
        {
            _logger.LogInformation($"Running application: {applicationType}");
            
            switch (applicationType.ToLowerInvariant())
            {
                case "tekla":
                    return await RunTeklaAsync(parameters);
                    
                case "solibri":
                    return await RunSolibriAsync(parameters);
                    
                case "simplebim":
                    return await RunSimpleBIMAsync(parameters);
                    
                case "rhino":
                    return await RunRhinoAsync(parameters);
                    
                default:
                    return await RunGenericApplicationAsync(applicationType, parameters);
            }
        }
        
        private async Task<bool> RunTeklaAsync(Dictionary<string, string> parameters)
        {
            _logger.LogInformation("Running Tekla application");
            
            // Tekla requires .NET Framework 4.8, so we'll need to call into another project
            // This is a placeholder for the actual implementation
            
            // Get the path to the Tekla connector executable
            string teklaConnectorPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "TeklaConnector", 
                "TeklaConnector.exe");
                
            if (!File.Exists(teklaConnectorPath))
            {
                _logger.LogError($"Tekla connector not found at {teklaConnectorPath}");
                throw new FileNotFoundException("Tekla connector executable not found", teklaConnectorPath);
            }
            
            // Build arguments
            string args = string.Join(" ", parameters.Select(p => $"--{p.Key}=\"{p.Value}\""));
            
            var startInfo = new ProcessStartInfo
            {
                FileName = teklaConnectorPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            
            using var process = Process.Start(startInfo);
            
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
            {
                _logger.LogError($"Tekla execution failed: {error}");
                return false;
            }
            
            _logger.LogInformation("Tekla execution completed successfully");
            return true;
        }
        
        private async Task<bool> RunSolibriAsync(Dictionary<string, string> parameters)
        {
            _logger.LogInformation("Running Solibri application");
            
            // Implementation for Solibri
            return await RunGenericApplicationAsync("Solibri", parameters);
        }
        
        private async Task<bool> RunSimpleBIMAsync(Dictionary<string, string> parameters)
        {
            _logger.LogInformation("Running SimpleBIM application");
            
            // Implementation for SimpleBIM
            return await RunGenericApplicationAsync("SimpleBIM", parameters);
        }
        
        private async Task<bool> RunRhinoAsync(Dictionary<string, string> parameters)
        {
            _logger.LogInformation("Running Rhino application");
            
            // Implementation for Rhino
            return await RunGenericApplicationAsync("Rhino", parameters);
        }
        
        private async Task<bool> RunGenericApplicationAsync(string applicationName, Dictionary<string, string> parameters)
        {
            _logger.LogInformation($"Running generic application: {applicationName}");
            
            // Get the executable path from parameters
            if (!parameters.TryGetValue("executablePath", out string executablePath))
            {
                _logger.LogError("No executable path specified");
                throw new ArgumentException("executablePath parameter is required", nameof(parameters));
            }
            
            // Build arguments if any
            string args = "";
            if (parameters.TryGetValue("arguments", out string arguments))
            {
                args = arguments;
            }
            
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = args,
                UseShellExecute = true
            };
            
            using var process = Process.Start(startInfo);
            
            if (process == null)
            {
                _logger.LogError($"Failed to start process: {executablePath}");
                return false;
            }
            
            // Optionally wait for the process to exit if needed
            if (parameters.TryGetValue("waitForExit", out string waitForExit) && 
                bool.TryParse(waitForExit, out bool shouldWait) && 
                shouldWait)
            {
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            
            return true;
        }
    }
}