using Microsoft.EntityFrameworkCore;
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
        public PayRollContext(DbContextOptions options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=EmployeePayRollDb;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<PayRollEntity> PayRolls { get; set; }
        

    }
}
