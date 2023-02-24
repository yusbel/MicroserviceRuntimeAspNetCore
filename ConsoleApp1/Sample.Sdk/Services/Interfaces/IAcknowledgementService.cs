using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Interfaces
{
    public interface IAcknowledgementService
    {
        Task<bool> SendAcknowledgement(
            Func<Task<IEnumerable<InComingEventEntity>>> getIncomingEventProcessed
            , Func<InComingEventEntity, Task<bool>> updateToProcessed);

        Task<(bool wasSent, EncryptionDecryptionFail reason)>
            SendAcknowledgementToSender(string encryptedMessage
                                        , EncryptedMessageMetadata encryptedMessageMetadata
                                        , CancellationToken token);
    }
}
