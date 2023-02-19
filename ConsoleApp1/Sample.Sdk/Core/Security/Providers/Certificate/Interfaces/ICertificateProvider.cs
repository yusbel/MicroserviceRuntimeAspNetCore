using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Certificate.Interfaces
{
    /// <summary>
    /// Internal use only
    /// </summary>
    internal interface ICertificateProvider
    {
        public X509Certificate2 GetHttpMessageCryptoCertificate(string certificateName);
    }
}
