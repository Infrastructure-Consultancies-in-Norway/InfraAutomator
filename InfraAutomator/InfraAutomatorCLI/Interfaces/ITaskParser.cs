using System.Threading.Tasks;
using InfraAutomatorCLI.Models;

namespace InfraAutomatorCLI.Interfaces
{
    public interface ITaskParser
    {
        Task<AutomationTask> ParseTaskFromFileAsync(string filePath);
    }
}