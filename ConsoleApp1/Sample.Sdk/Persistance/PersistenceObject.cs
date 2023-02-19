using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core;
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
using System.Threading.Tasks;
using System.Transactions;

namespace Sample.Sdk.Persistance
{
    public abstract class PersistenceObject<T, TC, TS> : BaseObject<T> where T : BaseObject<T> where TC : DbContext where TS : Entity
    {
        protected abstract TS GetInMemoryEntity();
        protected abstract void AttachEntity(TS entity);
        private readonly IEntityContext<TC, TS> _entityContext;
        private readonly ILogger? _logger;

        public PersistenceObject(ILogger logger
            , ISymetricCryptoProvider cryptoProvider
            , IAsymetricCryptoProvider asymetricCryptoProvider
            , IEntityContext<TC, TS> entityContext
            , IOptions<CustomProtocolOptions> options
            , IMessageBusSender sender) : base(sender, options, cryptoProvider, asymetricCryptoProvider)
        {
            Guard.ThrowWhenNull(logger, entityContext, sender);
            _entityContext = entityContext;
            _logger = logger;
        }
        protected override void LogMessage() => _logger?.LogInformation("Hello World");
        protected override async Task Save<TE>(TE message, bool sendNotification = true) 
        {
            var entity = GetInMemoryEntity();
            _entityContext.Add(entity);
            message.CorrelationId = entity.Id.ToString();
            var eventEntity = new ExternalEventEntity()
            {
                Id = Guid.NewGuid().ToString(),
                Body = System.Text.Json.JsonSerializer.Serialize(await EncryptExternalMessage(message)),
                CreationTime = DateTime.UtcNow.ToLong(),
                IsDeleted = false,
                Type = typeof(TE).FullName,
                Version = "1.0.0"
            };
            _entityContext.SaveWithEvent(eventEntity);
            //TODO: Would use event grid to propagate message
            if(!sendNotification) 
            {
                return;
            }
            try
            {
                //Notifications
                Task.Run(() => MessageNotifier<TE>.Notify(message));
            }
            catch (Exception e)
            {
                _logger?.LogError("An error occurred when sending notification for message {} with exception {}", message, e);
            }
        }

        protected async Task<TS> GetEntityById(Guid id) => await _entityContext.GetById(id);

    }
}
