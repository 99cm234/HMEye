# HMEye.TwincatServices

Provides services for communicating with TwinCAT 3 using Twincat.Ads, including support for caching frequently accessed variables and arrays to optimize performance. This package simplifies interaction with TwinCAT 3 by providing a thread-safe service layer and an optional caching mechanism to reduce direct PLC calls.

## To register in Program.cs

Register all services using the `AddTwincatServices` extension method, which reads `TwincatSettings` and `TwincatEventLogCacheSettings` from `appsettings.json` and registers all services as Singleton and Hosted services.

```csharp
// Register TwinCAT services
builder.Services.AddTwincatServices(builder.Configuration);
```

## Configuring TwincatCache

The `TwincatCache` can be configured in 3 different ways.

1. `TwincatCacheConfigProvider`: A manually defined collection of `TwincatCacheItemConfig` objects that are passed to `TwincatCache` on startup.
2. `TwincatCache.AddCacheItem()`: Accepts a `TwincatCacheItemConfig` at any point during run and uses it to add a Cache Item.
3. `TwincatCacheConfigLoader`: Automatically scans PLC for symbols with particular custom attributes to create a collection of `TwincatCacheItemConfig` objects and pass it to `TwincatCache` using the same mechanism as `TwincatCacheConfigProvider`. To use both config methods in one project, resulting `TwincatCacheItemConfig` collections must be combined.

If no configurations are provided in `TwincatCacheConfigProvider`, no variables will be cached by default.

### Configuration using `TwincatCacheConfigProvider.cs`

```csharp
namespace HMEye.TwincatServices.Cache;

public static class TwincatCacheConfigProvider
{
    public static IEnumerable<TwincatCacheItemConfig> GetCacheItemConfigs()
    {
        return new[]
        {
            new TwincatCacheItemConfig
            {
                Address = "MAIN.temperature",
                Type = typeof(float),
                PollInterval = 2000,
                IsReadOnly = true
            },
            new TwincatCacheItemConfig
            {
                Address = "MAIN.counter",
                Type = typeof(short),
                PollInterval = 500
            },
            new TwincatCacheItemConfig
            {
                Address = "MAIN.status",
                Type = typeof(bool),
                PollInterval = 100,
                IsReadOnly = true
            },
            new TwincatCacheItemConfig
            {
                Address = "MAIN.valuesArray",
                Type = typeof(int[]),
                IsArray = true,
                PollInterval = 5000
            },
            new TwincatCacheItemConfig
            {
                Address = "MAIN.stCustomStruct",
                Type = typeof(CustomStruct),    // Structs can be read in type-safe fashion if struct layout is duplicated exactly.
                IsArray = false,                // Note that type mappings do not always match those used when a single value is read.
                IsDynamic - false,              // 
                PollInterval = 5000,            // Arrays of custom structs can be read this way too (Type = typeof(CustomStruct[], IsArray = true,)).
            },
            new TwincatCacheItemConfig
            {
                Address = "MAIN.stDifferentCustomStruct",
                Type = typeof(object),  // Reads the symbol as a dynamic type. Good for when struct layout is not known,
                IsArray = false,        // or when struct is to be converted to JSON for logging or monitoring.
                IsDynamic - true,       //
                PollInterval = 5000,    // Arrays of custom structs can be read this way too.
            },
        };
    }
}

```

### Configuration using `TwincatCache.AddCacheItem()`

`TwincatCache` supports adding and removing cache items at runtime using the `AddCacheItem` and `RemoveCacheItem` methods.

- `AddCacheItem(TwincatCacheItemConfig config)` Adds a new cache item to the cache at any point during run.
- `RemoveCacheItem(string address)` Removes a cache item by address at any point during run.

```csharp
var cache = serviceProvider.GetService<ITwincatCache>();

IEnumerable<TwincatCacheItemConfig> configs = new List<TwincatCacheItemConfig>
{
    new TwincatCacheItemConfig
    {
        Address = "MAIN.Count",
        Type = typeof(int),
        PollInterval = 1000,
        IsReadOnly = true
    },
    new TwincatCacheItemConfig
    {
        Address = "MAIN.StartCommand",
        Type = typeof(bool),
        PollInterval = 200,
        IsReadOnly = false
    }
};

foreach (var config in configs)
{
    cache.AddCacheItem(config);
}

cache.RemoveCacheItem("MAIN.Count");
```

### Configuration using TwincatCacheConfigLoader

- Automatically generates `TwincatCacheItemConfig` objects by scanning PLC for symbols with custom attributes.
- Case insensitive.
- Symbols with `{attribute 'hmeye':='200'}` will be polled at the set `'hmeye'` interval in milliseconds.
- Symbols with `{attribute 'hmeye'}` but no specified polling frequency will be polled every 1000 msec.
- Symbols with `{attrubute 'IsReadOnly':='true'}` will be configured by TwincatCache to disallow writing a new value.
- Polling of "due" items is done every 100 milliseconds, so a symbol with a polling interval of 250 will be polled every 300 milliseconds.


Example of an attribute to be added to cache and polled every 500 msec.
```
{attribute 'hmeye':='500'}
setpoint     : LREAL;
```

Example of a readonly struct that is polled every 200 msec.
```
{attribute 'hmeye':='200'}
{attribute 'readonly':='true'}
stPumpStatus    : ST_Status;
```

## TwinCAT to .NET Data Type Mapping

```
+--------------------+-----------------------------+---------------------------------------------------+
| TwinCAT Data Type  | .NET Equivalent             | Notes                                             |
+--------------------+-----------------------------+---------------------------------------------------+
| BOOL               | bool                        |                                                   |
| BYTE               | byte                        | Unsigned 8-bit                                    |
| SINT               | sbyte                       | Signed 8-bit                                      |
| USINT              | byte                        | Alias of BYTE, explicitly unsigned                |
| WORD               | ushort                      | Unsigned 16-bit                                   |
| INT                | short                       | Signed 16-bit                                     |
| UINT               | ushort                      | Unsigned 16-bit                                   |
| DWORD              | uint                        | Unsigned 32-bit                                   |
| DINT               | int                         | Signed 32-bit                                     |
| UDINT              | uint                        | Unsigned 32-bit                                   |
| LWORD              | ulong                       | Unsigned 64-bit                                   |
| LINT               | long                        | Signed 64-bit                                     |
| ULINT              | ulong                       | Unsigned 64-bit                                   |
| REAL               | float                       | 32-bit floating point (IEEE 754 single-precision) |
| LREAL              | double                      | 64-bit floating point (IEEE 754 double-precision) |
| STRING             | string                      | ASCII (or UTF-8 in newer versions)                |
| WSTRING            | string                      | UTF-16 (wide characters)                          |
| TIME               | TimeSpan                    | Duration (32-bit milliseconds on the wire)        |
| LTIME              | TimeSpan                    | 64-bit high-resolution duration; converted to .NET ticks (100ns resolution) |
| DATE               | DateOnly                    | Date only                                         |
| LDATE              | DateOnly                    | Extended range date only                          |
| TOD (TimeOfDay)    | TimeOnly                    | Time since midnight                               |
| LTOD               | TimeOnly                    | Extended range time of day                        |
| DT (DateAndTime)   | DateTime                    | Full date and time                                |
| LDT                | DateTime                    | Extended range full date and time                 |
| FILETIME           | DateTime                    | Windows FILETIME (100ns intervals since 1601-01-01 UTC) |
+--------------------+-----------------------------+---------------------------------------------------+

```

## Accessing Cached PLC Operations

For PLC symbols that are cached, you can use the `TwincatCache` instance.

```csharp
@using HMEye.TwincatServices.Plc.PlcCache
@inject ITwincatCache DataCache;
@inject ISnackbar Snackbar;

@code{
    private bool _status;
    
    private void ReadStatus()
    {
        var result := DataCache.TryReadCached<bool>("MAIN.Status");
        if (!result.Error)
        {
            _status := result.Value;_
        }
        else
        {
            Snackbar.Add(result.ErrorMessage, Severity.Error);
        }
    }

    // Or if you like simple and don't care abou errors:
    private ReadStatus2
    {
        _status := DataCache.ReadCached<bool>("MAIN.Status");
    }
}
```

## Accessing Non-Cached PLC Operations

For PLC operations that are not frequently accessed and not cached, you can directly use the `ITwincatService` instance.

```csharp
@using HMEye.TwincatServices.Plc.PlcService
@inject ITwincatService PlcService;
@inject ISnackbar Snackbar;

@code{
    private bool _status;
    
    private void ReadStatus()
    {
        try
        {
            var _status = PlcService.ReadAsync<bool>("MAIN.Status");
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}
```
## Appsettings.json Example

```json
{
  "TwincatSettings": {
    "NetId": "199.4.42.250.1.1",
    "PlcPort": 851,
    "SystemPort": 10000,
    "Timeout": 10,
    "ReconnectDelaySeconds": 10
  },
    "TwincatEventCache": {
    "AlarmRefreshIntervalSeconds": 2,
    "EventRefreshIntervalSeconds": 5,
    "MaxCachedEvents": 100
  }
}
```
