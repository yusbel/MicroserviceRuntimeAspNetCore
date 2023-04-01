using SampleSdkRuntime.Data;

namespace SampleSdkRuntime.Providers.Registration
{
    internal interface IServiceCredentialProvider
    {
        Task<IEnumerable<ServiceCredential>> CreateCredentials(string appId, CancellationToken token);
    }
}