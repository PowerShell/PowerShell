// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Host;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// The RemoteHostMethodId enum.
    /// </summary>
    internal enum RemoteHostMethodId
    {
        // Host read-only properties.
        GetName = 1,
        GetVersion = 2,
        GetInstanceId = 3,
        GetCurrentCulture = 4,
        GetCurrentUICulture = 5,

        // Host methods.
        SetShouldExit = 6,
        EnterNestedPrompt = 7,
        ExitNestedPrompt = 8,
        NotifyBeginApplication = 9,
        NotifyEndApplication = 10,

        // Host UI methods.
        ReadLine = 11,
        ReadLineAsSecureString = 12,
        Write1 = 13,
        Write2 = 14,
        WriteLine1 = 15,
        WriteLine2 = 16,
        WriteLine3 = 17,
        WriteErrorLine = 18,
        WriteDebugLine = 19,
        WriteProgress = 20,
        WriteVerboseLine = 21,
        WriteWarningLine = 22,
        Prompt = 23,
        PromptForCredential1 = 24,
        PromptForCredential2 = 25,
        PromptForChoice = 26,

        // Host Raw UI read-write properties.
        GetForegroundColor = 27,
        SetForegroundColor = 28,
        GetBackgroundColor = 29,
        SetBackgroundColor = 30,
        GetCursorPosition = 31,
        SetCursorPosition = 32,
        GetWindowPosition = 33,
        SetWindowPosition = 34,
        GetCursorSize = 35,
        SetCursorSize = 36,
        GetBufferSize = 37,
        SetBufferSize = 38,
        GetWindowSize = 39,
        SetWindowSize = 40,
        GetWindowTitle = 41,
        SetWindowTitle = 42,

        // Host Raw UI read-only properties.
        GetMaxWindowSize = 43,
        GetMaxPhysicalWindowSize = 44,
        GetKeyAvailable = 45,

        // Host Raw UI methods.
        ReadKey = 46,
        FlushInputBuffer = 47,
        SetBufferContents1 = 48,
        SetBufferContents2 = 49,
        GetBufferContents = 50,
        ScrollBufferContents = 51,

        // IHostSupportsInteractiveSession methods.
        PushRunspace = 52,
        PopRunspace = 53,

        // IHostSupportsInteractiveSession read-only properties.
        GetIsRunspacePushed = 54,
        GetRunspace = 55,

        // IHostSupportsMultipleChoiceSelection
        PromptForChoiceMultipleSelection = 56,
    }

    /// <summary>
    /// Stores information about remote host methods. By storing information
    /// in this data structure we only need to transport enums on the wire.
    /// </summary>
    internal sealed class RemoteHostMethodInfo
    {
        /// <summary>
        /// Interface type.
        /// </summary>
        internal Type InterfaceType { get; }

        /// <summary>
        /// Name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Return type.
        /// </summary>
        internal Type ReturnType { get; }

        /// <summary>
        /// Parameter types.
        /// </summary>
        internal Type[] ParameterTypes { get; }

        /// <summary>
        /// Create a new instance of RemoteHostMethodInfo.
        /// </summary>
        internal RemoteHostMethodInfo(
            Type interfaceType,
            string name,
            Type returnType,
            Type[] parameterTypes)
        {
            InterfaceType = interfaceType;
            Name = name;
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        /// <summary>
        /// Look up.
        /// </summary>
        internal static RemoteHostMethodInfo LookUp(RemoteHostMethodId methodId)
        {
            switch (methodId)
            {
                // Host read-only properties.

                case RemoteHostMethodId.GetName:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "get_Name",
                        typeof(string),
                        Array.Empty<Type>());

                case RemoteHostMethodId.GetVersion:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "get_Version",
                        typeof(Version),
                        Array.Empty<Type>());

                case RemoteHostMethodId.GetInstanceId:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "get_InstanceId",
                        typeof(Guid),
                        Array.Empty<Type>());

                case RemoteHostMethodId.GetCurrentCulture:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "get_CurrentCulture",
                        typeof(System.Globalization.CultureInfo),
                        Array.Empty<Type>());

                case RemoteHostMethodId.GetCurrentUICulture:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "get_CurrentUICulture",
                        typeof(System.Globalization.CultureInfo),
                        Array.Empty<Type>());

                // Host methods.

                case RemoteHostMethodId.SetShouldExit:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "SetShouldExit",
                        typeof(void),
                        new Type[] { typeof(int) });

                case RemoteHostMethodId.EnterNestedPrompt:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "EnterNestedPrompt",
                        typeof(void),
                        Array.Empty<Type>());

                case RemoteHostMethodId.ExitNestedPrompt:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "ExitNestedPrompt",
                        typeof(void),
                        Array.Empty<Type>());

                case RemoteHostMethodId.NotifyBeginApplication:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "NotifyBeginApplication",
                        typeof(void),
                        Array.Empty<Type>());

                case RemoteHostMethodId.NotifyEndApplication:
                    return new RemoteHostMethodInfo(
                        typeof(PSHost),
                        "NotifyEndApplication",
                        typeof(void),
                        Array.Empty<Type>());

                // Host UI methods.

                case RemoteHostMethodId.ReadLine:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "ReadLine",
                        typeof(string),
                        Array.Empty<Type>());

                case RemoteHostMethodId.ReadLineAsSecureString:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "ReadLineAsSecureString",
                        typeof(System.Security.SecureString),
                        Array.Empty<Type>());

                case RemoteHostMethodId.Write1:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "Write",
                        typeof(void),
                        new Type[] { typeof(string) });

                case RemoteHostMethodId.Write2:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "Write",
                        typeof(void),
                        new Type[] { typeof(ConsoleColor), typeof(ConsoleColor), typeof(string) });

                case RemoteHostMethodId.WriteLine1:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "WriteLine",
                        typeof(void),
                        Array.Empty<Type>());

                case RemoteHostMethodId.WriteLine2:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "WriteLine",
                        typeof(void),
                        new Type[] { typeof(string) });

                case RemoteHostMethodId.WriteLine3:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "WriteLine",
                        typeof(void),
                        new Type[] { typeof(ConsoleColor), typeof(ConsoleColor), typeof(string) });

                case RemoteHostMethodId.WriteErrorLine:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "WriteErrorLine",
                        typeof(void),
                        new Type[] { typeof(string) });

                case RemoteHostMethodId.WriteDebugLine:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "WriteDebugLine",
                        typeof(void),
                        new Type[] { typeof(string) });

                case RemoteHostMethodId.WriteProgress:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "WriteProgress",
                        typeof(void),
                        new Type[] { typeof(long), typeof(ProgressRecord) });

                case RemoteHostMethodId.WriteVerboseLine:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "WriteVerboseLine",
                        typeof(void),
                        new Type[] { typeof(string) });

                case RemoteHostMethodId.WriteWarningLine:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "WriteWarningLine",
                        typeof(void),
                        new Type[] { typeof(string) });

                case RemoteHostMethodId.Prompt:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "Prompt",
                        typeof(Dictionary<string, PSObject>),
                        new Type[] { typeof(string), typeof(string), typeof(System.Collections.ObjectModel.Collection<FieldDescription>) });

                case RemoteHostMethodId.PromptForCredential1:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "PromptForCredential",
                        typeof(PSCredential),
                        new Type[] { typeof(string), typeof(string), typeof(string), typeof(string) });

                case RemoteHostMethodId.PromptForCredential2:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "PromptForCredential",
                        typeof(PSCredential),
                        new Type[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(PSCredentialTypes), typeof(PSCredentialUIOptions) });

                case RemoteHostMethodId.PromptForChoice:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostUserInterface),
                        "PromptForChoice",
                        typeof(int),
                        new Type[] { typeof(string), typeof(string), typeof(System.Collections.ObjectModel.Collection<ChoiceDescription>), typeof(int) });

                case RemoteHostMethodId.PromptForChoiceMultipleSelection:
                    return new RemoteHostMethodInfo(
                        typeof(IHostUISupportsMultipleChoiceSelection),
                        "PromptForChoice",
                        typeof(Collection<int>),
                        new Type[] { typeof(string), typeof(string), typeof(Collection<ChoiceDescription>), typeof(IEnumerable<int>) });

                // Host raw UI read-write properties.

                case RemoteHostMethodId.GetForegroundColor:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_ForegroundColor",
                        typeof(ConsoleColor),
                        Array.Empty<Type>());

                case RemoteHostMethodId.SetForegroundColor:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "set_ForegroundColor",
                        typeof(void),
                        new Type[] { typeof(ConsoleColor) });

                case RemoteHostMethodId.GetBackgroundColor:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_BackgroundColor",
                        typeof(ConsoleColor),
                        Array.Empty<Type>());

                case RemoteHostMethodId.SetBackgroundColor:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "set_BackgroundColor",
                        typeof(void),
                        new Type[] { typeof(ConsoleColor) });

                case RemoteHostMethodId.GetCursorPosition:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_CursorPosition",
                        typeof(Coordinates),
                        Array.Empty<Type>());

                case RemoteHostMethodId.SetCursorPosition:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "set_CursorPosition",
                        typeof(void),
                        new Type[] { typeof(Coordinates) });

                case RemoteHostMethodId.GetWindowPosition:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_WindowPosition",
                        typeof(Coordinates),
                        Array.Empty<Type>());

                case RemoteHostMethodId.SetWindowPosition:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "set_WindowPosition",
                        typeof(void),
                        new Type[] { typeof(Coordinates) });

                case RemoteHostMethodId.GetCursorSize:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_CursorSize",
                        typeof(int),
                        Array.Empty<Type>());

                case RemoteHostMethodId.SetCursorSize:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "set_CursorSize",
                        typeof(void),
                        new Type[] { typeof(int) });

                case RemoteHostMethodId.GetBufferSize:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_BufferSize",
                        typeof(Size),
                        Array.Empty<Type>());

                case RemoteHostMethodId.SetBufferSize:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "set_BufferSize",
                        typeof(void),
                        new Type[] { typeof(Size) });

                case RemoteHostMethodId.GetWindowSize:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_WindowSize",
                        typeof(Size),
                        Array.Empty<Type>());

                case RemoteHostMethodId.SetWindowSize:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "set_WindowSize",
                        typeof(void),
                        new Type[] { typeof(Size) });

                case RemoteHostMethodId.GetWindowTitle:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_WindowTitle",
                        typeof(string),
                        Array.Empty<Type>());

                case RemoteHostMethodId.SetWindowTitle:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "set_WindowTitle",
                        typeof(void),
                        new Type[] { typeof(string) });

                // Host raw UI read-only properties.

                case RemoteHostMethodId.GetMaxWindowSize:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_MaxWindowSize",
                        typeof(Size),
                        Array.Empty<Type>());

                case RemoteHostMethodId.GetMaxPhysicalWindowSize:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_MaxPhysicalWindowSize",
                        typeof(Size),
                        Array.Empty<Type>());

                case RemoteHostMethodId.GetKeyAvailable:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "get_KeyAvailable",
                        typeof(bool),
                        Array.Empty<Type>());

                // Host raw UI methods.

                case RemoteHostMethodId.ReadKey:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "ReadKey",
                        typeof(KeyInfo),
                        new Type[] { typeof(ReadKeyOptions) });

                case RemoteHostMethodId.FlushInputBuffer:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "FlushInputBuffer",
                        typeof(void),
                        Array.Empty<Type>());

                case RemoteHostMethodId.SetBufferContents1:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "SetBufferContents",
                        typeof(void),
                        new Type[] { typeof(Rectangle), typeof(BufferCell) });

                case RemoteHostMethodId.SetBufferContents2:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "SetBufferContents",
                        typeof(void),
                        new Type[] { typeof(Coordinates), typeof(BufferCell[,]) });

                case RemoteHostMethodId.GetBufferContents:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "GetBufferContents",
                        typeof(BufferCell[,]),
                        new Type[] { typeof(Rectangle) });

                case RemoteHostMethodId.ScrollBufferContents:
                    return new RemoteHostMethodInfo(
                        typeof(PSHostRawUserInterface),
                        "ScrollBufferContents",
                        typeof(void),
                        new Type[] { typeof(Rectangle), typeof(Coordinates), typeof(Rectangle), typeof(BufferCell) });

                // IHostSupportsInteractiveSession methods.

                case RemoteHostMethodId.PushRunspace:
                    return new RemoteHostMethodInfo(
                        typeof(IHostSupportsInteractiveSession),
                        "PushRunspace",
                        typeof(void),
                        new Type[] { typeof(System.Management.Automation.Runspaces.Runspace) });

                case RemoteHostMethodId.PopRunspace:
                    return new RemoteHostMethodInfo(
                        typeof(IHostSupportsInteractiveSession),
                        "PopRunspace",
                        typeof(void),
                        Array.Empty<Type>());

                // IHostSupportsInteractiveSession properties.

                case RemoteHostMethodId.GetIsRunspacePushed:
                    return new RemoteHostMethodInfo(
                        typeof(IHostSupportsInteractiveSession),
                        "get_IsRunspacePushed",
                        typeof(bool),
                        Array.Empty<Type>());

                case RemoteHostMethodId.GetRunspace:
                    return new RemoteHostMethodInfo(
                        typeof(IHostSupportsInteractiveSession),
                        "get_Runspace",
                        typeof(System.Management.Automation.Runspaces.Runspace),
                        Array.Empty<Type>());

                default:
                    Dbg.Assert(false, "All RemoteHostMethodId's should be handled. This code should not be reached.");
                    return null;
            }
        }
    }
}
