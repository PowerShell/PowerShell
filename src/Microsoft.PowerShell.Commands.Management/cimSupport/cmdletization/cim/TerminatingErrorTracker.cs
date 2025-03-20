// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Management.Infrastructure;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    /// <summary>
    /// Tracks (per-session) terminating errors in a given cmdlet invocation.
    /// </summary>
    internal sealed class TerminatingErrorTracker
    {
        #region Getting tracker for a given cmdlet invocation

        private static readonly ConditionalWeakTable<InvocationInfo, TerminatingErrorTracker> s_invocationToTracker =
            new();

        private static int GetNumberOfSessions(InvocationInfo invocationInfo)
        {
            // if user explicitly specifies CimSession, then the cmdlet runs against exactly those sessions
            object cimSessionArgument;
            if (invocationInfo.BoundParameters.TryGetValue("CimSession", out cimSessionArgument))
            {
                IList cimSessionArgumentAsList = (IList)cimSessionArgument;
                return cimSessionArgumentAsList.Count;
            }
            // else - either CimSession=localhost OR CimSession is based on CimInstance->CimSession affinity

            // CimInstance->CimSession affinity in instance cmdlets can come from:
            // 1. InputObject (either passed through pipeline or explicitly bound to the parameter)
            // 2. AssociatedObject (either passed through pipeline or explicitly bound to the parameter [we don't know the name of the parameter though])
            // CimInstance->CimSession affinity in static cmdlets can come from:
            // 1. Any method argument that is either a CimInstance or CimInstance[]
            // Additionally in both instance and static cmdlets, if the pipeline object is a CimInstance, then it can affect the session acted against
            if (invocationInfo.ExpectingInput)
            {
                // can get unlimited number of CimInstances through pipeline
                // - this translates into potentially unlimited number of CimSession we will work with
                return int.MaxValue;
            }

            int maxNumberOfSessionsIndicatedByCimInstanceArguments = 1;
            foreach (object cmdletArgument in invocationInfo.BoundParameters.Values)
            {
                if (cmdletArgument is CimInstance[] array)
                {
                    int numberOfSessionsAssociatedWithArgument = array
                        .Select(CimCmdletAdapter.GetSessionOfOriginFromCimInstance)
                        .Distinct()
                        .Count();
                    maxNumberOfSessionsIndicatedByCimInstanceArguments = Math.Max(
                        maxNumberOfSessionsIndicatedByCimInstanceArguments,
                        numberOfSessionsAssociatedWithArgument);
                }
            }

            return maxNumberOfSessionsIndicatedByCimInstanceArguments;
        }

        internal static TerminatingErrorTracker GetTracker(InvocationInfo invocationInfo, bool isStaticCmdlet)
        {
            var tracker = s_invocationToTracker.GetValue(
                invocationInfo,
                _ => new TerminatingErrorTracker(GetNumberOfSessions(invocationInfo)));

            return tracker;
        }

        internal static TerminatingErrorTracker GetTracker(InvocationInfo invocationInfo)
        {
            TerminatingErrorTracker tracker;
            bool foundTracker = s_invocationToTracker.TryGetValue(invocationInfo, out tracker);
            Dbg.Assert(foundTracker, "The other overload of GetTracker should always be called first");
            return tracker;
        }

        #endregion Getting tracker for a given cmdlet invocation

        #region Tracking terminating errors within a single cmdlet invocation

        private readonly int _numberOfSessions;
        private int _numberOfReportedSessionTerminatingErrors;

        private TerminatingErrorTracker(int numberOfSessions)
        {
            _numberOfSessions = numberOfSessions;
        }

        #region Tracking session's "connectivity" status

        private readonly ConcurrentDictionary<CimSession, bool> _sessionToIsConnected = new();

        internal void MarkSessionAsConnected(CimSession connectedSession)
        {
            _sessionToIsConnected.TryAdd(connectedSession, true);
        }

        internal bool DidSessionAlreadyPassedConnectivityTest(CimSession session)
        {
            bool alreadyPassedConnectivityTest = false;
            if (_sessionToIsConnected.TryGetValue(session, out alreadyPassedConnectivityTest))
            {
                return alreadyPassedConnectivityTest;
            }

            return false;
        }

        internal Exception GetExceptionIfBrokenSession(
            CimSession potentiallyBrokenSession,
            bool skipTestConnection,
            out bool sessionWasAlreadyTerminated)
        {
            if (IsSessionTerminated(potentiallyBrokenSession))
            {
                sessionWasAlreadyTerminated = true;
                return null;
            }

            Exception sessionException = null;
            if (!skipTestConnection &&
                !this.DidSessionAlreadyPassedConnectivityTest(potentiallyBrokenSession))
            {
                try
                {
                    CimInstance throwAwayCimInstance;
                    CimException cimException;
                    potentiallyBrokenSession.TestConnection(out throwAwayCimInstance, out cimException);
                    sessionException = cimException;
                    if (sessionException == null)
                    {
                        this.MarkSessionAsConnected(potentiallyBrokenSession);
                    }
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    sessionException = invalidOperationException;
                }
            }

            if (sessionException != null)
            {
                MarkSessionAsTerminated(potentiallyBrokenSession, out sessionWasAlreadyTerminated);
                return sessionException;
            }
            else
            {
                sessionWasAlreadyTerminated = false;
                return null;
            }
        }

        #endregion

        #region Tracking session's "terminated" status

        private readonly ConcurrentDictionary<CimSession, bool> _sessionToIsTerminated = new();

        internal void MarkSessionAsTerminated(CimSession terminatedSession, out bool sessionWasAlreadyTerminated)
        {
            bool closureSafeSessionWasAlreadyTerminated = false;
            _sessionToIsTerminated.AddOrUpdate(
                key: terminatedSession,
                addValue: true,
                updateValueFactory:
                    (CimSession key, bool isTerminatedValueInDictionary) =>
                    {
                        closureSafeSessionWasAlreadyTerminated = isTerminatedValueInDictionary;
                        return true;
                    });

            sessionWasAlreadyTerminated = closureSafeSessionWasAlreadyTerminated;
        }

        internal bool IsSessionTerminated(CimSession session)
        {
            bool isTerminated = _sessionToIsTerminated.GetOrAdd(session, false);
            return isTerminated;
        }

        #endregion

        #region Reporting errors in a way that takes session's "terminated" status into account

        internal CmdletMethodInvoker<bool> GetErrorReportingDelegate(ErrorRecord errorRecord)
        {
            ManualResetEventSlim manualResetEventSlim = new();
            object lockObject = new();
            Func<Cmdlet, bool> action = (Cmdlet cmdlet) =>
            {
                _numberOfReportedSessionTerminatingErrors++;
                if (_numberOfReportedSessionTerminatingErrors >= _numberOfSessions)
                {
                    cmdlet.ThrowTerminatingError(errorRecord);
                }
                else
                {
                    cmdlet.WriteError(errorRecord);
                }

                return false; // not really needed here, but required by CmdletMethodInvoker
            };

            return new CmdletMethodInvoker<bool>
            {
                Action = action,
                Finished = manualResetEventSlim, // not really needed here, but required by CmdletMethodInvoker
                SyncObject = lockObject, // not really needed here, but required by CmdletMethodInvoker
            };
        }

        #endregion

        #endregion Tracking terminating errors within a single cmdlet invocation
    }
}
