using Hj.Examples.Aspire.ServiceDefaults;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Enable handling forwarded headers.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
  ForwardedHeaders = ForwardedHeaders.All,
});

app.MapDefaultEndpoints();

app.MapGet("/target", (IConfiguration configuration) => configuration["EXAMPLE_TARGET_NAME"] ?? "unknown");

app.UseRouting();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}");

await app.RunAsync();
