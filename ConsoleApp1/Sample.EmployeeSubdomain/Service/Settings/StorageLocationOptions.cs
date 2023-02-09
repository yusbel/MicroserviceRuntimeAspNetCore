using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Service.Settings
{
    public class StorageLocationOptions
    {
        public const string StorageLocation = "EmployeeService:StorageLocation";

        public string Path { get; set; }
    }
}
