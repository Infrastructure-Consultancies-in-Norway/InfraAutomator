using System.Collections.Generic;
using System.Threading.Tasks;

namespace InfraAutomatorCLI.Interfaces
{
    public interface IApplicationRunner
    {
        Task<bool> RunApplicationAsync(string applicationType, Dictionary<string, string> parameters);
    }
}