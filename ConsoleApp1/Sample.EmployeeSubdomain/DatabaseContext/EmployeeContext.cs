using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.EmployeeSubdomain.Entities;
using Sample.Sdk.Core.DatabaseContext;
using Sample.Sdk.Data.Options;

namespace Sample.EmployeeSubdomain.DatabaseContext
{
    public class EmployeeContext : ServiceDbContext
    {
        private readonly ILogger<EmployeeContext> _logger;
        private readonly string _connStr = string.Empty;

        public EmployeeContext(IOptions<DatabaseSettingOptions> dbOptions,
            ILoggerFactory loggerFactory,
            DbContextOptions option) : base(option, dbOptions, loggerFactory.CreateLogger<ServiceDbContext>())
        {
            _connStr = dbOptions.Value.ConnectionString;
            _logger = loggerFactory.CreateLogger<EmployeeContext>();
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
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<EmployeeEntity> Employees { get; set; }
        
    }
}
