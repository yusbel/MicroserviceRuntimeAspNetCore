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

        public async Task Delete(Guid id) => _dbContext.Set<T>().Remove(await GetById(id));

        public IQueryable<T> GetAll() => _dbContext.Set<T>().AsNoTracking().AsQueryable();

        public async Task<T> GetById(Guid id) => await _dbContext.Set<T>().SingleOrDefaultAsync(item => item.Id == id);

        public async Task<bool> Save() => await _dbContext.SaveChangesAsync() == 1;

        public void Update(T entity) => _dbContext.Entry(entity).State = EntityState.Modified;

    }
}
