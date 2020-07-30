// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#if !UNIX

using System.Text;

namespace System.Management.Automation.Tracing
{
    /// <summary>
    /// Tracer.
    /// </summary>
    public sealed partial class Tracer : System.Management.Automation.Tracing.EtwActivity
    {
        /// <summary>
        /// DebugMessage.
        /// </summary>
        [EtwEvent(0xc000)]
        public void DebugMessage(Exception exception)
        {
            if (exception is null)
                return;

            DebugMessage(GetExceptionString(exception));
        }

        /// <summary>
        /// Converts exception object into a string.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static string GetExceptionString(Exception exception)
        {
            if (exception is null)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            while (WriteExceptionText(sb, exception))
            {
                exception = exception.InnerException;
            }

            return sb.ToString();
        }

        private static bool WriteExceptionText(StringBuilder sb, Exception e)
        {
            if (e is null)
                return false;

            sb.Append(e.GetType().Name);
            sb.Append(Environment.NewLine);
            sb.Append(e.Message);
            sb.Append(Environment.NewLine);

            return true;
        }
    }
}

#endif
