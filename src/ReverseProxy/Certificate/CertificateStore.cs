// <copyright file="CertificateStore.cs" company="Henrik Jensen">
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

using Hj.ReverseProxy.Abstraction;
using Hj.ReverseProxy.Certificate.Models;

namespace Hj.ReverseProxy.Certificate;

internal sealed class CertificateStore(
  ILogger<CertificateStore> logger,
  IFileStore fileStore,
  CertificateFactory certificateFactory)
{
  public X509Certificate2? LoadCa(SelfSignedOptions options)
  {
    GetCaFilePaths(options, out var caCrtPemFilePath, out var caKeyPemFilePath, out _);

    if (!fileStore.FileExists(caCrtPemFilePath) || !fileStore.FileExists(caKeyPemFilePath))
    {
      logger.LogInformation("Missing CA, cert path '{CertPemPath}', key path '{CaKeyPath}'", caCrtPemFilePath, caKeyPemFilePath);
      return null;
    }

    logger.LogInformation("Loading CA, cert path '{CertPemPath}', key path '{CaKeyPath}'", caCrtPemFilePath, caKeyPemFilePath);

    var certContents = fileStore.ReadAllText(caCrtPemFilePath);
    var keyContents = fileStore.ReadAllText(caKeyPemFilePath);
    var ca = X509Certificate2.CreateFromPem(certContents, keyContents);
    return ca;
  }

  public void SaveCa(SelfSignedOptions options, X509Certificate2 ca)
  {
    GetCaFilePaths(options, out var caCrtPemFilePath, out var caKeyPemFilePath, out var caPfxFilePath);
    logger.LogInformation("Saving CA, cert path '{CertPemPath}', key path '{CaKeyPemPath}', pfx path '{CaPfxPath}'", caCrtPemFilePath, caKeyPemFilePath, caPfxFilePath);

    fileStore.WriteAllText(caCrtPemFilePath, ca.ExportCertificatePem());
    fileStore.WriteAllText(caKeyPemFilePath, certificateFactory.ExportPrivateKeyPem(ca));

    // Intentionally skipping adding a password here to make it easier to import ca into a trusted root ca store.
    fileStore.WriteAllBytes(caPfxFilePath, ca.Export(X509ContentType.Pfx));
  }

  private void GetCaFilePaths(SelfSignedOptions options, out string caCrtPemFilePath, out string caKeyPemFilePath, out string caPfxFilePath)
  {
    caCrtPemFilePath = fileStore.CombinePath(options.CaFilePath, CertificateConstants.CaCrtFileName);
    caKeyPemFilePath = fileStore.CombinePath(options.CaFilePath, CertificateConstants.CaKeyFileName);
    caPfxFilePath = fileStore.CombinePath(options.CaFilePath, CertificateConstants.CaPfxFileName);
  }
}
