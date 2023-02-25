namespace Sample.Sdk.Services
{
    public interface IMessageRealtimeService
    {
        Task Compute(CancellationToken cancellationToken);
    }
}