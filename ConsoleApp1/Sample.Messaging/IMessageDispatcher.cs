using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;

namespace Sample.Messaging
{
    public interface IMessageDispatcher
    {
        Task<bool> DispatchAll();
        Task<bool> Dispatch(string subscriberKey);
    }
}