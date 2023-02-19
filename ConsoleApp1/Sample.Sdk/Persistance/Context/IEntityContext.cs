using Microsoft.EntityFrameworkCore;
using Sample.Sdk.EntityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Sample.Sdk.Persistance.Context
{
    /// <summary>
    /// It can be change to return the query to apply filter on another layer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEntityContext<TC, T> where TC : DbContext where T : Entity
    {
        bool SaveWithEvent(ExternalEventEntity eventEntity);
        Task<IQueryable<T>> GetAll();
        Task<T> GetById(Guid id);
        Task<bool> Save();
        Task Delete(Guid id);
        void Update(T entity);
        void Add(T add);
    }
}
