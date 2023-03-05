namespace Sample.Sdk.Msg.Interfaces
{
    public interface IInMemoryCollection<TList, T> where T : class where TList : class
    {
        void Add(T item);
        T Take();
        bool TryAdd(T item);
        bool TryTake(out T? item);
        bool TryTakeAll(out List<T> items);
    }
}