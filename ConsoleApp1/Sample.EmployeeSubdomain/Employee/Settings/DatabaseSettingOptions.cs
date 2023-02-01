using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Employee.Settings
{
    public class DatabaseSettingOptions
    {
        public const string DatabaseSetting = "EmployeeService:Database";
        public string ConnectionString { get; set; }
    }
}
