namespace Sample.Messaging
{
    public interface IInMemmoryMessage<T> where T : class
    {
        void Add(string key, T msg);
        IEnumerable<T> GetMessage(string Key);
        bool TryGetMessage(string Key, out IEnumerable<T> result);
    }
}