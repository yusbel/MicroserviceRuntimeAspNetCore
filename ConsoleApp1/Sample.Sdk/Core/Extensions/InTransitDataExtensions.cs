using Microsoft.Identity.Client;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
