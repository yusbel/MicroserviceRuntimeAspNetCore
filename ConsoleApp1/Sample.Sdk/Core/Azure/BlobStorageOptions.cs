using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Azure
{
    public class BlobStorageOptions
    {
        public const string Identifier = "ServiceRuntime:MessageSignatureBlobConnStr";
        public string EmployeeServiceMsgSignatureSecret { get; set; } = string.Empty;
        public string BlobConnStr { get; set; } = string.Empty;
    }
}
