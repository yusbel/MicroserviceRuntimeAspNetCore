using SampleSdkRuntime.Providers.Data;

namespace SampleSdkRuntime.Providers.Interfaces
{
    public interface IAsyncObserver<T, TResult>
    {
        Task<TResult> OnNext(T value);
    }
}