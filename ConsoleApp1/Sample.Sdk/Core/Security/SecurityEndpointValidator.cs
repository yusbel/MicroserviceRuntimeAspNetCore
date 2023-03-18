using Microsoft.Extensions.Options;
using Sample.Sdk.Core.Security.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security
{
    public class SecurityEndpointValidator : ISecurityEndpointValidator
    {
        private readonly IOptions<List<ExternalValidEndpointOptions>> _validOptions;

        public SecurityEndpointValidator(
            IOptions<List<ExternalValidEndpointOptions>> validOptions)
        {
            _validOptions = validOptions;
        }
        public bool IsWellKnownEndpointValid(string endpoint)
        {
            return _validOptions.Value.Any(e => e.WellknownSecurityEndpoint == endpoint);
        }

        public bool IsDecryptEndpointValid(string endpoint)
        {
            return _validOptions.Value.Any(e => e.DecryptEndpoint == endpoint);
        }

        public bool IsAcknowledgementValid(string endpoint)
        {
            return _validOptions.Value.Any(e => e.AcknowledgementEndpoint == endpoint);
        }
    }
}
