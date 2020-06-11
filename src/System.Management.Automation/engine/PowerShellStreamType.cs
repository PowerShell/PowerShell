// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    /// <summary>
    /// Enumeration of the possible PowerShell stream types.
    /// This enumeration is obsolete.
    /// </summary>
    /// <remarks>
    /// This enumeration is a public type formerly used in PowerShell Workflow,
    /// but kept due to its generic name and public accessibility.
    /// It is not used by any other PowerShell API, and is now obsolete
    /// and should not be used if possible.
    /// </remarks>
    [Obsolete("This enum type was used only in PowerShell Workflow and is now obsolete.", error: true)]
    public enum PowerShellStreamType
    {
        /// <summary>
        /// PSObject.
        /// </summary>
        Input = 0,

        /// <summary>
        /// PSObject.
        /// </summary>
        Output = 1,

        /// <summary>
        /// ErrorRecord.
        /// </summary>
        Error = 2,

        /// <summary>
        /// WarningRecord.
        /// </summary>
        Warning = 3,

        /// <summary>
        /// VerboseRecord.
        /// </summary>
        Verbose = 4,

        /// <summary>
        /// DebugRecord.
        /// </summary>
        Debug = 5,

        /// <summary>
        /// ProgressRecord.
        /// </summary>
        Progress = 6,

        /// <summary>
        /// InformationRecord.
        /// </summary>
        Information = 7
    }
}
