using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiThreading
{
    public class NumberIdentifier : IMessageIdentifier
    {
        public string Number { get;set; }
        public string Id { get => Number.ToString(); set { Number = value; } }
    }
}
