namespace HMEye.TwincatServices
{
	public class CacheItemArray<T> : ICacheItem
	{
		public string Address { get; }
		public Type Type => typeof(T[]);
		public T[]? Value { get; private set; }
		public DateTime LastUpdated { get; private set; }
		public int? PollInterval { get; }
		public bool IsReadOnly { get; }
		private readonly IPlcService _plcService;

		public CacheItemArray(string address, int? pollInterval, IPlcService plcService, bool isReadOnly = false)
		{
			Address = address ?? throw new ArgumentNullException(nameof(address));
			PollInterval = pollInterval;
			IsReadOnly = isReadOnly;
			_plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
		}

		public async Task GetAsync()
		{
			Value = await _plcService.ReadArrayAsync<T>(Address);
			LastUpdated = DateTime.UtcNow;
		}

		public async Task SetValueAsync(object value)
		{
			if (IsReadOnly)
				throw new InvalidOperationException($"Variable {Address} is read-only");

			if (value is T[] typedValue)
			{
				await _plcService.WriteArrayAsync(Address, typedValue);
				Value = typedValue;
				LastUpdated = DateTime.UtcNow;
			}
			else
			{
				throw new ArgumentException(
					$"Invalid type for {Address}. Expected {typeof(T[]).Name}, got {value?.GetType().Name ?? "null"}"
				);
			}
		}

		public object? GetValue() => Value;

		public bool IsDueForPolling() =>
			PollInterval.HasValue && DateTime.UtcNow >= LastUpdated + TimeSpan.FromMilliseconds(PollInterval.Value);

		public IWriteOperation CreateWriteOperation(object value)
		{
			if (value is T[] typedValue)
				return new PlcWriteArrayOperation<T>(Address, typedValue);
			else
				throw new ArgumentException(
					$"Invalid type for {Address}. Expected {typeof(T[]).Name}, got {value?.GetType().Name ?? "null"}"
				);
		}
	}
}
