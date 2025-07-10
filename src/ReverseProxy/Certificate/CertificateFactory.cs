// <copyright file="CertificateFactory.cs" company="Henrik Jensen">
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

using Hj.ReverseProxy.Certificate.Strategy;

namespace Hj.ReverseProxy.Certificate;

internal sealed class CertificateFactory(
  ILogger<CertificateFactory> logger,
  IEnumerable<ICertificateStrategy> strategies)
{
  public X509Certificate2 CreateCa(AsymmetricAlgorithm key, string subjectName)
  {
    var strategy = GetStrategy(key);

    var request = strategy.CreateCertificateRequest(key, new(subjectName));
    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
    request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
    request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

    var utcNow = DateTimeOffset.UtcNow;
    var validFrom = utcNow.AddDays(-1);
    var validTo = utcNow.AddYears(10);

    var ca = request.CreateSelfSigned(validFrom, validTo);
    logger.LogInformation("Creating CA, subject '{SubjectName}', valid from '{ValidFrom}', valid to '{ValidTo}', thumbprint '{Thumbprint}', serial '{SerialNumber}'", subjectName, validFrom, validTo, ca.Thumbprint, ca.SerialNumber);
    return ca;
  }

  public X509Certificate2 CreateCertificate(AsymmetricAlgorithm key, X509Certificate2 ca, string subjectName, Action<SubjectAlternativeNameBuilder> configureSan)
  {
    var strategy = GetStrategy(key);

    var request = strategy.CreateCertificateRequest(key, new(subjectName));

    var sanBuilder = new SubjectAlternativeNameBuilder();
    configureSan(sanBuilder);
    request.CertificateExtensions.Add(sanBuilder.Build());

    request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new("1.3.6.1.5.5.7.3.1")], true));
    request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

    var caSignatureGenerator = GetStrategy(ca).GetSignatureGenerator(ca);

    var utcNow = DateTimeOffset.UtcNow;
    var validFrom = utcNow.AddDays(-1);
    var validTo = utcNow.AddYears(1);

    var serialNumber = new byte[16];
    RandomNumberGenerator.Fill(serialNumber);

    logger.LogInformation("Creating certificate, subject '{SubjectName}', CA thumbprint '{Thumbprint}', CA serial '{SerialNumber}'", subjectName, ca.Thumbprint, ca.SerialNumber);
    var certificate = request.Create(
        ca.IssuerName,
        caSignatureGenerator,
        validFrom,
        validTo,
        serialNumber);

    var certificateWithKey = strategy.CopyWithPrivateKey(certificate, key);

    var pfxBytes = certificateWithKey.Export(X509ContentType.Pkcs12);
#pragma warning disable SYSLIB0057 // Type or member is obsolete
    var pfx = Environment.OSVersion.Platform == PlatformID.Win32NT
      ? new X509Certificate2(pfxBytes, (string?)null, X509KeyStorageFlags.Exportable)
      : new X509Certificate2(pfxBytes, (string?)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
    return pfx;
  }

  public AsymmetricAlgorithm CreateKey(string algorithmOid) => GetStrategy(algorithmOid).CreateKey();

  public string ExportPrivateKeyPem(X509Certificate2 certificate)
    => GetStrategy(certificate).ExportPrivateKeyPem(certificate);

  private ICertificateStrategy GetStrategy(X509Certificate2 certificate) => GetStrategy(certificate.GetKeyAlgorithm());

  private ICertificateStrategy GetStrategy(string publicKeyAlgOid)
    => strategies.FirstOrDefault(x => x.CanHandle(publicKeyAlgOid)) ?? throw new NotSupportedException($"Algorithm oid '{publicKeyAlgOid}' is not supported");

  private ICertificateStrategy GetStrategy(AsymmetricAlgorithm key)
  {
    var keyType = key.GetType();
    return strategies.FirstOrDefault(x => x.CanHandle(keyType)) ?? throw new NotSupportedException($"Algorithm type '{keyType.Name}' is not supported");
  }
}
