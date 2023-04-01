namespace Sample.Sdk.Data
{
    public class SymetricResultDto
    {
        public string EncryptedData { get; init; } = string.Empty;
        public List<string> Key { get; init; } = new List<string>();
        public List<string> Nonce { get; init; } = new List<string>();
        public List<string> Tag { get; init; } = new List<string>();
    }


}
