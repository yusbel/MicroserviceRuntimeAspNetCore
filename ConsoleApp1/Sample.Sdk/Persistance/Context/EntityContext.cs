using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sample.Sdk.EntityModel;
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
        public EntityContext(ILoggerFactory logger, TC dbContext)
        {           
            _logger = logger.CreateLogger("EntityContext");
            _dbContext = dbContext;
        }

        public void Add(T add)
        {
            _dbContext.Add(add);
        }

        public async Task Delete(Guid id)
        {
            _dbContext.Set<T>().Remove(await GetById(id));
        }

        public async Task<IQueryable<T>> GetAll()
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () => await Task.Run(()=> _dbContext.Set<T>().AsNoTracking().AsQueryable()));
            return result;
        }

        public async Task<T> GetById(Guid id)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () => await _dbContext.Set<T>().SingleOrDefaultAsync(item => item.Id == id));
        }

        public async Task<bool> Save()
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            var transaction = new TransactionEntity() { Id = Guid.NewGuid().ToString() };
            _dbContext.Add(transaction);
            await strategy.ExecuteInTransaction(
                _dbContext,
                operation: async (context) => 
                {
                    await context.SaveChangesAsync(acceptAllChangesOnSuccess: false);
                },
                verifySucceeded: (context) =>
                {
                    return context.Set<TransactionEntity>().AsNoTracking().Any(transaction => transaction.Id == transaction.Id);
                });
            _dbContext.ChangeTracker.AcceptAllChanges();

            try
            {
                //no concern if this operation fail, a mechanism to clean this table should be in place
                _dbContext.Set<TransactionEntity>().Remove(transaction);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception e) { }
            return await _dbContext.SaveChangesAsync() == 1;
        }

        public async Task<bool> SaveWithEvent(ExternalEventEntity eventEntity)
        {
            _dbContext.Add(eventEntity);
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteInTransaction(
                _dbContext, 
                operation: async (context) => 
                    {
                       await context.SaveChangesAsync(acceptAllChangesOnSuccess: false);
                    }, 
                verifySucceeded: (context) => 
                    {
                        return context.Set<ExternalEventEntity>().AsNoTracking().Any(ee=> ee.Id == eventEntity.Id);
                    });
            _dbContext.ChangeTracker.AcceptAllChanges();
            return true;
        }

        public void Update(T entity) => _dbContext.Entry(entity).State = EntityState.Modified;

    }
}
