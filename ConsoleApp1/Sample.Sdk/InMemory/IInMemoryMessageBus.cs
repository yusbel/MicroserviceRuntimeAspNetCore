using System.Collections.Concurrent;

namespace Sample.Sdk.InMemory
{
    public interface IInMemoryMessageBus<T> where T : class
    {
        void Add(string msgKey, T msg);
        IEnumerable<T> GetAndRemove(string msgKey);
        bool TryGetAndRemove(string msgKey, out IEnumerable<T> result);
        bool TryGet(string key, out BlockingCollection<T> result);
    }
}