# PlcDataService

`PlcDataService` allows client pages concurrent access to PLC data.

- Reads PLC variables at set interval.
- Shows two ways to write setpoints:
  - `SetTemperatureSetpoint`: This works well for variables updated at higher frequency. Writing to a local variable and only updating PLC on change.
  - `SetHumiditySetpointAsync`: For low-frequency updates. Simpler. Easier to use and adequate for most PLC HMI use cases.
- Tracks errors (e.g., PLC connection loss) wit `Error` and `ErrorMessage` properties for UI feedback.

## How to Use

### 1. Register the Service:
   In `Program.cs`, add `PlcDataService` as a singleton and hosted service:
   ```csharp
   builder.Services.AddSingleton<PlcDataService>();
   builder.Services.AddHostedService<PlcDataService>();
   ```
   Ensure `PlcService` (from `HMEye.TwincatServices`) and `ILogger<PlcDataService>` are registered.

### 2. Use in Blazor Pages:
   Inject `PlcDataService` and access its properties or methods:
   ```csharp
   @inject PlcDataService PlcData

   <p>Temperature: @PlcData.Temperature °C</p>
   <button @onclick="async () => await PlcData.SetHumiditySetpointAsync(50)">Set Humidity to 50%</button>
   ```

### 3. Handling Complex Types:
   For complex types (structs or whatever) use `record` types (e.g., `record ControlConfig(short Speed, short Mode)`) to thread safety.

## Notes
- make update interval configurable.
- `PlcService` is assumed to be a singleton hosted service.
- make an interface for this?
- write a wiki for this shit?