namespace Sample.Sdk.Interface.Caching
{
    public interface IInMemoryCollection<T> where T : class
    {
        void Add(T item);
        T Take();
        bool TryAdd(T item);
        bool TryTake(out T? item);
        bool TryTakeAll(out List<T> items);
    }
}