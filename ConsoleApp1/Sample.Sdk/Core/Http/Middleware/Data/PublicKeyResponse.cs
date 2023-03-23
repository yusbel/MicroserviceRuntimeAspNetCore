using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Http.Middleware.Data
{
    public class PublicKeyResponse
    {
        public string KeyId { get; init; } = string.Empty;
        public string KeyBase64String { get; init; } = string.Empty;
    }
}
