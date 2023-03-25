using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.PayRoll.Entities;
using Sample.Sdk.Core.EntityDatabaseContext;
using Sample.Sdk.EntityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Sample.PayRoll.DatabaseContext
{
    /// <summary>
    /// PayRool
    /// </summary>
    public class PayRollContext : ServiceDbContext
    {
        private readonly IOptions<DatabaseSettingOptions> _dbOptions;

        public PayRollContext(DbContextOptions options,
            ILoggerFactory loggerFactory,
            IOptions<DatabaseSettingOptions> dbOptions) : base(options, 
                dbOptions, 
                loggerFactory.CreateLogger<ServiceDbContext>()) 
        {
            _dbOptions = dbOptions;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_dbOptions.Value.ConnectionString, sqlOptions => 
            {
                sqlOptions.EnableRetryOnFailure(10, TimeSpan.FromSeconds(30), null);
            });
            optionsBuilder.EnableDetailedErrors();
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<PayRollEntity> PayRolls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PayRollEntity>().HasKey(payRoll=> payRoll.Id).IsClustered();
            modelBuilder.Entity<PayRollEntity>().Property("MonthlySalary").HasColumnType("decimal");
            
            base.OnModelCreating(modelBuilder);
        }


    }
}
