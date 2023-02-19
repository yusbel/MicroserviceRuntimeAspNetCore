using Microsoft.Extensions.Configuration;
using Sample.Sdk.Core.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Configuration
{
    /// {Identifier}_{Service}_{AZURE_CLIENT_ID}
    /// 
    /// $Env:AZURE_CLIENT_ID="generated-app-ID"
    /// $Env:AZURE_CLIENT_SECRET="random-password"
    /// $Env:AZURE_TENANT_ID="tenant-ID"
    internal static class MicrosoftConfigurationExtensions
    {
        /// <summary>
        /// Sort by key string, create options per service.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="identifier">service identifer that use azure client</param>
        /// <returns></returns>
        public static IEnumerable<AzurePrincipleAccount> GetAzureOptions(this IConfiguration configuration, string identifier) 
        {
            var prefix = configuration.GetValue<string>("AzureOptionPrefix");
            var defaultOption = new AzurePrincipleAccount()
            { 
                AZURE_CLIENT_ID= configuration.GetValue<string>("AZURE_CLIENT_ID"),
                AZURE_CLIENT_SECRET= configuration.GetValue<string>("AZURE_CLIENT_SECRET"),
                AZURE_TENANT_ID = configuration.GetValue<string>("AZURE_TENANT_ID")
                
            };
            var sortedList = configuration.AsEnumerable()
                .Where(kv => kv.Key.StartsWith(prefix))
                .ToList();
            sortedList.Sort((keyValue, otherKeyValue) => 
            {
                return keyValue.Key.CompareTo(otherKeyValue.Key);
            });

            sortedList.ForEach((item) => 
            {
                //create a list based on the identifier
            });
            return Enumerable.Empty<AzurePrincipleAccount>();
        }
    }
}
