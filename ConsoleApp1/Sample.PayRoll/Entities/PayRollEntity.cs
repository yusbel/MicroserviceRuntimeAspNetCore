using Sample.Sdk.Data.Entities;
using Sample.Sdk.EntityModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Entities
{
    [Table(name: "PayRoll")]
    public class PayRollEntity : Entity
    {
       
        public decimal MonthlySalary { get; set; }
        public bool MailPaperRecord { get; set; }
        public string EmployeeIdentifier { get; set; }
    }
}
