using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.EmployeeSubdomain.Service.Entities;
using Sample.EmployeeSubdomain.Service.Settings;
using Sample.Sdk.EntityModel;

namespace Sample.EmployeeSubdomain.Service.DatabaseContext
{
    public class EmployeeContext : DbContext
    {
        private readonly ILogger<EmployeeContext> _logger;
        private readonly string _connStr = string.Empty;

        //For design time
        public EmployeeContext(DbContextOptions options, string connStr) : base(options)
        {
            _connStr = connStr;
        }
        public EmployeeContext(IOptions<DatabaseSettingOptions> dbOptions,
            ILoggerFactory loggerFactory,
            DbContextOptions option) : base(option)
        {
            (_connStr, _logger) = (dbOptions.Value.ConnectionString, loggerFactory.CreateLogger<EmployeeContext>());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connStr, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
            optionsBuilder.EnableDetailedErrors();
            base.OnConfiguring(optionsBuilder);
        }
        public DbSet<EmployeeEntity> Employees { get; set; }
        public DbSet<ExternalEventEntity> ExternalEvents { get; set; }
        public DbSet<TransactionEntity> Transactions { get; set; }
    }
}
