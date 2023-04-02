namespace Sample.Sdk.Data.Registration
{
    public class ServiceCredential
    {
        public enum CredType { SecretClient }
        public enum CredProviderType { ActiveDirectory }
        public CredProviderType ProviderType { get; set; }
        public CredType CredentialType { get; init; }
        public string ClientId { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public bool PersistSecretOnKeyVault { get; init; } = true;
        public string ServiceSecretKeyCertificateName { get; set; } = string.Empty;
        public string AppIdentifier { get; set; } = string.Empty;
        public string ServiceSecretText { get; init; } = string.Empty;
        public IEnumerable<(string Type, string Value)> ServiceCredInfo { get; init; } = Enumerable.Empty<(string, string)>();
    }
}
