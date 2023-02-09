using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.EntityModel
{
    public class Entity
    {
        private Guid _id;
        [Key]
        public Guid Id 
        {
            get 
            {
                if(_id == Guid.Empty)
                    Id = Guid.NewGuid();
                return _id;
            }
            set 
            {
                _id = value;
            }
        }
    }
}
