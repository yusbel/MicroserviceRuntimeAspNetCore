namespace Sample.Sdk.Core
{
    public interface IServiceContext
    {
        IEnumerable<byte[]> GetAesKeys();
    }
}