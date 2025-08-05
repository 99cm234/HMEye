using Blazored.LocalStorage;
using DotNetEnv;
using HMEye.Components;
using HMEye.DumbAuth;
using HMEye.DumbTs;
using HMEye.ScreenWakeLock;
using HMEye.TwincatServices;
using MudBlazor.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapAuthEndpoints();

app.Run();
