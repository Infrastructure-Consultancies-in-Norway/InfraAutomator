using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Python.Runtime;

namespace InfraAutomatorCLI.Services
{
    public class PythonRuntime : IDisposable
    {
        private readonly ILogger<PythonRuntime> _logger;
        private bool _initialized = false;
        private bool _disposedValue;

        public PythonRuntime(ILogger<PythonRuntime> logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            string py_home = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed-amd64");

            // Create a direct log file for capturing errors
            string crashLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "python-crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(crashLogPath));
            
            try
            {
                _logger.LogInformation("Initializing embedded Python runtime");
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: Starting Python initialization\n");
                
                // Check if embedded Python directory exists
                if (!Directory.Exists(py_home))
                {
                    string errorMsg = $"Python embedded directory not found at: {py_home}";
                    _logger.LogError(errorMsg);
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: {errorMsg}\n");
                    throw new DirectoryNotFoundException(errorMsg);
                }
                
                // Log all Python DLLs for diagnostic purposes
                var pythonDlls = Directory.GetFiles(py_home, "python*.dll");
                _logger.LogInformation($"Found {pythonDlls.Length} Python DLLs:");
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: Found {pythonDlls.Length} Python DLLs:\n");
                
                foreach (var dll in pythonDlls)
                {
                    string dllInfo = $"- {Path.GetFileName(dll)}";
                    _logger.LogInformation(dllInfo);
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: {dllInfo}\n");
                }
                
                // Verify the Python DLL exists
                var pythonDll = Path.Combine(py_home, "python312.dll");
                if (!File.Exists(pythonDll))
                {
                    string errorMsg = $"Python DLL not found at: {pythonDll}";
                    _logger.LogError(errorMsg);
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: {errorMsg}\n");
                    throw new FileNotFoundException(errorMsg);
                }
                
                // Log file version information for the DLL
                try
                {
                    var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(pythonDll);
                    string versionInfo = $"Python DLL version: {fileVersionInfo.FileVersion}, Product version: {fileVersionInfo.ProductVersion}";
                    _logger.LogInformation(versionInfo);
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: {versionInfo}\n");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: Failed to get DLL version info: {ex.Message}\n");
                }
                
                var libDir = Path.Combine(py_home, "Lib");
                if (!Directory.Exists(libDir))
                {
                    string warnMsg = $"Python Lib directory not found at: {libDir}";
                    _logger.LogWarning(warnMsg);
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: WARNING - {warnMsg}\n");
                }
                
                // Configure environment before initialization
                Environment.SetEnvironmentVariable("PYTHONHOME", py_home);
                Environment.SetEnvironmentVariable("PYTHONPATH", libDir);
                Environment.SetEnvironmentVariable("PATH", 
                    $"{py_home};{Environment.GetEnvironmentVariable("PATH")}");
                
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: Environment variables set:\n");
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: - PYTHONHOME={py_home}\n");
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: - PYTHONPATH={libDir}\n");
                
                // IMPORTANT CHANGE: Set the Runtime.PythonDLL first and ONLY that
                // Don't access any PythonEngine properties before initializing
                Runtime.PythonDLL = pythonDll;
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: Runtime.PythonDLL = {Runtime.PythonDLL}\n");
                
                // Initialize Python engine first, before setting other properties
                _logger.LogInformation("Attempting to initialize Python engine...");
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: *** CRITICAL POINT - About to call PythonEngine.Initialize() ***\n");

                // Wrap the critical call in try/catch with direct file logging
                try
                {
                    // Initialize first
                    PythonEngine.Initialize();
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: PythonEngine.Initialize() succeeded!\n");
                    
                    // NOW set the Python paths and properties after initialization
                    PythonEngine.PythonHome = py_home;  // This line was causing the issue before
                    PythonEngine.ProgramName = "Infra Automator";
                    
                    // Set Python paths correctly
                    string py_path = py_home;
                    string[] py_paths = {"DLLs", "Lib", "Lib/site-packages"};
                    
                    foreach (string p in py_paths)
                    {
                        string fullPath = Path.Combine(py_home, p.Replace("/", Path.DirectorySeparatorChar.ToString()));
                        if (Directory.Exists(fullPath))
                        {
                            py_path += Path.PathSeparator + fullPath;
                        }
                    }
                    
                    PythonEngine.PythonPath = py_path;
                    
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: PythonEngine properties set after initialization\n");
                }
                catch (Exception ex)
                {
                    // Directly write to file in case logger isn't working
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: CRITICAL ERROR in PythonEngine.Initialize(): {ex.GetType().FullName}\n");
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: Message: {ex.Message}\n");
                    File.AppendAllText(crashLogPath, $"{DateTime.Now}: Stack trace: {ex.StackTrace}\n");
                    
                    if (ex.InnerException != null)
                    {
                        File.AppendAllText(crashLogPath, $"{DateTime.Now}: Inner exception: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}\n");
                        File.AppendAllText(crashLogPath, $"{DateTime.Now}: Inner stack trace: {ex.InnerException.StackTrace}\n");
                    }
                    
                    // Also log through the logger if it's still working
                    _logger.LogError(ex, "Critical error during Python engine initialization");
                    throw;
                }
                
                // Start thread support
                PythonEngine.BeginAllowThreads();
                
                _logger.LogInformation("Python Version: {v}", PythonEngine.Version.Trim());
                _logger.LogInformation("Python Home: {home}", PythonEngine.PythonHome);
                _logger.LogInformation("Python Path: {path}", PythonEngine.PythonPath);
                
                _initialized = true;
                _logger.LogInformation("Python runtime initialization complete");
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: Python runtime initialization completed successfully\n");
            }
            catch (DllNotFoundException ex)
            {
                string errorMsg = $"Failed to load Python DLL: {ex.Message}";
                _logger.LogError(ex, errorMsg);
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: {errorMsg}\n{ex}\n");
                throw;
            }
            catch (BadImageFormatException ex)
            {
                string errorMsg = $"Python DLL architecture mismatch: {ex.Message}";
                _logger.LogError(ex, errorMsg);
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: {errorMsg}\n{ex}\n");
                throw;
            }
            catch (Exception ex)
            {
                string errorMsg = $"Failed to initialize Python runtime: {ex.GetType().Name} - {ex.Message}";
                _logger.LogError(ex, errorMsg);
                File.AppendAllText(crashLogPath, $"{DateTime.Now}: {errorMsg}\n{ex}\n");
                throw;
            }
        }

        public void ExecutePythonScript(string scriptPath, dynamic parameters)
        {
            if (!_initialized)
                Initialize();

            _logger.LogInformation($"Executing Python script: {scriptPath}");
            
            using (Py.GIL()) // Acquire the Python GIL
            {
                try
                {
                    // Create a Python scope
                    using (var scope = Py.CreateScope())
                    {
                        // Set parameters in Python scope
                        foreach (var param in parameters)
                        {
                            scope.Set(param.Key, param.Value);
                        }
                        
                        // Execute the Python file
                        string scriptCode = File.ReadAllText(scriptPath);
                        scope.Exec(scriptCode);
                        
                        // You can also get results from the scope
                        // var result = scope.Get<object>("result_variable");
                    }
                    
                    _logger.LogInformation("Python script executed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing Python script: {scriptPath}");
                    throw;
                }
            }
        }

        public bool TestPythonRuntime()
        {
            try
            {
                Initialize();
                
                using (Py.GIL())
                {
                    // Try to execute a simple Python expression
                    using (var scope = Py.CreateScope())
                    {
                        scope.Exec("import sys; print(f'Python {sys.version} on {sys.platform}')");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Python runtime test failed");
                return false;
            }
        }

        public void DiagnosePythonEnvironment()
        {
            string diagLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "python-diagnostics.log");
            Directory.CreateDirectory(Path.GetDirectoryName(diagLogPath));
            
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== Python Environment Diagnostics at {DateTime.Now} ===");
                sb.AppendLine($"Current Directory: {Environment.CurrentDirectory}");
                sb.AppendLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                
                string py_home = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed-amd64");
                sb.AppendLine($"Python Home: {py_home}");
                sb.AppendLine($"Python Home exists: {Directory.Exists(py_home)}");
                
                if (Directory.Exists(py_home))
                {
                    sb.AppendLine("\nPython Home Contents:");
                    foreach (var file in Directory.GetFiles(py_home))
                    {
                        sb.AppendLine($"- {Path.GetFileName(file)}");
                    }
                    
                    sb.AppendLine("\nPython DLL Files:");
                    foreach (var file in Directory.GetFiles(py_home, "python*.dll"))
                    {
                        var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(file);
                        sb.AppendLine($"- {Path.GetFileName(file)}: {info.FileVersion}, {info.FileMajorPart}.{info.FileMinorPart}.{info.FileBuildPart}");
                    }
                    
                    // Check for critical directories
                    string[] criticalDirs = {"DLLs", "Lib", "Lib/site-packages"};
                    sb.AppendLine("\nCritical Directories:");
                    foreach (string dir in criticalDirs)
                    {
                        string fullPath = Path.Combine(py_home, dir.Replace("/", Path.DirectorySeparatorChar.ToString()));
                        sb.AppendLine($"- {dir}: {Directory.Exists(fullPath)}");
                    }
                }
                
                // Check environment variables
                sb.AppendLine("\nEnvironment Variables:");
                sb.AppendLine($"PATH: {Environment.GetEnvironmentVariable("PATH")}");
                sb.AppendLine($"PYTHONHOME: {Environment.GetEnvironmentVariable("PYTHONHOME")}");
                sb.AppendLine($"PYTHONPATH: {Environment.GetEnvironmentVariable("PYTHONPATH")}");
                
                // Check architecture
                sb.AppendLine($"\nProcess Architecture: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
                sb.AppendLine($"OS Architecture: {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")}");
                
                // Write diagnostics to file
                File.WriteAllText(diagLogPath, sb.ToString());
                _logger.LogInformation($"Python environment diagnostics written to {diagLogPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Python environment diagnostics");
                File.AppendAllText(diagLogPath, $"\nERROR generating diagnostics: {ex}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_initialized)
                    {
                        _logger.LogInformation("Shutting down Python runtime");
                        PythonEngine.Shutdown();
                    }
                }
                
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}