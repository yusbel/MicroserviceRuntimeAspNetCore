namespace Sample.Sdk.Data.Registration
{
    public class RuntimeServiceInfo
    {
        public enum FaultyType { InfoDataTypeMissMatch, TimeOutReached }
        public bool WasSuccessful { get; set; }
    }
}
