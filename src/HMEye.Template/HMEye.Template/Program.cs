using Blazored.LocalStorage;
using HMEye.Components;
using HMEye.DumbAuth;
using HMEye.ScreenWakeLock;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
string appDataDir = Path.Combine(appDataPath, "HMEye");
Directory.CreateDirectory(appDataDir);

builder.Services.AddDumbAuth(appDataDir);
builder.Services.AddScoped<ScreenWakeLockService>();
builder.Services.AddBlazoredLocalStorage();

builder.WebHost.ConfigureKestrel(
	(context, options) =>
	{
		options.Configure(context.Configuration.GetSection("Kestrel"));
	}
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapAuthEndpoints();

app.Run();
