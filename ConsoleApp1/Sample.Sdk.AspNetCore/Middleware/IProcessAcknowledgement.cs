using Sample.Sdk.Msg.Data;

namespace Sample.Sdk.AspNetCore.Middleware
{
    public interface IProcessAcknowledgement
    {
        public Task<bool> Process(MessageProcessedAcknowledgement messageProcessedAcknowledgement);
    }
}