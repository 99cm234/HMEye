using System.Text.Json;
using HMEye.TwincatServices.Models;
using Microsoft.AspNetCore.Mvc;
using HMEye.Twincat.Cache.PlcCache;

namespace HMEye.Twincat.Endpoints;

public static class PlcDataEndpoints
{
	public static void MapPlcDataEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/api/plc-data").RequireAuthorization("ApiKeyOrAuthenticated");

		group.MapGet(
			"/{type}/{address}",
			(string type, string address, [FromServices] IPlcCache dataCache) =>
			{
				var result = ResolveRead(dataCache, type, address);
				if (result.Error != null)
				{
					return Results.BadRequest(new { Error = result.Error });
				}
				return Results.Ok(new { Value = result.Value });
			}
		);

		group.MapPost(
			"/bulk-read",
			(BulkRequest request, [FromServices] IPlcCache dataCache) =>
			{
				var results = new Dictionary<string, object>();
				foreach (var item in request.Items)
				{
					var result = ResolveRead(dataCache, item.Type, item.Address);
					results[item.Address] =
						result.Error != null ? new { Error = result.Error } : new { Value = result.Value };
				}
				return Results.Ok(results);
			}
		);

		group.MapPost(
			"/{type}/{address}",
			(string type, string address, [FromBody] JsonElement body, [FromServices] IPlcCache dataCache) =>
			{
				var result = ResolveWrite(dataCache, type, address, body);
				if (!result.Success)
				{
					return Results.BadRequest(new { Error = result.Error });
				}
				return Results.Ok(new { Success = true });
			}
		);

		group.MapPost(
			"/bulk-write",
			(BulkRequest request, [FromServices] IPlcCache dataCache) =>
			{
				var results = new Dictionary<string, object>();
				foreach (var item in request.Items)
				{
					if (item.Value.ValueKind == JsonValueKind.Undefined)
					{
						results[item.Address] = new { Error = "Value is missing." };
						continue;
					}

					var result = ResolveWrite(dataCache, item.Type, item.Address, item.Value);
					results[item.Address] =
						result.Error != null ? new { Error = result.Error } : new { Success = true };
				}
				return Results.Ok(results);
			}
		);
	}

	private static (object? Value, string? Error) ResolveRead(IPlcCache dataCache, string type, string address)
	{
		return type.ToLower() switch
		{
			"bool" => ReadValue<bool>(dataCache, address),
			"byte" => ReadValue<byte>(dataCache, address),
			"int" => ReadValue<int>(dataCache, address),
			"uint" => ReadValue<uint>(dataCache, address),
			"short" => ReadValue<short>(dataCache, address),
			"ushort" => ReadValue<ushort>(dataCache, address),
			"long" => ReadValue<long>(dataCache, address),
			"ulong" => ReadValue<ulong>(dataCache, address),
			"float" => ReadValue<float>(dataCache, address),
			"double" => ReadValue<double>(dataCache, address),
			"string" => ReadValue<string>(dataCache, address),
			_ => (null, $"Unsupported type: {type}"),
		};
	}

	private static (object? Value, string? Error) ReadValue<T>(IPlcCache dataCache, string address)
	{
		var result = dataCache.TryReadCached<T>(address);
		if (result.Error)
		{
			return (null, result.ErrorMessage);
		}
		return (result.Value, null);
	}

	private static (bool Success, string? Error) ResolveWrite(
		IPlcCache dataCache,
		string type,
		string address,
		JsonElement body
	)
	{
		return type.ToLower() switch
		{
			"bool" => WriteValue<bool>(dataCache, address, body),
			"byte" => WriteValue<byte>(dataCache, address, body),
			"int" => WriteValue<int>(dataCache, address, body),
			"uint" => WriteValue<uint>(dataCache, address, body),
			"short" => WriteValue<short>(dataCache, address, body),
			"ushort" => WriteValue<ushort>(dataCache, address, body),
			"long" => WriteValue<long>(dataCache, address, body),
			"ulong" => WriteValue<long>(dataCache, address, body), // Generic JSON doesn't distinguish long/ulong easily, allowing fallback
			"float" => WriteValue<float>(dataCache, address, body),
			"double" => WriteValue<double>(dataCache, address, body),
			"string" => WriteValue<string>(dataCache, address, body),
			_ => (false, $"Unsupported type: {type}"),
		};
	}

	private static (bool Success, string? Error) WriteValue<T>(IPlcCache dataCache, string address, JsonElement body)
		where T : notnull
	{
		T value;
		try
		{
			value = JsonSerializer.Deserialize<T>(body.GetRawText())!;
		}
		catch
		{
			return (false, "Invalid value format.");
		}

		if (value == null)
			return (false, "Value cannot be null.");

		var result = dataCache.TryWriteQueue(address, value);
		if (!result.Success)
		{
			return (false, result.ErrorMessage);
		}

		return (true, null);
	}
}
