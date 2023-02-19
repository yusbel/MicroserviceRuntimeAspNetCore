using Sample.Sdk;
using Sample.Sdk.Msg;
using System.Collections.Concurrent;

namespace Sample.Sdk.InMemory
{
    /// <summary>
    /// Use factory method
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InMemoryMessageBus<T> : IInMemoryMessageBus<T> where T : class
    {
        //Each subscriber have a ienumerable of messages
        private ConcurrentDictionary<string, BlockingCollection<T>> messages = new ConcurrentDictionary<string, BlockingCollection<T>>();
        public void Add(string msgKey, T msg)
        {
            Guard.ThrowWhenNull(msgKey, msg);
            var newMsgs = new BlockingCollection<T>
            {
                msg
            };
            if (messages.TryGetValue(msgKey, out var msgs))
            {
                msgs.ToList().ForEach(message => newMsgs.Add(message));
                messages.TryUpdate(msgKey, newMsgs, msgs);
                return;
            }
            messages.TryAdd(msgKey, newMsgs);
        }
        public IEnumerable<T> GetAndRemove(string msgKey)
        {
            return messages.TryRemove(msgKey, out var msgs) ? msgs != null ? msgs.GetConsumingEnumerable() //null found 
                                                                           : Enumerable.Empty<T>()
                                                            : Enumerable.Empty<T>();//no found
        }
        public bool TryGetAndRemove(string msgKey, out IEnumerable<T> result)
        {
            result = GetAndRemove(msgKey).AsEnumerable();
            return result == Enumerable.Empty<T>() ? false : true;
        }

        public bool TryGet(string key, out BlockingCollection<T> result) 
        {
            if (messages.TryGetValue(key, out result)) 
            {
                return true;
            }
            return false;
        }
    }
}