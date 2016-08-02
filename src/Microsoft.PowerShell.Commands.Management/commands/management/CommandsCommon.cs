/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The base class for all command processor classes. It provides
    /// abstract methods to execute a command.
    /// </summary>
    internal static class CommandsCommon
    {
#if CORECLR
        // AccessViolationException/StackOverflowException Not In CoreCLR.
        // The CoreCLR team told us to not check for these exceptions because they
        // usually won't be caught.
        internal static void CheckForSevereException(Cmdlet cmdlet, Exception e) { }
#else
        // Keep in sync:
        // S.M.A.CommandProcessorBase.CheckForSevereException
        // S.M.A.Internal.ConsoleHost.CheckForSevereException
        // S.M.A.Commands.CommandsCommon.CheckForSevereException
        // S.M.A.Commands.UtilityCommon.CheckForSevereException
        /// <summary>
        /// Checks whether the exception is a severe exception which should
        /// cause immediate process failure.
        /// </summary>
        /// <param name="cmdlet"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// CB says 02/23/2005: I personally would err on the side
        /// of treating OOM like an application exception, rather than
        /// a critical system failure.I think this will be easier to justify
        /// in Orcas, if we tease apart the two cases of OOM better.
        /// But even in Whidbey, how likely is it that we couldnt JIT
        /// some backout code?  At that point, the process or possibly
        /// the machine is likely to stop executing soon no matter
        /// what you do in this routine.  So I would just consider
        /// AccessViolationException.  (I understand why you have SO here,
        /// at least temporarily).
        /// </remarks>
        internal static void CheckForSevereException(Cmdlet cmdlet, Exception e)
        {
            if (e is AccessViolationException || e is StackOverflowException)
            {
                try
                {
                    if (!alreadyFailing)
                    {
                        alreadyFailing = true;

                        // Log a command health event for this critical error.
                        MshLog.LogCommandHealthEvent(
                            cmdlet.Context,
                            e,
                            Severity.Critical);
                    }
                }
                finally
                {
                    if (!designForTestability_SkipFailFast)
                        WindowsErrorReporting.FailFast(e);
                }
            }
        }
        private static bool alreadyFailing = false;

        // 2005/04/22-JonN Adding this capability so that we can
        // get some test coverage on this function
        private static bool designForTestability_SkipFailFast = false;
#endif
    }
}

