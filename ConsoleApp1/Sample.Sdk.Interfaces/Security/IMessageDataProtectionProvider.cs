using Sample.Sdk.Data;

namespace Sample.Sdk.Interface.Security
{
    public interface IMessageDataProtectionProvider
    {
        Task<List<KeyValuePair<SymetricResult, SymetricResult>>> Protect(
            List<KeyValuePair<SymetricResult, SymetricResult>> data,
            byte[] aad,
            CancellationToken token);

        Task<List<KeyValuePair<string, string>>>
            UnProtect(List<KeyValuePair<SymetricResult, SymetricResult>> data,
            byte[] aad,
            CancellationToken token);

    }
}