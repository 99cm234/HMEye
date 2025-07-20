namespace HMEye.DumbAuth.ConnectionTracking;

public class ConnectionTracker
{
	private int _count = 0;
	private readonly int _max;
	private readonly object _lock = new();

	public int Count => _count;
	public int Max => _max;

	public ConnectionTracker(IConfiguration configuration)
	{
		var dumbAuthConfig = configuration.GetSection("DumbAuth");
		_max = dumbAuthConfig.GetValue<int>("ConnectionLimit:MaxClientConnections", 10);
	}

	public bool TryAdd()
	{
		lock (_lock)
		{
			if (_count >= _max) return false;
			_count++;
			return true;
		}
	}

	public void Remove()
	{
		lock (_lock)
		{
			if (_count > 0) _count--;
		}
	}
}
