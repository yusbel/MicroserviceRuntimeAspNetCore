using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;

namespace Sample.Sdk.Persistance
{
    public abstract class PersistenceObject<T, TC, TS> : BaseObject<T> where T : BaseObject<T> where TC : DbContext where TS : Entity
    {
        protected abstract TS? GetInMemoryEntity();
        protected abstract void AttachEntity(TS entity);
        private readonly IEntityContext<TC, TS> _entityContext;
        private readonly ILogger? _logger;

        public PersistenceObject(
            ILogger logger
            , ISymetricCryptoProvider cryptoProvider
            , IAsymetricCryptoProvider asymetricCryptoProvider
            , IEntityContext<TC, TS> entityContext
            , IOptions<CustomProtocolOptions> options
            , IMessageBusSender sender) : 
            base(sender
                , options
                , cryptoProvider
                , asymetricCryptoProvider
                , logger)
        {
            Guard.ThrowWhenNull(logger, entityContext, sender);
            _entityContext = entityContext;
            _logger = logger;
        }
        protected override void LogMessage() => _logger?.LogInformation("Hello World");

        /// <summary>
        /// Save entity
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApplicationSaveException">Ocurr when retriving entity or save entity fail</exception>
        protected override async Task Save(CancellationToken token)
        {
            TS? entity;
            try
            {
                entity = GetInMemoryEntity();
            }
            catch (Exception e)
            {
                throw new ApplicationSaveException("Retrieving entity fail with exception", e);
            }
            if (entity == null || string.IsNullOrEmpty(entity.Id))
            {
                throw new ApplicationSaveException("Entity to save is null or identifier is null");
            }
            _entityContext.Add(entity);
            try
            {
                await _entityContext.Save(token);
            }
            catch (Exception e)
            {
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
        protected override async Task<bool> Save<TE>(TE message, CancellationToken token, bool sendNotification = false) 
        {
            if (message == null || string.IsNullOrEmpty(message.Key)) 
            {
                throw new ApplicationSaveException("Save was invoked with message null value or key was null");
            }
            if (token.IsCancellationRequested) 
                token.ThrowIfCancellationRequested();
            TS? entity;
            try
            {
                entity = GetInMemoryEntity();
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
            message.CorrelationId = entity.Id.ToString();
            var eventEntity = new ExternalEventEntity()
            {
                Id = Guid.NewGuid().ToString(),
                MessageKey = message.Key,
                CreationTime = DateTime.UtcNow.ToLong(),
                IsDeleted = false,
                Type = typeof(TE).FullName!,
                Version = "1.0.0"
            };
            (bool wasEncrypted, EncryptedMessageMetadata? msg) = await EncryptExternalMessage(message, token);
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
                _entityContext.SaveWithEvent(eventEntity, token);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                throw new ApplicationSaveException("Failed to save", e);
            }
        }

        protected async Task<TS> GetEntityById(Guid id, CancellationToken token) => 
            await _entityContext.GetById(id, token);

    }
}
