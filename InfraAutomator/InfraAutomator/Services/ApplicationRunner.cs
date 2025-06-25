using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using InfraAutomator.Interfaces;
using Microsoft.Extensions.Logging;

namespace InfraAutomator.Services
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
            var teklaConnectorPath = parameters.GetValueOrDefault("teklaConnectorPath", "TeklaConnector.exe");
            
            if (!File.Exists(teklaConnectorPath))
            {
                _logger.LogError($"Tekla connector not found: {teklaConnectorPath}");
                return false;
            }
            
            // Execute the connector with the parameters
            var processStartInfo = new ProcessStartInfo
            {
                FileName = teklaConnectorPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            // Add parameters
            foreach (var param in parameters)
            {
                processStartInfo.ArgumentList.Add($"--{param.Key}={param.Value}");
            }
            
            var process = Process.Start(processStartInfo);
            
            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            
            if (!string.IsNullOrEmpty(stdout))
            {
                _logger.LogInformation($"Tekla connector output: {stdout}");
            }
            
            if (!string.IsNullOrEmpty(stderr))
            {
                _logger.LogError($"Tekla connector error: {stderr}");
            }
            
            return process.ExitCode == 0;
        }
        
        private async Task<bool> RunSolibriAsync(Dictionary<string, string> parameters)
        {
            _logger.LogInformation("Running Solibri application");
            
            // Similar implementation to Tekla, but for Solibri
            // For now, just return success
            return await Task.FromResult(true);
        }
        
        private async Task<bool> RunSimpleBIMAsync(Dictionary<string, string> parameters)
        {
            _logger.LogInformation("Running SimpleBIM application");
            
            // Similar implementation to Tekla, but for SimpleBIM
            // For now, just return success
            return await Task.FromResult(true);
        }
        
        private async Task<bool> RunRhinoAsync(Dictionary<string, string> parameters)
        {
            _logger.LogInformation("Running Rhino application");
            
            // Similar implementation to Tekla, but for Rhino
            // For now, just return success
            return await Task.FromResult(true);
        }
        
        private async Task<bool> RunGenericApplicationAsync(string applicationType, Dictionary<string, string> parameters)
        {
            _logger.LogInformation($"Running generic application: {applicationType}");
            
            if (!parameters.TryGetValue("path", out var appPath))
            {
                _logger.LogError("No application path provided");
                return false;
            }
            
            if (!File.Exists(appPath))
            {
                _logger.LogError($"Application not found: {appPath}");
                return false;
            }

            var redirStdOut = true; // Default to true if not specified
            if (parameters.TryGetValue("redirStdOut", out var redirStdOutStr))
            {
                // parse the redirStdOut to a boolean value
                if (bool.TryParse(redirStdOutStr, out redirStdOut))
                {
                    _logger.LogError($"Invalid value for redirStdOut: {redirStdOutStr}");
                }
            }

            var redirStdErr = true; // Default to true if not specified
            if (parameters.TryGetValue("redirStdErr", out var redirStdErrStr))
            {
                // parse the redirStdOut to a boolean value
                if (bool.TryParse(redirStdErrStr, out redirStdOut))
                {
                    _logger.LogError($"Invalid value for redirStdOut: {redirStdErrStr}");
                }
            }


            var processStartInfo = new ProcessStartInfo
            {
                FileName = appPath,
                RedirectStandardOutput = redirStdOut,
                RedirectStandardError = redirStdErr,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            // Add arguments if provided
            if (parameters.TryGetValue("arguments", out var arguments))
            {
                processStartInfo.Arguments = arguments;
            }
            
            // Add working directory if provided
            if (parameters.TryGetValue("workingDirectory", out var workingDirectory))
            {
                processStartInfo.WorkingDirectory = workingDirectory;
            }
            
            var process = Process.Start(processStartInfo);
            
            //var stdout = await process.StandardOutput.ReadToEndAsync();
            //var stderr = await process.StandardError.ReadToEndAsync();

            if (!parameters.TryGetValue("await", out var awaitProcessStr) || 
                !bool.TryParse(awaitProcessStr, out var awaitProcess))
            {
                awaitProcess = true; // Default to waiting for the process to exit
            }

            if (awaitProcess)
            {
                await process.WaitForExitAsync();
            }
            else
            {
                _logger.LogInformation($"Not waiting for process to complete as await=false");
                return true; // Return success since we're not waiting for the process
            }
            
            if (!string.IsNullOrEmpty(stdout))
            {
                _logger.LogInformation($"Application output: {stdout}");
            }
            
            if (!string.IsNullOrEmpty(stderr))
            {
                _logger.LogError($"Application error: {stderr}");
            }
            
            return process.ExitCode == 0;
        }
    }
}
