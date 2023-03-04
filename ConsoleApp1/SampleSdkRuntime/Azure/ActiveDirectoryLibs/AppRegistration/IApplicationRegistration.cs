using Microsoft.Graph;

namespace SampleSdkRuntime.Azure.ActiveDirectoryLibs.AppRegistration
{
    public interface IApplicationRegistration
    {
        Task<(bool isValid, ServiceDependecyStatus.Setup reason)>
            VerifyApplicationSettings(
            string appIdentifier,
            CancellationToken cancellationToken,
            string prefix = "Service");
        Task<bool> DeleteAll(string appIdentifier,
                                            CancellationToken cancellationToken,
                                            string prefix = "Service");

        Task<(bool wasFound, Application? app, ServicePrincipal? principal, string? clientSecret)>
            GetApplicationDetails(string appId, CancellationToken token, string prefix = "Service");

        Task<(bool wasSuccess, Application? app, ServicePrincipal? principal, string? clientSecret)>
            DeleteAndCreate(string appIdentifier, CancellationToken token, string prefix = "Service");
    }
}