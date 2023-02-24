using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Services.Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Interfaces
{
    /// <summary>
    /// Interface that define the computation for message between services
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageComputation<T> : IExternalMessageConverter<T> where T : class, IMessageIdentifier
    {
        Task<bool> SaveInComingEventEntity(InComingEventEntity eventEntity, CancellationToken cancellationToken);
        Task<IEnumerable<InComingEventEntity>> GetInComingEventsAsync(
                                            Func<InComingEventEntity, bool> condition,
                                            CancellationToken cancellationToken);
        Task<bool> ProcessExternalMessage(ExternalMessage externalMessage, CancellationToken cancellationToken);

        Task<bool> UpdateInComingEventEntity(InComingEventEntity eventEntity, CancellationToken cancellationToken);

    }
}
