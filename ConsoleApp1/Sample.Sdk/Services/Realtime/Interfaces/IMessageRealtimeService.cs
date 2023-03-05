namespace Sample.Sdk.Services.Realtime.Interfaces
{
    public interface IMessageRealtimeService
    {
        Task Compute(CancellationToken cancellationToken);
    }
}