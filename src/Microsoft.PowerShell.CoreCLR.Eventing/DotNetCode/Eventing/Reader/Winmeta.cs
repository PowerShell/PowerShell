// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** Contains eventing constants defined by the Windows
** environment.
**
============================================================*/

using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// WindowsEventLevel.
    /// </summary>
    public enum StandardEventLevel
    {
        /// <summary>
        /// Log always.
        /// </summary>
        LogAlways = 0,
        /// <summary>
        /// Only critical errors.
        /// </summary>
        Critical,
        /// <summary>
        /// All errors, including previous levels.
        /// </summary>
        Error,
        /// <summary>
        /// All warnings, including previous levels.
        /// </summary>
        Warning,
        /// <summary>
        /// All informational events, including previous levels.
        /// </summary>
        Informational,
        /// <summary>
        /// All events, including previous levels.
        /// </summary>
        Verbose
    }
    /// <summary>
    /// WindowsEventTask.
    /// </summary>
    public enum StandardEventTask
    {
        /// <summary>
        /// Undefined task.
        /// </summary>
        None = 0
    }

    /// <summary>
    /// EventOpcode.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Opcode", Justification = "matell: Shipped public in 3.5, breaking change to fix now.")]
    public enum StandardEventOpcode
    {
        /// <summary>
        /// An informational event.
        /// </summary>
        Info = 0,
        /// <summary>
        /// An activity start event.
        /// </summary>
        Start,
        /// <summary>
        /// An activity end event.
        /// </summary>
        Stop,
        /// <summary>
        /// A trace collection start event.
        /// </summary>
        DataCollectionStart,
        /// <summary>
        /// A trace collection end event.
        /// </summary>
        DataCollectionStop,
        /// <summary>
        /// An extensional event.
        /// </summary>
        Extension,
        /// <summary>
        /// A reply event.
        /// </summary>
        Reply,
        /// <summary>
        /// An event representing the activity resuming from the suspension.
        /// </summary>
        Resume,
        /// <summary>
        /// An event representing the activity is suspended, pending another activity's completion.
        /// </summary>
        Suspend,
        /// <summary>
        /// An event representing the activity is transferred to another component, and can continue to work.
        /// </summary>
        Send,
        /// <summary>
        /// An event representing receiving an activity transfer from another component.
        /// </summary>
        Receive = 240
    }

    /// <summary>
    /// EventOpcode.
    /// </summary>
    [Flags]
    public enum StandardEventKeywords : long
    {
        /// <summary>
        /// Wild card value.
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Events providing response time information.
        /// </summary>
        ResponseTime = 0x01000000000000,
        /// <summary>
        /// WDI context events.
        /// </summary>
        WdiContext = 0x02000000000000,
        /// <summary>
        /// WDI diagnostic events.
        /// </summary>
        WdiDiagnostic = 0x04000000000000,
        /// <summary>
        /// SQM events.
        /// </summary>
        Sqm = 0x08000000000000,
        /// <summary>
        /// FAiled security audits.
        /// </summary>
        AuditFailure = 0x10000000000000,
        /// <summary>
        /// Successful security audits.
        /// </summary>
        AuditSuccess = 0x20000000000000,
        /// <summary>
        /// Incorrect CorrelationHint value mistakenly shipped in .NET 3.5. Don't use: duplicates AuditFailure.
        /// </summary>
        [Obsolete("Incorrect value: use CorrelationHint2 instead", false)]
        CorrelationHint = 0x10000000000000,
        /// <summary>
        /// Transfer events where the related Activity ID is a computed value and not a GUID.
        /// </summary>
        CorrelationHint2 = 0x40000000000000,
        /// <summary>
        /// Events raised using classic eventlog API.
        /// </summary>
        EventLogClassic = 0x80000000000000
    }
}