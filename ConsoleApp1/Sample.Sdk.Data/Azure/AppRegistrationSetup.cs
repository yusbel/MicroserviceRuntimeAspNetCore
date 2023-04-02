using Microsoft.Graph.Models;

namespace Sample.Sdk.Data.Azure
{
    public class AppRegistrationSetup
    {
        public bool WasSuccessful { get; init; }
        public (string ClientId, string ClientSecret) AppCred { get; init; }
        public (string Id, string Name) UserCred { get; init; }
        public (string Type, string Value) AddData { get; init; }

        public static AppRegistrationSetup Create(bool wasSuccessful,
            Application? app,
            ServicePrincipal? servicePrincipal,
            string clientSecret)
        {
            if (app == null || servicePrincipal == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            return new AppRegistrationSetup()
            {
                WasSuccessful = wasSuccessful,
                UserCred = (servicePrincipal!.Id!, servicePrincipal!.AppDisplayName!),
                AppCred = (app!.AppId!, clientSecret)
            };
        }

    }
}
