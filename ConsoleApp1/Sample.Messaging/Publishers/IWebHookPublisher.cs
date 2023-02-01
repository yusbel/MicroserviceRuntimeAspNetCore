namespace Sample.Messaging.Publishers
{
    public interface IWebHookPublisher
    {
        Task<bool> Publish(string webHookUrl, string message);
    }
}