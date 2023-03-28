using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.EntityModel;
using Sample.Sdk.InMemory.InMemoryListMessage;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Persistance.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Persistance.Context
{
    public class EntityContext<TC, T> : IEntityContext<TC, T> where TC : DbContext where T : Entity
    {
        private readonly ILogger _logger;
        private TC _dbContext;
        internal event EventHandler<ExternalMessageEventArgs> OnSave;

        public EntityContext(
            ILoggerFactory logger, 
            TC dbContext)
        {           
            _logger = logger.CreateLogger("EntityContext");
            _dbContext = dbContext;
        }

        public void Add(T add)
        {
            _dbContext.Add(add);
        }

        public async Task Delete(Guid id, CancellationToken token)
        {
            _dbContext.Set<T>().Remove(await GetById(id, token));
        }

        public async Task<IQueryable<T>> GetAll()
        {
            var result = await Task.Run(()=> _dbContext.Set<T>().AsNoTracking().AsQueryable());
            return result;
        }

        public async Task<T?> GetById(Guid id, CancellationToken token)
        {  
            try
            {
                return await _dbContext.Set<T>().SingleOrDefaultAsync(item => item.Id == id.ToString(), token);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                throw;
            }
        }

        public async Task<bool> Save(CancellationToken token)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            var transaction = new TransactionEntity() { Id = Guid.NewGuid().ToString() };
            _dbContext.Add(transaction);
            if(token.IsCancellationRequested) 
                return false;
            
            await strategy.ExecuteInTransactionAsync(
                operation: async () => 
                {
                    await _dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, token).ConfigureAwait(false);
                },
                verifySucceeded: async() =>
                {
                    return await _dbContext.Set<TransactionEntity>().AsNoTracking().AnyAsync(transaction => transaction.Id == transaction.Id).ConfigureAwait(false);
                }).ConfigureAwait(false);

            _dbContext.ChangeTracker.AcceptAllChanges();

            _ = Task.Run(async () =>
                {
                    try
                    {
                        //no concern if this operation fail, a mechanism to clean this table should be in place
                        _dbContext.Set<TransactionEntity>().Remove(transaction);
                        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch (Exception e) 
                    {
                        e.LogException(_logger.LogCritical);
                    }
                });
            return true;
        }

        public async Task<bool> SaveWithEvent(OutgoingEventEntity eventEntity, CancellationToken token)
        {
            _dbContext.Add(eventEntity);
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            
            if (token.IsCancellationRequested)
                return false;
           
            await strategy.ExecuteInTransactionAsync(
                operation: async () =>
                    {
                        await _dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false).ConfigureAwait(false);
                    },
                verifySucceeded: async () =>
                    {
                        return await _dbContext.Set<OutgoingEventEntity>().AsNoTracking().AnyAsync(ee => ee.Id == eventEntity.Id).ConfigureAwait(false);
                    }).ConfigureAwait(false);

            _dbContext.ChangeTracker.AcceptAllChanges();
            if (OnSave != null) 
            {
                OnSave(this, new ExternalMessageEventArgs() 
                { 
                    ExternalMessage = eventEntity!.ConvertToExternalMessage() 
                });
            }
            return true;
            }

        public void Update(T entity) => _dbContext.Entry(entity).State = EntityState.Modified;

    }
}
