namespace MultiThreading
{
    public interface IInMemoryProducerConsumerCollection<T> where T : class
    {
        public int Count { get; }
        bool TryAdd(T item);
        bool TryTake(out T? item);
        bool TryAddAndRemoveCache(T? item);
        bool TryTakeDeDuplicate(out T? item, CancellationToken token);
        bool TryTakeAllWithoutDuplicate(out IList<T> items, CancellationToken token, int pageSize = 0);
    }
}