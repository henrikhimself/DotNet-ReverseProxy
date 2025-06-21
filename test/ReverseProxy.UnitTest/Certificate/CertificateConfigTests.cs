using Hj.ReverseProxy.Certificate;
using Microsoft.Extensions.Configuration;

namespace Hj.ReverseProxy.UnitTest.Certificate;

public class CertificateConfigTests
{
  [Fact]
  public void GetOptions_GivenMissingCaFilePath_Throws()
  {
    // arrange
    var configuration = CreateConfiguration(new()
    {
      { "SelfSignedCertificate:AlgorithmOid", CertificateConstants.RsaOid },
      { "SelfSignedCertificate:SubjectName", "CN=Test CA" },
    });

    var sut = new CertificateConfig(configuration);

    // act & assert
    Assert.ThrowsAny<InvalidOperationException>(sut.GetOptions);
  }

  [Fact]
  public void GetOptions_GivenMissingAlgorithmOid_UseDefault()
  {
    // arrange
    var configuration = CreateConfiguration(new()
    {
      { "SelfSignedCertificate:CaFilePath", "/my/ca/path" },
      { "SelfSignedCertificate:SubjectName", "CN=Test CA" },
    });

    var sut = new CertificateConfig(configuration);

    // act
    var result = sut.GetOptions();

    // Assert
    Assert.Equal(CertificateConstants.EcdsaOid, result.AlgorithmOid);
  }

  [Fact]
  public void GetOptions_GivenMissingSubjectName_UseDefault()
  {
    // arrange
    var configuration = CreateConfiguration(new()
    {
      { "SelfSignedCertificate:CaFilePath", "/my/ca/path" },
      { "SelfSignedCertificate:AlgorithmOid", CertificateConstants.RsaOid },
    });

    var sut = new CertificateConfig(configuration);

    // act
    var result = sut.GetOptions();

    // Assert
    Assert.Equal(CertificateConstants.DefaultCaSubjectName, result.SubjectName);
  }

  private static IConfiguration CreateConfiguration(Dictionary<string, string> settings)
    => new ConfigurationBuilder().AddInMemoryCollection(settings!).Build();
}
