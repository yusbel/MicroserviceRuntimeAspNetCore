using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Realtime.Interfaces
{
    internal interface ISendExternalMessage
    {
        void SendMessage(ExternalMessage externalMessage);
    }
}
