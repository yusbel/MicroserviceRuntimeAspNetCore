using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Keys;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.InMemory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sample.Sdk.Core.Security.DataProtection
{
    public static class DataProtectionBuilderExtensions
    {
        public static IDataProtectionBuilder PersistsKeyToMemoryCache(this IDataProtectionBuilder builder) 
        {
            builder.Services.AddSingleton(typeof(IConfigureOptions<KeyManagementOptions>), serviceProvider => 
            {
                var configOptions = new ConfigureOptions<KeyManagementOptions>(keyMngmtOptions =>
                {
                    var keyClient = serviceProvider.GetRequiredService<KeyClient>();
                    var logger = serviceProvider.GetRequiredService<ILogger<InMemoryDataProtectionKey>>();
                    var cache = serviceProvider.GetRequiredService<IMemoryCacheState<string, List<XElement>>>();
                    var option = serviceProvider.GetRequiredService<IOptions<AzureKeyVaultOptions>>();
                    keyMngmtOptions.XmlRepository = new InMemoryDataProtectionKey(cache, keyClient, logger, option);
                });
                return configOptions;
            });
            return builder;
        }

        private static int InMemoryCacheState<T1, T2>()
        {
            throw new NotImplementedException();
        }
    }
}
