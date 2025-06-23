using System;
using System.IO;
using System.Threading.Tasks;
using InfraAutomator.Interfaces;
using InfraAutomator.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace InfraAutomator.Services
{
    public class YamlTaskParser : ITaskParser
    {
        private readonly ILogger<YamlTaskParser> _logger;
        
        public YamlTaskParser(ILogger<YamlTaskParser> logger)
        {
            _logger = logger;
        }
        
        public async Task<AutomationTask> ParseTaskFromFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"Parsing task file: {filePath}");
                
                string yamlContent = await File.ReadAllTextAsync(filePath);
                
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                
                var task = deserializer.Deserialize<AutomationTask>(yamlContent);
                
                _logger.LogInformation($"Successfully parsed task '{task.Name}' with {task.Steps.Count} steps");
                
                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing YAML task file: {filePath}");
                throw;
            }
        }
    }
}
