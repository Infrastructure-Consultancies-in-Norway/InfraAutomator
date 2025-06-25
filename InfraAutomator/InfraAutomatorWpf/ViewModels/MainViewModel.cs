using InfraAutomator.Interfaces;
using InfraAutomator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfUtilities.Utils;

namespace InfraAutomatorWpf.ViewModels
{
    public class MainViewModel : NotifierBase
    {

        private const string _applicationName = "InfraAutomator";
        private string _applicationVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        private string _assemblyVersion;
        public string AssemblyVersion 
        { 
            get => _assemblyVersion; 
            set => SetNotify(ref _assemblyVersion, value); 
        }


        // Create a singleton of ApplicationRunner
        private readonly ILogger<MainViewModel> _logger;
        private readonly IApplicationRunner _applicationRunner;
        private readonly TaskRunner _taskRunner;
        private readonly ITaskParser _taskParser;
        private readonly IScriptExecutor _scriptExecutor;

        #region Properties for YAML Task Automation

        private string _yamlFilePath;
        public string YamlFilePath
        {
            get => _yamlFilePath;
            set => SetNotify(ref _yamlFilePath, value);
        }

        public ICommand BrowseYamlFileCommand => new RelayCommand(_ => BrowseYamlFile());
        public ICommand RunTaskCommand => new AsyncRelayCommand(RunYamlTaskAsync, CanRunTask);

        #endregion

        private string _inputFilePath;
        public string InputFilePath
        {
            get => _inputFilePath;
            set => SetNotify(ref _inputFilePath, value);
        }

        private string _outputFilePath;
        public string OutputFilePath
        {
            get => _outputFilePath;
            set => SetNotify(ref _outputFilePath, value);
        }

        private string _selectedOutputFormat;
        public string SelectedOutputFormat
        {
            get => _selectedOutputFormat;
            set => SetNotify(ref _selectedOutputFormat, value);
        }

        public List<string> OutputFormats { get; } = new List<string>
        {
            //"HTML",
            //"PDF",
            //"DOCX",
            "md"
        };

        public ICommand BrowseInputFileCommand => new RelayCommand(_ => BrowseInputFile());
        public ICommand BrowseOutputFileCommand => new RelayCommand(_ => BrowseOutputFile());
        public ICommand ConvertCommand => new RelayCommand(_ => ConvertFile(), _ => CanConvert());

        public MainViewModel()
        {
#if DEBUG
            InputFilePath = @"C:\Users\fkjn\Downloads\SOS_10LJA_F_KON_TESTING.pdf";
            OutputFilePath = @"C:\Users\fkjn\Downloads";
            YamlFilePath = @"E:\Github\InfraAutomator\InfraAutomator\InfraAutomatorCLI\Examples\sample-task.yaml";
            CanRunTask();
#endif

            // Initialize the assembly version
            // Get the three-part version number from the assembly
            _assemblyVersion = $"{_applicationName} Version: {_applicationVersion}";


            var serviceProvider = ConfigureServices();
            _applicationRunner = serviceProvider.GetRequiredService<IApplicationRunner>();
            _logger = serviceProvider.GetRequiredService<ILogger<MainViewModel>>();
            _taskParser = serviceProvider.GetRequiredService<ITaskParser>();
            //_scriptExecutor = serviceProvider.GetRequiredService<IScriptExecutor>();
            _taskRunner = serviceProvider.GetRequiredService<TaskRunner>();

            // Default values
            SelectedOutputFormat = OutputFormats[0]; // Default to first item
        }

        #region YAML Task Methods

        private void BrowseYamlFile()
        {
            // Use a file dialog to select a YAML file
            //var yamlFilter = "YAML Files (*.yaml;*.yml)|*.yaml;*.yml|All Files (*.*)|*.*";
            string filePath = Common.OpenFileDialogPath("yaml", 
                              Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (!string.IsNullOrEmpty(filePath))
            {
                YamlFilePath = filePath;
            }
        }

        private bool CanRunTask()
        {
            return !string.IsNullOrEmpty(YamlFilePath) && File.Exists(YamlFilePath);
        }

        private async Task RunYamlTaskAsync()
        {
            try
            {
                _logger.LogInformation($"Starting execution of YAML task: {YamlFilePath}");
                await _taskRunner.ExecuteTaskFromFileAsync(YamlFilePath);
                _logger.LogInformation("Task execution completed successfully");
                MessageBox.Show("Task execution completed successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during task execution");
                //Console.WriteLine($"Error: {ex.Message}");
                // You could add UI-specific error handling here, like showing a message box
                MessageBox.Show($"Error: {ex.Message}", "Task Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void BrowseInputFile()
        {
            string filePath = Common.OpenFileDialogPath("*", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (!string.IsNullOrEmpty(filePath))
            {
                InputFilePath = filePath;
            }
        }

        private void BrowseOutputFile()
        {
            string folderPath = Common.OpenFolderDialogPath();
            if (!string.IsNullOrEmpty(folderPath))
            {
                OutputFilePath = folderPath;
            }
        }

        private bool CanConvert()
        {
            return !string.IsNullOrEmpty(InputFilePath) && 
                   !string.IsNullOrEmpty(OutputFilePath) && 
                   !string.IsNullOrEmpty(SelectedOutputFormat);
        }

        private void ConvertFile()
        {
            // Here you would call the markitdown.exe application with the parameters
            // This is a placeholder for the actual implementation
            string markitdownRelativePath = @"python-embed-amd64\Scripts\markitdown.exe";
            var assemblyPath = Directory.GetCurrentDirectory(); //Assembly.GetExecutingAssembly().Location;
            var markitdownPath = Path.Combine(assemblyPath, markitdownRelativePath);

            // Add your code to execute markitdown.exe with parameters
            // You might want to use ApplicationRunner from InfraAutomator.Services
            try
            {
                // Example: ApplicationRunner.Run(markitdownPath, InputFilePath, OutputFilePath, SelectedOutputFormat);
                // Actual conversion logic goes here
                // To make the ApplicationRunner work, we need to convert out logic into steps like we do with the yaml input.


                var outputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(InputFilePath);
                var arguments = new StringBuilder();
                arguments.Append($"{InputFilePath} ");
                arguments.Append($"-o ");
                arguments.Append($"{OutputFilePath}\\{outputFileNameWithoutExtension}.{SelectedOutputFormat}");

                var result = _applicationRunner.RunApplicationAsync(
                    markitdownPath,
                    new Dictionary<string, string>
                    {
                        { "path", markitdownPath },
                        { "arguments", arguments.ToString()},
                        { "redirStdErr", "false" },
                        { "redirStdOut", "false" }
                    }).GetAwaiter().GetResult();

                if (result)
                {
                    _logger.LogInformation($"File converted successfully to {SelectedOutputFormat} format.");
                    MessageBox.Show($"File converted successfully to {SelectedOutputFormat} format.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _logger.LogError("File conversion failed.");
                    MessageBox.Show("File conversion failed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                _logger.LogError(ex, "An error occurred during file conversion");
                MessageBox.Show($"Error: {ex.Message}", "File Conversion Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddLogging(configure =>
            {
                configure.SetMinimumLevel(LogLevel.Debug);
                // You can add other logging providers if needed
            });

            // Register services
            services.AddSingleton<ITaskParser, YamlTaskParser>();
            services.AddSingleton<TaskRunner>();
            services.AddSingleton<IScriptExecutor, ScriptExecutorFactory>();
            services.AddSingleton<IApplicationRunner, ApplicationRunner>();
            services.AddSingleton<PythonRuntime>();
            services.AddSingleton<CSharpScriptExecutor>();

            return services.BuildServiceProvider();
        }
    }
}
