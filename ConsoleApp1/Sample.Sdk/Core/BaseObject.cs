using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg.Data;

namespace Sample.Sdk.Core
{
    public abstract class BaseObject<T> where T : BaseObject<T>
    {
        private IMessageBusSender _msgSender;

        public BaseObject(IMessageBusSender senderMessageDurable) => (_msgSender) = (senderMessageDurable);
        protected abstract Task Save<TE>(TE message, bool sendNotification) where TE : ExternalMessage;
        protected abstract void LogMessage();
        public void SaveAndLog(T toSave)
        {
        }
    } 
}
