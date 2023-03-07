using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Azure.ResourceManager.Resources;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Security.Providers.Protocol.State;

namespace Sample.Sdk.Core
{
    public abstract class BaseObject
    {
        private readonly IOptions<CustomProtocolOptions> _protocolOptions;
        private readonly ISymetricCryptoProvider _cryptoProvider;
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;
        private readonly ILogger _logger;

        public BaseObject(IOptions<CustomProtocolOptions> protocolOptions
            , ISymetricCryptoProvider cryptoProvider
            , IAsymetricCryptoProvider asymetricCryptoProvider
            , ILogger logger)
        {
            _protocolOptions = protocolOptions;
            _cryptoProvider = cryptoProvider;
            _asymetricCryptoProvider = asymetricCryptoProvider;
            _logger = logger;
        }
        protected abstract Task<bool> Save(ExternalMessage message, CancellationToken token, bool sendNotification);
        protected abstract Task Save(CancellationToken token);
        
    } 
}
