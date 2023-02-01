using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Persistance
{
    public abstract class PersistenceObject<T, TC, TS> : BaseObject<T> where T : BaseObject<T> where TC : DbContext where TS : Entity
    {
        protected abstract TS GetInMemoryEntity();
        protected abstract void AttachEntity(TS entity);
        private readonly IEntityContext<TC, TS> _entityContext;
        public readonly ILogger? _logger;

        public PersistenceObject(ILogger logger, IEntityContext<TC, TS> entityContext)
        {
            logger.LogInformation($"Base object is entity context {entityContext == null}");
            _entityContext = entityContext;
            _logger = logger;
        }
        protected override void Log() => _logger?.LogInformation("Hello World");
        protected override async Task Save(Action notifier = null)
        {
            var employee = GetInMemoryEntity();
            employee.Id = Guid.NewGuid();
            _entityContext.Add(employee);
            await _entityContext.Save();
            if (notifier != null)
                notifier.Invoke();
        }

        protected async Task<TS> GetEntityById(Guid id) => await _entityContext.GetById(id);

    }
}
