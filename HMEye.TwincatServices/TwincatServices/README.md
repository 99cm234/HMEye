# HMEye.TwincatServices

Provides services for communicating with TwinCAT 3 using Twincat.Ads.

## To register in Program.cs

All 3 services should be registered as Singleton and Hosted services. Use TwincatSettings.cs to to load appsettings.json

```csharp
// TwincatSettings from appsettings.json
builder.Services.Configure<TwincatSettings>(builder.Configuration.GetSection("TwincatSettings"));

// Register as singleton and hosted
builder.Services.AddSingleton<IPlcService, PlcService>();
builder.Services.AddHostedService<IPlcService>(sp => sp.GetRequiredService<IPlcService>());

builder.Services.AddSingleton<IEventLoggerService, EventLoggerService>();
builder.Services.AddHostedService<IEventLoggerService>(sp => sp.GetRequiredService<IEventLoggerService>());

builder.Services.AddSingleton<ISystemService, SystemService>();
builder.Services.AddHostedService<ISystemService>(sp => sp.GetRequiredService<ISystemService>());
```

## Appsettings.json example
```json
{
  "TwincatSettings": {
    "NetId": "199.4.42.250.1.1",
    "PlcPort": 851,
    "SystemPort": 10000,
    "Timeout": 10,
    "ReconnectDelaySeconds": 10
  }
}
```