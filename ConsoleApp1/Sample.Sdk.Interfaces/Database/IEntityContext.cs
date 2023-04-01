using Microsoft.EntityFrameworkCore;
using Sample.Sdk.Data.Entities;

namespace Sample.Sdk.Interface.Database
{
    /// <summary>
    /// It can be change to return the query to apply filter on another layer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntityContext<TC, T> where TC : DbContext where T : Entity
    {
        Task<bool> SaveWithEvent(OutgoingEventEntity eventEntity, CancellationToken token);
        Task<IQueryable<T>> GetAll();
        Task<T?> GetById(Guid id, CancellationToken token);
        Task<bool> Save(CancellationToken token);
        Task Delete(Guid id, CancellationToken token);
        void Update(T entity);
        void Add(T add);
    }
}
