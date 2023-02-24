using Sample.Sdk.Core.Http.Data;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol.Http;
using Sample.Sdk.Services.Interfaces;
using Sample.Sdk.EntityModel;

namespace Sample.Sdk.Services
{
    internal class AcknowledgementService : IAcknowledgementService
    {
        private readonly ILogger<AcknowledgementService> _logger;
        private readonly ISecurePointToPoint _securePointToPoint;
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;
        private readonly IHttpClientResponseConverter _responseConverter;

        public AcknowledgementService(
            ILogger<AcknowledgementService> logger,
            ISecurePointToPoint securePointToPoint,
            IAsymetricCryptoProvider asymetricCryptoProvider,
            IHttpClientResponseConverter responseConverter)
        {
            _logger = logger;
            _securePointToPoint = securePointToPoint;
            _asymetricCryptoProvider = asymetricCryptoProvider;
            _responseConverter = responseConverter;
        }

        public async Task<bool> SendAcknowledgement(
            Func<Task<IEnumerable<InComingEventEntity>>> getIncomingEventProcessed
            , Func<InComingEventEntity, Task<bool>> updateToProcessed)
        {
            try
            {
                IEnumerable<InComingEventEntity> events;
                try
                {
                    events = await getIncomingEventProcessed();
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "An error ocurred when retrieving incoming events from database");
                    return false;
                }
                EncryptedMessageMetadata? encryptMsgMetadata;
                foreach (var inComingEvent in events)
                {
                    try
                    {
                        encryptMsgMetadata = System.Text.Json.JsonSerializer.Deserialize<EncryptedMessageMetadata>(inComingEvent.Body);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error ocurred when deserializing message from database");
                        continue;
                    }
                    if (encryptMsgMetadata == null)
                    {
                        _logger.LogCritical($"A message in the database incomming events can not be deserialized to encrypted message metadata");
                        continue;
                    }
                    (bool wasSent, EncryptionDecryptionFail reason) sentResult;
                    try
                    {
                        sentResult = await SendAcknowledgementToSender(inComingEvent.Body, encryptMsgMetadata, CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error ocurred when sending the acknowledge message to sender");
                        await Task.Delay(1000); //adding delay in case is a glitch
                        continue;
                    }
                    try
                    {
                        if (sentResult.wasSent)
                        {
                            await updateToProcessed(inComingEvent);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, "An error ocurred when updating the acknoedlegement message sent");
                    }

                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogCritical("An error occurred {}", e);
                return false;
            }
        }


        public async Task<(bool wasSent, EncryptionDecryptionFail reason)>
            SendAcknowledgementToSender(string encryptedMessage
                                        , EncryptedMessageMetadata encryptedMessageMetadata
                                        , CancellationToken token)
        {
            (bool wasCreated, PointToPointChannel? channel) =
                                    await _securePointToPoint.GetOrCreateSessionChannel(
                                            encryptedMessageMetadata.WellKnownEndpoint
                                            , token);
            if (!wasCreated || channel == null || channel.ChannelState == null)
            {
                return (false, default);
            }
            var createdOn = DateTime.Now.Ticks;
            string base64SessionId;
            try
            {
                base64SessionId = Convert.ToBase64String(channel.ChannelState.SessionIdentifierEncrypted);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error occurred when converting to base 64");
                return (false, EncryptionDecryptionFail.Base64StringConvertionFail);
            }
            var baseSign = $"{base64SessionId}:{createdOn}:{encryptedMessage}";
            var signature = await _asymetricCryptoProvider.CreateSignature(Encoding.UTF8.GetBytes(baseSign), token);
            if (!signature.wasCreated || signature.data == null)
            {
                return (false, signature.reason);
            }
            string base64Signature;
            try
            {
                base64Signature = Convert.ToBase64String(signature.data);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error has occurred");
                return (false, EncryptionDecryptionFail.Base64StringConvertionFail);
            }
            var acknowledgeMsg = new MessageProcessedAcknowledgement()
            {
                CreatedOn = createdOn,
                EncryptedExternalMessage = encryptedMessage,
                PointToPointSessionIdentifier = base64SessionId,
                Signature = base64Signature
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(acknowledgeMsg));
            Uri ackUri;
            try
            {
                ackUri = new Uri(encryptedMessageMetadata.AcknowledgementEndpoint);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when buildign the uri using the metatada endpoint passed in the message");
                return (false, EncryptionDecryptionFail.InValidAcknowledgementUri);
            }
            if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();
            (bool isValid, AcknowledgeResponse? validResp, AcknowledgeResponse? inValidResp) =
                await _responseConverter.InvokePost<AcknowledgeResponse, AcknowledgeResponse>(ackUri, content, token);

            return (isValid, default);
        }
    }
}
