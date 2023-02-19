using Microsoft.EntityFrameworkCore;
using Sample.PayRoll.Entities;
using Sample.Sdk.EntityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Sample.PayRoll.DatabaseContext
{
    public class PayRollContext : DbContext
    {
        public PayRollContext(DbContextOptions options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=EmployeePayRollDb;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            base.OnConfiguring(optionsBuilder);
        }
        public DbSet<PayRollEntity> PayRolls { get; set; }

        public DbSet<TransactionEntity> Transactions { get; set; }
    }
}
