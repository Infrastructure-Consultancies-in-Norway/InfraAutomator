using System;
using System.Collections.Generic;

namespace InfraAutomatorCLI.Models
{
    public class AutomationTask
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TaskStep> Steps { get; set; } = new List<TaskStep>();
    }

    public class TaskStep
    {
        public string Type { get; set; } // Application, PythonScript, CSharpScript
        public string Name { get; set; }
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
    }
}