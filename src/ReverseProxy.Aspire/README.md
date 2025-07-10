# A Reverse Proxy for .NET Aspire.
This package integrates the HenrikJensen.ReverseProxy package into a .NET Aspire solution.

It contains an "WithReverseProxyReference" annotation for configuring Aspire resources in the Program.cs of the AppHost.

It also contains a start up filter to be used in the website that acts as the reverse proxy. Registering the "ServiceDiscoveryStartupFilter" will configure the reverse proxy website to expose annotated resources.
