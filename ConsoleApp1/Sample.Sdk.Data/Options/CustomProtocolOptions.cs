using Sample.Sdk.Data.Constants;

namespace Sample.Sdk.Data.Options
{
    public class CustomProtocolOptions
    {
        public static string Identifier = Environment.GetEnvironmentVariable(ConfigVar.CUSTOM_PROTOCOL)!;
        public string WellknownSecurityEndpoint { get; set; } = string.Empty;
        public string DecryptEndpoint { get; set; } = string.Empty;
        public string AcknowledgementEndpoint { get; set; } = string.Empty;
        public int SessionDurationInSeconds { get; set; }
        public string CryptoEndpoint { get; set; } = string.Empty;
        public string SignDataKeyId { get; set; } = string.Empty;
    }
}
