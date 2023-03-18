namespace SampleSdkRuntime.Data
{
    public class ServiceCryptoKey 
    {
        public enum CryptoKeyProviderType { AzureKeyVault }
        public enum KeyType { Rsa, Ec }
        public enum KeyLenght { Size128bit, Size256bit, Size1024bit, Size2048bit, Size4096bit }
        public string ServiceKeyId { get; set; } = string.Empty;
        public string ServiceKeyName { get; set; } = string.Empty;
        public string ServiceKeyVaule { get; set; } = string.Empty;
    }
}
