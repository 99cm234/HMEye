# HMEye.TwincatServices

Provides services for communicating with TwinCAT 3 using Twincat.Ads, including support for caching frequently accessed variables and arrays to optimize performance. This package simplifies interaction with TwinCAT 3 by providing a thread-safe service layer and an optional caching mechanism to reduce direct PLC calls.

## To register in Program.cs

Register all services using the `AddTwincatServices` extension method, which configures `TwincatSettings` and `PlcEventCacheSettings` from `appsettings.json` and registers all services as Singleton and Hosted services.

```csharp
// Register TwinCAT services
builder.Services.AddTwincatServices(builder.Configuration);
```

## Configuring PlcDataCache

The `PlcDataCache` can be configured in 3 different ways.

1. `PlcDataCacheConfigProvider`: A manually defined collection of `CacheItemConfig` objects that are passed to `PlcDataCache` on startup.
2. `PlcDataCache.AddConfigItem()`: Accepts a `CacheItemConfig` at any point during run and uses it to add a Cache Item.
3. `PlcDataCacheConfigLoader`: Automatically scans PLC for symbols with particular custom attributes to create a collection of `CacheItemConfig` objects and pass it to `PlcDataCache` using the same mechamism as `PlcDataCacheConfigProvider`. To use both config methods in one project, resulting `CacheItemConfig` collections must be combined.

If no configurations are provided in `PlcDataCacheConfigProvider`, no variables will be cached by default.

### Configuration using `PlcDataCacheConfigProvider.cs`

```csharp
namespace HMEye.TwincatServices
{
    public static class PlcDataCacheConfigProvider
    {
        public static IEnumerable<CacheItemConfig> GetCacheItemConfigs()
        {
            return new[]
            {
                new CacheItemConfig
                {
                    Address = "MAIN.temperature",
                    Type = typeof(float),
                    PollInterval = 2000,
                    IsReadOnly = true
                },
                new CacheItemConfig
                {
                    Address = "MAIN.counter",
                    Type = typeof(short),
                    PollInterval = 500
                },
                new CacheItemConfig
                {
                    Address = "MAIN.status",
                    Type = typeof(bool),
                    PollInterval = 100,
                    IsReadOnly = true
                },
                new CacheItemConfig
                {
                    Address = "MAIN.valuesArray",
                    Type = typeof(int),  // Element type for int[]. Array lengths must match.
                    IsArray = true,
                    PollInterval = 5000
                }
            };
        }
    }
}
```

When accessing the array, use the array type in the generic methods, e.g., `ReadImmediateAsync<int[]>("MAIN.valuesArray")`.

### Configuration using `PlcDataCache.AddConfigItem()`

`PlcDataCache` supports adding and removing cache items at runtime using the `AddCacheItem` and `RemoveCacheItem` methods.

- `AddCacheItem(CacheItemConfig config)` Adds a new cache item to the cache at any point during run. Can be used to cache custom structs without the use of dynamic typing that is required if structs are cached automatically using `PlcDataCacheConfigLoader`.

- `RemoveCacheItem(string address)` Removes a cache item by address at any point during run.

```csharp
var cache = serviceProvider.GetService<IPlcDataCache>();

[StructLayout(LayoutKind.Sequential)]
public struct MyPlcStruct
{
    public float Temperature; // Maps to REAL
    public int Pressure;      // Maps to DINT
}

IEnumerable<CacheItemConfig> configs = new List<CacheItemConfig>
{
    new CacheItemConfig
    {
        Address = "MAIN.plcStruct",
        Type = typeof(MyPlcStruct),
        PollInterval = 1000,
        IsReadOnly = false
    },
    new CacheItemConfig
    {
        Address = "MAIN.anotherVariable",
        Type = typeof(double),
        PollInterval = 500,
        IsReadOnly = true
    }
};

foreach (var config in configs)
{
    cache.AddCacheItem(config);
}

cache.RemoveCacheItem("MAIN.anotherVariable");
```

### Configuration using PlcDataCacheConfigLoader

- Automatically generates `CacheItemConfig` objects by scanning PLC for symbols with custom attributes.
- Case insensitive.
- Polling of "due" items is done every 100 milliseconds, so for example an item with a polling interval of 250 will be polled every 300 milliseconds.
- Symbols with `{attribute 'hmeye'}` but no specified polling frequency will be polled every 1000 msec.

Example of an attribute to be added to cache and polled every 500 msec. `readonly` disallows writing a new value.
```
{attribute 'hmeye':='500'}
{attribute 'readonly':='true'}
temperature     : LREAL;
```

Example of a functon block that has all it's symbols added to cache and polled every 200 msec.
```
{attribute 'hmeye':='200'}
fbPump     : FB_PumpStation
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

**Note**: Arrays of the above types are supported. Set `IsArray` to `true` and specify the element type in `Type`.

## Accessing Non-Cached PLC Operations

For PLC operations that are not frequently accessed and not cached, you can directly use the `TwincatPlcService` instance.

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
  "PlcEventCache": {
    "AlarmRefreshIntervalSeconds": 2,
    "EventRefreshIntervalSeconds": 5,
    "MaxCachedEvents": 100
  }
}
```