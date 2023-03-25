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

namespace Sample.PayRoll.DatabaseContext
{
    public class DesignDbContextFactory : IDesignTimeDbContextFactory<PayRollContext>
    {
        public PayRollContext CreateDbContext(string[] args)
        {
            var connStr = "Server=127.0.0.1;Database=EmployeePayRollDb;User Id=sa;Password=67Wg3o@SqlS3rv3r;";
            var optionDbSettings = new OptionsWrapper<DatabaseSettingOptions>(new DatabaseSettingOptions() { ConnectionString = connStr });
            var optionBuilder = new DbContextOptionsBuilder<PayRollContext>();
            var loggerFactory = LoggerFactory.Create(configure => { });
            optionBuilder.UseSqlServer(connStr);
            return new PayRollContext(optionBuilder.Options, loggerFactory, optionDbSettings);
        }
    }
}
