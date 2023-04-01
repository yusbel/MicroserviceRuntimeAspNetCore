using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core
{
    public static class Guard
    {
        public static object ThrowWhenNull(params object[] obj) => obj == null || obj.ToList().Any(item => item == null) ? throw new ArgumentNullException(nameof(obj)) : obj;
    }
}
