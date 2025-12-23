namespace HMEye.DumbAuth.ConnectionTracking;

using HMEye.DumbAuth.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;

public class ConnectionLimitHandler : CircuitHandler
{
	private readonly IHttpContextAccessor _contextAccessor;
	private readonly ConnectionTracker _tracker;
	private readonly NavigationManager _navigationManager;
	private readonly AuthenticationStateProvider _authStateProvider;
	private readonly UserSessionTracker _sessionTracker;
	private readonly ILogger<ConnectionLimitHandler> _logger;
	private bool _isAllowed;
	private SessionRecord? _sessionRecord;

	public ConnectionLimitHandler(
		ConnectionTracker tracker,
		NavigationManager navigationManager,
		IHttpContextAccessor contextAccessor,
		AuthenticationStateProvider authStateProvider,
		UserSessionTracker sessionTracker,
		ILogger<ConnectionLimitHandler> logger
	)
	{
		_tracker = tracker;
		_navigationManager = navigationManager;
		_contextAccessor = contextAccessor;
		_authStateProvider = authStateProvider;
		_sessionTracker = sessionTracker;
		_logger = logger;
	}

	public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		_isAllowed = _tracker.TryAdd();

		if (_isAllowed)
		{
			await HandleSessionTrackingAsync(circuit, cancellationToken);
		}
		else
		{
			_logger.LogWarning("Connection rejected. Max client limit reached ({Count})", _tracker.Count);
		}

		await base.OnCircuitOpenedAsync(circuit, cancellationToken);
	}

	private async Task HandleSessionTrackingAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		var httpContext = _contextAccessor.HttpContext;
		var authState = await _authStateProvider.GetAuthenticationStateAsync();
		var username = authState.User.Identity is { IsAuthenticated: true }
			? authState.User.Identity.Name ?? ""
			: "";

		if (httpContext is null) return;

		httpContext.Request.Cookies.TryGetValue("sessionId", out var existingSessionId);
		var clientIp = httpContext.Connection?.RemoteIpAddress?.ToString() ?? "";
		var clientHostName = ResolveClientHostName(clientIp);

		_sessionRecord = new SessionRecord
		{
			Username = username,
			SessionId = existingSessionId ?? "",
			ClientHostName = clientHostName,
			ClientIp = clientIp,
		};

		_sessionTracker.Add(_sessionRecord);

		_logger.LogInformation("Circuit {CircuitId} opened for user {Username} from IP {ClientIp}, Host {ClientHost}",
			circuit.Id, username, clientIp, clientHostName);
	}

	private string ResolveClientHostName(string ip)
	{
		if (string.IsNullOrWhiteSpace(ip))
			return "";

		try
		{
			return System.Net.Dns.GetHostEntry(ip)?.HostName ?? "";
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to resolve host name for IP: {ClientIp}", ip);
			return "";
		}
	}

	public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		if (!_isAllowed)
		{
			_logger.LogDebug("Navigating rejected connection to server-full page.");
			_navigationManager.NavigateTo(
				$"/account/server-full?error={_tracker.Count}-clients-connected",
				forceLoad: true
			);
		}
		return base.OnConnectionUpAsync(circuit, cancellationToken);
	}

	public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
	{
		if (_isAllowed)
			_tracker.Remove();

		if (_sessionRecord is not null)
			_sessionTracker.Remove(_sessionRecord);

		_logger.LogInformation("Circuit {CircuitId} closed.", circuit.Id);

		return base.OnCircuitClosedAsync(circuit, cancellationToken);
	}
}
