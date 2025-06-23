using System.Threading.Tasks;
using InfraAutomator.Models;

namespace InfraAutomator.Interfaces
{
    public interface ITaskParser
    {
        Task<AutomationTask> ParseTaskFromFileAsync(string filePath);
    }
}
