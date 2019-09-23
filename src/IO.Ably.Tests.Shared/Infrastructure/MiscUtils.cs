using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IO.Ably.Tests
{
    internal static class MiscUtils
    {
        public static string AddRandomSuffix(this string str)
        {
            if (str.IsEmpty())
            {
                return str;
            }

            return str + "_" + Guid.NewGuid().ToString("D").Substring(0, 8);
        }

        public static Task<AblyResponse> ToAblyResponse(this string txt)
        {
            return Task.FromResult(new AblyResponse() { TextResponse = txt });
        }

        public static Task<AblyResponse> ToAblyJsonResponse(this string txt)
        {
            return Task.FromResult(new AblyResponse() { TextResponse = txt, Type = ResponseType.Json });
        }

        public static Task<AblyResponse> ToTask(this AblyResponse r)
        {
            return Task.FromResult(r);
        }


        // From unity codebase
        public static void SafeExecute(Action action, ILogger logger = null, string caller = null)
        {
            SafeExecute(action, logger, caller==null ? (Func<string>)null : () => caller);
        }

        public static void SafeExecute(Action action, ILogger logger, Func<string> callerGetter)
        {
            try
            {
                action();
            }
            catch (Exception exc)
            {
                try
                {
                    if (logger != null)
                    {
                        string caller = null;
                        if (callerGetter != null)
                        {
                            try
                            {
                                caller = callerGetter();
                            }
                            catch (Exception)
                            {

                            }
                        }


                        foreach (var e in exc.FlattenAggregate())
                        {
                            logger.Warning($"Ignoring {e.GetType().FullName} exception thrown from an action called by {caller ?? String.Empty}.");
                        }
                    }
                }
                catch (Exception)
                {
                    // now really, really ignore.
                }
            }
        }

        public static List<Exception> FlattenAggregate(this Exception exc)
        {
            var result = new List<Exception>();
            if (exc is AggregateException)
            {
                result.AddRange(exc.InnerException.FlattenAggregate());
            }
            else
            {
                result.Add(exc);
            }

            return result;
        }
    }
}
