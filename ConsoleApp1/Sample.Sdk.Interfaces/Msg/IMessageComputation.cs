using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk.Data.Entities;
using System.Linq.Expressions;

namespace Sample.Sdk.Interface.Msg
{
    /// <summary>
    /// Interface that define the computation for message between services
    /// </summary>
    public interface IMessageComputation
    {
        Task<bool> SaveInComingEventEntity(
                            IServiceScope serviceScope,
                            InComingEventEntity eventEntity,
                            CancellationToken cancellationToken);
        Task<IEnumerable<InComingEventEntity>> GetInComingEventsAsync(
                                            IServiceScope serviceScope,
                                            Expression<Func<InComingEventEntity, bool>> condition,
                                            CancellationToken cancellationToken);

        Task<bool> UpdateEventStatus(
            IServiceScope serviceScope,
            InComingEventEntity eventEntity,
            Expression<Func<InComingEventEntity, bool>> propertyToUpdate,
            CancellationToken cancellationToken);

    }
}
