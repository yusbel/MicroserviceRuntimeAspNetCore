using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.EntityModel;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Providers.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        public async Task<IEnumerable<ExternalMessage>> GetMessages(CancellationToken cancellationToken,
            Expression<Func<OutgoingEventEntity, bool>> condition)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
            var outgoingEvents = new List<OutgoingEventEntity>();
            try
            {
                outgoingEvents = await dbContext.OutgoingEvents
                                                    .Where(condition)
                                                    .AsNoTracking()
                                                    .ToListAsync(cancellationToken)
                                                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                e.LogException(_logger.LogCritical);
            }
            return ConvertToExternalMessage(outgoingEvents);
        }

        /// <summary>
        /// Update sent message to deleted
        /// </summary>
        /// <param name="sentMsgs">Messages sent</param>
        /// <param name="cancellationToken">Cancellation token to stop this operation</param>
        /// <returns></returns>
        public async Task<int> UpdateSentMessages(IEnumerable<ExternalMessage> sentMsgs, 
            CancellationToken cancellationToken, 
            Action<ExternalMessage, Exception> failSend) 
        {
            if(sentMsgs == null || !sentMsgs.Any()) { return 0; }
            var tasks= new List<Task>();
            foreach (var sentMsg in sentMsgs) 
            {
                cancellationToken.ThrowIfCancellationRequested();
                var task = Task.Run(async() => 
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                        var entity = await dbContext.OutgoingEvents.FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == sentMsg.Id, cancellationToken).ConfigureAwait(false);
                        if (entity != null)
                        {
                            entity.IsDeleted = true;
                            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (Exception e)
                    {
                        failSend?.Invoke(sentMsg, e);
                        throw;
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
                e.LogException(_logger.LogCritical);
            }
            return tasks.Count(task => task.IsCompletedSuccessfully);
        }

        /// <summary>
        /// Update messages sent
        /// </summary>
        /// <param name="sentMsgs">A enumerable of messages sent identifiers</param>
        /// <param name="cancellationToken">To cancell the current operation</param>
        /// <param name="failSent">To invoke with messages identifier that fail to be send</param>
        /// <returns></returns>
        public async Task<int> UpdateSentMessages(IEnumerable<string> sentMsgs,
            CancellationToken cancellationToken,
            Func<OutgoingEventEntity, OutgoingEventEntity> updateEntity,
            Action<string, Exception> failSent)
        {
            if(sentMsgs == null || !sentMsgs.Any()) { return 0; }
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var sentMsg in sentMsgs) 
            {
                cancellationToken.ThrowIfCancellationRequested();
                var task = Task.Run(async () => 
                {
                    using var scope = _serviceProvider.CreateScope();
                    using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
                    try 
                    {
                        var entityToUpdate = await dbContext.OutgoingEvents
                                                    .FirstOrDefaultAsync(e => !e.IsDeleted && e.Id == sentMsg, cancellationToken)
                                                    .ConfigureAwait(false);
                        if(entityToUpdate != null) 
                        {
                            updateEntity?.Invoke(entityToUpdate);
                            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch(Exception e) 
                    {
                        failSent?.Invoke(sentMsg, e);
                        exceptions.Add(e);
                    }
                }, cancellationToken);
                _ = task.ConfigureAwait(false);
                tasks.Add(task);
            }
            try 
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch(Exception e) { exceptions.Add(e); }
            exceptions.ForEach(e => e.LogException(_logger.LogCritical));
            return tasks.Count(t => t.IsCompletedSuccessfully);
        }
        
    }
}
