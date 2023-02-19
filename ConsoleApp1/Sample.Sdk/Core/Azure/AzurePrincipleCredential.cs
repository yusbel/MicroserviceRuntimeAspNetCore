using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Azure
{
    public class AzurePrincipleCredential : DefaultAzureCredential
    {
        public static AzurePrincipleCredential Create(List<AzurePrincipleAccount> options, IConfiguration configuration, string azureClientId) 
        {
            var option = options.FirstOrDefault(o => o.AZURE_CLIENT_ID == azureClientId);
            if (option == null)
            {
                throw new ApplicationException("Invalid client app id");
            }
            configuration.AsEnumerable().ToList().Add(new KeyValuePair<string, string>("AZURE_CLIENT_ID", option.AZURE_CLIENT_ID));
            configuration.AsEnumerable().ToList().Add(new KeyValuePair<string, string>("AZURE_CLIENT_SECRET", option.AZURE_CLIENT_SECRET));
            configuration.AsEnumerable().ToList().Add(new KeyValuePair<string, string>("AZURE_TENANT_ID", option.AZURE_TENANT_ID));
            return new AzurePrincipleCredential();
        }
    }
}
