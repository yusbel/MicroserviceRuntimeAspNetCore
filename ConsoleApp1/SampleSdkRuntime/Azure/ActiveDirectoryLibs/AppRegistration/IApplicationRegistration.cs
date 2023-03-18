﻿using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace SampleSdkRuntime.Azure.ActiveDirectoryLibs.AppRegistration
{
    internal interface IApplicationRegistration
    {
        Task<(bool isValid, ServiceDependecyStatus.Setup reason)>
            VerifyApplicationSettings(
            string appIdentifier,
            CancellationToken cancellationToken);
        Task<bool> DeleteAll(string appIdentifier,
                                            CancellationToken cancellationToken);

        Task<AppRegistrationSetup>
            GetApplicationDetails(string appId, CancellationToken token);

        Task<AppRegistrationSetup>
            DeleteAndCreate(string appIdentifier, CancellationToken token);
    }
}