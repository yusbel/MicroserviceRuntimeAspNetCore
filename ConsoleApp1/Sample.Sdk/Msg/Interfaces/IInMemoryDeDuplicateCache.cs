

namespace Sample.Sdk.Msg.Interfaces
{
    public interface IInMemoryDeDuplicateCache<T> where T : class
    {
        public int Count { get; }
        bool TryAdd(T item);
        bool TryTakeAll(out List<T?> items);
        bool TryTake(out T? item);
        bool TryAddAndRemoveCache(T? item);
        bool TryTakeDeDuplicate(out T? item, CancellationToken token);
        bool TryTakeAllWithoutDuplicate(out IList<T> items, CancellationToken token, int pageSize = 0);
    }
}