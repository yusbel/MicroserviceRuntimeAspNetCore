using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Interfaces
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
