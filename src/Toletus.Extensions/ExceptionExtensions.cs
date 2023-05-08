using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Toletus.Extensions
{
    public static class ExceptionExtensions
    {
        /// <summary>
        ///  Provides full stack trace for the exception that occurred.
        /// </summary>
        /// <param name="exception">Exception object.</param>
        /// <param name="environmentStackTrace">Environment stack trace, for pulling additional stack frames.</param>
        public static string ToLogString(this Exception exception, string environmentStackTrace)
        {
            var environmentStackTraceLines =
                GetUserStackTraceLines(environmentStackTrace);

            var messages = exception.MessagesToString();

            var logMessage = messages;

            if (environmentStackTraceLines.Count <= 0) return logMessage;

            try
            {
                environmentStackTraceLines.RemoveAt(0);

                var stackTraceLines = GetStackTraceLines(exception.StackTrace);
                stackTraceLines.AddRange(environmentStackTraceLines);

                var fullStackTrace = string.Join(Environment.NewLine, stackTraceLines);

                logMessage += Environment.NewLine + fullStackTrace;
            }
            catch (Exception e) { }

            return logMessage;
        }

        public static string MessagesToString(this Exception ex)
        {
            var ret = ex.Message;

            while (ex.InnerException != null)
            {
                ret += Environment.NewLine + ex.InnerException.Message;
                ex = ex.InnerException;
            }

            return ret;
        }

        /// <summary>
        ///  Gets a list of stack frame lines, as strings.
        /// </summary>
        /// <param name="stackTrace">Stack trace string.</param>
        private static List<string> GetStackTraceLines(string stackTrace)
        {
            return stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
        }

        /// <summary>
        ///  Gets a list of stack frame lines, as strings, only including those for which line number is known.
        /// </summary>
        /// <param name="fullStackTrace">Full stack trace, including external code.</param>
        private static List<string> GetUserStackTraceLines(string fullStackTrace)
        {
            var regex = new Regex(@"([^\)]*\)) in (.*):line (\d)*$");

            var stackTraceLines = GetStackTraceLines(fullStackTrace);

            return stackTraceLines.Where(stackTraceLine => regex.IsMatch(stackTraceLine)).ToList();
        }
    }
}