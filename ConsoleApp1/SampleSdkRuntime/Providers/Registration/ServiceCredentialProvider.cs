using Microsoft.Graph.Education.Classes.Item.Assignments.Item.Submissions.Item.Return;
using SampleSdkRuntime.AzureAdmin.ActiveDirectoryLibs.AppRegistration;
using SampleSdkRuntime.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Providers.Registration
{
    internal class ServiceCredentialProvider : IServiceCredentialProvider
    {
        private readonly IApplicationRegistration _appReg;

        public ServiceCredentialProvider(IApplicationRegistration appReg)
        {
            _appReg = appReg;
        }

        public async Task<IEnumerable<ServiceCredential>> CreateCredentials(string appId, CancellationToken token)
        {
            AppRegistrationSetup appRegResult = null;
            
            if (appRegResult == null || !appRegResult.WasSuccessful)
            {
                appRegResult = await _appReg.DeleteAndCreate(appId, token).ConfigureAwait(false);
            }
            return new List<ServiceCredential>()
            {
                new ServiceCredential()
                {
                    ClientId = appRegResult.AppCred.ClientId,
                    CredentialType = ServiceCredential.CredType.SecretClient,
                    ProviderType = ServiceCredential.CredProviderType.ActiveDirectory,
                    ServiceSecretText = appRegResult.AppCred.ClientSecret,
                    UserId = appRegResult.UserCred.Id,
                    ServiceCredInfo = new List<(string,string)>()
                    {
                        {
                            (appRegResult.AddData.Type, appRegResult.AddData.Value)
                        }
                    }
                }
            };
        }
    }
}
