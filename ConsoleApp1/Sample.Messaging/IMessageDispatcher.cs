using Sample.Sdk.Msg;

namespace Sample.Messaging
{
    public interface IMessageDispatcher
    {
        Task<bool> Dispatch(string key, IMessage message);
        Task<bool> Dispatch(string key, string message);
    }
}