# Copilot instructions

## Project Overview
This repository is a .NET library that combines Microsoft YARP (Yet Another Reverse Proxy) with runtime configuration APIs and automatic self-signed ephemeral certificate generation. It's designed to simplify development and testing of reverse proxy scenarios. The `examples/` directory is set up to use Aspire. Aspire is an orchestrator for the entire application and will take care of configuring dependencies, building, and running the application. The resources that make up the application are defined in `examples/Aspire.AppHost/Program.cs` including application code and external dependencies.

The `src/ReverseProxy` directory implements the core functionality of the library. The `src/ReverseProxy.Aspire` directory is an extension that integrates `src/ReverseProxy` into an Aspire solution for managing YARP route/cluster configuration.

### Key Components
1. **ReverseProxy Core** (`src/ReverseProxy`)
   - **ReverseProxyApp**: Manages routes/clusters using YARP's configuration system
   - **Certificate Management**: Automatic generation of self-signed ephemeral certificates with caching and trust added by a local CA
   - **Runtime API**: Web API endpoints for managing routes/clusters at runtime

2. **ReverseProxy.Aspire** (`src/ReverseProxy.Aspire`)
   - Integrates with the Aspire orchestration platform for route/cluster configuration for services without requiring the **Runtime API**
   - `WithReverseProxyReference()`: Extension for configuring Aspire resources
   - `ServiceDiscoveryStartupFilter`: Auto-discovers and configures annotated Aspire resources as YARP routes/clusters

3. **Examples** (`examples/`)
   - AppHost: Aspire app host demonstrating the reverse proxy in action
   - ReverseProxy: Example reverse proxy service using the library
   - Website: Example backend service discoverable by the proxy

## Architecture Patterns

### Configuration Layering
- **InMemoryConfigProvider**: Holds runtime-added routes/clusters
- **IProxyConfigProvider**: Base abstraction; implementations are merged in ReverseProxyApp
- Multiple providers can coexist; routes/clusters are combined across all

### Dependency Injection Pattern
Services use primary constructor injection with sealed internal classes:
```csharp
internal sealed class ReverseProxyApp(
  IConfigValidator configValidator,
  InMemoryConfigProvider inMemoryConfigProvider,
  IEnumerable<IProxyConfigProvider> proxyConfigProviders)
```

Use the extension methods in `src/ReverseProxy/ReverseProxyExtensions.cs` to configure services:
- `ConfigureReverseProxy()`: Registers core services
- `UseSelfSignedCertificate()`: Enables automatic certificate generation on Kestrel

### Certificate Management
- **Lazy Generation**: Certificates are created on-demand via ServerCertificateSelector
- **Caching**: IMemoryCache prevents redundant certificate generation
- **Wildcard Support**: Handles both `*.example.com` and specific domain names
- **Self-Signed CA**: Generated once and installed into the trusted root store on the platform; certificates derive trust from it

## General recommendations for working with Aspire
1. Before making any changes in the `examples/` directory, always run the apphost using `aspire run` and inspect the state of resources to make sure you are building from a known state.
1. Changes to the `examples/Aspire.AppHost/Program.cs` file will require a restart of the application to take effect.
2. Make changes incrementally and run the aspire application using the `aspire run` command to validate changes.
3. Use the Aspire MCP tools to check the status of resources and debug issues.

## Running the example application
To run the example application run the following command:

```
aspire run
```

If there is already an instance of the application running it will prompt to stop the existing instance. You only need to restart the application if code in `examples/Aspire.AppHost/Program.cs` is changed, but if you experience problems it can be useful to reset everything to the starting state.

## Checking Aspire resources
To check the status of Aspire resources defined in the AppHost model use the _list resources_ tool. This will show you the current state of each resource and if there are any issues. If a resource is not running as expected you can use the _execute resource command_ tool to restart it or perform other actions.

## Listing Aspire integrations
IMPORTANT! When a user asks you to add a resource to the AppHost model you should first use the _list integrations_ tool to get a list of the current versions of all the available integrations. You should try to use the version of the integration which aligns with the version of the Aspire.AppHost.Sdk. Some integration versions may have a preview suffix. Once you have identified the correct integration you should always use the _get integration docs_ tool to fetch the latest documentation for the integration and follow the links to get additional guidance.

## Debugging Aspire issues
IMPORTANT! Aspire is designed to capture rich logs and telemetry for all resources defined in the AppHost model. Use the following diagnostic tools when debugging issues with the application before making changes to make sure you are focusing on the right things.

1. _list structured logs_; use this tool to get details about structured logs.
2. _list console logs_; use this tool to get details about console logs.
3. _list traces_; use this tool to get details about traces.
4. _list trace structured logs_; use this tool to get logs related to a trace

## Other Aspire MCP tools
1. _select apphost_; use this tool if working with multiple app hosts within a workspace.
2. _list apphosts_; use this tool to get details about active app hosts.

## Updating the Aspire AppHost
The user may request that you update the Aspire apphost. You can do this using the `aspire update` command. This will update the apphost to the latest version and some of the Aspire specific packages in referenced projects, however you may need to manually update other packages in the solution to ensure compatibility. You can consider using the `dotnet-outdated` with the users consent. To install the `dotnet-outdated` tool use the following command:

```
dotnet tool install --global dotnet-outdated-tool
```

## Aspire workload
IMPORTANT! The aspire workload is obsolete. You should never attempt to install or use the Aspire workload.

## Official Aspire documentation
IMPORTANT! Always prefer official documentation when available. The following sites contain the official documentation for Aspire and related components.

1. https://aspire.dev
2. https://learn.microsoft.com/dotnet/aspire
3. https://nuget.org (for specific integration package details)

## Testing Strategy

### Test Framework & Patterns
- **xUnit** with **SutFactory** for mocking
- **SutFactory** (HenrikJensen.SutFactory): Custom builder pattern for automatic arrangement of the dependency graph while creating the System Under Test instance
  - Reduces boilerplate; use `SutBuilder` → `InputBuilder` → `Instance<T>()` or if no spy instances are inspected, the preferred pattern `SystemUnderTest.For<T>(arrange => { })` where 'arrange' is an InputBuilder.
- **Arrange-Act-Assert**: Standard pattern with setup helpers (see `SetHappyPath(InputBuilder arrange)` in tests)

### Key Test Files
- `test/ReverseProxy.UnitTest/ReverseProxy/ReverseProxyApiTests.cs`: Tests the API endpoints
- `test/ReverseProxy.UnitTest/ReverseProxy/ReverseProxyAppTests.cs`: Tests configuration management
- `test/ReverseProxy.UnitTest/Certificate/CertificateAppTests.cs`: Tests certificate generation
- `test/ReverseProxy.UnitTest/Certificate/CertificateConfigTests.cs`: Tests configuration management
- `test/ReverseProxy.Aspire.UnitTest/ServiceDiscoveryTests.cs`: Tests the Aspire service discovery

### Running Tests
To test the library run the following command from the solution root:

```
dotnet test
```

## Build & Development

### Multi-Targeting
Projects target both `net8.0` and `net10.0`. Compatibility with Windows, macOS and Linux must be ensured when adding and changing code. Use dotnet test to verify.

### Code Analysis & Style
- **EnforceCodeStyleInBuild**: Code style violations fail the build
- Configuration: `stylecop.json` requires `xmlHeader: true` for packable projects

### Package Management
Central package versioning via `Directory.Packages.props`:
- Add new dependencies by adding `<PackageVersion>` entries there
- Projects reference by `<PackageReference Include="Name" />` (version auto-inherited)

### Global Usings
Common namespaces are globally imported via `Directory.Build.props` and project `.csproj` files

## Integration Points

### YARP Integration
- Uses `Yarp.ReverseProxy` NuGet package
- Core types: `RouteConfig`, `ClusterConfig`, `DestinationConfig`
- Validation via `IConfigValidator`

### Aspire Integration
- DI service registration via `IServiceCollection`.
- Startup filters for configuring reverse proxy routes/clusters using injected service discovery environment variables.
- Resource builders follow Aspire conventions

### Extension Points
- **IProxyConfigProvider**: Implement to add custom configuration sources
- **ICertificateConfig**: Customize certificate generation options (algorithm, subject name)
- **IFileStore**: Abstract file storage for certificates

