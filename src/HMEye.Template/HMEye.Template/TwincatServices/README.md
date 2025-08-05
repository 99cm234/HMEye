# HMEye.TwincatServices

Provides services for communicating with TwinCAT 3 using Twincat.Ads, including support for caching frequently accessed variables and arrays to optimize performance. This package simplifies interaction with TwinCAT 3 by providing a thread-safe service layer and an optional caching mechanism to reduce direct PLC calls.

## To register in Program.cs

Register all services using the `AddTwincatServices` extension method, which configures `TwincatSettings` and `PlcEventCacheSettings` from `appsettings.json` and registers all services as Singleton and Hosted services.

```csharp
// Register TwinCAT services
builder.Services.AddTwincatServices(builder.Configuration);
```

## Configuring PlcDataCache

The `PlcDataCache` is configured via the `PlcDataCacheConfigProvider` class, which defines a collection of `CacheItemConfig` objects specifying the PLC variables to cache at startup. To modify the initial cache items, update the `GetCacheItemConfigs` method in `PlcDataCacheConfigProvider.cs`. This approach provides type safety and IntelliSense support, making it easy to manage large lists of variables.

If no configurations are provided in `PlcDataCacheConfigProvider`, no variables will be cached by default.

### Example Configuration in `PlcDataCacheConfigProvider.cs`

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
                    Type = typeof(int),  // Element type for int[]
                    IsArray = true,
                    PollInterval = 2000
                }
            };
        }
    }
}
```

### Configuring Array Variables

`PlcDataCache` supports caching of array variables. To configure an array, set the `IsArray` property to `true` in the `CacheItemConfig` and specify the element type in the `Type` property. For example, for an array of integers:

```csharp
new CacheItemConfig
{
    Address = "MAIN.valuesArray",
    Type = typeof(int),  // Element type
    IsArray = true,
    PollInterval = 2000
}
```

When accessing the array, use the array type in the generic methods, e.g., `ReadImmediateAsync<int[]>("MAIN.valuesArray")`.

### Dynamic Cache Management

`PlcDataCache` supports adding and removing cache items at runtime using the `AddCacheItem` and `RemoveCacheItem` methods.

- **`AddCacheItem(CacheItemConfig config)`**  
  Adds a new cache item to the cache.  
  - **Parameters**: `config` - Configuration with address, type, poll interval, etc.  
  - **Throws**:  
    - `ArgumentNullException` if `config` is null.  
    - `InvalidOperationException` if the address already exists.  
    - `Exception` if item creation fails (e.g., invalid type).  
  - **Note**: New items are polled in the next polling cycle.

- **`RemoveCacheItem(string address)`**  
  Removes a cache item by its address.  
  - **Parameters**: `address` - The address of the item to remove.  
  - **Throws**: `ArgumentException` if `address` is null or empty.  
  - **Note**: If the address doesn’t exist, the method does nothing silently.

**Usage Example**:
```csharp
var cache = serviceProvider.GetService<IPlcDataCache>();
var config = new CacheItemConfig
{
    Address = "MAIN.newVariable",
    Type = typeof(int),
    PollInterval = 1000,
    IsReadOnly = false
};
cache.AddCacheItem(config);
cache.RemoveCacheItem("MAIN.newVariable");
```

**Important Notes**:
- Newly added items may not be polled until the next polling cycle.
- Removed items might be polled once more if removal occurs during a cycle.
- All operations are thread-safe due to the use of `ConcurrentDictionary`.

## TwinCAT to .NET Data Type Mapping

Below is a chart of commonly used TwinCAT data types and their equivalent .NET types, useful for configuring `CacheItemConfig` in `PlcDataCacheConfigProvider`.

```
+--------------------+-----------------------------+
| TwinCAT Data Type  | .NET Equivalent             |
+--------------------+-----------------------------+
| BOOL               | bool                        |
| BYTE               | byte                        |
| SINT               | sbyte                       |
| INT                | short                       |
| DINT               | int                         |
| LINT               | long                        |
| REAL               | float                       |
| LREAL              | double                      |
| STRING             | string                      |
| TIME               | uint / TimeSpan             |
| DATE               | DateTime                    |
| TOD (TimeOfDay)    | TimeSpan                    |
+--------------------+-----------------------------+
```

**Note**: Arrays of the above types are supported. Set `IsArray` to `true` and specify the element type in `Type`.

## Accessing Non-Cached PLC Operations

For PLC operations that are not frequently accessed and thus not cached, you can directly use the `TwincatPlcService` instance. This includes methods like `ReadDeviceStateAsync`, `PlcCommandAsync`, and events like `ConnectionSuccess` and `ConnectionLost`. Since these are expected to be used infrequently, direct access is safe and efficient.

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