using Sample.Sdk.Core.Security.Providers.Certificate.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Certificate
{
    /// <summary>
    /// Internal use only. 
    /// </summary>
    internal class AzureKeyVaultCertificateProvider : ICertificateProvider
    {
        private readonly AzureKeyVaultCertificateProviderOptions _options;

        public AzureKeyVaultCertificateProvider(AzureKeyVaultCertificateProviderOptions options)
        {
            Guard.ThrowWhenNull(options);
            _options = options;
        }
        public X509Certificate2 GetHttpMessageCryptoCertificate(string certificateName)
        {
            throw new NotImplementedException();
        }
    }
}
