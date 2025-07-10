using Hj.ReverseProxy.Aspire;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Add example website.
var website = builder.AddProject<Examples_Aspire_Website>("Website", options =>
{
  // Do not use endpoint configuration found in the Website project. We let aspire set up everything.
  options.ExcludeLaunchProfile = true;
  options.ExcludeKestrelEndpoints = true;
})
  // Add a HTTP endpoint. The reverse proxy will set up a secure HTTPS endpoint for you to connect to this resource.
  .WithHttpEndpoint();

// Add reverse proxy website.
var reverseProxy = builder
  .AddProject<Examples_Aspire_ReverseProxy>("Reverse-Proxy")
  // Map a host name to the endpoint of the example website.
  .WithReverseProxyReference("Website", website.GetEndpoint("http"), "example-website.local");

// Wait for the website to be healthy.
reverseProxy.WaitFor(website);

await builder.Build().RunAsync();
