using Hj.ReverseProxy;
using Hj.ReverseProxy.Aspire;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
  // Adds a certificate resolver to Kestrel that dynamically generates a self-signed certificate when
  // needed to service a HTTP request. Trust is added to these using a Root Certificate Authority that
  // is created and saved to a configured location (see "SelfSignedCertificate" section in appsettings).
  // You need to make your system trust this Root CA by importing it into your certificate store. Also
  // make sure you don't delete the generated Root CA once created and imported.
  options.UseSelfSignedCertificate();
});

// Register injected Aspire resource service discovery variables as YARP Routes and Clusters on startup.
// If you're missing some at runtime then you may want to consider making the reverse proxy resource
// wait for the resources that it should proxy.
builder.Services.AddTransient<IStartupFilter, ServiceDiscoveryStartupFilter>();

// Configure the reverse proxy.
builder.Services.ConfigureReverseProxy(builder.Configuration);

var app = builder.Build();

// Use the configured reverse proxy.
app.UseReverseProxy();

// No further routes are registered. This means that you will get a 404 error status on all requests
// that do not match a proxied Aspire resource.
await app.RunAsync();
