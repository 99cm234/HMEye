namespace HMEye.Twincat.Cache.PlcCache;

public class PlcCacheItemConfig
{
	public required string Address { get; init; }
	public required Type Type { get; init; } // For arrays, this is the element type (e.g., typeof(int) for int[])
	public bool IsArray { get; init; }
	public bool IsDynamic { get; init; }
	public int? PollInterval { get; init; }
	public bool IsReadOnly { get; init; }
}
