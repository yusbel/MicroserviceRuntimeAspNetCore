using static Sample.Sdk.Data.Enums.Enums;

namespace Sample.Sdk.Data.Options
{
    public class AzureKeyVaultOptions
    {
        public const string SERVICE_SECURITY_KEYVAULT_SECTION_APP_CONFIG = "ServiceSdk:Security:AzureKeyVaultOptions";
        public const string RUNTIME_KEYVAULT_SECTION_APP_CONFIG = "ServiceRuntime:AzureKeyVaultOptions";
        public HostTypeOptions Type { get; set; }
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
