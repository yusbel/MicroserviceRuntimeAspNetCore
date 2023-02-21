using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Msg.Data;

namespace Sample.Sdk.AspNetCore.Middleware
{
    public interface IProcessAcknowledgement
    {
        public Task<(bool, AcknowledgementResponseType)> Process(MessageProcessedAcknowledgement messageProcessedAcknowledgement);
    }
}