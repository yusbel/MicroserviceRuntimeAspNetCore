using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Sample.Sdk.Msg;

namespace Sample.Sdk.Core
{
    public abstract class BaseObject<T> where T : BaseObject<T>
    {
        protected abstract Task Save(Action notifier = null);
        protected abstract void Log();
        public void SaveAndLog(T toSave)
        {
            toSave.Save();
            toSave.Log();

        }
    }
}
