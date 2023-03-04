using System.Collections.Concurrent;

namespace SampleSdkRuntime.Providers.Data
{
    public class VerificationRepairResult 
    {
        public bool Success { get; init; }
        public VerificationType VerificationType { get; init; }
        public ConcurrentDictionary<string, string>? Parameters { get; init; }
    }
}
