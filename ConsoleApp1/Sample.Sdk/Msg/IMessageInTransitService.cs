using Sample.Sdk.Msg.Data;

namespace Sample.Sdk.Msg
{
    public interface IMessageInTransitService
    {
        ExternalMessage Bind(ExternalMessage message);
    }
}