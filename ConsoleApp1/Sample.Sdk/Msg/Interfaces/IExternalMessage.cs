using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Interfaces
{
    public interface IExternalMessage : IMessage
    {
        public string CorrelationId { get; set; }//use the entity ide saved in the db when the event is produced by a db entity 
        public string Content { get; set; }
    }
}
