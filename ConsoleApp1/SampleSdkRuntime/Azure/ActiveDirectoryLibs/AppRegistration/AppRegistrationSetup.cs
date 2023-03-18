using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.ActiveDirectoryLibs.AppRegistration
{
    internal class AppRegistrationSetup
    {
        internal bool WasSuccessful { get; init; }
        internal (string ClientId, string ClientSecret) AppCred { get; init; }
        internal (string Id, string Name) UserCred { get; init; }
        internal (string Type, string Value) AddData { get; init; }

        internal static AppRegistrationSetup Create(bool wasSuccessful,
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
