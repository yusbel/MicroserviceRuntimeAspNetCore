using Sample.Sdk;
using Sample.Sdk.Msg;
using System.Collections.Concurrent;

namespace Sample.Messaging
{
    public class InMemmoryMessage<T> : IInMemmoryMessage<T> where T : class
    {
        private ConcurrentDictionary<string, IEnumerable<T>> messages = new ConcurrentDictionary<string, IEnumerable<T>>();
        public void Add(string key, T msg)
        {
            Guard.ThrowWhenNull(key, msg);
            var toAdd = new List<T>() { msg };
            if (messages.TryGetValue(key, out var msgs))
            {
                var newMsgs = msgs.ToList();
                newMsgs.AddRange(toAdd);
                messages.TryUpdate(key, newMsgs, msgs);
                return;
            }
            messages.TryAdd(key, toAdd);
        }
        public IEnumerable<T> GetMessage(string Key)
        {
            messages.TryRemove(Key, out var msg);
            return msg ?? new List<T>();
        }



        public bool TryGetMessage(string Key, out IEnumerable<T> result)
        {
            result = new List<T>();
            if (messages.TryRemove(Key, out result)) 
            {
                return true;
            }
            return false;
        }

        public static InMemmoryMessage<T> Create()
        {
            return new InMemmoryMessage<T>();
        }
    }
}