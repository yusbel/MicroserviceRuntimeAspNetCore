using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg
{
    public interface IMessage
    {
        public string SerializationType { get; set; }
    }
}
