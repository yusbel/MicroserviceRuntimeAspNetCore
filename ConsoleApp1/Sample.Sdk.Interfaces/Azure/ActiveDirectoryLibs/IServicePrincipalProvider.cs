﻿using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Sample.Sdk.Interface.Azure.ActiveDirectoryLibs
{
    public interface IServicePrincipalProvider
    {
        Task<ServicePrincipal?> Create(string identifier, CancellationToken cancellationToken);
        Task<bool> DeleteServicePricipal(string identifier, CancellationToken cancellationToken);
        Task<ServicePrincipal?> GetServicePrincipal(string identifier, CancellationToken cancellationToken);
    }
}