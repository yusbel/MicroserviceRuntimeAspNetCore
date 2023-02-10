using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Settings
{
    public class DatabaseSettingOptions
    {
        public const string DatabaseSetting = "Employee:Database";
        public string ConnectionString { get; set; }
    }
}
