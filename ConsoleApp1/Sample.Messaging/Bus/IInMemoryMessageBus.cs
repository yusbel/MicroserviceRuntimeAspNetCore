namespace Sample.Messaging.Bus
{
    public interface IInMemoryMessageBus<T> where T : class
    {
        void Add(string msgKey, T msg);
        IEnumerable<T> GetAndRemove(string msgKey);
        bool TryGetAndRemove(string msgKey, out IEnumerable<T> result);
    }
}