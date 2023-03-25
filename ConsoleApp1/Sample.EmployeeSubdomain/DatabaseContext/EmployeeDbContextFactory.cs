using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.Sdk.Core.EntityDatabaseContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.DatabaseContext
{
    public class EmployeeDbContextFactory : IDesignTimeDbContextFactory<EmployeeContext>
    {
        public EmployeeContext CreateDbContext(string[] args)
        {
            var connStr = "Server=127.0.0.1;Database=EmployeeDb;User Id=sa;Password=67Wg3o@SqlS3rv3r;";
            var optionDbSettings = new OptionsWrapper<DatabaseSettingOptions>(new DatabaseSettingOptions() { ConnectionString = connStr });

            var optionBuilder = new DbContextOptionsBuilder<EmployeeContext>();
            var loggerFactory = LoggerFactory.Create(option => { { } });
            optionBuilder.UseSqlServer(connStr);
            return new EmployeeContext(optionDbSettings, loggerFactory, optionBuilder.Options);
        }
    }
}
