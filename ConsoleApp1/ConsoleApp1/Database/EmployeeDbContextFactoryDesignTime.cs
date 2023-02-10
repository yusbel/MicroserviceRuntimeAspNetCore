using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sample.EmployeeSubdomain.DatabaseContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1.Database
{
    public class EmployeeDbContextFactoryDesignTime : IDesignTimeDbContextFactory<EmployeeContext>
    {
        private readonly string _connStr = @"Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=EmployeeDb; Integrated Security=True; Connect Timeout=30; Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        public EmployeeContext CreateDbContext(string[] args)
        { 
            var optionsBuilder = new DbContextOptionsBuilder<EmployeeContext>();
           
            return new EmployeeContext(optionsBuilder.Options, _connStr);
        }
    }
}
