using System.Collections.Generic;
using System.Threading.Tasks;

namespace InfraAutomator.Interfaces
{
    public interface IApplicationRunner
    {
        Task<bool> RunApplicationAsync(string applicationType, Dictionary<string, string> parameters);
    }
}
