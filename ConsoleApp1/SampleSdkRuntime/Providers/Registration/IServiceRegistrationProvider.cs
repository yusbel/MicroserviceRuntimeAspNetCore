using SampleSdkRuntime.Data;

namespace SampleSdkRuntime.Providers.Registration
{
    internal interface IServiceRegistrationProvider
    {
        Task<ServiceRegistration> GetServiceRegistration(string appId, CancellationToken token);
    }
}