using Sample.Sdk.Data.Msg;

namespace Sample.Sdk.Interface.Msg
{
    public interface IMessageInTransitService
    {
        ExternalMessage Bind(ExternalMessage message);
    }
}