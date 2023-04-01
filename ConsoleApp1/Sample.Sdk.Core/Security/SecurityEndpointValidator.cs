using Microsoft.Extensions.Options;
using Sample.Sdk.Data.Enums;
using Sample.Sdk.Data.Options;
using Sample.Sdk.Interface.Security;

namespace Sample.Sdk.Core.Security
{
    public class SecurityEndpointValidator : ISecurityEndpointValidator
    {
        private readonly IOptions<List<ExternalValidEndpointOptions>> _validOptions;
        private readonly IOptions<List<AzureMessageSettingsOptions>> _settingsOptions;

        public SecurityEndpointValidator(
            IOptions<List<ExternalValidEndpointOptions>> validOptions,
            IOptions<List<AzureMessageSettingsOptions>> settingsOptions)
        {
            _validOptions = validOptions;
            _settingsOptions = settingsOptions;
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

        public bool IsMessageEndpointValid(string messageQueue, string connectionEndpoint)
        {
            var options = _settingsOptions.Value.Where(o => o.ConfigType == Enums.AzureMessageSettingsOptionType.Receiver).ToList();
            var endpoints = options.SelectMany(o => o.MessageInTransitOptions)
                        .ToList()
                        .Where(option => option.MsgQueueEndpoint == messageQueue && option.MsgQueueEndpoint == connectionEndpoint)
                        .ToList();
            return endpoints.Any();
        }
    }
}
