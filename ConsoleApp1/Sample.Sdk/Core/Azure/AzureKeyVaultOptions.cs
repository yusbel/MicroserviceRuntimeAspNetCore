using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static Sample.Sdk.Core.Enums.Enums;

namespace Sample.Sdk.Core.Azure
{
    public class AzureKeyVaultOptions
    {
        public const string SERVICE_SECURITY_KEYVAULT_SECTION_APP_CONFIG = "ServiceSdk:Security:AzureKeyVaultOptions";
        public const string RUNTIME_KEYVAULT_SECTION_APP_CONFIG = "ServiceRuntime:AzureKeyVaultOptions";
        public AzureKeyVaultOptionsType Type { get; set; }
        public string VaultUri { get; set; } = string.Empty;
        public string DefaultCertificateName { get; set; } = string.Empty;
        public List<string> CertificateNames { get; init; } = new List<string>();
        public string ResourceId { get; set; } = string.Empty;

        public static AzureKeyVaultOptions Create() 
        {
            return new AzureKeyVaultOptions();
        }
    }
}
