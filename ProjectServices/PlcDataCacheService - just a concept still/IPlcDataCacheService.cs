using System;
using System.Threading;
using System.Threading.Tasks;

namespace HMEye.Services;

public interface IPlcDataCacheService : IDisposable
{
    Task<ReadResult<T>> GetValueAsync<T>(string address, CancellationToken ct = default);
    Task<WriteResult> SetValueAsync<T>(string address, T value, bool immediate = false, CancellationToken ct = default);
    bool Error { get; }
    string ErrorMessage { get; }
    DateTime LastErrorTime { get; }
}