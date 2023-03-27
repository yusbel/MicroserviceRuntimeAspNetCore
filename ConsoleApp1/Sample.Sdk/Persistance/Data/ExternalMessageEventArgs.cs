using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Persistance.Data
{
    internal class ExternalMessageEventArgs : EventArgs
    {
        internal ExternalMessage ExternalMessage { get; set; } = default;
    }
}
