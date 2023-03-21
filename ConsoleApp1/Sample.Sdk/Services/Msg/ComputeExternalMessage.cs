using Microsoft.Extensions.Logging;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Msg
{
    public class ComputeExternalMessage : IComputeExternalMessage
    {
        private readonly ILogger<ComputeExternalMessage> _logger;

        public ComputeExternalMessage(ILogger<ComputeExternalMessage> logger)
        {
            _logger = logger;
        }
        public Task<bool> ProcessExternalMessage(List<KeyValuePair<string, string>> externalMessage, 
                                                    CancellationToken cancellationToken)
        {
            foreach(var key in externalMessage) 
            {
                _logger.LogInformation($"Key: {key.Key}   Value: {key.Value}");
            }
            return Task.FromResult(true);
        }
    }
}
