namespace Sample.Sdk.Data.Options
{
    public class InComingMessageSignatureOptions
    {
        public const string Identifier = "Security:InComingMessageSignature";

        public List<InComingMessageSignatureOption> Options { get; set; } = new List<InComingMessageSignatureOption>();
    }
}
