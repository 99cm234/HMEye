using System.Collections.Concurrent;
using System.Threading.Tasks;
using HMEye.TwincatServices;

namespace HMEye.Services;

public class MonitoredVariable<T> : IMonitoredVariable
{
    public string Address { get; }
    public T? Value { get; private set; }

    public MonitoredVariable(string address)
    {
        Address = address;
    }

    public async Task PollAsync(IPlcService plc, ConcurrentDictionary<string, object> cache)
    {
        Value = await plc.ReadVariableAsync<T>(Address);
        cache[Address] = Value!;
    }
}