using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using InfraAutomator.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace InfraAutomator.Services
{
    public class CSharpScriptExecutor : IScriptExecutor
    {
        private readonly ILogger<CSharpScriptExecutor> _logger;

        public CSharpScriptExecutor(ILogger<CSharpScriptExecutor> logger)
        {
            _logger = logger;
        }

        public async Task<object> ExecuteAsync(string scriptPath, Dictionary<string, string> parameters)
        {
            _logger.LogInformation($"Executing C# script: {scriptPath}");
            
            // Read the script content
            string scriptContent = await File.ReadAllTextAsync(scriptPath);
            
            // Compile the script
            Assembly scriptAssembly = CompileScript(scriptContent);
            
            // Find and instantiate the ICSharpScript implementation
            ICSharpScript scriptInstance = InstantiateScript(scriptAssembly, scriptPath, parameters);
            
            // Execute the script
            scriptInstance.Execute();
            
            return "C# script executed successfully";
        }
        
        private Assembly CompileScript(string scriptContent)
        {
            // Add common namespace imports if not present in the script
            if (!scriptContent.Contains("using System;") || !scriptContent.Contains("using System.Collections.Generic;"))
            {
                scriptContent = 
                    @"using System;
                    using System.Collections.Generic;
                    using System.IO;
                    using System.Linq;
                    using System.Threading.Tasks;
                    " + scriptContent;
            }

            // Parse the script into a syntax tree
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(scriptContent);
            
            // Get the assembly references needed for compilation
            var references = GetMetadataReferences();
            
            // Create a compilation
            CSharpCompilation compilation = CSharpCompilation.Create(
                "InMemoryScriptAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            
            // Compile to an in-memory assembly
            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);
            
            if (!result.Success)
            {
                var errors = string.Join(Environment.NewLine, 
                    result.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.ToString()));
                        
                throw new Exception($"Script compilation failed:{Environment.NewLine}{errors}");
            }
            
            ms.Seek(0, SeekOrigin.Begin);
            return Assembly.Load(ms.ToArray());
        }
        
        private ICSharpScript InstantiateScript(Assembly assembly, string scriptPath, Dictionary<string, string> parameters)
        {
            // Find all types in the assembly that implement ICSharpScript
            var scriptTypes = assembly.GetTypes()
                .Where(t => typeof(ICSharpScript).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();
                
            if (!scriptTypes.Any())
            {
                throw new Exception("No ICSharpScript implementation found in the script");
            }
            
            // Get the first implementation (assuming one script per file)
            var scriptType = scriptTypes.First();
            
            // Try to find a suitable constructor
            var fileInfo = new FileInfo(scriptPath);
            var scriptName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            var description = $"Script loaded from {fileInfo.Name}";
            
            try
            {
                // Try constructor with name, description, path, and parameters
                var instance = Activator.CreateInstance(scriptType, scriptName, description, scriptPath, parameters) as ICSharpScript;
                if (instance != null) return instance;
                
                // Try parameterless constructor as fallback
                instance = Activator.CreateInstance(scriptType) as ICSharpScript;
                if (instance != null)
                {
                    // Set properties manually if parameterless constructor is used
                    instance.Name = scriptName;
                    instance.Description = description;
                    instance.ScriptPath = scriptPath;
                    instance.Parameters = parameters ?? new Dictionary<string, string>();
                    return instance;
                }
                
                throw new Exception($"Failed to instantiate {scriptType.Name}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error instantiating script: {ex.Message}", ex);
            }
        }
        
        private List<MetadataReference> GetMetadataReferences()
        {
            var references = new List<MetadataReference>();
            
            // Get the runtime directory
            var systemRefLocation = typeof(object).Assembly.Location;
            var runtimeDir = Path.GetDirectoryName(systemRefLocation);
            
            // Essential .NET Core/.NET 8 references
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));                // System.Private.CoreLib
            references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));            // System.Linq
            references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));               // System.Console
            references.Add(MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location));         // System.Collections
            references.Add(MetadataReference.CreateFromFile(typeof(Task<>).Assembly.Location));                // System.Threading.Tasks
            
            // Add reference to our own assembly for the ICSharpScript interface
            references.Add(MetadataReference.CreateFromFile(typeof(ICSharpScript).Assembly.Location));
            
            // Add all assemblies from the runtime directory
            if (runtimeDir != null)
            {
                // Add critical .NET assemblies
                var runtimeAssemblies = new[]
                {
                    "System.Runtime.dll",
                    "System.Collections.dll",
                    "System.IO.dll",
                    "System.Text.RegularExpressions.dll",
                    "System.Linq.dll",
                    "System.Threading.dll",
                    "System.Private.CoreLib.dll",
                    "netstandard.dll"
                };
                
                foreach (var assembly in runtimeAssemblies)
                {
                    string assemblyPath = Path.Combine(runtimeDir, assembly);
                    if (File.Exists(assemblyPath))
                    {
                        references.Add(MetadataReference.CreateFromFile(assemblyPath));
                    }
                }
            }
            
            // Add reference to the current executing assembly
            references.Add(MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location));
            
            return references;
        }
    }
}