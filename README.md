# InfraAutomator

InfraAutomator is a flexible automation tool designed to execute sequences of tasks from different applications, C# scripts, and Python scripts. It provides a unified way to automate infrastructure and application-related operations.

## Table of Contents

- [InfraAutomator Library](#infraautomator-library)
- [InfraAutomator CLI](#infraautomator-cli)
- [Task Configuration](#task-configuration)
- [Creating C# Scripts](#creating-c-scripts)
- [Creating Python Scripts](#creating-python-scripts)

## InfraAutomator Library

The `InfraAutomator.csproj` library provides core functionality for task automation, script execution, and application integration.

### Adding to Your Project

1. Add a reference to the InfraAutomator project:

   ```xml
   <ProjectReference Include="..\InfraAutomator\InfraAutomator.csproj" />
   ```
2. Required dependencies are automatically included:
   - Microsoft.CodeAnalysis (for C# script compilation)
   - Microsoft.Extensions.Logging
   - pythonnet (for Python integration)
   - YamlDotNet (for task file parsing)

3. Register the required services in your dependency injection container:

```csharp
services.AddSingleton<ITaskParser, YamlTaskParser>(); services.AddSingleton<TaskRunner>(); services.AddSingleton<IScriptExecutor, ScriptExecutorFactory>(); services.AddSingleton<IApplicationRunner, ApplicationRunner>(); services.AddSingleton<PythonRuntime>(); services.AddSingleton<CSharpScriptExecutor>();
```

### Using the Library

The main entry point to functionality is through the `TaskRunner` class:
```csharp
var taskRunner = serviceProvider.GetRequiredService<TaskRunner>(); await taskRunner.ExecuteTaskFromFileAsync("path/to/task.yml");
```

## InfraAutomator CLI

The `InfraAutomatorCLI.csproj` provides a command-line interface to execute automation tasks defined in YAML files.

### Usage

```bash
InfraAutomatorCLI.exe <path-to-yaml-task-file>
```
### Building and Running

1. Build the CLI project:
```bash
dotnet build InfraAutomatorCLI.csproj
```

2. Run with a YAML task file:
```bash
dotnet run --project InfraAutomatorCLI.csproj -- path/to/your/task.yml
```

## Task Configuration

Tasks are defined in YAML files with the following structure:
```yaml
name: Example Task description: This is an example automation task
steps:
•	type: CSharpScript name: Execute C# script parameters: scriptPath: path/to/script.cs param1: value1 param2: value2
•	type: PythonScript name: Execute Python script parameters: scriptPath: path/to/script.py param1: value1 param2: value2
•	type: Application name: Run external application parameters: executablePath: path/to/app.exe arguments: --arg1 value1 --arg2 value2
```
Each task consists of:

- `name`: Task name
- `description`: Task description
- `steps`: A list of actions to be executed sequentially

Each step has:

- `type`: Step type (CSharpScript, PythonScript, Application)
- `name`: Step name
- `parameters`: Key-value pairs for step execution

## Creating C# Scripts

InfraAutomator supports C# scripts through the `ICSharpScript` interface.

### Implementing ICSharpScript

1. Create a C# file with a class that implements the `ICSharpScript` interface:
```csharp
using System; 
using System.Collections.Generic; 
using InfraAutomator.Interfaces;

namespace YourNamespace 
{ 
    public class YourScript : ICSharpScript 
    { 
        public string Name { get; set; } 
        public string Description { get; set; } 
        public string ScriptPath { get; set; } 
        public Dictionary<string, string> Parameters { get; set; }
    
        // Constructor with parameters
        public YourScript(string name, string description, string scriptPath, Dictionary<string, string> parameters)
        {
            Name = name;
            Description = description;
            ScriptPath = scriptPath;
            Parameters = parameters ?? new Dictionary<string, string>();
        }
    
        // Main execution method
        public void Execute()
        {
            Console.WriteLine($"Executing script: {Name}");
        
            // Access parameters
            string param1 = Parameters.ContainsKey("param1") ? Parameters["param1"] : null;
        
            // Your script implementation here
            // ...
        }
    }
}
```

### Script Execution

When your C# script is referenced in a YAML task:

1. The script file is loaded and compiled at runtime
2. A class implementing ICSharpScript is instantiated
3. The `Execute()` method is called with the provided parameters

## Creating Python Scripts

InfraAutomator supports Python scripts using the embedded Python runtime.

### Python Script Requirements

1. Create a Python script file (`.py`)
2. Parameters from the task file will be passed as variables to the script

### Example Python script:

Access parameters that were passed from the task file
```python
print(f"Parameter param1 = {param1}") 
print(f"Parameter param2 = {param2}")

*Your Python code here*

def main(): 
    print("Executing Python script") # Perform operations

if name == "main": main()
```


### Python Runtime

The tool includes an embedded Python runtime (Python 3.12) with the necessary environment. No separate Python installation is required.
