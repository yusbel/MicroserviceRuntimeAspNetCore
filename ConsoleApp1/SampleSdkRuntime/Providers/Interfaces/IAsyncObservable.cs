using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Providers.Interfaces
{
    public interface IAsyncObservable<T, TResult>
    {
        IDisposable Subscribe(IAsyncObserver<T, TResult> observer);
    }
}
