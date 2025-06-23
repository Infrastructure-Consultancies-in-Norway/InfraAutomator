using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfraAutomator.Interfaces;

namespace InfraAutomatorCLI.Examples.Scripts
{
    public class CSharpScriptExample : ICSharpScript
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ScriptPath { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public CSharpScriptExample(string name, string description, string scriptPath, Dictionary<string, string> parameters)
        {
            Name = name;
            Description = description;
            ScriptPath = scriptPath;
            Parameters = parameters ?? new Dictionary<string, string>();
        }
        public void Execute()
        {
            // Placeholder for C# script execution logic
            Console.WriteLine($"Executing C# script: {Name}");
            Console.WriteLine($"Description: {Description}");
            Console.WriteLine($"Script Path: {ScriptPath}");
            foreach (var param in Parameters)
            {
                Console.WriteLine($"Parameter: {param.Key} = {param.Value}");
            }
        }
    }
}
