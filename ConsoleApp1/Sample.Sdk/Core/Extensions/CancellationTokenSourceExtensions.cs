using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Extensions
{
    public static class CancellationTokenSourceExtensions
    {
        private static long errorCounter = 0;
        public static bool CancelIfDatabaseIsErroring(
            this CancellationTokenSource tokenSource, 
            Exception exception, 
            int tolerance) 
        {
            if (tokenSource == null) 
            { 
                return false; 
            }
            if (exception is DbUpdateException dbUpdate) 
            {
                Interlocked.Increment(ref errorCounter);
            }
            if(Interlocked.Read(ref errorCounter) >= tolerance) 
            {
                tokenSource.Cancel();
                return true;
            }
            return false;
        }


    }
}
