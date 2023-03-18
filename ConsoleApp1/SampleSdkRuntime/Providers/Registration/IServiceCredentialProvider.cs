using SampleSdkRuntime.Data;

namespace SampleSdkRuntime.Providers.Registration
{
    internal interface IServiceCredentialProvider
    {
        Task<IEnumerable<ServiceCredential>> CreateOrGetCredentials(string appId, CancellationToken token);
    }
}