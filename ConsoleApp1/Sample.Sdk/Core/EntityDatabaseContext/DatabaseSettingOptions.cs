using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.EntityDatabaseContext
{
    public class DatabaseSettingOptions
    {
        public const string DatabaseSetting = "DbConnectionString";
        public string ConnectionString { get; set; }
    }
}
