using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Azure
{
    public class AzureKeyVaultOptions
    {
        public const string Identifier = "ServiceSdk:Security:AzureKeyVaultOptions";
        public string VaultUri { get; set; }
        public string KeyVaultCertificateIdentifier { get; set; }
    }
}
