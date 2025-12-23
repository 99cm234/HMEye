using HMEye.Twincat.Contracts.Models;
using HMEye.Twincat.Contracts.TypeMaps;
using Microsoft.Extensions.Options;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace HMEye.Twincat.Cache.PlcCache;

public class PlcCacheConfigLoader : IDisposable
{
	private readonly ILogger<PlcCacheConfigLoader> _logger;
	private readonly TimeSpan _defaultTimeout;
	private readonly string? _netId;
	private readonly int _port;
	private ISymbolCollection<ISymbol>? _symbols;
	private readonly AdsClient _adsClient = new();
	private IAdsSymbolLoader? _symbolLoader;
	private bool _disposed;

	public PlcCacheConfigLoader(ILogger<PlcCacheConfigLoader> logger, IOptions<TwincatSettings> options)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		var settings = options.Value ?? throw new InvalidOperationException("TwincatSettings is not configured.");
		_netId = settings.NetId;
		_port = settings.PlcPort;
		_defaultTimeout = TimeSpan.FromSeconds(settings.Timeout);
		EnsureConnected();
	}

	/// <summary>
	/// Creates a linked cancellation token source that combines the external token with a timeout token.
	/// </summary>
	/// <param name="externalToken">The external cancellation token provided by the caller.</param>
	/// <returns>A tuple containing the linked cancellation token source and the timeout cancellation token source.</returns>
	private (CancellationTokenSource linkedCts, CancellationTokenSource timeoutCts) CreateLinkedCancellationTokenSource(
		CancellationToken externalToken
	)
	{
		var timeoutCts = new CancellationTokenSource(_defaultTimeout);
		var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, timeoutCts.Token);
		return (linkedCts, timeoutCts);
	}

	/// <summary>
	/// Ensures that the PLC connection is established.
	/// </summary>
	/// <returns>True if connected or reconnection was successful; otherwise, throws an error.</returns>
	private bool EnsureConnected()
	{
		if (!_adsClient.IsConnected && _netId != null)
		{
			try
			{
				_adsClient.Connect(_netId, _port);
				_logger.LogInformation("PlcDataCacheConfigLoader successfully connected TwinCAT PLC AdsClient.");
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "PlcDataCacheConfigLoader failed to connect to TwinCAT PLC AdsClient.");
				throw;
			}
		}
		return true;
	}

	/// <summary>
	/// Ensures that the symbol loader is initialized for accessing PLC symbols.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous initialization of the symbol loader.</returns>
	/// <exception cref="Exception">Thrown when symbol initialization fails.</exception>
	private async Task EnsureSymbolLoaderInitializedAsync(CancellationToken cancellationToken = default)
	{
		if (_symbolLoader != null)
			return;

		var settings = new SymbolLoaderSettings(SymbolsLoadMode.DynamicTree);
		_symbolLoader = (IAdsSymbolLoader)SymbolLoaderFactory.Create(_adsClient, settings);
		_symbols =
			(await _symbolLoader.GetSymbolsAsync(cancellationToken)).Symbols
			?? throw new Exception("PlcDataCacheConfigLoader failed to initialize symbols.");
	}

	/// <summary>
	/// Generates a collection of cache configurations by scanning all top-level PLC symbols for variables with the HMEye attribute.
	/// </summary>
	/// <remarks>
	/// This method connects to the PLC via an AdsClient, loads all top-level symbols using a dynamic tree, and recursively scans each for variables with the HMEye attribute, which specifies the polling interval in milliseconds. The IsReadOnly attribute, if present and set to true, marks the symbol as read-only. Attribute names are case-insensitive (e.g., HMEye, hmeye, HMEYE are equivalent). Attributes are defined as separate pragmas above the variable declaration (e.g., {attribute 'HMEye' := '500'}). If multiple attributes with the same name (ignoring case) are found, the first valid value is used, and a warning is logged.
	/// <code>
	///	// Example PLC symbols:
	///		{attribute 'HMEye' := '500'}
	///		{attribute 'IsReadOnly' := 'true'}
	///		MAIN.MyVariable : INT
	///
	///		{attribute 'hmeye' := '1000'}
	///		GVL_Sensors.SensorValue : REAL
	///
	///		{attribute 'HMEYE' := '200'}
	///		PRG_Control.State : BOOL
	/// </code>
	/// </remarks>
	/// <param name="cancellationToken">A token that can be used to cancel the operation. If cancellation is requested, the method will throw an exception.</param>
	/// <returns>A task that resolves to an <see cref="IEnumerable{T}"/> of <see cref="PlcCacheItemConfig"/> objects representing the cacheable symbols.</returns>
	/// <exception cref="Exception">Thrown if symbol initialization fails or symbols are not loaded.</exception>
	public async Task<IEnumerable<PlcCacheItemConfig>> CreateCacheItemConfigs(
		CancellationToken cancellationToken = default
	)
	{
		var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
		using (linkedCts)
		using (timeoutCts)
		{
			await EnsureSymbolLoaderInitializedAsync(linkedCts.Token);
			if (_symbols == null)
				throw new Exception("Symbols not initialized.");

			var configs = new List<PlcCacheItemConfig>();
			_logger.LogInformation("Scanning {SymbolCount} top-level symbols for HMEye attributes.", _symbols.Count);
			foreach (var symbol in _symbols)
			{
				cancellationToken.ThrowIfCancellationRequested();
				configs.AddRange(ScanSymbolRecursive(symbol, cancellationToken));
			}

			_logger.LogInformation("Found {ConfigCount} cacheable symbols with HMEye attributes.", configs.Count);
			return configs;
		}
	}

	/// <summary>
	/// Recursively scans a symbol and its sub-symbols to collect cache configurations for variables with the HMEye attribute, including sub-symbols of a tagged parent.
	/// </summary>
	/// <remarks>
	/// Attribute names are case-insensitive (e.g., HMEye, hmeye, HMEYE are equivalent). If multiple attributes with the same name (ignoring case) are found, the first valid value is used, and a warning is logged. Invalid attribute values (e.g., non-integer HMEye) cause the symbol to be skipped or use default values, with warnings logged.
	/// </remarks>
	private IEnumerable<PlcCacheItemConfig> ScanSymbolRecursive(ISymbol symbol, CancellationToken cancellationToken)
	{
		var results = new List<PlcCacheItemConfig>();
		cancellationToken.ThrowIfCancellationRequested();

		foreach (var child in symbol.SubSymbols)
		{
			cancellationToken.ThrowIfCancellationRequested();

			string address = child.InstancePath;
			IDataType? type = ResolveAliasSafe(child.DataType);
			Type clrType = MapType(child);
			int pollingRate = 1000;
			bool isReadOnly = false;
			bool isArray = type?.Category == DataTypeCategory.Array;
			bool isDynamic = clrType == typeof(object);

			// Check for HMEye attribute (case-insensitive)
			if (
				TryGetAttributesCaseInsensitive(child.Attributes, "HMEye", out var hmEyeAttrs)
				&& hmEyeAttrs?.Length > 0
			)
			{
				var hmEyeAttr = hmEyeAttrs[0];
				if (hmEyeAttrs.Length > 1)
				{
					_logger.LogWarning(
						"Multiple PollingRate attributes (case-insensitive) found for symbol {Address}. Using first value: {Value}.",
						address,
						hmEyeAttr.Value
					);
				}

				if (int.TryParse(hmEyeAttr.Value, out var pr))
				{
					pollingRate = pr;
				}
				else
				{
					_logger.LogWarning(
						"Invalid PollingRate attribute value for symbol {Address}: {Value}. Using default {Default}.",
						address,
						hmEyeAttr.Value,
						pollingRate
					);
				}

				// Check for IsReadOnly attribute (case-insensitive)
				if (
					TryGetAttributesCaseInsensitive(child.Attributes, "IsReadOnly", out var roAttrs)
					&& roAttrs?.Length > 0
				)
				{
					var roAttr = roAttrs[0];
					if (roAttrs.Length > 1)
					{
						_logger.LogWarning(
							"Multiple IsReadOnly attributes found for symbol {Address}. Using first value: {Value}.",
							address,
							roAttr.Value
						);
					}

					if (bool.TryParse(roAttr.Value, out var ro))
					{
						isReadOnly = ro;
					}
					else
					{
						_logger.LogWarning(
							"Invalid IsReadOnly attribute value for symbol {Address}: {Value}. Using default {Default}.",
							address,
							roAttr.Value,
							isReadOnly
						);
					}
				}

				results.Add(
					new PlcCacheItemConfig
					{
						Address = address,
						Type = clrType, 
						IsArray = isArray,
						IsDynamic = isDynamic,
						PollInterval = pollingRate,
						IsReadOnly = isReadOnly,
					}
				);
			}
			// Only recurse into subsymbols of symbols without 'HMEye' attribute:
			// Twincat DynamicTree symbol behavior marks all subsymbols with the attribute of the parent,
			// so recursion into a symbol marked with 'HMEye' attribute would leave ALL subsymbols cached.
			// Which would kind of suck.
			else if (child.SubSymbols.Count > 0)
			{
				results.AddRange(ScanSymbolRecursive(child, cancellationToken));
			}
		}
		return results;
	}

	/// <summary>
	/// Retrieves attributes from the collection case-insensitively, returning all matches.
	/// </summary>
	/// <param name="attributes">The attribute collection to search.</param>
	/// <param name="name">The attribute name to find (case-insensitive).</param>
	/// <param name="matchingAttributes">The array of matching attributes, or null if none found.</param>
	/// <returns>True if at least one attribute is found; otherwise, false.</returns>
	private bool TryGetAttributesCaseInsensitive(
		ITypeAttributeCollection attributes,
		string name,
		out ITypeAttribute[]? matchingAttributes
	)
	{
		var matches = attributes
			.Where(attr => string.Equals(attr.Name, name, StringComparison.OrdinalIgnoreCase))
			.Cast<ITypeAttribute>()
			.ToArray();
		matchingAttributes = matches.Length > 0 ? matches : null;
		return matches.Length > 0;
	}

	private static IDataType? ResolveAliasSafe(IDataType? type)
	{
		while (type is IAliasType alias && alias.BaseType != null)
		{
			type = alias.BaseType;
		}
		return type;
	}

	public static Type MapType(ISymbol child)
	{
		var type = ResolveAliasSafe(child.DataType);
		if (type is null)
			return typeof(object);

		// Try resolve by name directly
		if (TryResolveByName(type.Name, out var resolved))
			return resolved ?? typeof(object);

		// Handle special categories
		return type.Category switch
		{
			DataTypeCategory.Array when child is IArrayInstance array =>
				(ResolveElementType(array.ElementType) ?? typeof(object)).MakeArrayType(),

			DataTypeCategory.Struct when child is IStructInstance structInstance =>
				ResolveByName(structInstance.DataType) ?? typeof(object),

			DataTypeCategory.Enum when child is IEnumType enumType =>
				ResolveByName(enumType.BaseType) ?? typeof(object),

			_ => typeof(object)
		};
	}

	private static bool TryResolveByName(string? name, out Type? type)
	{
		if (name is not null)
		{
			if (TypeMaps.Map.TryGetValue(name, out type))
				return true;

			if (CustomTypeMaps.Map.TryGetValue(name, out type))
				return true;
		}
		type = null;
		return false;
	}

	private static Type? ResolveByName(IDataType? type) =>
		TryResolveByName(ResolveAliasSafe(type)?.Name, out var t) ? t : null;

	private static Type? ResolveElementType(IDataType? elementType) =>
		TryResolveByName(ResolveAliasSafe(elementType)?.Name, out var t) ? t : null;

	public void Dispose()
	{
		if (_disposed)
			return;

		_adsClient.Dispose();

		_disposed = true;
	}
}
