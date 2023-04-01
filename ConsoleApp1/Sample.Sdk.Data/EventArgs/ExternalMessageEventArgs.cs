using Sample.Sdk.Data.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.EventArgs
{
    public class ExternalMessageEventArgs : System.EventArgs
    {
        public ExternalMessage ExternalMessage { get; set; } = default;
    }
}
