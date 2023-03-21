using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Services.Interfaces;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Msg
{
    public class ComputeReceivedMessage : IMessageComputation
    {
        private readonly ILogger<ComputeReceivedMessage> _logger;

        public ComputeReceivedMessage(ILogger<ComputeReceivedMessage> logger)
        {
            _logger = logger;
        }
        public async Task<IEnumerable<InComingEventEntity>> GetInComingEventsAsync(
                        IServiceScope serviceScope,
                        Expression<Func<InComingEventEntity, bool>> condition,
                        CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                return await dbContext.InComingEvents
                                        .Where(condition)
                                        .ToListAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return Enumerable.Empty<InComingEventEntity>();
            }
        }

        public async Task<bool> SaveInComingEventEntity(
            IServiceScope serviceScope,
            InComingEventEntity eventEntity,
            CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                dbContext.Add(eventEntity);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return false;
            }
        }

        public async Task<bool> UpdateInComingEventEntity(
            IServiceScope serviceScope,
            InComingEventEntity eventEntity,
            CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                dbContext.Entry(eventEntity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
                return false;
            }
        }
    }
}
