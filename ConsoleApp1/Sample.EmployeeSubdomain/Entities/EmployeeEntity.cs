using Sample.Sdk.Data.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sample.EmployeeSubdomain.Entities
{
    [Table(name: "Employees")]
    public class EmployeeEntity : Entity
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
