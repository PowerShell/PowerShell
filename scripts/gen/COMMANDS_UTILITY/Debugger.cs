﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;



/// <summary>
///   A strongly-typed resource class, for looking up localized strings, etc.
/// </summary>
// This class was auto-generated by the StronglyTypedResourceBuilder
// class via a tool like ResGen or Visual Studio.
// To add or remove a member, edit your .ResX file then rerun ResGen
// with the /str option, or rebuild your VS project.
[global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
internal class Debugger {
    
    private static global::System.Resources.ResourceManager resourceMan;
    
    private static global::System.Globalization.CultureInfo resourceCulture;
    
    [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
    internal Debugger() {
    }
    
    /// <summary>
    ///   Returns the cached ResourceManager instance used by this class.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static global::System.Resources.ResourceManager ResourceManager {
        get {
            if (object.ReferenceEquals(resourceMan, null)) {
                global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Debugger", typeof(Debugger).GetTypeInfo().Assembly);
                resourceMan = temp;
            }
            return resourceMan;
        }
    }
    
    /// <summary>
    ///   Overrides the current thread's CurrentUICulture property for all
    ///   resource lookups using this strongly typed resource class.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    internal static global::System.Globalization.CultureInfo Culture {
        get {
            return resourceCulture;
        }
        set {
            resourceCulture = value;
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to There is no breakpoint with ID &apos;{0}&apos;..
    /// </summary>
    internal static string BreakpointIdNotFound {
        get {
            return ResourceManager.GetString("BreakpointIdNotFound", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Cannot set breakpoint. The language mode for this session is incompatible with the system-wide language mode..
    /// </summary>
    internal static string CannotSetBreakpointInconsistentLanguageMode {
        get {
            return ResourceManager.GetString("CannotSetBreakpointInconsistentLanguageMode", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Wait-Debugger called on line {0} in {1}..
    /// </summary>
    internal static string DebugBreakMessage {
        get {
            return ResourceManager.GetString("DebugBreakMessage", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to File &apos;{0}&apos; does not exist..
    /// </summary>
    internal static string FileDoesNotExist {
        get {
            return ResourceManager.GetString("FileDoesNotExist", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Line cannot be less than 1..
    /// </summary>
    internal static string LineLessThanOne {
        get {
            return ResourceManager.GetString("LineLessThanOne", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Failed to persist debug options for Process {0}..
    /// </summary>
    internal static string PersistDebugPreferenceFailure {
        get {
            return ResourceManager.GetString("PersistDebugPreferenceFailure", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Debugging is not supported on remote sessions..
    /// </summary>
    internal static string RemoteDebuggerNotSupported {
        get {
            return ResourceManager.GetString("RemoteDebuggerNotSupported", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Breakpoints cannot be set in the remote session because remote debugging is not supported by the current host..
    /// </summary>
    internal static string RemoteDebuggerNotSupportedInHost {
        get {
            return ResourceManager.GetString("RemoteDebuggerNotSupportedInHost", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to You cannot debug the default host Runspace using this cmdlet. To debug the default Runspace use the normal debugging commands from the host..
    /// </summary>
    internal static string RunspaceDebuggingCannotDebugDefaultRunspace {
        get {
            return ResourceManager.GetString("RunspaceDebuggingCannotDebugDefaultRunspace", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to To end the debugging session type the &apos;Detach&apos; command at the debugger prompt, or type &apos;Ctrl+C&apos; otherwise..
    /// </summary>
    internal static string RunspaceDebuggingEndSession {
        get {
            return ResourceManager.GetString("RunspaceDebuggingEndSession", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Cannot debug Runspace. There is no host or host UI. The debugger requires a host and host UI for debugging..
    /// </summary>
    internal static string RunspaceDebuggingNoHost {
        get {
            return ResourceManager.GetString("RunspaceDebuggingNoHost", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Cannot debug Runspace. The host has no debugger. Try debugging the Runspace inside the Windows PowerShell console or the Windows PowerShell ISE, both of which have built-in debuggers..
    /// </summary>
    internal static string RunspaceDebuggingNoHostRunspaceOrDebugger {
        get {
            return ResourceManager.GetString("RunspaceDebuggingNoHostRunspaceOrDebugger", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to No Runspace was found..
    /// </summary>
    internal static string RunspaceDebuggingNoRunspaceFound {
        get {
            return ResourceManager.GetString("RunspaceDebuggingNoRunspaceFound", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Command or script completed..
    /// </summary>
    internal static string RunspaceDebuggingScriptCompleted {
        get {
            return ResourceManager.GetString("RunspaceDebuggingScriptCompleted", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Debugging Runspace: {0}.
    /// </summary>
    internal static string RunspaceDebuggingStarted {
        get {
            return ResourceManager.GetString("RunspaceDebuggingStarted", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to More than one Runspace was found. Only one Runspace can be debugged at a time..
    /// </summary>
    internal static string RunspaceDebuggingTooManyRunspacesFound {
        get {
            return ResourceManager.GetString("RunspaceDebuggingTooManyRunspacesFound", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Cannot set debug options on Runspace {0} because it is not in the Opened state..
    /// </summary>
    internal static string RunspaceOptionInvalidRunspaceState {
        get {
            return ResourceManager.GetString("RunspaceOptionInvalidRunspaceState", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to No debugger was found for Runspace {0}..
    /// </summary>
    internal static string RunspaceOptionNoDebugger {
        get {
            return ResourceManager.GetString("RunspaceOptionNoDebugger", resourceCulture);
        }
    }
    
    /// <summary>
    ///   Looks up a localized string similar to Cannot set breakpoint on file &apos;{0}&apos;; only *.ps1 and *.psm1 files are valid..
    /// </summary>
    internal static string WrongExtension {
        get {
            return ResourceManager.GetString("WrongExtension", resourceCulture);
        }
    }
}
