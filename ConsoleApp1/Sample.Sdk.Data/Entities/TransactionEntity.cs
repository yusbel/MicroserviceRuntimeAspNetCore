using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Entities
{
    [Table("Transactions")]
    public class TransactionEntity
    {
        public string Id { get; set; }
    }
}
