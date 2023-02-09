using Sample.Sdk.Msg.Interfaces;

namespace Sample.Messaging
{
    public interface IMessageDispatcher
    {
        Task<bool> Dispatch(string key, IExternalMessage message);
        Task<bool> Dispatch(string key, string message);
    }
}