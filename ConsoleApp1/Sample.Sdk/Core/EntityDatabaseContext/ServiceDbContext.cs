using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sample.Sdk.EntityModel;

namespace Sample.Sdk.Core.EntityDatabaseContext
{
    public class ServiceDbContext : DbContext
    {
        public ServiceDbContext(DbContextOptions options):base(options) 
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=EmployeePayRollDb;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
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
        public DbSet<ExternalEventEntity> ExternalEvents { get; set; }
    }
}
