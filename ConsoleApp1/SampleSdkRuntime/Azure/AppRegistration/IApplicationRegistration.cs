using Microsoft.Graph;

namespace SampleSdkRuntime.Azure.AppRegistration
{
    public interface IApplicationRegistration
    {
        Task<(bool isValid, Application? app)>
            VerifyApplicationSettings(
            string appIdentifier,
            CancellationToken cancellationToken,
            string prefix = "Service");
        
        Task<(bool wasSuccess, Application? app)> GetOrCreate(string appIdentifier, CancellationToken token, string prefix = "Service");

        Task<bool> DeleteAll(string appIdentifier,
                                            CancellationToken cancellationToken,
                                            string prefix = "Service");
    }
}