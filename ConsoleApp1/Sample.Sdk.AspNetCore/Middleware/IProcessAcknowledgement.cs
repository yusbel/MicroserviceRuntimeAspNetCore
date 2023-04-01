using Sample.Sdk.Msg.Data;
using Sample.Sdk.Security.Providers.Protocol.State;

namespace Sample.Sdk.AspNetCore.Middleware
{
    public interface IProcessAcknowledgement
    {
        public Task<(bool, AcknowledgementResponseType)> Process(MessageProcessedAcknowledgement messageProcessedAcknowledgement);
    }
}