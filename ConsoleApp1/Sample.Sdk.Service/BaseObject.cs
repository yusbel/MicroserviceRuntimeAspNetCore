using Sample.Sdk.Data.Msg;

namespace Sample.Sdk.Core
{
    public abstract class BaseObject
    {
        protected abstract Task<bool> Save(ExternalMessage message, CancellationToken token, bool sendNotification);
        protected abstract Task Save(CancellationToken token);
        
    } 
}
