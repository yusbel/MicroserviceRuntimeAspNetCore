using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    struct Point
    {
        public int x;
        public int y;

        public string str
        {
            get => str ?? "";
            set => str = value;
        }

    }

    internal class Util
    {

    }
}
