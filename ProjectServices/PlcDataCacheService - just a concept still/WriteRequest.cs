using System.Threading;
using System.Threading.Tasks;
using HMEye.TwincatServices;

namespace HMEye.Services;

public interface IWriteRequest
{
    string Address { get; }
    Task ExecuteAsync(IPlcService plc, CancellationToken ct);
}

public class WriteRequest<T> : IWriteRequest
{
    public string Address { get; }
    public T Value { get; }

    public WriteRequest(string address, T value)
    {
        Address = address;
        Value = value;
    }

    public async Task ExecuteAsync(IPlcService plc, CancellationToken ct)
    {
        await plc.WriteVariableAsync(Address, Value, ct);
    }
}