# ReverseProxy

[Features](#features) • [Quick Start](#quick-start) • [Usage](#usage) • [Examples](#examples) • [API Reference](#api-reference)

A .NET library that combines [Microsoft YARP](https://microsoft.github.io/reverse-proxy/) (Yet Another Reverse Proxy) with runtime configuration APIs and automatic self-signed certificate generation. Designed to simplify development and testing of reverse proxy scenarios with minimal setup.

## Features

- **Developer Ready** - Built on YARP's reverse proxy engine
- **Automatic HTTPS** - Self-signed certificates generated on-demand with local CA trust for seamless HTTPS testing
- **Aspire Integration** - Support for Aspire service discovery
- **Runtime Configuration** - Add and update routes and clusters dynamically via REST API
- **Wildcard Support** - Supports both specific domains and wildcard certificates
- **Automatic Caching** - Certificates are cached to avoid regeneration
- **Multi-Platform** - Works on Windows, macOS (including .NET 8 compatibility), and Linux

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Aspire](https://aspire.dev/get-started/prerequisites/) (if using Aspire integration)

## Quick Start

### Installation

Install the NuGet package in your project:

```bash
dotnet add package HenrikJensen.ReverseProxy
```

For Aspire integration, also install:

```bash
dotnet add package HenrikJensen.ReverseProxy.Aspire
```

### Basic Setup

**1. Configure services in `Program.cs`:**

```csharp
// Enable automatic self-signed certificates signed by local CA
builder.WebHost.ConfigureKestrel(options =>
{
    options.UseSelfSignedCertificate();
});

// Add reverse proxy services
builder.Services.ConfigureReverseProxy(builder.Configuration);

// Use service discovery to set up routes and clusters for Aspire resources (optional)
builder.Services.AddTransient<IStartupFilter, ServiceDiscoveryStartupFilter>();

// Map the reverse proxy
app.UseReverseProxy();

// Map the runtime configuration API (optional)
app.UseReverseProxyApi();
```

**2. Start your application:**

```bash
dotnet run
```

The reverse proxy is now running with automatic HTTPS certificate generation and Aspire service discovery!

### Configuration

Certificate generation options are loaded from the `SelfSignedCertificate` configuration section.

```json
{
  "SelfSignedCertificate": {
    "CaFilePath": "{REVERSEPROXY_HOME}",
    "CaName": "ReverseProxy-RootCA",
    "AlgorithmOid": "1.2.840.10045.2.1",
    "SubjectName": "CN=ReverseProxy Root CA"
  }
}
```

#### SelfSignedCertificate options

| Key | Required | Default | Description |
| --- | --- | --- | --- |
| `CaFilePath` | Yes | None | Directory where the CA files are read/written. |
| `CaName` | No | `ReverseProxy-RootCA` | Base name used for generated CA files. |
| `AlgorithmOid` | No | `1.2.840.10045.2.1` (ECDSA) | Key algorithm OID used for CA and leaf certificate creation. |
| `SubjectName` | No | `CN=ReverseProxy Root CA` | Subject name for the generated CA certificate. |

#### Environment variable support

If `CaFilePath` contains `{REVERSEPROXY_HOME}`, the value is replaced with the `REVERSEPROXY_HOME` environment variable at runtime.

```bash
export REVERSEPROXY_HOME="$HOME/.reverseproxy"
```

Example:

```json
{
  "SelfSignedCertificate": {
    "CaFilePath": "{REVERSEPROXY_HOME}/certs"
  }
}
```

#### Algorithm OIDs

- `1.2.840.10045.2.1` (ECDSA, default)
- `1.2.840.113549.1.1.1` (RSA)

#### Files generated in `CaFilePath`

Using `CaName = ReverseProxy-RootCA`, the following files are generated:

- `ReverseProxy-RootCA.crt.pem`
- `ReverseProxy-RootCA.key.pem`
- `ReverseProxy-RootCA.pfx`

## Usage

### Aspire Integration

Use the Aspire integration to automatically configure routes based on service discovery:

**In your AppHost project:**

```csharp
using Hj.ReverseProxy.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

// Add your backend resource service
var website = builder.AddProject<Projects.MyWebsite>("website")
    .WithHttpEndpoint();

// Add reverse proxy with HTTPS
var reverseProxy = builder
    .AddProject<Projects.ReverseProxy>("reverse-proxy")
    .WithHttpsEndpoint(port: 443);

// Configure reverse proxy to route to the website
reverseProxy.WithReverseProxyReference(
    serviceName: "website",
    endpoint: website.GetEndpoint("http"),
    hostName: "my-website.local"
);

await builder.Build().RunAsync();
```

**Access your service:**
```
https://my-website.local
```

The reverse proxy automatically:
- Generates a trusted self-signed certificate for `my-website.local`
- Discovers the website's endpoint from Aspire
- Routes HTTPS traffic to your backend HTTP resource service

### Runtime Configuration via API

Add routes and clusters dynamically using the REST API:

**Add a route:**
```bash
curl -X POST http://localhost:5000/route \
  -H "Content-Type: application/json" \
  -d '{
    "routes": [{
      "routeId": "api-route",
      "clusterId": "api-cluster",
      "match": {
        "path": "/api/{**catch-all}"
      }
    }]
  }'
```

**Add a cluster:**
```bash
curl -X POST http://localhost:5000/cluster \
  -H "Content-Type: application/json" \
  -d '{
    "clusters": [{
      "clusterId": "api-cluster",
      "destinations": {
        "destination1": {
          "address": "https://catfact.ninja/fact"
        }
      }
    }]
  }'
```

**Get current configuration:**
```bash
# List all routes
curl http://localhost:5000/route

# List all clusters
curl http://localhost:5000/cluster
```

## Examples

The `examples` directory contains a complete example demonstrating:
- A website using https and a custom host name
- Configuring YARP routes/clusters via
    - Aspire service discovery integration
    - Appsettings.json file
    - Runtime REST API
- An Aspire AppHost configuration
- A ReverseProxyApi.http file for calling the Runtime REST API

**Run the example:**
- Set the `REVERSEPROXY_HOME` environment variable, or update the `SelfSignedCertificate:CaFilePath` value in appsettings.json to specify where the generated Certificate Authority should be stored
- Install the generated `ReverseProxy-RootCA` (pem or pfx) into your system's trusted root store
- Add an entry to your hosts file that maps `example-website.local` to 127.0.0.1 and ::1
- Consider rebooting to clear certificate and dns caches

```bash
cd examples/Aspire.AppHost
dotnet run
```

- Open the Aspire dashboard to see all services running
- Open the website using the custom host name https://example-website.local:8443/

## API Reference

### Configuration API

The runtime configuration API is exposed at the configured route prefix (default: `/`).
An optional route prefix can be provided during configuration.

#### Routes

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/route` | List all configured routes |
| `POST` | `/route` | Add or update routes |

#### Clusters

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/cluster` | List all configured clusters |
| `POST` | `/cluster` | Add or update clusters |

### Noteworthy Extension Methods

#### `UseSelfSignedCertificate()`
Enables automatic self-signed certificate generation for HTTPS endpoints.

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.UseSelfSignedCertificate();
});
```

#### `ConfigureReverseProxy()`
Registers reverse proxy services in the DI container.

```csharp
builder.Services.ConfigureReverseProxy(builder.Configuration);
```

#### `MapReverseProxyApi()`
Maps the runtime configuration API endpoints with an optional route prefix.

```csharp
app.MapReverseProxyApi(routePrefix: "/api");
```

#### `WithReverseProxyReference()`
Configures Aspire service discovery for a resource service.

```csharp
reverseProxy.WithReverseProxyReference(
    serviceName: "my-service",
    endpoint: service.GetEndpoint("http"),
    hostName: "my-service.local"
);
```

## Troubleshooting

### Certificate trust issues

If you encounter certificate trust warnings:
- **Windows**: The CA must be manually installed in the `Trusted Root Certification Authorities` store
- **macOS**: You need to manually trust the CA in Keychain Access
- **Linux**: Add the CA certificate to your system's trust store (location varies by distribution)

### Port conflicts

If you see port binding errors:
- Ports below 1024 (like 443) require administrator/root privileges when running `dotnet run`

### Aspire service discovery not working

Ensure:
- Service names match between `WithReverseProxyReference()` and your resource service configuration
