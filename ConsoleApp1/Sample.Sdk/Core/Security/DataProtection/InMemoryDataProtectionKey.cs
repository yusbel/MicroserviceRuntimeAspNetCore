using Azure.Security.KeyVault.Keys;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.InMemory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Sample.Sdk.Core.Security.DataProtection
{
    public class InMemoryDataProtectionKey : IXmlRepository
    {
        private readonly IMemoryCacheState<string, List<XElement>> _memoryCacheState;
        private readonly KeyClient _keyClient;
        private readonly ILogger<InMemoryDataProtectionKey> _logger;
        private readonly IOptions<AzureKeyVaultOptions> _keyVaultOptions;
        private readonly string _keyDataProtection;
        public InMemoryDataProtectionKey(IMemoryCacheState<string, List<XElement>> memoryCacheState, 
            KeyClient keyClient,
            ILogger<InMemoryDataProtectionKey> logger,
            IOptions<AzureKeyVaultOptions> keyVaultOptions) 
        {
            _memoryCacheState = memoryCacheState;
            _keyClient = keyClient;
            _logger = logger;
            _keyVaultOptions = keyVaultOptions;
            _keyDataProtection = $"{Environment.GetEnvironmentVariable("SERVICE_INSTANCE_ID") ?? Environment.MachineName}-MessageKeyDataProtection";
        }
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            try
            {
                var key = _memoryCacheState.Cache.Get<List<XElement>>(_keyDataProtection);
                if (key == null) 
                {
                    return new List<XElement>();
                }
                return key;
            }
            catch (Exception)
            { 
                throw;
            }
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            _memoryCacheState.Cache.Set(_keyDataProtection, new List<XElement> { element });
        }
    }
}
