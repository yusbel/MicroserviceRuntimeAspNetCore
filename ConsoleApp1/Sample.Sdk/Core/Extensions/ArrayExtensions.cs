using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Extensions
{
    public static class ArrayExtensions
    {
        public static int GetIndexByteArray(this byte[] bytes, int item) 
        {
            for(var i = 0; i < bytes.Length; i++) 
            {
                if (bytes[i] == item) 
                    return i;
            }
            return -1;
        }
    }
}
