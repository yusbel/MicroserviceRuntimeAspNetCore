using Sample.Sdk.Data.Registration;

namespace Sample.Sdk.Interface.Registration
{
    public interface IServiceCredentialProvider
    {
        Task<IEnumerable<ServiceCredential>> CreateCredentials(string appId, CancellationToken token);
    }
}