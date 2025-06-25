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
using System.Windows.Input;
using WpfUtilities.Utils;

namespace InfraAutomatorWpf.ViewModels
{
    public class MainViewModel : NotifierBase
    {
        // Create a singleton of ApplicationRunner
        private readonly ILogger<MainViewModel> _logger;

        private readonly IApplicationRunner _applicationRunner;

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
#endif


            var serviceProvider = ConfigureServices();
            _applicationRunner = serviceProvider.GetRequiredService<IApplicationRunner>();
            _logger = serviceProvider.GetRequiredService<ILogger<MainViewModel>>();

            // Default values
            SelectedOutputFormat = OutputFormats[0]; // Default to first item
        }

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



                //var process = new Process();
                //process.StartInfo.FileName = markitdownPath;
                //process.StartInfo.Arguments = arguments.ToString();
                //process.StartInfo.UseShellExecute = false;
                //process.StartInfo.RedirectStandardOutput = true;
                //process.StartInfo.RedirectStandardError = true;
                //process.Start();

                //// Read the output synchronously - be careful with large outputs
                //string output = process.StandardOutput.ReadToEnd();
                //string error = process.StandardError.ReadToEnd();
                //process.WaitForExit();

                var result = _applicationRunner.RunApplicationAsync(
                    markitdownPath,
                    new Dictionary<string, string>
                    {
                        { "path", markitdownPath },
                        { "arguments", arguments.ToString()},
                        { "redirStdErr", "false" },
                        { "redirStdOut", "false" }
                    }).GetAwaiter().GetResult();

            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine($"Error during conversion: {ex.Message}");
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
            services.AddSingleton<IApplicationRunner, ApplicationRunner>();

            return services.BuildServiceProvider();
        }

    }
}
