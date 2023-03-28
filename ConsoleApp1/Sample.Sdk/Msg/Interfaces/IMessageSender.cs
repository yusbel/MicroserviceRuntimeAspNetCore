﻿using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sample.Sdk.EntityModel.MessageHandlingReason;

namespace Sample.Sdk.Msg.Interfaces
{
    public interface IMessageSender
    {
        Task<(bool WasSent, SendFailedReason Reason)> 
            Send(CancellationToken token,
                    ExternalMessage msg,
                    Action<ExternalMessage> onSent,
                    Action<ExternalMessage, SendFailedReason?, Exception?> onError);

        Task<bool> SendMessages(Func<ExternalMessage, string> getQueue,
            IEnumerable<ExternalMessage> messages,
            Action<IEnumerable<ExternalMessage>> onSent,
            Action<IEnumerable<ExternalMessage>, Exception> onError,
            CancellationToken token);
    }
}
