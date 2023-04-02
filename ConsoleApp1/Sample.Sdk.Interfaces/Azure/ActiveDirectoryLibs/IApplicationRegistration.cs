using Microsoft.Graph.Models;
using Sample.Sdk.Data.Azure;
using Sample.Sdk.Data.Enums;

namespace Sample.Sdk.Interface.Azure.ActiveDirectoryLibs
{
    public interface IApplicationRegistration
    {
        Task<(bool isValid, Enums.Setup reason)>
            VerifyApplicationSettings(
            string appIdentifier,
            CancellationToken cancellationToken);
        Task<bool> DeleteAll(string appIdentifier,
                                            CancellationToken cancellationToken);

        Task<AppRegistrationSetup>
            GetApplicationDetails(string appId, CancellationToken token);

        Task<AppRegistrationSetup>
            DeleteAndCreate(string appIdentifier, CancellationToken token);

        Task<Application?> GetApplication(string appId, CancellationToken token);
    }
}