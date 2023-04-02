using Sample.Sdk.Data.Registration;

namespace Sample.Sdk.Interface.Registration
{
    public interface IServiceRegistrationProvider
    {
        Task<(bool isValid, ServiceRegistration reg)> GetServiceRegistration(string appId, CancellationToken token);
    }
}