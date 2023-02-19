using Sample.Sdk.Core.Azure;

namespace Sample.Sdk.Core.Security.Providers.Certificate
{
    internal class AzureKeyVaultCertificateProviderOptions
    {
        internal string CertificateIdentifier { get; init; }
    }
}