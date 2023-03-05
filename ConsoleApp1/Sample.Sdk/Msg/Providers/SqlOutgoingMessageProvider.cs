using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Providers
{
    public class SqlOutgoingMessageProvider : OutgoingMessageProvider, IOutgoingMessageProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SqlOutgoingMessageProvider> _logger;

        public SqlOutgoingMessageProvider(IServiceProvider serviceProvider,
            ILogger<SqlOutgoingMessageProvider> logger) : base(logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        /// <summary>
        /// Sql durable storage for outgoing event entity
        /// </summary>
        /// <param name="cancellationToken">Token to cancel operation</param>
        /// <returns>IEnumerable<ExternalMessage></returns>
        public async Task<IEnumerable<ExternalMessage>> GetMessages(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            List<OutgoingEventEntity> outgoingEvents = new List<OutgoingEventEntity>();
            try
            {
                outgoingEvents = await dbContext.OutgoingEvents
                                                    .Where(e => !e.IsDeleted)
                                                    .AsNoTracking()
                                                    .ToListAsync(cancellationToken)
                                                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogCriticalException(_logger, "An error ocurred when retrieving outgoing events");
                throw;
            }
            return ConvertToExternalMessage(outgoingEvents);
        }

        /// <summary>
        /// Update sent message to deleted
        /// </summary>
        /// <param name="sentMsgs">Messages sent</param>
        /// <param name="cancellationToken">Cancellation token to stop this operation</param>
        /// <returns></returns>
        public async Task<int> UpdateSentMessages(IEnumerable<ExternalMessage> sentMsgs, CancellationToken cancellationToken) 
        {
            if(sentMsgs == null || !sentMsgs.Any()) { return 0; }
            var tasks= new List<Task>();
            foreach (var sentMsg in sentMsgs) 
            {
                var task = Task.Run(async() => 
                {
                    using var scope = _serviceProvider.CreateScope();
                    using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                    var entity = await dbContext.OutgoingEvents.FirstOrDefaultAsync(e => !e.IsDeleted, cancellationToken).ConfigureAwait(false);
                    if (entity != null) 
                    {
                        entity.IsDeleted = true;
                        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    }
                }, cancellationToken);
                _ = task.ConfigureAwait(false);
                tasks.Add(task);
            }
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception e) 
            {
                e.LogCriticalException(_logger);
            }
            return tasks.Count(task => task.IsCompletedSuccessfully);
        }

        
    }
}
