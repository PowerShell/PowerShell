/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Management.Infrastructure.Options
{
    /// <summary>
    /// Cim PromptType
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "This is a direct copy of the native flags enum (which has zero as one of the members.")]
    public enum CimPromptType : int
    {
        None = Native.MiPromptType.PROMPTTYPE_NORMAL,
        Normal = Native.MiPromptType.PROMPTTYPE_NORMAL,
        Critical = Native.MiPromptType.PROMPTTYPE_CRITICAL,
    };

    /// <summary>
    /// Cim callback mode
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "This is a direct copy of the native flags enum (which has zero as one of the members.")]
    public enum CimCallbackMode : int
    {
        None = 0,
        Report = Native.MiCallbackMode.CALLBACK_REPORT,
        Inquire = Native.MiCallbackMode.CALLBACK_INQUIRE,
        Ignore = Native.MiCallbackMode.CALLBACK_IGNORE,
    };

    /// <summary>
    /// Cim Response Type
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "This is a direct copy of the native flags enum (which has zero as one of the members.")]
    public enum CimResponseType : int
    {
        None = 0,
        No = Native.MIResponseType.MIResponseTypeNo,
        Yes = Native.MIResponseType.MIResponseTypeYes,
        NoToAll = Native.MIResponseType.MIResponseTypeNoToAll,
        YesToAll = Native.MIResponseType.MIResponseTypeYesToAll,
    };

    /// <summary>
    /// <para>
    /// Write message channel
    /// </para>
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "This is a direct copy of the native flags enum (which has zero as one of the members.")]
    public enum CimWriteMessageChannel : int
    {
        Warning =   Native.MIWriteMessageChannel.MIWriteMessageChannelWarning,
        Verbose =   Native.MIWriteMessageChannel.MIWriteMessageChannelVerbose,
        Debug =     Native.MIWriteMessageChannel.MIWriteMessageChannelDebug,
    };

    /// <summary>
    /// delegate to set the WriteMessage Callback.
    /// </summary>
    /// <value></value>
    public delegate void WriteMessageCallback(UInt32 channel, string message);

    /// <summary>
    /// delegate to set the WriteProgress Callback.
    /// </summary>
    /// <value></value>
    public delegate void WriteProgressCallback(
        string activity,
        string currentOperation,
        string statusDescription,
        UInt32 percentageCompleted,
        UInt32 secondsRemaining);

    /// <summary>
    /// delegate to set the WriteError Callback.
    /// </summary>
    /// <value></value>
    public delegate CimResponseType WriteErrorCallback(CimInstance cimError);

    /// <summary>
    /// delegate to set the PromptUser Callback.
    /// </summary>
    /// <value></value>
    public delegate CimResponseType PromptUserCallback(string message, CimPromptType promptType);
}

