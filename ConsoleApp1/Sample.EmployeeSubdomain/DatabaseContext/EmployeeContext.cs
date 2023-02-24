using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.EmployeeSubdomain.Entities;
using Sample.EmployeeSubdomain.Settings;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.EntityModel;

namespace Sample.EmployeeSubdomain.DatabaseContext
{
    public class EmployeeContext : ServiceDbContext
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExternalEventEntity>().HasKey(e => e.Id);
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<EmployeeEntity> Employees { get; set; }
        
    }
}
