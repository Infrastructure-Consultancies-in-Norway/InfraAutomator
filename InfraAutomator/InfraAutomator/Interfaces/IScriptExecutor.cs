using System.Collections.Generic;
using System.Threading.Tasks;

namespace InfraAutomator.Interfaces
{
    public interface IScriptExecutor
    {
        Task<object> ExecuteAsync(string scriptPath, Dictionary<string, string> parameters);
    }
}
