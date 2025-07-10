using Microsoft.Extensions.Configuration;

namespace Hj.ReverseProxy.Aspire.UnitTest;

public class ServiceDiscoveryTests
{
  [Fact]
  public void DiscoverEndpointList_GivenInvalidQuery_ReturnsEmpty()
  {
    // arrange
    var config = GetConfiguration([]);

    // act & assert
    Assert.Throws<InvalidOperationException>(() => ServiceDiscovery.DiscoverEndpointList(config, "invalid-query"));
  }

  [Fact]
  public void DiscoverEndpointList_GivenMissingServiceDiscoverySection_ReturnsEmpty()
  {
    // arrange
    var config = GetConfiguration([]);

    // act
    var result = ServiceDiscovery.DiscoverEndpointList(config, "https://service");

    // assert
    Assert.Empty(result);
  }

  [Fact]
  public void DiscoverEndpointList_GivenService_ReturnsEndpoints()
  {
    // arrange
    var config = GetConfiguration(new Dictionary<string, string>()
    {
      { "Services:Service:Endpoint:0", "http://localhost" },
      { "Services:Service:Endpoint:1", "https://localhost" },
    });

    // act
    var result = ServiceDiscovery.DiscoverEndpointList(config, "endpoint://service");

    // assert
    Assert.Equal("http://localhost", result[0]);
    Assert.Equal("https://localhost", result[1]);
  }

  [Fact]
  public void DiscoverEndpointList_GivenServiceAndNamedEndpoint_ReturnsNamedEndpoint()
  {
    // arrange
    var config = GetConfiguration(new Dictionary<string, string>()
    {
      { "Services:Service:Endpoint:0", "https://localhost" },
      { "Services:Service:Named-Endpoint:0", "https://named-endpoint" },
    });

    // act
    var result = ServiceDiscovery.DiscoverEndpointList(config, "https://_named-endpoint.service");

    // assert
    var item = Assert.Single(result);
    Assert.Equal("https://named-endpoint", item);
  }

  [Theory]
  [InlineData("http")]
  [InlineData("https")]
  public void DiscoverEndpointList_GivenServiceWithAllowedSchemes_ReturnsEndpointWithAllowedScheme(string allowedScheme)
  {
    // arrange
    var config = GetConfiguration(new Dictionary<string, string>()
    {
      { "Services:Service:Endpoint:0", "http://localhost" },
      { "Services:Service:Endpoint:1", "https://localhost" },
    });

    string[] allowedSchemes = [allowedScheme];

    // act
    var result = ServiceDiscovery.DiscoverEndpointList(config, "http+https://_endpoint.service", allowedSchemes, false);

    // assert
    var item = Assert.Single(result);
    Assert.Equal(allowedScheme + "://localhost", item);
  }

  [Theory]
  [InlineData("")]
  [InlineData("http+")]
  public void DiscoverEndpointList_GivenInvalidAllowedSchemes_SkipsAllowedSchemeReturnsNone(string allowedScheme)
  {
    // arrange
    var config = GetConfiguration(new Dictionary<string, string>()
    {
      { "Services:Service:Endpoint:0", "http://localhost" },
      { "Services:Service:Endpoint:1", "https://localhost" },
    });

    string[]? allowedSchemes = [allowedScheme];

    // act
    var result = ServiceDiscovery.DiscoverEndpointList(config, "http+https://_endpoint.service", allowedSchemes, false);

    // assert
    Assert.Empty(result);
  }

  private static IConfiguration GetConfiguration(Dictionary<string, string> settings)
  {
    var initialData = settings.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value));
    return new ConfigurationBuilder().AddInMemoryCollection(initialData).Build();
  }
}
