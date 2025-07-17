using System.Collections.Concurrent;
using System.Threading.Tasks;
using HMEye.TwincatServices;

namespace HMEye.Services;

public interface IMonitoredVariable
{
    string Address { get; }
    Task PollAsync(IPlcService plc, ConcurrentDictionary<string, object> cache);
}