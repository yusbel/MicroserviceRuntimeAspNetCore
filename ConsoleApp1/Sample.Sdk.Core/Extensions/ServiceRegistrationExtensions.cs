using Sample.Sdk.Data.Azure;
using Sample.Sdk.Data.Registration;

namespace Sample.Sdk.Core.Extensions
{
    public static class ServiceRegistrationExtensions
    {
        public static ServiceRegistration Assign(this ServiceRegistration serviceReg, AppRegistrationSetup appReg)
        {
            if (appReg == null)
                return serviceReg;

            serviceReg.Credentials.Add(new ServiceCredential()
            {
                ClientId = appReg.AppCred.ClientId,
                CredentialType = ServiceCredential.CredType.SecretClient,
                ProviderType = ServiceCredential.CredProviderType.ActiveDirectory,
                ServiceSecretText = appReg.AppCred.ClientSecret,
                UserId = appReg.UserCred.Id,
                ServiceCredInfo = new List<(string, string)>()
                {
                    {
                        (appReg.AddData.Type, appReg.AddData.Value)
                    }
                }
            });
            serviceReg.WasSuccessful = appReg.WasSuccessful;

            return serviceReg;
        }
    }
}
