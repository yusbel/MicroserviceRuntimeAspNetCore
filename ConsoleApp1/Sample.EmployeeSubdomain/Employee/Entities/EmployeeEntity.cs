using Sample.Sdk.EntityModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Employee.Entities
{
    [Table(name: "Employees")]
    public class EmployeeEntity : Entity
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
