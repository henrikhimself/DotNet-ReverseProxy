// <copyright file="ResourceBuilderExtensions.cs" company="Henrik Jensen">
// Copyright 2025 Henrik Jensen
//
// Licensed under the Apache License, Version 2.0 (the "License")
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace Hj.ReverseProxy.Aspire;

public static class ResourceBuilderExtensions
{
  public static IResourceBuilder<T> WithReverseProxyReference<T>(this IResourceBuilder<T> builder, string serviceName, EndpointReference endpointReference, string hostName, int port = 443)
    where T : IResourceWithEnvironment
  {
    var externalUrl = $"https://{hostName}:{port}";

    builder
      .WithUrl(externalUrl)
      .WithReference(endpointReference)
      .WithEnvironment(context =>
      {
        context.EnvironmentVariables[$"{Constants.EnvPrefix}__{serviceName}"] = hostName;
      });

    return builder;
  }
}
