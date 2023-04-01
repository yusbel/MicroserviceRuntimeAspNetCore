using Microsoft.Extensions.Logging;

namespace Sample.Sdk.Core.Extensions
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
        [Obsolete("This method is obsolete call LogException method")]
        public static (bool wasHandle, Exception? exception) LogCriticalException(this Exception? e, ILogger logger, string msg = "", params object?[] objects)
        {
            if (e == null && logger != null)
            {
                logger.LogCritical(msg, objects);
                return (true, default);
            }
            if (e is AggregateException aggException)
            {
                foreach (var exception in aggException.Flatten().InnerExceptions)
                {
                    if (exception == null)
                    {
                        continue;
                    }
                    logger?.LogCritical(exception, msg, objects);
                }
                return (true, aggException);
            }
            logger?.LogCritical(e, msg, objects);
            return (true, e);
        }

        public static (bool wasHandle, Exception?) LogException(this Exception? e, Action<Exception?, string?, object?[]> action, string msg = "", params object?[] objects)
        {
            if (e == null)
            {
                action?.Invoke(default, msg, objects);
                return (true, default);
            }
            if (e is AggregateException aggException)
            {
                foreach (var exception in aggException.Flatten().InnerExceptions)
                {
                    if (exception == null)
                    {
                        continue;
                    }
                    action?.Invoke(exception, msg, objects);
                }
                return (true, aggException);
            }
            action?.Invoke(e, msg, objects);
            return (true, e);
        }
    }
}
