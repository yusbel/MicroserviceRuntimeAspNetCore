using Sample.Sdk.Data.Msg;
using System.Text;

namespace Sample.Sdk.Core.Extensions
{
    public static class InTransitDataExtensions
    {
        public static byte[] GetAadData(this InTransitData inTransitData)
        {
            return Encoding.UTF8.GetBytes($"{inTransitData.MsgQueueName}" +
                $"{inTransitData.MsgQueueEndpoint}" +
                $"{inTransitData.CertificateVaultUri}" +
                $"{inTransitData.CertificateKey}" +
                $"{inTransitData.MsgDecryptScope}" +
                $"{inTransitData.CryptoEndpoint}" +
                $"{inTransitData.SignDataKeyId}");
        }
    }
}
