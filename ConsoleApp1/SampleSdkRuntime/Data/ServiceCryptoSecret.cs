namespace SampleSdkRuntime.Data
{
    public class ServiceCryptoSecret
    {
        public enum ServiceCryptoType { AzureKeyVault }
        public string SecretId { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string SecretText { get; set; } = string.Empty;
    }
}