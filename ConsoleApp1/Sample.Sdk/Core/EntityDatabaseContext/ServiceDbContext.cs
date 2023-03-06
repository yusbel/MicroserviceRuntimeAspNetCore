using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.EntityModel;

namespace Sample.Sdk.Core.EntityDatabaseContext
{
    public class ServiceDbContext : DbContext
    {
        private readonly IOptions<DatabaseSettingOptions> _dbOptions;
        private readonly ILogger<ServiceDbContext> _logger;

        public ServiceDbContext(DbContextOptions options,
            IOptions<DatabaseSettingOptions> dbOptions,
            ILogger<ServiceDbContext> logger) :base(options)
        {
            _dbOptions = dbOptions;
            _logger = logger;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_dbOptions.Value.ConnectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);//Figuere out sql server error codes and exceptions
            });
            optionsBuilder.EnableDetailedErrors();
            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        /// Refactor into a DbContext by the SDK
        /// </summary>
        public DbSet<TransactionEntity> Transactions { get; set; }

        /// <summary>
        /// Refactor into a dbcontext by the sdk
        /// </summary>
        public DbSet<InComingEventEntity> InComingEvents { get; set; }

        /// <summary>
        /// Table to keep copy of event send via message
        /// </summary>
        public DbSet<OutgoingEventEntity> OutgoingEvents { get; set; }
    }
}
