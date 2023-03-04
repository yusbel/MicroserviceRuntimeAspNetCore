using System.Collections.Concurrent;

namespace SampleSdkRuntime.Providers.Data
{
    public class VerificationResult 
    {
        public bool Success { get; init; }
        public VerificationType VerificationType { get; init; }
        public ConcurrentDictionary<string, string>? Parameters { get; init; }
    }
}
