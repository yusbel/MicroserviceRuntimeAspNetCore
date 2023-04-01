namespace Sample.Sdk.Interface.Msg
{
    public interface IMessageRealtimeService
    {
        Task Compute(CancellationToken cancellationToken);
    }
}