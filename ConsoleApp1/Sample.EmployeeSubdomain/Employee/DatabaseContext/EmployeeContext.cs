using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.EmployeeSubdomain.Employee.Entities;
using Sample.EmployeeSubdomain.Employee.Settings;

namespace Sample.EmployeeSubdomain.Employee.DatabaseContext
{
    public class EmployeeContext : DbContext
    {
        private readonly IOptions<DatabaseSettingOptions> _dbOptions;
        private readonly ILogger<EmployeeContext> _logger;

        public EmployeeContext(IOptions<DatabaseSettingOptions> dbOptions, ILoggerFactory loggerFactory, DbContextOptions option) : base(option)
        {
            (_dbOptions, _logger) = (dbOptions, loggerFactory.CreateLogger<EmployeeContext>());
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_dbOptions.Value.ConnectionString);
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<EmployeeEntity> Employees { get; set; }
    }
}
