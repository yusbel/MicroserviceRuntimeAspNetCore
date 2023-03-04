namespace SampleSdkRuntime.Data
{
    public readonly struct ServiceAccountInfo
    {
        public ServiceAccountInfo()
        {
        }

        public string ApplicationClientId { get; init; } = string.Empty;
        public string TenantId { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty; 
    }
}