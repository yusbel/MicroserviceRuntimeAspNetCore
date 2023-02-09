namespace Sample.Sdk.Services.Interfaces
{
    public interface IServiceMessageReceiver
    {
        Task Process(CancellationToken token);
    }
}