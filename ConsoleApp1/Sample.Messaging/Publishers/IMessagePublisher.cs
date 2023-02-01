namespace Sample.Messaging.Publishers
{
    public interface IMessagePublisher
    {
        Task Publish();
    }
}