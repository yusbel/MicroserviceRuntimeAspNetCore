using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Exceptions
{
    public static class AggregateExceptionExtensions
    {
        /// <summary>
        /// Process: log aggregate exception and exceptions.
        /// </summary>
        /// <param name="e">Exception to process</param>
        /// <param name="logger">Logger to log the exception</param>
        /// <param name="objects">Params to log</param>
        /// <returns></returns>
        public static (bool wasHandle, Exception? e) LogCriticalException(this Exception e, ILogger logger, params object[] objects) 
        {
            if (e == null || logger == null) 
            {
                return (false, default);
            }
            if(e is AggregateException aggException) 
            {
                foreach (var exception in aggException.Flatten().InnerExceptions) 
                {
                    if (exception == null) 
                    {
                        continue;
                    }
                    logger.LogCritical(exception, exception.Message, objects);
                }
                return (true, aggException);
            }
            logger.LogCritical(e, message: "Exception has occurred", objects);
            return (true, e);
        }
    }
}
