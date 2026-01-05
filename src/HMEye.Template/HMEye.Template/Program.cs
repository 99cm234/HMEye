using Blazored.LocalStorage;
using DotNetEnv;
using HMEye.Components;
using HMEye.DumbAuth;
using HMEye.DumbTs;
using HMEye.Extensions;
using HMEye.ScreenWakeLock;
using HMEye.Twincat;
using MudBlazor.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHMEyeOpenApi();

// Add feature specific configuration files
builder
	.Configuration.AddJsonFile("appsettings.dumbauth.json", optional: true, reloadOnChange: true)
	.AddJsonFile("appsettings.modbus.json", optional: true, reloadOnChange: true)
	.AddJsonFile("appsettings.twincat.json", optional: true, reloadOnChange: true)
	.AddJsonFile("appsettings.yarp.json", optional: true, reloadOnChange: true);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
string appDataDir = Path.Combine(appDataPath, "HMEye");
Directory.CreateDirectory(appDataDir);

builder.Services.AddDumbAuth(builder.Configuration, appDataDir);

builder.Services.AddDumbTsLogging(TimeSpan.FromSeconds(2), appDataDir);
builder.Services.AddScoped<ScreenWakeLockService>();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddTwincatServices(builder.Configuration);

builder.Services.AddAuthorizationBuilder().AddHMEyePolicies(builder.Configuration);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.MapHMEyeScalar(builder.Configuration);

app.UseHttpsRedirection();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapAuthEndpoints();
app.MapTwincatEndpoints();

app.MapReverseProxy();

app.Run();
