using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Extensions
{
    public static class DateTimeExtension
    {
        public static long ToLong(this DateTime datetime)
        {
            return datetime.Ticks;
        }
    }
}
