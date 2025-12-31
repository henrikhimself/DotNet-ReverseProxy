# Copilot Instructions for DotNet-ReverseProxy

## Project Overview

**HenrikJensen.ReverseProxy** is a .NET library that combines Microsoft YARP (Yet Another Reverse Proxy) with runtime configuration APIs and automatic self-signed certificate generation. It's designed to simplify development and testing of reverse proxy scenarios.

### Key Components

1. **ReverseProxy Core** ([src/ReverseProxy/](../src/ReverseProxy/))
   - **ReverseProxyApp**: Manages routes and clusters using YARP's configuration system
   - **Certificate Management**: Automatic generation of self-signed certificates with caching with trust added by a local CA
   - **Runtime API**: Web API endpoints for adding/managing routes and clusters dynamically

2. **ReverseProxy.Aspire** ([src/ReverseProxy.Aspire/](../src/ReverseProxy.Aspire/))
   - Integrates with .NET Aspire orchestration platform for route/cluster configuration for services without requiring API calls
   - `WithReverseProxyReference()`: Extension for configuring Aspire resources
   - `ServiceDiscoveryStartupFilter`: Auto-discovers and configures annotated Aspire resources

3. **Examples** ([examples/](../examples/))
   - AppHost: Aspire app host demonstrating the reverse proxy in action
   - ReverseProxy: Example reverse proxy service using the library
   - Website: Example backend service discoverable by the proxy

## Architecture Patterns

### Configuration Layering
- **InMemoryConfigProvider**: Holds runtime-added routes/clusters
- **IProxyConfigProvider**: Base abstraction; implementations are merged in ReverseProxyApp
- Multiple providers can coexist; routes/clusters are combined across all

### Dependency Injection Pattern
Services use constructor injection with sealed internal classes:
```csharp
internal sealed class ReverseProxyApp(
  IConfigValidator configValidator,
  InMemoryConfigProvider inMemoryConfigProvider,
  IEnumerable<IProxyConfigProvider> proxyConfigProviders)
```

Use the extension methods in [ReverseProxyExtensions.cs](../src/ReverseProxy/ReverseProxyExtensions.cs) to configure services:
- `ConfigureReverseProxy()`: Registers core services
- `UseSelfSignedCertificate()`: Enables automatic certificate generation on Kestrel

### Certificate Management
- **Lazy Generation**: Certificates are created on-demand via ServerCertificateSelector
- **Caching**: IMemoryCache prevents redundant certificate generation
- **Wildcard Support**: Handles both `*.example.com` and specific domain names
- **Self-Signed CA**: Generated once and installed into the trusted root store on the platform; certificates derive trust from it

## Testing Strategy

### Test Framework & Patterns
- **xUnit** with **NSubstitute** for mocking
- **SutFactory** (HenrikJensen.SutFactory): Custom builder pattern for automatic arrangement of the dependency graph while creating the System Under Test instance
  - Reduces boilerplate; use `SutBuilder` → `InputBuilder` → `Instance<T>()` or if no spy instances are inspected, the preferred pattern `SystemUnderTest.For<T>(arrange => { })` where 'arrange' is an InputBuilder.
- **Arrange-Act-Assert**: Standard pattern with setup helpers (see `SetHappyPath(InputBuilder arrange)` in tests)

### Key Test Files
- [ReverseProxy.UnitTest/ReverseProxy/ReverseProxyApiTests.cs](../test/ReverseProxy.UnitTest/ReverseProxy/ReverseProxyApiTests.cs): Tests the API endpoints
- [ReverseProxy.UnitTest/ReverseProxy/ReverseProxyAppTests.cs](../test/ReverseProxy.UnitTest/ReverseProxy/ReverseProxyAppTests.cs): Tests configuration management

### Running Tests
Prefer to use the cli command "dotnet test" from the solution root. Ignore the Invoke-Test.ps1 PowerShell script as its purpose is to generate a human readable test report in HTML format that shows test coverage.

## Build & Development

### Multi-Targeting
Projects target both `net8.0` and `net10.0`. Compatibility with Windows, macOS and Linux must be ensured when adding and changing code.

### Code Analysis & Style
- **StyleCop.Analyzers**: Enforces XML documentation headers on public members
- **EnforceCodeStyleInBuild**: Code style violations fail the build
- Configuration: [stylecop.json](../stylecop.json) requires `xmlHeader: true` for packable projects

### Package Management
Central package versioning via [Directory.Packages.props](../Directory.Packages.props):
- Add new dependencies by adding `<PackageVersion>` entries there
- Projects reference by `<PackageReference Include="Name" />` (version auto-inherited)

### Global Usings
Common namespaces are globally imported via [Directory.Build.props](../Directory.Build.props) and project `.csproj` files:
```csharp
<Using Include="System.Collections.Generic" />
<Using Include="Microsoft.Extensions.Logging" />
<Using Include="Microsoft.Extensions.Options" />
```

## Integration Points

### YARP Integration
- Uses `Yarp.ReverseProxy` NuGet package (v2.3.0)
- Core types: `RouteConfig`, `ClusterConfig`, `DestinationConfig`
- Validation via `IConfigValidator`

### Aspire Integration
- Targets `Aspire.Hosting.AppHost` (v9.5.1)
- DI service registration via `IServiceCollection`.
- Sstartup filters for configuring reverse proxy routes/clusters using injected service discovery environment variables.
- Resource builders follow Aspire conventions

### Extension Points
- **IProxyConfigProvider**: Implement to add custom configuration sources
- **ICertificateConfig**: Customize certificate generation options (algorithm, subject name)
- **IFileStore**: Abstract file storage for certificates

## Common Tasks

### Adding a New Route at Runtime
1. Create `RouteConfig` with required properties (`RouteId`, `ClusterId`, `Match`)
2. Call `ReverseProxyApp.AddRouteAsync(route, allowOverwrite: true)`
3. Validation errors are wrapped in `AggregateException`

### Adding Certificate Generation Strategy
1. Implement `ICertificateConfig` (provides values for key algorithm, ca path and name, subject name)
2. Register in DI via `ConfigureReverseProxy()` extension
3. Caching is automatic; no changes needed to `CertificateApp`

### Publishing Changes
- Package ID: `HenrikJensen.ReverseProxy`
- Use semantic versioning (current: 1.0.0-beta5)
- Update version in [src/ReverseProxy/ReverseProxy.csproj](../src/ReverseProxy/ReverseProxy.csproj#L14)
- Ensure all tests pass before publishing
- Ensure all code is compatible with Windows, macOS and Linux

## References
- **Solution**: [ReverseProxy.sln](../ReverseProxy.sln) (projects + examples + tests)
- **Global Configuration**: [global.json](../global.json) (SDK version pinning)
