using SampleSdkRuntime.Azure.ActiveDirectoryLibs.AppRegistration;
using SampleSdkRuntime.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Extensions
{
    internal static class ServiceRegistrationExtensions
    {
        internal static ServiceRegistration Assign(this ServiceRegistration serviceReg, AppRegistrationSetup appReg) 
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
