namespace HMEye.DumbAuth.ConnectionTracking;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;

public class ConnectionLimitHandler : CircuitHandler
{
	private readonly ConnectionTracker _tracker;
	private readonly NavigationManager _navigationManager;

	public ConnectionLimitHandler(ConnectionTracker tracker, NavigationManager NavigationManager)
	{
		_tracker = tracker;
		_navigationManager = NavigationManager;
	}

	public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		if (!_tracker.TryAdd())
		{
			_navigationManager.NavigateTo($"/account/server-full?error={_tracker.Count.ToString()}-clients-connected", forceLoad: true);
		}
		return base.OnConnectionUpAsync(circuit, cancellationToken);
	}

	public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		_tracker.Remove();
		return base.OnCircuitClosedAsync(circuit, cancellationToken);
	}
}

