// <copyright file="CertificateConstants.cs" company="Henrik Jensen">
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

namespace Hj.ReverseProxy.Certificate;

internal static class CertificateConstants
{
  public const string RsaOid = "1.2.840.113549.1.1.1";

  public const string EcdsaOid = "1.2.840.10045.2.1";

  public const string DefaultCaSubjectName = "CN=ReverseProxy Root CA";

  public const string CaCrtFileName = "ca.crt.pem";

  public const string CaKeyFileName = "ca.key.pem";

  public const string CaPfxFileName = "ca.pfx";
}
