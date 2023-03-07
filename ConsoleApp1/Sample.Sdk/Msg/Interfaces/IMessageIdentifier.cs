using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Interfaces
{
    /// <summary>
    /// Define the identifier used to identify duplicate messages
    /// </summary>
    public interface IMessageIdentifier
    {
        /// <summary>
        /// Used by inmemory deduplicate collection
        /// </summary>
        public string Id { get; set; }
    }
}
