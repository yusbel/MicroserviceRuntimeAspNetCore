using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Azure;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Core.Security;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Data.Options;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace Sample.Sdk.Persistance
{
    public abstract class PersistenceObject<TContext, TState> : BaseObject where TContext : DbContext where TState : Entity
    {
        private readonly IEntityContext<TContext, TState> _entityContext;
        private readonly IMessageCryptoService _messageCryptoService;
        private readonly IMessageInTransitService _metaDataService;
        private readonly ILogger _logger;
        public abstract TState? GetEntity(); 
        protected abstract void AttachEntity(TState entity);

        public PersistenceObject(
            ILogger logger
            , ISymetricCryptoProvider cryptoProvider
            , IAsymetricCryptoProvider asymetricCryptoProvider
            , IEntityContext<TContext, TState> entityContext
            , IOptions<CustomProtocolOptions> options
            , IMessageSender sender
            , IMessageCryptoService messageCryptoService
            , IMessageInTransitService metaDataService) : 
            base(options
                , cryptoProvider
                , asymetricCryptoProvider
                , logger)
        {
            Guard.ThrowWhenNull(logger, entityContext, sender);
            _entityContext = entityContext;
            _messageCryptoService = messageCryptoService;
            _metaDataService = metaDataService;
            _logger = logger;
        }

        /// <summary>
        /// Save entity
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApplicationSaveException">Ocurr when retriving entity or save entity fail</exception>
        protected override async Task Save(CancellationToken token)
        {
            TState? entity;
            try
            {
                entity = GetEntity();
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                throw new ApplicationSaveException("Retrieving entity fail with exception", e);
            }
            if (entity == null || string.IsNullOrEmpty(entity.Id))
            {
                throw new ApplicationSaveException("Entity to save is null or identifier is null");
            }
            _entityContext.Add(entity);
            try
            {
                await _entityContext.Save(token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                throw new ApplicationSaveException("Saving entity fail", e);
            }
        }
        /// <summary>
        /// Save entity and message in a transaction
        /// </summary>
        /// <typeparam name="TE"></typeparam>
        /// <param name="message"></param>
        /// <param name="sendNotification"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationSaveException">Ocurred when message is null, entity is null, encryption, deserialization or save fail</exception>
        protected override async Task<bool> Save(ExternalMessage message, CancellationToken token, bool sendNotification = false) 
        {
            if (message == null) 
            {
                throw new ApplicationSaveException("Save was invoked with message null value or key was null");
            }
            token.ThrowIfCancellationRequested();
            TState? entity;
            try
            {
                entity = GetEntity();
            }
            catch (Exception e)
            {
                throw new ApplicationSaveException("Retrieving entity to save raised an exception", e);
            }
            if(entity == null || string.IsNullOrEmpty(entity.Id)) 
            {
                throw new ApplicationSaveException("Entity to save is null or identifier is null");
            }
            _entityContext.Add(entity);
            try
            {
                message.EntityId = entity.Id;
                message.CorrelationId = entity.Id;
                message.Content = JsonSerializer.Serialize(entity);
                message = _metaDataService.Bind(message);
            }
            catch (Exception)
            {
                throw;
            }
            var eventEntity = message.ConvertToOutgoingEventEntity(Guid.NewGuid().ToString());

            (bool wasEncrypted, EncryptedMessage? msg) = await _messageCryptoService.EncryptExternalMessage(message, token).ConfigureAwait(false);
            if(!wasEncrypted || msg == null) 
            {
                throw new ApplicationSaveException("Encrypted the message fail, verify exceptions logged");
            }
            try 
            {
                eventEntity.Body = JsonSerializer.Serialize(msg);
            }
            catch (Exception e) 
            {
                throw new ApplicationSaveException("Serializing encrypted message fail", e);
            }
            try
            {
                await _entityContext.SaveWithEvent(eventEntity, token).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                throw new ApplicationSaveException("Failed to save", e);
            }
        }

        public async Task Load(Guid employeeId, CancellationToken cancellationToken) 
        {
            try 
            {
                var entity = await _entityContext.GetById(employeeId, cancellationToken).ConfigureAwait(false);
                AttachEntity(entity!);
            }
            catch(Exception e) 
            {
                e.LogException(_logger.LogCritical);
            }
        }

    }
}
