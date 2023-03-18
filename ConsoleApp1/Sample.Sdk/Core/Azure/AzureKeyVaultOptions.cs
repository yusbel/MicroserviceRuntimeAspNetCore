using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sample.Sdk.Core.Azure
{
    public class AzureKeyVaultOptions
    {
        public const string SERVICE_SECURITY_KEYVAULT_SECTION = "ServiceSdk:Security:AzureKeyVaultOptions";
        public string VaultUri { get; set; } = string.Empty;
        public string KeyVaultCertificateIdentifier { get; set; } = string.Empty;
        public string MessageEncryptionKeyIdentifier { get; set; } = string.Empty;
        public string KeyVaultResourceId { get; set; } = string.Empty;
        public string MessageEncryptionKeyName { get; set; } = string.Empty;

        public static AzureKeyVaultOptions Create() 
        {
            return new AzureKeyVaultOptions();
        }
    }
}
