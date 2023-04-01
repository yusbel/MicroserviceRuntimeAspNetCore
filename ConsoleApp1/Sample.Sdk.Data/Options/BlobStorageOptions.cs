namespace Sample.Sdk.Data.Options
{
    public class BlobStorageOptions
    {
        public const string Identifier = "ServiceRuntime:MessageSignatureBlobConnStr";
        public string EmployeeServiceMsgSignatureSecret { get; set; } = string.Empty;
        public string BlobConnStr { get; set; } = string.Empty;
    }
}
