using Blazored.LocalStorage;
using DotNetEnv;
using HMEye.Components;
using HMEye.DumbAuth;
using HMEye.DumbTs;
using HMEye.ScreenWakeLock;
using HMEye.Twincat;
using HMEye.Twincat.Endpoints;
using MudBlazor.Services;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add feature specific configuration files
builder.Configuration
		.AddJsonFile("appsettings.dumbauth.json", optional: true, reloadOnChange: true)
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

builder.Services.AddAuthorizationBuilder()
	.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"))
	.AddPolicy("RequireViewer", policy => policy.RequireRole("User", "Admin"))
	.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(_ => true))
	.AddPolicy("GrafanaPolicy", policy =>
		{
			policy.RequireAssertion(context =>
			{
				var httpContext = context.Resource as HttpContext;
				// Check for admin role from any source (Cookie or API Key)
				if (context.User.IsInRole("Admin")) return true;

				if (httpContext != null)
				{
					if (
						httpContext.Request.Path.StartsWithSegments("/grafana/public-dashboards")
						|| httpContext.Request.Path.StartsWithSegments("/grafana/public")
						|| httpContext.Request.Path.StartsWithSegments("/grafana/api/public")
					)
					{
						return true;
					}
					return context.User.IsInRole("Admin");
				}
				return false;
			});
		}
);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();
app.MapAuthEndpoints();
app.MapPlcDataEndpoints();

app.MapReverseProxy();

app.Run();
