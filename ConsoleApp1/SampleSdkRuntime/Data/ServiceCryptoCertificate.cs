namespace SampleSdkRuntime.Data
{
    public class ServiceCryptoCertificate 
    {
        public enum CryptoCerProviderType { AzureKeyVault }
        public bool HasPrivateKey { get; set; }
        public string CerLocation { get; set; } = string.Empty;
        public string CerId { get; set; } = string.Empty;
        public byte[] PublicKey { get; set; } 
        public byte[] PrivateKey { get; set; }
    }
}
