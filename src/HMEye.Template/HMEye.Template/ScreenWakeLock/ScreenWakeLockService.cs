using Microsoft.JSInterop;

namespace HMEye.ScreenWakeLock;

public class ScreenWakeLockService : IAsyncDisposable
{
	private readonly IJSRuntime _jsRuntime;
	private readonly ILogger<ScreenWakeLockService> _logger;
	private IJSObjectReference? _wakeLock;

	/// <summary>
	/// Screen Wake Lock in as per
	/// <see href="https://dev.to/this-is-learning/how-to-prevent-the-screen-turn-off-after-a-while-in-blazor-4b29">Emanuele Bartolesi for This is Learning</see>
	/// But this is a drastically streamlined version using a scoped service and a JSRuntime to trigger page visibility change events,
	/// allowing wake lock re-request when page visibility changes to visible.
	/// </summary>
	public ScreenWakeLockService(IJSRuntime jsRuntime, ILogger<ScreenWakeLockService> logger)
	{
		_jsRuntime = jsRuntime;
		_logger = logger;
	}

	/// <summary>
	/// Checks if the browser supports the screen wake lock API.
	/// </summary>
	public async Task<bool> IsSupportedAsync()
	{
		try
		{
			return await _jsRuntime.InvokeAsync<bool>("eval", "!!navigator.wakeLock?.request");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to check wake lock support.");
			return false;
		}
	}

	/// <summary>
	/// Requests a screen wake lock to keep the device's display on.
	/// </summary>
	public async Task RequestWakeLockAsync()
	{
		try
		{
			_wakeLock = await _jsRuntime.InvokeAsync<IJSObjectReference>("navigator.wakeLock.request", "screen");
			_logger.LogInformation("Wake lock acquired.");
		}
		catch (JSException jsEx)
		{
			_logger.LogWarning(jsEx, "Wake lock request failed (possibly due to visibility).");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error requesting wake lock.");
		}
	}

	/// <summary>
	/// Releases the screen wake lock, allowing the display to turn off.
	/// </summary>
	public async Task ReleaseWakeLockAsync()
	{
		if (_wakeLock is null)
			return;

		try
		{
			await _wakeLock.InvokeVoidAsync("release");
			await _wakeLock.DisposeAsync();
			_logger.LogInformation("Wake lock released.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to release wake lock.");
		}
		finally
		{
			_wakeLock = null;
		}
	}

	public async ValueTask DisposeAsync()
	{
		await ReleaseWakeLockAsync();
	}
}
