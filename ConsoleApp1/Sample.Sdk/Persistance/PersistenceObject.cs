using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core;
using Sample.Sdk.EntityModel;
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
        public readonly ILogger? _logger;

        public PersistenceObject(ILogger logger, IEntityContext<TC, TS> entityContext, IMessageBusSender sender) : base(sender)
        {
            logger.LogInformation($"Base object is entity context {entityContext == null}");
            _entityContext = entityContext;
            _logger = logger;
        }
        protected override void LogMessage() => _logger?.LogInformation("Hello World");
        protected override async Task Save(IExternalMessage message, Action notifier = null)
        {
            _entityContext.Add(GetInMemoryEntity());

            await _entityContext.SaveWithEvent(new ExternalEventEntity()
            {
                Id = Guid.NewGuid(),
                Body = System.Text.Json.JsonSerializer.Serialize(message),
                CreationTime = DateTime.UtcNow.ToLong(),
                IsDeleted = false,
                Type = typeof(ExternalEventEntity).Name,
                Version = "1.0.0"
            });
            if (notifier != null) 
            {
                notifier();
            }
        }

        protected async Task<TS> GetEntityById(Guid id) => await _entityContext.GetById(id);

    }
}
