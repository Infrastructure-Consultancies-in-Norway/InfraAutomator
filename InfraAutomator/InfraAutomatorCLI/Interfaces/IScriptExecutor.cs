using System.Collections.Generic;
using System.Threading.Tasks;

namespace InfraAutomatorCLI.Interfaces
{
    public interface IScriptExecutor
    {
        Task<object> ExecuteAsync(string scriptPath, Dictionary<string, string> parameters);
    }
}