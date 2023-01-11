// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Provider;
using System.Management.Automation.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PowerShell.Commands;

using Debug = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Runspaces
{
    internal static class EarlyStartup
    {
        internal static void Init()
        {
            // Code added here should:
            //   * run every time we start PowerSHell
            //   * have high CPU cost
            //   * be ordered from most expensive to least expensive, or at least needed earliest
            //   * this method should return quickly, so all work should be run in one or more tasks.
            //   * code called from here should correctly handle being called twice, in case initialization
            //     is needed in the main code path before the task completes.
            //
            // Code added here should not:
            //   * count on running - not all hosts will call this method
            //   * have high disk cost

            // We shouldn't create too many tasks.
#if !UNIX
            // Amsi initialize can be a little slow.
            Task.Run(() => AmsiUtils.WinScanContent(content: string.Empty, sourceMetadata: string.Empty, warmUp: true));
#endif
            // Initialize the types 'Compiler', 'CachedReflectionInfo', and 'ExpressionCache'.
            // Their type initializers do a lot of reflection operations.
            // We will access 'Compiler' members when creating the first session state.
            Task.Run(() => _ = Compiler.DottedLocalsTupleType);

            // One other task for other stuff that's faster, but still a little slow.
            Task.Run(() =>
            {
                // Loading the resources for System.Management.Automation can be expensive,
                // so force that to happen early on a background thread.
                _ = RunspaceInit.OutputEncodingDescription;

                // This will init some tables and could load some assemblies.
                // We will access 'LanguagePrimitives' when binding built-in variables for the Runspace.
                LanguagePrimitives.GetEnumerator(null);

                // This will init some tables and could load some assemblies.
                // We will access 'TypeAccelerators' when auto-loading the PSReadLine module, which happens last.
                _ = TypeAccelerators.builtinTypeAccelerators;
            });
        }
    }

    /// <summary>
    /// Baseclass for defining elements that can be added
    /// to an InitialSessionState object.
    /// </summary>
    public abstract class InitialSessionStateEntry
    {
        /// <summary>
        /// The ctor so that each derived class has a name.
        /// </summary>
        /// <param name="name"></param>
        protected InitialSessionStateEntry(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The name of this entry.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The SnapIn to load from initially.
        /// </summary>
        public PSSnapInInfo PSSnapIn { get; private set; }

        internal void SetPSSnapIn(PSSnapInInfo psSnapIn)
        {
            PSSnapIn = psSnapIn;
        }

        /// <summary>
        /// The SnapIn to load from initially.
        /// </summary>
        public PSModuleInfo Module { get; private set; }

        internal void SetModule(PSModuleInfo module)
        {
            Module = module;
        }

        /// <summary>
        /// Shallow-clone this object.
        /// </summary>
        /// <returns>The cloned object...</returns>
        public abstract InitialSessionStateEntry Clone();
    }

    /// <summary>
    /// Class to constrain session state entries.
    /// </summary>
    public abstract class ConstrainedSessionStateEntry : InitialSessionStateEntry
    {
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="visibility"></param>
        protected ConstrainedSessionStateEntry(string name, SessionStateEntryVisibility visibility)
            : base(name)
        {
            Visibility = visibility;
        }

        /// <summary>
        /// </summary>
        public SessionStateEntryVisibility Visibility { get; set; }
    }

    /// <summary>
    /// Command class so that all the commands can derive off this one.
    /// Adds the flexibility of adding additional derived class,
    /// such as ProxyCommand for Exchange.
    /// Derived classes - Alias, Application, Cmdlet, Function, Script.
    /// </summary>
    public abstract class SessionStateCommandEntry : ConstrainedSessionStateEntry
    {
        /// <summary>
        /// Base constructor for all SessionState commands.
        /// </summary>
        /// <param name="name"></param>
        protected SessionStateCommandEntry(string name)
            : base(name, SessionStateEntryVisibility.Public)
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="visibility"></param>
        protected internal SessionStateCommandEntry(string name, SessionStateEntryVisibility visibility)
            : base(name, visibility)
        {
        }

        /// <summary>
        /// Returns the type of the command using an enum
        /// instead of requiring a full reflection type check.
        /// </summary>
        public CommandTypes CommandType { get; internal set; }

        /// <summary>
        /// Is internal so it can be set by the engine code...
        /// This is used to specify whether this command was imported or not
        /// If noClobber is specified during Import-Module, it is set to false.
        /// </summary>
        internal bool _isImported = true;
    }

    /// <summary>
    /// Type file configuration entry...
    /// </summary>
    public sealed class SessionStateTypeEntry : InitialSessionStateEntry
    {
        /// <summary>
        /// Loads all entries from the types file.
        /// </summary>
        /// <param name="fileName"></param>
        public SessionStateTypeEntry(string fileName)
            : base(fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw PSTraceSource.NewArgumentException(nameof(fileName));
            }

            FileName = fileName.Trim();
        }

        /// <summary>
        /// Loads all the types specified in the typeTable.
        /// </summary>
        /// <param name="typeTable"></param>
        public SessionStateTypeEntry(TypeTable typeTable)
            : base("*")
        {
            if (typeTable == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeTable));
            }

            TypeTable = typeTable;
        }

        /// <summary>
        /// Loads all entries from the typeData.
        /// </summary>
        /// <param name="typeData"></param>
        /// <param name="isRemove"></param>
        public SessionStateTypeEntry(TypeData typeData, bool isRemove)
            : base("*")
        {
            if (typeData == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeData));
            }

            TypeData = typeData;
            IsRemove = isRemove;
        }

        /// <summary>
        /// Shallow-clone this object.
        /// </summary>
        /// <returns>The cloned object.</returns>
        public override InitialSessionStateEntry Clone()
        {
            SessionStateTypeEntry entry;
            if (FileName != null)
            {
                entry = new SessionStateTypeEntry(FileName);
            }
            else if (TypeTable != null)
            {
                entry = new SessionStateTypeEntry(TypeTable);
            }
            else
            {
                entry = new SessionStateTypeEntry(TypeData, IsRemove);
            }

            entry.SetPSSnapIn(this.PSSnapIn);
            entry.SetModule(this.Module);
            return entry;
        }

        /// <summary>
        /// The pathname of the types.ps1xml file. This can be null if
        /// TypeTable constructor or TypeData constructor is used.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The TypeTable specified with constructor. This can be null if
        /// FileName constructor or TypeData constructor is used.
        /// </summary>
        public TypeTable TypeTable { get; }

        /// <summary>
        /// The TypeData we want to update with. This can be null if
        /// FileName constructor or TypeTable constructor is used.
        /// </summary>
        public TypeData TypeData { get; }

        /// <summary>
        /// The operation will be done on the typedata. This is only
        /// meaningful when the TypeData constructor is used.
        /// </summary>
        public bool IsRemove { get; }

        // So that we can specify the type information on the fly,
        // without using Types.ps1xml file
        // public SessionStateTypeEntry(string name, xmlreader definition);
        // public string Definition { get; }
    }

    /// <summary>
    /// Format file configuration entry...
    /// </summary>
    public sealed class SessionStateFormatEntry : InitialSessionStateEntry
    {
        /// <summary>
        /// Loads the entire formats file.
        /// </summary>
        /// <param name="fileName"></param>
        public SessionStateFormatEntry(string fileName)
            : base("*")
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw PSTraceSource.NewArgumentException(nameof(fileName));
            }

            FileName = fileName.Trim();
        }

        /// <summary>
        /// Loads all the format data specified in the formatTable.
        /// </summary>
        /// <param name="formattable"></param>
        public SessionStateFormatEntry(FormatTable formattable)
            : base("*")
        {
            if (formattable == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(formattable));
            }

            Formattable = formattable;
        }

        /// <summary>
        /// Loads all the format data specified in the typeDefinition.
        /// </summary>
        /// <param name="typeDefinition"></param>
        public SessionStateFormatEntry(ExtendedTypeDefinition typeDefinition)
            : base("*")
        {
            if (typeDefinition == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(typeDefinition));
            }

            FormatData = typeDefinition;
        }

        /// <summary>
        /// Shallow-clone this object...
        /// </summary>
        /// <returns>The cloned object.</returns>
        public override InitialSessionStateEntry Clone()
        {
            SessionStateFormatEntry entry;

            if (FileName != null)
            {
                entry = new SessionStateFormatEntry(FileName);
            }
            else if (Formattable != null)
            {
                entry = new SessionStateFormatEntry(Formattable);
            }
            else
            {
                entry = new SessionStateFormatEntry(FormatData);
            }

            entry.SetPSSnapIn(this.PSSnapIn);
            entry.SetModule(this.Module);
            return entry;
        }

        /// <summary>
        /// The name of the format file referenced by this entry...
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The FormatTable specified with constructor. This can be null if
        /// FileName constructor is used.
        /// </summary>
        public FormatTable Formattable { get; }

        /// <summary>
        /// The FormatData specified with constructor.
        /// This can be null if the FileName or FormatTable constructors are used.
        /// </summary>
        public ExtendedTypeDefinition FormatData { get; }

        // So that we can specify the format information on the fly,
        // without using Format.ps1xml file
        // public SessionStateFormatEntry(string name, xmlreader definition);
        // public string Definition { get; }
    }

    /// <summary>
    /// An assembly to load for this sessionstate...
    /// </summary>
    public sealed class SessionStateAssemblyEntry : InitialSessionStateEntry
    {
        /// <summary>
        /// Create a named entry for the assembly to load with both the
        /// name and the path to the assembly as a backup.
        /// </summary>
        /// <param name="name">The name of the assembly to load.</param>
        /// <param name="fileName">The path to the assembly to use as an alternative.</param>
        public SessionStateAssemblyEntry(string name, string fileName)
            : base(name)
        {
            FileName = fileName;
        }

        /// <summary>
        /// Create a named entry for the assembly to load, specifying
        /// just the name.
        /// </summary>
        /// <param name="name">The name of the assembly to load.</param>
        public SessionStateAssemblyEntry(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Shallow-clone this object.
        /// </summary>
        /// <returns>The cloned object.</returns>
        public override InitialSessionStateEntry Clone()
        {
            var entry = new SessionStateAssemblyEntry(Name, FileName);
            entry.SetPSSnapIn(this.PSSnapIn);
            entry.SetModule(this.Module);
            return entry;
        }

        /// <summary>
        /// Return the assembly file name...
        /// </summary>
        public string FileName { get; }
    }

    /// <summary>
    /// List a cmdlet to add to this session state entry.
    /// </summary>
    public sealed class SessionStateCmdletEntry : SessionStateCommandEntry
    {
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="implementingType"></param>
        /// <param name="helpFileName"></param>
        public SessionStateCmdletEntry(string name, Type implementingType, string helpFileName)
            : base(name, SessionStateEntryVisibility.Public)
        {
            ImplementingType = implementingType;
            HelpFileName = helpFileName;
            CommandType = CommandTypes.Cmdlet;
        }

        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="implementingType"></param>
        /// <param name="helpFileName"></param>
        /// <param name="visibility"></param>
        internal SessionStateCmdletEntry(string name, Type implementingType, string helpFileName, SessionStateEntryVisibility visibility)
            : base(name, visibility)
        {
            ImplementingType = implementingType;
            HelpFileName = helpFileName;
            CommandType = CommandTypes.Cmdlet;
        }

        /// <summary>
        /// Shallow-clone this object...
        /// </summary>
        /// <returns></returns>
        public override InitialSessionStateEntry Clone()
        {
            SessionStateCmdletEntry entry = new SessionStateCmdletEntry(Name, ImplementingType, HelpFileName, Visibility);
            entry.SetPSSnapIn(this.PSSnapIn);
            entry.SetModule(this.Module);
            return entry;
        }

        /// <summary>
        /// </summary>
        public Type ImplementingType { get; }

        /// <summary>
        /// </summary>
        public string HelpFileName { get; }
    }

    /// <summary>
    /// </summary>
    public sealed class SessionStateProviderEntry : ConstrainedSessionStateEntry
    {
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="implementingType"></param>
        /// <param name="helpFileName"></param>
        public SessionStateProviderEntry(string name, Type implementingType, string helpFileName)
            : base(name, SessionStateEntryVisibility.Public)
        {
            ImplementingType = implementingType;
            HelpFileName = helpFileName;
        }

        internal SessionStateProviderEntry(string name, Type implementingType, string helpFileName, SessionStateEntryVisibility visibility)
            : base(name, visibility)
        {
            ImplementingType = implementingType;
            HelpFileName = helpFileName;
        }

        /// <summary>
        /// Shallow-clone this object...
        /// </summary>
        /// <returns>The cloned object.</returns>
        public override InitialSessionStateEntry Clone()
        {
            SessionStateProviderEntry entry = new SessionStateProviderEntry(Name, ImplementingType, HelpFileName, this.Visibility);
            entry.SetPSSnapIn(this.PSSnapIn);
            entry.SetModule(this.Module);
            return entry;
        }

        /// <summary>
        /// </summary>
        public Type ImplementingType { get; }

        /// <summary>
        /// </summary>
        public string HelpFileName { get; }
    }

    /// <summary>
    /// </summary>
    public sealed class SessionStateScriptEntry : SessionStateCommandEntry
    {
        /// <summary>
        /// Create a session state command entry instance.
        /// </summary>
        /// <param name="path">The path to the script.</param>
        public SessionStateScriptEntry(string path)
            : base(path, SessionStateEntryVisibility.Public)
        {
            Path = path;
            CommandType = CommandTypes.ExternalScript;
        }

        /// <summary>
        /// Create a session state command entry instance with the specified visibility.
        /// </summary>
        /// <param name="path">The path to the script.</param>
        /// <param name="visibility">Visibility of the script.</param>
        internal SessionStateScriptEntry(string path, SessionStateEntryVisibility visibility)
            : base(path, visibility)
        {
            Path = path;
            CommandType = CommandTypes.ExternalScript;
        }

        /// <summary>
        /// Shallow-clone this object...
        /// </summary>
        /// <returns>The cloned object.</returns>
        public override InitialSessionStateEntry Clone()
        {
            SessionStateScriptEntry entry = new SessionStateScriptEntry(Path, Visibility);
            entry.SetModule(this.Module);
            return entry;
        }

        /// <summary>
        /// </summary>
        public string Path { get; }
    }

    /// <summary>
    /// </summary>
    public sealed class SessionStateAliasEntry : SessionStateCommandEntry
    {
        /// <summary>
        /// Define an alias entry to add to the initial session state.
        /// </summary>
        /// <param name="name">The name of the alias entry to add.</param>
        /// <param name="definition">The name of the command it resolves to.</param>
        public SessionStateAliasEntry(string name, string definition)
            : base(name, SessionStateEntryVisibility.Public)
        {
            Definition = definition;
            CommandType = CommandTypes.Alias;
        }

        /// <summary>
        /// Define an alias entry to add to the initial session state.
        /// </summary>
        /// <param name="name">The name of the alias entry to add.</param>
        /// <param name="definition">The name of the command it resolves to.</param>
        /// <param name="description">A description of the purpose of the alias.</param>
        public SessionStateAliasEntry(string name, string definition, string description)
            : base(name, SessionStateEntryVisibility.Public)
        {
            Definition = definition;
            CommandType = CommandTypes.Alias;
            Description = description;
        }

        /// <summary>
        /// Define an alias entry to add to the initial session state.
        /// </summary>
        /// <param name="name">The name of the alias entry to add.</param>
        /// <param name="definition">The name of the command it resolves to.</param>
        /// <param name="description">A description of the purpose of the alias.</param>
        /// <param name="options">Options defining the scope visibility, readonly and constant.</param>
        public SessionStateAliasEntry(string name, string definition, string description, ScopedItemOptions options)
            : base(name, SessionStateEntryVisibility.Public)
        {
            Definition = definition;
            CommandType = CommandTypes.Alias;
            Description = description;
            Options = options;
        }

        /// <summary>
        /// Define an alias entry to add to the initial session state.
        /// </summary>
        /// <param name="name">The name of the alias entry to add.</param>
        /// <param name="definition">The name of the command it resolves to.</param>
        /// <param name="description">A description of the purpose of the alias.</param>
        /// <param name="options">Options defining the scope visibility, readonly and constant.</param>
        /// <param name="visibility"></param>
        internal SessionStateAliasEntry(string name, string definition, string description, ScopedItemOptions options, SessionStateEntryVisibility visibility)
            : base(name, visibility)
        {
            Definition = definition;
            CommandType = CommandTypes.Alias;
            Description = description;
            Options = options;
        }
        /// <summary>
        /// Shallow-clone this object...
        /// </summary>
        /// <returns>The cloned object.</returns>
        public override InitialSessionStateEntry Clone()
        {
            SessionStateAliasEntry entry = new SessionStateAliasEntry(Name, Definition, Description, Options, Visibility);
            entry.SetModule(this.Module);
            return entry;
        }

        /// <summary>
        /// The string defining the body of this alias...
        /// </summary>
        public string Definition { get; }

        /// <summary>
        /// A string describing this alias...
        /// </summary>
        public string Description { get; } = string.Empty;

        /// <summary>
        /// Options controlling scope visibility and setability for this entry.
        /// </summary>
        public ScopedItemOptions Options { get; } = ScopedItemOptions.None;
    }

    /// <summary>
    /// </summary>
    public sealed class SessionStateApplicationEntry : SessionStateCommandEntry
    {
        /// <summary>
        /// Used to define a permitted script in this session state. If the path is
        /// "*", then any path is permitted.
        /// </summary>
        /// <param name="path">The full path to the application.</param>
        public SessionStateApplicationEntry(string path)
            : base(path, SessionStateEntryVisibility.Public)
        {
            Path = path;
            CommandType = CommandTypes.Application;
        }

        /// <summary>
        /// Used to define a permitted script in this session state. If the path is
        /// "*", then any path is permitted.
        /// </summary>
        /// <param name="path">The full path to the application.</param>
        /// <param name="visibility">Sets the external visibility of the path.</param>
        internal SessionStateApplicationEntry(string path, SessionStateEntryVisibility visibility)
            : base(path, visibility)
        {
            Path = path;
            CommandType = CommandTypes.Application;
        }

        /// <summary>
        /// Shallow-clone this object...
        /// </summary>
        /// <returns>The cloned object.</returns>
        public override InitialSessionStateEntry Clone()
        {
            SessionStateApplicationEntry entry = new SessionStateApplicationEntry(Path, Visibility);
            entry.SetModule(this.Module);
            return entry;
        }

        /// <summary>
        /// The path to this application...
        /// </summary>
        public string Path { get; }
    }

    /// <summary>
    /// </summary>
    public sealed class SessionStateFunctionEntry : SessionStateCommandEntry
    {
        /// <summary>
        /// Represents a function definition in an Initial session state object.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="definition">The definition of the function.</param>
        /// <param name="options">Options controlling scope-related elements of this object.</param>
        /// <param name="helpFile">The name of the help file associated with the function.</param>
        public SessionStateFunctionEntry(string name, string definition, ScopedItemOptions options, string helpFile)
            : base(name, SessionStateEntryVisibility.Public)
        {
            Definition = definition;
            CommandType = CommandTypes.Function;
            Options = options;
            ScriptBlock = ScriptBlock.Create(Definition);
            ScriptBlock.LanguageMode = PSLanguageMode.FullLanguage;

            HelpFile = helpFile;
        }

        /// <summary>
        /// Represents a function definition in an Initial session state object.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="definition">The definition of the function.</param>
        /// <param name="helpFile">The name of the help file associated with the function.</param>
        public SessionStateFunctionEntry(string name, string definition, string helpFile)
            : this(name, definition, ScopedItemOptions.None, helpFile)
        {
        }

        /// <summary>
        /// Represents a function definition in an Initial session state object.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="definition">The definition of the function.</param>
        public SessionStateFunctionEntry(string name, string definition)
            : this(name, definition, ScopedItemOptions.None, null)
        {
        }

        /// <summary>
        /// This is an internal copy constructor.
        /// </summary>
        internal SessionStateFunctionEntry(string name, string definition, ScopedItemOptions options,
            SessionStateEntryVisibility visibility, ScriptBlock scriptBlock, string helpFile)
            : base(name, visibility)
        {
            Definition = definition;
            CommandType = CommandTypes.Function;
            Options = options;
            ScriptBlock = scriptBlock;
            HelpFile = helpFile;
        }

        internal static SessionStateFunctionEntry GetDelayParsedFunctionEntry(string name, string definition, bool isProductCode, PSLanguageMode languageMode)
        {
            var fnEntry = GetDelayParsedFunctionEntry(name, definition, isProductCode);
            fnEntry.ScriptBlock.LanguageMode = languageMode;
            return fnEntry;
        }

        internal static SessionStateFunctionEntry GetDelayParsedFunctionEntry(string name, string definition, bool isProductCode)
        {
            var sb = ScriptBlock.CreateDelayParsedScriptBlock(definition, isProductCode);
            return new SessionStateFunctionEntry(name, definition, ScopedItemOptions.None, SessionStateEntryVisibility.Public, sb, null);
        }

        internal static SessionStateFunctionEntry GetDelayParsedFunctionEntry(string name, string definition, ScriptBlock sb)
        {
            return new SessionStateFunctionEntry(name, definition, ScopedItemOptions.None, SessionStateEntryVisibility.Public, sb, null);
        }

        /// <summary>
        /// Shallow-clone this object...
        /// </summary>
        /// <returns>The cloned object.</returns>
        public override InitialSessionStateEntry Clone()
        {
            SessionStateFunctionEntry entry = new SessionStateFunctionEntry(Name, Definition, Options, Visibility, ScriptBlock, HelpFile);
            entry.SetModule(this.Module);
            return entry;
        }

        /// <summary>
        /// Sets the name of the help file associated with the function.
        /// </summary>
        internal void SetHelpFile(string help)
        {
            HelpFile = help;
        }

        /// <summary>
        /// The string to use to define this function...
        /// </summary>
        public string Definition { get; }

        /// <summary>
        /// The script block for this function.
        /// </summary>
        internal ScriptBlock ScriptBlock { get; set; }

        /// <summary>
        /// Options controlling scope visibility and setability for this entry.
        /// </summary>
        public ScopedItemOptions Options { get; } = ScopedItemOptions.None;

        /// <summary>
        /// The name of the help file associated with the function.
        /// </summary>
        public string HelpFile { get; private set; }
    }

    /// <summary>
    /// </summary>
    public sealed class SessionStateVariableEntry : ConstrainedSessionStateEntry
    {
        /// <summary>
        /// Is used to define a variable that should be created when
        /// the runspace is opened. Note - if this object is cloned,
        /// then the clone will contain a reference to the original object
        /// not a clone of it.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value to set the variable to.</param>
        /// <param name="description">A descriptive string to attach to the variable.</param>
        public SessionStateVariableEntry(string name, object value, string description)
            : base(name, SessionStateEntryVisibility.Public)
        {
            Value = value;
            Description = description;
        }

        /// <summary>
        /// Is used to define a variable that should be created when
        /// the runspace is opened. Note - if this object is cloned,
        /// then the clone will contain a reference to the original object
        /// not a clone of it.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value to set the variable to.</param>
        /// <param name="description">A descriptive string to attach to the variable.</param>
        /// <param name="options">Options like readonly, constant, allscope, etc.</param>
        public SessionStateVariableEntry(string name, object value, string description, ScopedItemOptions options)
            : base(name, SessionStateEntryVisibility.Public)
        {
            Value = value;
            Description = description;
            Options = options;
        }

        /// <summary>
        /// Is used to define a variable that should be created when
        /// the runspace is opened. Note - if this object is cloned,
        /// then the clone will contain a reference to the original object
        /// not a clone of it.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value to set the variable to.</param>
        /// <param name="description">A descriptive string to attach to the variable.</param>
        /// <param name="options">Options like readonly, constant, allscope, etc.</param>
        /// <param name="attributes">A list of attributes to attach to the variable.</param>
        public SessionStateVariableEntry(string name, object value, string description, ScopedItemOptions options, Collection<Attribute> attributes)
            : base(name, SessionStateEntryVisibility.Public)
        {
            Value = value;
            Description = description;
            Options = options;
            _attributes = attributes;
        }

        /// <summary>
        /// Is used to define a variable that should be created when
        /// the runspace is opened. Note - if this object is cloned,
        /// then the clone will contain a reference to the original object
        /// not a clone of it.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value to set the variable to.</param>
        /// <param name="description">A descriptive string to attach to the variable.</param>
        /// <param name="options">Options like readonly, constant, allscope, etc.</param>
        /// <param name="attribute">A single attribute to attach to the variable.</param>
        public SessionStateVariableEntry(string name, object value, string description, ScopedItemOptions options, Attribute attribute)
            : base(name, SessionStateEntryVisibility.Public)
        {
            Value = value;
            Description = description;
            Options = options;
            _attributes = new Collection<Attribute>();
            _attributes.Add(attribute);
        }

        /// <summary>
        /// Is used to define a variable that should be created when
        /// the runspace is opened. Note - if this object is cloned,
        /// then the clone will contain a reference to the original object
        /// not a clone of it.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value to set the variable to.</param>
        /// <param name="description">A descriptive string to attach to the variable.</param>
        /// <param name="options">Options like readonly, constant, allscope, etc.</param>
        /// <param name="attributes">A single attribute to attach to the variable.</param>
        /// <param name="visibility"></param>
        internal SessionStateVariableEntry(string name, object value, string description, ScopedItemOptions options, Collection<Attribute> attributes, SessionStateEntryVisibility visibility)
            : base(name, visibility)
        {
            Value = value;
            Description = description;
            Options = options;
            _attributes = attributes;
        }

        /// <summary>
        /// Shallow-clone this object...
        /// </summary>
        /// <returns>The cloned object.</returns>
        public override InitialSessionStateEntry Clone()
        {
            // Copy the attribute collection if necessary...
            Collection<Attribute> attrs = null;
            if (_attributes != null && _attributes.Count > 0)
            {
                attrs = new Collection<Attribute>(_attributes);
            }

            return new SessionStateVariableEntry(Name, Value, Description, Options, attrs, Visibility);
        }

        /// <summary>
        /// The value to bind to this variable.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// The description associated with this variable.
        /// </summary>
        public string Description { get; } = string.Empty;

        /// <summary>
        /// The options associated with this variable (e.g. readonly, allscope, etc.)
        /// </summary>
        public ScopedItemOptions Options { get; } = ScopedItemOptions.None;

        /// <summary>
        /// The attributes that will be attached to this object.
        /// </summary>
        public Collection<Attribute> Attributes
        {
            get { return _attributes ??= new Collection<Attribute>(); }
        }

        private Collection<Attribute> _attributes;
    }

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class InitialSessionStateEntryCollection<T> : IEnumerable<T> where T : InitialSessionStateEntry
    {
        /// <summary>
        /// Create an empty collection...
        /// </summary>
        public InitialSessionStateEntryCollection()
        {
            _internalCollection = new Collection<T>();
        }

        /// <summary>
        /// Create an new collection, copying in the passed items...
        /// </summary>
        /// <param name="items"></param>
        public InitialSessionStateEntryCollection(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            _internalCollection = new Collection<T>();

            foreach (T item in items)
            {
                _internalCollection.Add(item);
            }
        }

        /// <summary>
        /// Clone this collection.
        /// </summary>
        /// <returns>The cloned collection.</returns>
        public InitialSessionStateEntryCollection<T> Clone()
        {
            InitialSessionStateEntryCollection<T> result;
            lock (_syncObject)
            {
                result = new InitialSessionStateEntryCollection<T>();

                foreach (T item in _internalCollection)
                {
                    result.Add((T)item.Clone());
                }
            }

            return result;
        }

        /// <summary>
        /// Reset the collection.
        /// </summary>
        public void Reset()
        {
            lock (_syncObject)
            {
                _internalCollection.Clear();
            }
        }

        /// <summary>
        /// Returns a count of the number of items in the collection...
        /// </summary>
        public int Count
        {
            get { return _internalCollection.Count; }
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                T result;
                lock (_syncObject)
                {
                    result = _internalCollection[index];
                }

                return result;
            }
        }

        /// <summary>
        /// To find the entries based on name.
        /// Why collection - Different SnapIn/modules and same entity names.
        /// If used on command collection entry, then for the same name, one can have multiple output.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Collection<T> this[string name]
        {
            get
            {
                Collection<T> result = new Collection<T>();
                lock (_syncObject)
                {
                    foreach (T element in _internalCollection)
                    {
                        if (element.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        {
                            result.Add(element);
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Find entries based on string name which can include wildcards.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Collection<T> LookUpByName(string name)
        {
            if (name == null)
            {
                throw new PSArgumentNullException(nameof(name));
            }

            Collection<T> result = new Collection<T>();
            WildcardPattern namePattern = WildcardPattern.Get(name, WildcardOptions.IgnoreCase);
            lock (_syncObject)
            {
                foreach (T element in _internalCollection)
                {
                    if (namePattern.IsMatch(element.Name))
                    {
                        result.Add(element);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        public void RemoveItem(int index)
        {
            lock (_syncObject)
            {
                _internalCollection.RemoveAt(index);
            }
        }

        /// <summary>
        /// Remove a number of items starting at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public void RemoveItem(int index, int count)
        {
            lock (_syncObject)
            {
                while (count-- > 0)
                {
                    _internalCollection.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Clears the collection...
        /// </summary>
        public void Clear()
        {
            lock (_syncObject)
            {
                _internalCollection.Clear();
            }
        }

        /// <summary>
        /// This overload exists so that we can remove items based on the item name, rather than
        /// its position in the collection. The type argument can be null but we'll throw an error if
        /// we can't distinguish between multiple entries of the same name but different types
        /// and the type hasn't been specified.
        /// BUGBUG - brucepay - the throw thing is not implemented yet...
        /// </summary>
        /// <param name="name">The name of the element to remove.</param>
        /// <param name="type">The type of object to remove, can be null to remove any type.</param>
        public void Remove(string name, object type)
        {
            ArgumentNullException.ThrowIfNull(name);

            lock (_syncObject)
            {
                Type objType = null;

                if (type != null)
                {
                    objType = type as Type ?? type.GetType();
                }

                // Work backwards through the collection...
                for (int i = _internalCollection.Count - 1; i >= 0; i--)
                {
                    T element = _internalCollection[i];
                    if (element == null)
                    {
                        continue;
                    }

                    if ((objType == null || element.GetType() == objType) &&
                        string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        _internalCollection.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Add an item to this collection.
        /// </summary>
        /// <param name="item">The item to add...</param>
        public void Add(T item)
        {
            ArgumentNullException.ThrowIfNull(item);

            lock (_syncObject)
            {
                _internalCollection.Add(item);
            }
        }

        /// <summary>
        /// Add items to this collection.
        /// </summary>
        /// <param name="items"></param>
        public void Add(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            lock (_syncObject)
            {
                foreach (T element in items)
                {
                    _internalCollection.Add(element);
                }
            }
        }

        /// <summary>
        /// Get enumerator for this collection.
        /// </summary>
        /// <returns></returns>
        /// <!--
        /// Enumerator work is not thread safe by default. Any code trying
        /// to do enumeration on this collection should lock it first.
        ///
        /// Need to document this.
        /// -->
        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _internalCollection.GetEnumerator();
        }

        /// <summary>
        /// Get enumerator for this collection.
        /// </summary>
        /// <returns></returns>
        /// <!--
        /// Enumerator work is not thread safe by default. Any code trying
        /// to do enumeration on this collection should lock it first.
        ///
        /// Need to document this.
        /// -->
        IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            return _internalCollection.GetEnumerator();
        }

        private readonly Collection<T> _internalCollection;

        // object to use for locking
        private readonly object _syncObject = new object();
    }

    /// <summary>
    /// Allows you to define the set of elements that should be
    /// present when Session State is created.
    /// </summary>
    public class InitialSessionState
    {
        #region Helper methods for restricting commands needed by implicit and interactive remoting

        private static void RemoveDisallowedEntries<T>(InitialSessionStateEntryCollection<T> list, List<string> allowedNames, Func<T, string> nameGetter)
            where T : InitialSessionStateEntry
        {
            List<string> namesToRemove = new List<string>();
            foreach (T entry in list)
            {
                string entryName = nameGetter(entry);

                // if entryName is not present in allowedNames list, then remove it
                if (!allowedNames.Exists(allowedName => allowedName.Equals(entryName, StringComparison.OrdinalIgnoreCase)))
                {
                    namesToRemove.Add(entry.Name);
                }
            }

            foreach (string nameToRemove in namesToRemove)
            {
                list.Remove(nameToRemove, null /* remove any type with this name */);
            }
        }

        private static void MakeDisallowedEntriesPrivate<T>(InitialSessionStateEntryCollection<T> list, List<string> allowedNames, Func<T, string> nameGetter)
            where T : ConstrainedSessionStateEntry
        {
            foreach (T entry in list)
            {
                string entryName = nameGetter(entry);

                // Aliases to allowed commands are OK
                SessionStateAliasEntry aliasEntry = entry as SessionStateAliasEntry;
                if (aliasEntry != null)
                {
                    if (allowedNames.Exists(allowedName => allowedName.Equals(aliasEntry.Definition, StringComparison.OrdinalIgnoreCase)))
                    {
                        aliasEntry.Visibility = SessionStateEntryVisibility.Public;
                        continue;
                    }
                }

                // if entryName is not present in allowedNames list, then remove it
                if (!allowedNames.Exists(allowedName => allowedName.Equals(entryName, StringComparison.OrdinalIgnoreCase)))
                {
                    entry.Visibility = SessionStateEntryVisibility.Private;
                }
            }
        }

        /// <summary>
        /// Creates an initial session state from a PSSC configuration file.
        /// </summary>
        /// <param name="path">The path to the PSSC session configuration file.</param>
        /// <returns>InitialSessionState object.</returns>
        public static InitialSessionState CreateFromSessionConfigurationFile(string path)
        {
            return CreateFromSessionConfigurationFile(path, null);
        }

        /// <summary>
        /// Creates an initial session state from a PSSC configuration file.
        /// </summary>
        /// <param name="path">The path to the PSSC session configuration file.</param>
        /// <param name="roleVerifier">
        /// The verifier that PowerShell should call to determine if groups in the Role entry apply to the
        /// target session. If you have a WindowsPrincipal for a user, for example, create a Function that
        /// checks windowsPrincipal.IsInRole().
        /// </param>
        /// <returns>InitialSessionState object.</returns>
        public static InitialSessionState CreateFromSessionConfigurationFile(
            string path,
            Func<string, bool> roleVerifier)
        {
            return CreateFromSessionConfigurationFile(path, roleVerifier, validateFile: false);
        }

        /// <summary>
        /// Creates an initial session state from a PSSC configuration file.
        /// </summary>
        /// <param name="path">The path to the PSSC session configuration file.</param>
        /// <param name="roleVerifier">
        /// The verifier that PowerShell should call to determine if groups in the Role entry apply to the
        /// target session. If you have a WindowsPrincipal for a user, for example, create a Function that
        /// checks windowsPrincipal.IsInRole().
        /// </param>
        /// <param name="validateFile">Validates the file contents for supported SessionState options.</param>
        /// <returns>InitialSessionState object.</returns>
        public static InitialSessionState CreateFromSessionConfigurationFile(
            string path,
            Func<string, bool> roleVerifier,
            bool validateFile)
        {
            if (path is null)
            {
                throw new PSArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new PSInvalidOperationException(
                    StringUtil.Format(ConsoleInfoErrorStrings.ConfigurationFileDoesNotExist, path));
            }

            if (!path.EndsWith(".pssc", StringComparison.OrdinalIgnoreCase))
            {
                throw new PSInvalidOperationException(
                    StringUtil.Format(ConsoleInfoErrorStrings.NotConfigurationFile, path));
            }

            Remoting.DISCPowerShellConfiguration discConfiguration = new Remoting.DISCPowerShellConfiguration(path, roleVerifier, validateFile);
            return discConfiguration.GetInitialSessionState(null);
        }

        /// <summary>
        /// Creates an <see cref="InitialSessionState"/> instance that exposes only the minimal
        /// set of commands needed by give set of <paramref name="sessionCapabilities"/>.
        /// All commands that are not needed are made private in order to minimize the attack surface.
        /// </summary>
        /// <param name="sessionCapabilities">
        /// What capabilities the session should have.
        /// </param>
        /// <returns></returns>
        public static InitialSessionState CreateRestricted(SessionCapabilities sessionCapabilities)
        {
            // only remote server has been requested
            if (sessionCapabilities == SessionCapabilities.RemoteServer)
            {
                return CreateRestrictedForRemoteServer();
            }

            return Create();
        }

        private static InitialSessionState CreateRestrictedForRemoteServer()
        {
            InitialSessionState iss = Create();
            iss.LanguageMode = PSLanguageMode.NoLanguage;
            iss.ThrowOnRunspaceOpenError = true;
            iss.UseFullLanguageModeInDebugger = false;

            iss.Commands.Add(BuiltInFunctions);
            iss.Commands.Add(BuiltInAliases);

            // Load the default snapins - all commands will be private by default.
            Collection<PSSnapInInfo> defaultSnapins = PSSnapInReader.ReadEnginePSSnapIns();
            foreach (PSSnapInInfo si in defaultSnapins)
            {
                // ImportPSSnapIn always sets "out warning" to "null";  all our internal calls ignore/discard "out warning"
                PSSnapInException warning;
                iss.ImportPSSnapIn(si, out warning);
            }

            // restrict what gets exposed
            List<string> allowedCommands = new List<string>();

            // required by implicit remoting and interactive remoting
            allowedCommands.Add("Get-Command");
            allowedCommands.Add("Get-FormatData");
            allowedCommands.Add("Clear-Host");
            allowedCommands.Add("Select-Object"); // used to serialize exactly the properties that we need (+ at the right depth)
            // used if available by implicit remoting
            allowedCommands.Add("Get-Help"); // used when displaying help for implicit remoting proxy functions
            allowedCommands.Add("Measure-Object"); // used to have nice progress bars when import/export-pssession is running
            // required by interactive remoting
            allowedCommands.Add("Out-Default"); // appended to every command line
            allowedCommands.Add("Exit-PSSession"); // used by the user to exit the session

            // We don't remove these entries so that they can be called by commands in the runspace.
            // Setting them to 'Private' ensures that the user can't call them.
            MakeDisallowedEntriesPrivate(
                iss.Commands,
                allowedCommands,
                commandEntry => commandEntry.Name);

            // Ensure that only PowerShell core formats are included in the restricted session.
            IncludePowerShellCoreFormats(iss);

            List<string> allowedTypes = new List<string>();
            allowedTypes.Add("types.ps1xml");
            allowedTypes.Add("typesV3.ps1xml");
            RemoveDisallowedEntries(
                iss.Types,
                allowedTypes,
                typeEntry => IO.Path.GetFileName(typeEntry.FileName));

            // No providers are visible by default
            foreach (SessionStateProviderEntry provider in iss.Providers)
            {
                provider.Visibility = SessionStateEntryVisibility.Private;
            }

            // Add built-in variables.
            iss.Variables.Add(BuiltInVariables);

            // wrap some commands in a proxy function to restrict their parameters
            foreach (KeyValuePair<string, CommandMetadata> proxyFunction in CommandMetadata.GetRestrictedCommands(SessionCapabilities.RemoteServer))
            {
                string commandName = proxyFunction.Key;

                // make the cmdlet private
                Collection<SessionStateCommandEntry> originalCmdlet = iss.Commands[commandName];
                Diagnostics.Assert(originalCmdlet != null, "Proxied cmdlets should be imported at this point");
                Diagnostics.Assert(originalCmdlet.Count == 1, "Exactly one cmdlet with a given name");
                originalCmdlet[0].Visibility = SessionStateEntryVisibility.Private;

                // and add a public proxy function
                string proxyBody = ProxyCommand.Create(proxyFunction.Value, string.Empty, false);
                iss.Commands.Add(new SessionStateFunctionEntry(commandName, proxyBody));
            }

            return iss;
        }

        private static void IncludePowerShellCoreFormats(InitialSessionState iss)
        {
            string psHome = Utils.DefaultPowerShellAppBase;
            if (string.IsNullOrEmpty(psHome))
            {
                return;
            }

            iss.Formats.Clear();
            foreach (var coreFormat in Platform.FormatFileNames)
            {
                iss.Formats.Add(new SessionStateFormatEntry(Path.Combine(psHome, coreFormat)));
            }
        }

        #endregion

        /// <summary>
        /// Ctor for Custom-Shell - Do we need this?
        /// </summary>
        protected InitialSessionState()
        {
        }

        // Creates an empty EE
        /// <summary>
        /// Creates an empty InitialSessionState object...
        /// </summary>
        /// <returns></returns>
        public static InitialSessionState Create()
        {
            InitialSessionState iss = new InitialSessionState();

            // TODO: the following code is probably needed for the hosted constrained runspace
            // There are too many things that depend on the built-in variables. At the same time,
            // these variables can't be public or they become a security issue.
            // This change still needs to be spec-reviewed before turning it on. It also seems to
            // be causing test failures - i suspect due to lack test isolation - brucepay Mar 06/2008
#if false
            // Add the default variables and make them private...
            iss.AddVariables(BuiltInVariables);
            foreach (SessionStateVariableEntry v in iss.Variables)
            {
                v.Visibility = SessionStateEntryVisibility.Private;
            }
#endif
            return iss;
        }

        /// <summary>
        /// Creates the default PowerShell one with default cmdlets, provider etc.
        /// BuiltIn functions, aliases need to be available through default
        /// InitialSessionstate constructor. Need to have this discussion for packaging as well.
        /// </summary>
        /// <returns></returns>
        public static InitialSessionState CreateDefault()
        {
            // Read all of the registered PSSnapins
            Collection<PSSnapInInfo> defaultSnapins;
            PSSnapInException warning;

            InitialSessionState ss = new InitialSessionState();

            ss.Variables.Add(BuiltInVariables);
            ss.Commands.Add(new SessionStateApplicationEntry("*"));
            ss.Commands.Add(new SessionStateScriptEntry("*"));
            ss.Commands.Add(BuiltInFunctions);
            ss.Commands.Add(BuiltInAliases);

            defaultSnapins = PSSnapInReader.ReadEnginePSSnapIns();

            foreach (PSSnapInInfo si in defaultSnapins)
            {
                try
                {
                    ss.ImportPSSnapIn(si, out warning);
                }
                catch (PSSnapInException)
                {
                    throw;
                }
#if DEBUG
                // NOTE:
                // This code is for testing a module-based shell. It is only available when the shell is complied
                // in debug mode and is not intended to be a feature.
                // July 31 2008 - brucepay
                // Only load the core snapins at this point...
                if (Environment.GetEnvironmentVariable("PowerShellMinimal") != null)
                {
                    if (si.Name.Equals("Microsoft.PowerShell.Host", StringComparison.OrdinalIgnoreCase))
                        break;
                }
#endif
            }

            // Remove duplicated assemblies
            HashSet<string> assemblyList = new HashSet<string>();

            for (int i = ss.Assemblies.Count - 1; i >= 0; i--)
            {
                string assembly = ss.Assemblies[i].FileName;
                if (!string.IsNullOrEmpty(assembly))
                {
                    if (assemblyList.Contains(assembly))
                    {
                        ss.Assemblies.RemoveItem(i);
                    }
                    else
                    {
                        assemblyList.Add(assembly);
                    }
                }
            }

            ss.LanguageMode = PSLanguageMode.FullLanguage;
            ss.AuthorizationManager = new Microsoft.PowerShell.PSAuthorizationManager(Utils.DefaultPowerShellShellID);

            return ss.Clone();
        }

        /// <summary>
        /// Creates the default PowerShell one with default cmdlets, provider etc.
        /// The default cmdlets, provider, etc are loaded via Modules.
        /// For loading Microsoft.PowerShell.Core module only.
        /// </summary>
        /// <returns></returns>
        public static InitialSessionState CreateDefault2()
        {
            InitialSessionState ss = new InitialSessionState();

            ss.Variables.Add(BuiltInVariables);
            ss.Commands.Add(new SessionStateApplicationEntry("*"));
            ss.Commands.Add(new SessionStateScriptEntry("*"));
            ss.Commands.Add(BuiltInFunctions);
            ss.Commands.Add(BuiltInAliases);

            ss.ImportCorePSSnapIn();
            ss.LanguageMode = PSLanguageMode.FullLanguage;
            ss.AuthorizationManager = new Microsoft.PowerShell.PSAuthorizationManager(Utils.DefaultPowerShellShellID);

            return ss.Clone();
        }

        internal static bool IsEngineModule(string moduleName)
        {
            return EngineModules.Contains(moduleName) || NestedEngineModules.Contains(moduleName);
        }

        internal static bool IsNestedEngineModule(string moduleName)
        {
            return NestedEngineModules.Contains(moduleName);
        }

        internal static bool IsConstantEngineModule(string moduleName)
        {
            return ConstantEngineModules.Contains(moduleName) || ConstantEngineNestedModules.Contains(moduleName);
        }

        /// <summary>
        /// Clone this InitialSessionState object. The collections are
        /// recursively cloned as well as the elements in the collections.
        /// Note however, that the contents of the individual entries
        /// are not deep-cloned. This is only an issue for variable
        /// entries which may have reference types. These objects
        /// will be added by reference rather than by value.
        /// </summary>
        /// <returns>The cloned object.</returns>
        public InitialSessionState Clone()
        {
            InitialSessionState ss = new InitialSessionState();

            ss.Variables.Add(this.Variables.Clone());
            ss.EnvironmentVariables.Add(this.EnvironmentVariables.Clone());
            ss.Commands.Add(this.Commands.Clone());
            ss.Assemblies.Add(this.Assemblies.Clone());
            ss.Providers.Add(this.Providers.Clone());
            ss.Types.Add(this.Types.Clone());
            ss.Formats.Add(this.Formats.Clone());

            foreach (string startupScript in this.StartupScripts)
            {
                ss.StartupScripts.Add(startupScript);
            }

            foreach (string unresolvedCommandsToExpose in this.UnresolvedCommandsToExpose)
            {
                ss.UnresolvedCommandsToExpose.Add(unresolvedCommandsToExpose);
            }

            foreach (Hashtable dynamicVariableToDefine in this.DynamicVariablesToDefine)
            {
                ss.DynamicVariablesToDefine.Add(dynamicVariableToDefine);
            }

            foreach (var pair in this.CommandModifications)
            {
                ss.CommandModifications.Add(pair.Key, pair.Value);
            }

            ss.DefaultCommandVisibility = this.DefaultCommandVisibility;
            ss.AuthorizationManager = this.AuthorizationManager;
            ss.LanguageMode = this.LanguageMode;
            ss.TranscriptDirectory = this.TranscriptDirectory;
            ss.UserDriveEnabled = this.UserDriveEnabled;
            ss.UserDriveUserName = this.UserDriveUserName;
            ss.UserDriveMaximumSize = this.UserDriveMaximumSize;

            if (_wasExecutionPolicySet)
            {
                ss.ExecutionPolicy = this.ExecutionPolicy;
            }

            ss.UseFullLanguageModeInDebugger = this.UseFullLanguageModeInDebugger;
            ss.ThreadOptions = this.ThreadOptions;
            ss.ThrowOnRunspaceOpenError = this.ThrowOnRunspaceOpenError;
            ss.ApartmentState = this.ApartmentState;

            foreach (ModuleSpecification modSpec in this.ModuleSpecificationsToImport)
            {
                ss.ModuleSpecificationsToImport.Add(modSpec);
            }

            foreach (string mod in this.CoreModulesToImport)
            {
                ss.CoreModulesToImport.Add(mod);
            }

            ss.DisableFormatUpdates = this.DisableFormatUpdates;

            foreach (var s in ImportedSnapins)
            {
                ss.ImportedSnapins.Add(s.Key, s.Value);
            }

            return ss;
        }

        /// <summary>
        /// Want to get away from SnapIn and console file. Have modules and assemblies instead.
        /// Specify the registered SnapIn name or name collection.
        /// </summary>
        /// <param name="snapInName"></param>
        /// <returns></returns>
        public static InitialSessionState Create(string snapInName)
        {
            return new InitialSessionState();
        }

        /// <summary>
        /// </summary>
        /// <param name="snapInNameCollection"></param>
        /// <param name="warning"></param>
        /// <returns></returns>
        public static InitialSessionState Create(string[] snapInNameCollection, out PSConsoleLoadException warning)
        {
            warning = null;
            return new InitialSessionState();
        }

        /// <summary>
        /// </summary>
        /// <param name="snapInPath"></param>
        /// <param name="warnings"></param>
        /// <returns></returns>
        public static InitialSessionState CreateFrom(string snapInPath, out PSConsoleLoadException warnings)
        {
            warnings = null;
            return new InitialSessionState();
        }

        /// <summary>
        /// </summary>
        /// <param name="snapInPathCollection"></param>
        /// <param name="warnings"></param>
        /// <returns></returns>
        public static InitialSessionState CreateFrom(string[] snapInPathCollection, out PSConsoleLoadException warnings)
        {
            warnings = null;
            return new InitialSessionState();
        }

        /// <summary>
        /// Specifies the language mode to be used for this session state instance.
        /// </summary>
        public PSLanguageMode LanguageMode { get; set; } = PSLanguageMode.NoLanguage;

        /// <summary>
        /// Specifies the directory to be used for collection session transcripts.
        /// </summary>
        public string TranscriptDirectory { get; set; } = null;

        /// <summary>
        /// True when session opted for a User PSDrive.
        /// </summary>
        internal bool UserDriveEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// User name for the user drive.  This will be part of the root path for the User PSDrive.
        /// </summary>
        internal string UserDriveUserName
        {
            get;
            set;
        }

        /// <summary>
        /// Optional maximum size value for User drive (in bytes).
        /// </summary>
        internal long UserDriveMaximumSize
        {
            get;
            set;
        }

        /// <summary>
        /// Forces all session script input parameters to have validation.
        /// </summary>
        internal bool EnforceInputParameterValidation
        {
            get;
            set;
        }

        /// <summary>
        /// Specifies the execution policy to be used for this session state instance.
        /// </summary>
        public Microsoft.PowerShell.ExecutionPolicy ExecutionPolicy
        {
            get
            {
                return _executionPolicy;
            }

            set
            {
                _executionPolicy = value;
                _wasExecutionPolicySet = true;
            }
        }

        private Microsoft.PowerShell.ExecutionPolicy _executionPolicy = Microsoft.PowerShell.ExecutionPolicy.Default;
        private bool _wasExecutionPolicySet = false;

        /// <summary>
        /// If true the PowerShell debugger will use FullLanguage mode, otherwise it will use the current language mode.
        /// </summary>
        public bool UseFullLanguageModeInDebugger { get; set; } = false;

        /// <summary>
        /// ApartmentState of the thread used to execute commands.
        /// </summary>
        public ApartmentState ApartmentState { get; set; } = Runspace.DefaultApartmentState;

        /// <summary>
        /// This property determines whether a new thread is created for each invocation of a command.
        /// </summary>
        public PSThreadOptions ThreadOptions { get; set; } = PSThreadOptions.Default;

        /// <summary>
        /// If this property is set and there was a runspace creation error, then
        /// throw an exception, otherwise just continue creating the runspace even though it may
        /// be in an inconsistent state.
        /// </summary>
        public bool ThrowOnRunspaceOpenError { get; set; } = false;

        /// <summary>
        /// This property will be set only if we are refreshing the Type/Format settings by calling UpdateTypes/UpdateFormats directly.
        /// In this case, we should wait until all type/format entries get processed. After that, if there were errors
        /// generated, we throw them as an exception.
        /// </summary>
        internal bool RefreshTypeAndFormatSetting = false;

        /// <summary>
        /// Specifies the authorization manager to be used for this session state instance.
        /// If no authorization manager is specified, then the default authorization manager
        /// for PowerShell will be used which checks the ExecutionPolicy before running a command.
        /// </summary>
        public virtual AuthorizationManager AuthorizationManager { get; set; } = new Microsoft.PowerShell.PSAuthorizationManager(Utils.DefaultPowerShellShellID);

        internal PSHost Host = null;

        /// <summary>
        /// Add a list of modules to import when the runspace is created.
        /// </summary>
        /// <param name="name">The modules to add.</param>
        /// <returns></returns>
        public void ImportPSModule(params string[] name)
        {
            ArgumentNullException.ThrowIfNull(name);

            foreach (string n in name)
            {
                ModuleSpecificationsToImport.Add(new ModuleSpecification(n));
            }
        }

        /// <summary>
        /// Clears ImportPSModule list.
        /// </summary>
        internal void ClearPSModules()
        {
            ModuleSpecificationsToImport.Clear();
        }

        /// <summary>
        /// Add a list of modules to import when the runspace is created.
        /// </summary>
        /// <param name="modules">
        /// The modules, whose specifications are specified by <paramref name="modules"/>,
        /// to add.
        /// </param>
        public void ImportPSModule(IEnumerable<ModuleSpecification> modules)
        {
            ArgumentNullException.ThrowIfNull(modules);

            foreach (var moduleSpecification in modules)
            {
                ModuleSpecificationsToImport.Add(moduleSpecification);
            }
        }

        /// <summary>
        /// Imports all the modules from the specified module path by default.
        /// </summary>
        /// <param name="path">
        /// Path from which all modules need to be imported.
        /// </param>
        public void ImportPSModulesFromPath(string path)
        {
            string expandedpath = Environment.ExpandEnvironmentVariables(path);
            var availableModuleFiles = ModuleUtils.GetDefaultAvailableModuleFiles(expandedpath);
            ImportPSModule(availableModuleFiles.ToArray());
        }

        /// <summary>
        /// Add a list of core modules to import when the runspace is created.
        /// </summary>
        /// <param name="name">The modules to add.</param>
        /// <returns></returns>
        internal void ImportPSCoreModule(string[] name)
        {
            ArgumentNullException.ThrowIfNull(name);

            foreach (string n in name)
            {
                CoreModulesToImport.Add(n);
            }
        }

        /// <summary>
        /// Imported modules.
        /// </summary>
        public ReadOnlyCollection<ModuleSpecification> Modules
        {
            get { return new ReadOnlyCollection<ModuleSpecification>(ModuleSpecificationsToImport); }
        }

        internal Collection<ModuleSpecification> ModuleSpecificationsToImport { get; } = new Collection<ModuleSpecification>();

        internal Dictionary<string, PSSnapInInfo> ImportedSnapins { get; } = new Dictionary<string, PSSnapInInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the dictionary of core modules to import on runspace creation...
        /// </summary>
        internal HashSet<string> CoreModulesToImport { get; } = new HashSet<string>();

        /// <summary>
        /// The list of assemblies to load...
        /// </summary>
        public virtual InitialSessionStateEntryCollection<SessionStateAssemblyEntry> Assemblies
        {
            get
            {
                if (_assemblies == null)
                {
                    Interlocked.CompareExchange(ref _assemblies, new InitialSessionStateEntryCollection<SessionStateAssemblyEntry>(), null);
                }

                return _assemblies;
            }
        }

        private InitialSessionStateEntryCollection<SessionStateAssemblyEntry> _assemblies;

        /// <summary>
        /// List of types to use for this session state instance...
        /// </summary>
        public virtual InitialSessionStateEntryCollection<SessionStateTypeEntry> Types
        {
            get
            {
                if (_types == null)
                {
                    Interlocked.CompareExchange(ref _types, new InitialSessionStateEntryCollection<SessionStateTypeEntry>(), null);
                }

                return _types;
            }
        }

        private InitialSessionStateEntryCollection<SessionStateTypeEntry> _types;

        /// <summary>
        /// </summary>
        public virtual InitialSessionStateEntryCollection<SessionStateFormatEntry> Formats
        {
            get
            {
                if (_formats == null)
                {
                    Interlocked.CompareExchange(ref _formats, new InitialSessionStateEntryCollection<SessionStateFormatEntry>(), null);
                }

                return _formats;
            }
        }

        private InitialSessionStateEntryCollection<SessionStateFormatEntry> _formats;

        /// <summary>
        /// If set to true, disables any updates to format table. This includes disabling
        /// format table updates through Update-FormatData, Import-Module etc.
        /// All the disabling happens silently ie., the user will not get any exception.
        /// By default, this is set to False.
        /// </summary>
        public bool DisableFormatUpdates { get; set; }

        /// <summary>
        /// </summary>
        public virtual InitialSessionStateEntryCollection<SessionStateProviderEntry> Providers
        {
            get
            {
                if (_providers == null)
                {
                    Interlocked.CompareExchange(ref _providers, new InitialSessionStateEntryCollection<SessionStateProviderEntry>(), null);
                }

                return _providers;
            }
        }

        private InitialSessionStateEntryCollection<SessionStateProviderEntry> _providers;

        /// <summary>
        /// List of commands (Alias, Application, Cmdlets, Function, Script) for this entry.
        /// </summary>
        public virtual InitialSessionStateEntryCollection<SessionStateCommandEntry> Commands
        {
            get
            {
                if (_commands == null)
                {
                    Interlocked.CompareExchange(ref _commands, new InitialSessionStateEntryCollection<SessionStateCommandEntry>(), null);
                }

                return _commands;
            }
        }

        private InitialSessionStateEntryCollection<SessionStateCommandEntry> _commands;

        internal SessionStateEntryVisibility DefaultCommandVisibility { get; set; }

        internal HashSet<string> UnresolvedCommandsToExpose
        {
            get
            {
                if (_unresolvedCommandsToExpose == null)
                {
                    Interlocked.CompareExchange(ref _unresolvedCommandsToExpose, new HashSet<string>(StringComparer.OrdinalIgnoreCase), null);
                }

                return _unresolvedCommandsToExpose;
            }
        }

        private HashSet<string> _unresolvedCommandsToExpose;

        internal Dictionary<string, Hashtable> CommandModifications
        {
            get
            {
                if (_commandModifications == null)
                {
                    Interlocked.CompareExchange(ref _commandModifications, new Dictionary<string, Hashtable>(StringComparer.OrdinalIgnoreCase), null);
                }

                return _commandModifications;
            }
        }

        private Dictionary<string, Hashtable> _commandModifications;

        internal List<Hashtable> DynamicVariablesToDefine
        {
            get
            {
                if (_dynamicVariablesToDefine == null)
                {
                    Interlocked.CompareExchange(ref _dynamicVariablesToDefine, new List<Hashtable>(), null);
                }

                return _dynamicVariablesToDefine;
            }
        }

        private List<Hashtable> _dynamicVariablesToDefine;

        /// <summary>
        /// </summary>
        public virtual InitialSessionStateEntryCollection<SessionStateVariableEntry> Variables
        {
            get
            {
                if (_variables == null)
                {
                    Interlocked.CompareExchange(ref _variables, new InitialSessionStateEntryCollection<SessionStateVariableEntry>(), null);
                }

                return _variables;
            }
        }

        private InitialSessionStateEntryCollection<SessionStateVariableEntry> _variables;

        /// <summary>
        /// </summary>
        public virtual InitialSessionStateEntryCollection<SessionStateVariableEntry> EnvironmentVariables
        {
            get
            {
                if (_environmentVariables == null)
                {
                    Interlocked.CompareExchange(ref _environmentVariables, new InitialSessionStateEntryCollection<SessionStateVariableEntry>(), null);
                }

                return _environmentVariables;
            }
        }

        private InitialSessionStateEntryCollection<SessionStateVariableEntry> _environmentVariables;

        /// <summary>
        /// </summary>
        public virtual HashSet<string> StartupScripts
        {
            get
            {
                if (_startupScripts == null)
                {
                    Interlocked.CompareExchange(ref _startupScripts, new HashSet<string>(), null);
                }

                return _startupScripts;
            }
        }

        private HashSet<string> _startupScripts = new HashSet<string>();

        private readonly object _syncObject = new object();

        internal void Bind(ExecutionContext context, bool updateOnly, PSModuleInfo module, bool noClobber, bool local, bool setLocation)
        {
            Host = context.EngineHostInterface;
            lock (_syncObject)
            {
                SessionStateInternal ss = context.EngineSessionState;

                // Clear the application and script collections...
                if (!updateOnly)
                {
                    ss.Applications.Clear();
                    ss.Scripts.Clear();
                }

                // If the initial session state made some commands private by way of
                // VisibleCmdlets / etc., then change the default command visibility for
                // the session state so that newly imported modules aren't exposed accidentally.
                if (DefaultCommandVisibility == SessionStateEntryVisibility.Private)
                {
                    ss.DefaultCommandVisibility = SessionStateEntryVisibility.Private;
                }

                try
                {
                    // Load assemblies before anything else - we may need to resolve types in the loaded
                    // assemblies as part of loading formats or types, and that can't be done in parallel.
                    Bind_LoadAssemblies(context);

                    var actions = new Action[]
                    {
                        () => Bind_UpdateTypes(context, updateOnly),
                        () => Bind_UpdateFormats(context, updateOnly),
                        () => Bind_BindCommands(module, noClobber, local, ss),
                        () => Bind_LoadProviders(ss),
                        () => Bind_SetVariables(ss),
                        Bind_SetEnvironment
                    };

                    if (updateOnly)
                    {
                        // We're typically called to import a module. It seems like this could
                        // still happen in parallel, but calls to WriteError on the wrong thread
                        // get silently swallowed, so instead just run the actions serially on this thread.
                        foreach (var action in actions)
                        {
                            action();
                        }
                    }
                    else
                    {
                        Parallel.Invoke(actions);
                    }
                }
                catch (AggregateException e)
                {
                    e = e.Flatten();
                    foreach (var exception in e.InnerExceptions)
                    {
                        MshLog.LogEngineHealthEvent(
                            context,
                            MshLog.EVENT_ID_CONFIGURATION_FAILURE,
                            exception,
                            Severity.Warning);
                    }

                    if (this.ThrowOnRunspaceOpenError)
                    {
                        // Just throw the first error
                        throw e.InnerExceptions[0];
                    }

                    context.ReportEngineStartupError(e.Message);
                }
                catch (Exception e)
                {
                    MshLog.LogEngineHealthEvent(
                        context,
                        MshLog.EVENT_ID_CONFIGURATION_FAILURE,
                        e,
                        Severity.Warning);

                    if (this.ThrowOnRunspaceOpenError)
                    {
                        throw;
                    }

                    context.ReportEngineStartupError(e.Message);
                }

                // Set the language mode
                if (!updateOnly)
                {
                    ss.LanguageMode = LanguageMode;
                }

                // Set the execution policy
                if (_wasExecutionPolicySet)
                {
                    string shellId = context.ShellID;
                    SecuritySupport.SetExecutionPolicy(Microsoft.PowerShell.ExecutionPolicyScope.Process, ExecutionPolicy, shellId);
                }
            }

            SetSessionStateDrive(context, setLocation: setLocation);
        }

        private void Bind_SetVariables(SessionStateInternal ss)
        {
            bool etwEnabled = RunspaceEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadVariablesStart();
            }

            // Add all of the variables to session state...
            foreach (SessionStateVariableEntry var in Variables)
            {
                ss.AddSessionStateEntry(var);
            }

            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadVariablesStop();
            }
        }

        private void Bind_SetEnvironment()
        {
            bool etwEnabled = RunspaceEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadEnvironmentVariablesStart();
            }

            foreach (SessionStateVariableEntry var in EnvironmentVariables)
            {
                Environment.SetEnvironmentVariable(var.Name, var.Value.ToString());
            }

            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadEnvironmentVariablesStop();
            }
        }

        private void Bind_UpdateTypes(ExecutionContext context, bool updateOnly)
        {
            bool etwEnabled = RunspaceEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                RunspaceEventSource.Log.UpdateTypeTableStart();
            }

            this.UpdateTypes(context, updateOnly);
            if (etwEnabled)
            {
                RunspaceEventSource.Log.UpdateTypeTableStop();
            }
        }

        private void Bind_UpdateFormats(ExecutionContext context, bool updateOnly)
        {
            bool etwEnabled = RunspaceEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                RunspaceEventSource.Log.UpdateFormatTableStart();
            }

            this.UpdateFormats(context, updateOnly);

            if (etwEnabled)
            {
                RunspaceEventSource.Log.UpdateFormatTableStop();
            }
        }

        private void Bind_LoadProviders(SessionStateInternal ss)
        {
            bool etwEnabled = RunspaceEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadProvidersStart();
            }

            // Add all of the providers to session state...
            foreach (SessionStateProviderEntry provider in Providers)
            {
                if (etwEnabled)
                {
                    RunspaceEventSource.Log.LoadProviderStart(provider.Name);
                }

                ss.AddSessionStateEntry(provider);
                if (etwEnabled)
                {
                    RunspaceEventSource.Log.LoadProviderStop(provider.Name);
                }
            }

            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadProvidersStop();
            }
        }

        private void Bind_BindCommands(PSModuleInfo module, bool noClobber, bool local, SessionStateInternal ss)
        {
            bool etwEnabled = RunspaceEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadCommandsStart();
            }

            foreach (SessionStateCommandEntry cmd in Commands)
            {
                if (etwEnabled)
                {
                    RunspaceEventSource.Log.LoadCommandStart(cmd.Name);
                }

                SessionStateCmdletEntry ssce = cmd as SessionStateCmdletEntry;
                if (ssce != null)
                {
                    if (noClobber && ModuleCmdletBase.CommandFound(ssce.Name, ss))
                    {
                        ssce._isImported = false;
                        continue;
                    }

                    ss.AddSessionStateEntry(ssce, local);

                    cmd.SetModule(module);
                    continue;
                }

                cmd.SetModule(module);

                SessionStateFunctionEntry ssfe = cmd as SessionStateFunctionEntry;
                if (ssfe != null)
                {
                    ss.AddSessionStateEntry(ssfe);
                    continue;
                }

                SessionStateAliasEntry ssae = cmd as SessionStateAliasEntry;
                if (ssae != null)
                {
                    ss.AddSessionStateEntry(ssae, StringLiterals.Local);
                    continue;
                }

                SessionStateApplicationEntry ssappe = cmd as SessionStateApplicationEntry;
                if (ssappe != null)
                {
                    if (ssappe.Visibility == SessionStateEntryVisibility.Public)
                    {
                        ss.AddSessionStateEntry(ssappe);
                    }

                    continue;
                }

                SessionStateScriptEntry ssse = cmd as SessionStateScriptEntry;
                if (ssse != null)
                {
                    if (ssse.Visibility == SessionStateEntryVisibility.Public)
                    {
                        ss.AddSessionStateEntry(ssse);
                    }

                    continue;
                }

                if (etwEnabled)
                {
                    RunspaceEventSource.Log.LoadCommandStop(cmd.Name);
                }
            }

            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadCommandsStop();
            }
        }

        private void Bind_LoadAssemblies(ExecutionContext context)
        {
            bool etwEnabled = RunspaceEventSource.Log.IsEnabled();
            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadAssembliesStart();
            }

            // Load the assemblies and initialize the assembly cache...
            foreach (SessionStateAssemblyEntry ssae in Assemblies)
            {
                if (etwEnabled)
                {
                    RunspaceEventSource.Log.LoadAssemblyStart(ssae.Name, ssae.FileName);
                }

                // Specify the source only if this is for module loading.
                // The source is used for porper cleaning of the assembly cache when a module is unloaded.
                Assembly asm = context.AddAssembly(ssae.Module?.Name, ssae.Name, ssae.FileName, out Exception error);

                if (asm == null || error != null)
                {
                    // If no module was found but there was no specific error, then
                    // create a not found error.
                    if (error == null)
                    {
                        string msg = StringUtil.Format(global::Modules.ModuleAssemblyFound, ssae.Name);
                        error = new DllNotFoundException(msg);
                    }

                    // If this occurs while loading a module manifest, just
                    // throw the exception instead of writing it out...
                    if ((!string.IsNullOrEmpty(context.ModuleBeingProcessed) &&
                         Path.GetExtension(context.ModuleBeingProcessed)
                             .Equals(
                                 StringLiterals.PowerShellDataFileExtension,
                                 StringComparison.OrdinalIgnoreCase)) ||
                        ThrowOnRunspaceOpenError)
                    {
                        throw error;
                    }
                    else
                    {
                        context.ReportEngineStartupError(error.Message);
                    }
                }

                if (etwEnabled)
                {
                    RunspaceEventSource.Log.LoadAssemblyStop(ssae.Name, ssae.FileName);
                }
            }

            if (etwEnabled)
            {
                RunspaceEventSource.Log.LoadAssembliesStop();
            }
        }

        internal Exception BindRunspace(Runspace initializedRunspace, PSTraceSource runspaceInitTracer)
        {
            // Get the initial list of public commands from session in a lazy way, so that we can defer
            // the work until it's actually needed.
            //
            // We could use Lazy<> with an initializer for the same purpose, but we can save allocations
            // by using the local function. It avoids allocating the delegate, and it's more efficient on
            // capturing variables from the enclosing scope by using a struct.
            HashSet<CommandInfo> publicCommands = null;
            HashSet<CommandInfo> GetPublicCommands()
            {
                if (publicCommands != null)
                {
                    return publicCommands;
                }

                publicCommands = new HashSet<CommandInfo>();
                foreach (CommandInfo sessionCommand in initializedRunspace.ExecutionContext.SessionState.InvokeCommand.GetCommands(
                            name: "*",
                            CommandTypes.Alias | CommandTypes.Function | CommandTypes.Filter | CommandTypes.Cmdlet,
                            nameIsPattern: true))
                {
                    if (sessionCommand.Visibility == SessionStateEntryVisibility.Public)
                    {
                        publicCommands.Add(sessionCommand);
                    }
                }

                return publicCommands;
            }

            var unresolvedCmdsToExpose = new HashSet<string>(this.UnresolvedCommandsToExpose, StringComparer.OrdinalIgnoreCase);
            if (CoreModulesToImport.Count > 0 || unresolvedCmdsToExpose.Count > 0)
            {
                // If a user has any module with the same name as that of the core module( or nested module inside the core module)
                // in his module path, then that will get loaded instead of the actual nested module (from the GAC - in our case)
                // Hence, searching only from the system module path while loading the core modules
                ProcessModulesToImport(initializedRunspace, CoreModulesToImport, ModuleIntrinsics.GetPSHomeModulePath(), GetPublicCommands(), unresolvedCmdsToExpose);
            }

            // Win8:328748 - functions defined in global scope end up in a module
            // Since we import the core modules, EngineSessionState's module is set to the last imported module. So, if a function is defined in global scope, it ends up in that module.
            // Setting the module to null fixes that.
            initializedRunspace.ExecutionContext.EngineSessionState.Module = null;

            if (ModuleSpecificationsToImport.Count > 0 || unresolvedCmdsToExpose.Count > 0)
            {
                Exception moduleImportException = ProcessModulesToImport(initializedRunspace, ModuleSpecificationsToImport, string.Empty, GetPublicCommands(), unresolvedCmdsToExpose);
                if (moduleImportException != null)
                {
                    runspaceInitTracer.WriteLine(
                        "Runspace open failed while loading module: First error {1}",
                        moduleImportException);
                    return moduleImportException;
                }
            }

            // If we still have unresolved commands after importing specified modules, then try finding associated module for
            // each unresolved command and import that module.
            if (unresolvedCmdsToExpose.Count > 0)
            {
                string[] foundModuleList = GetModulesForUnResolvedCommands(unresolvedCmdsToExpose, initializedRunspace.ExecutionContext);
                if (foundModuleList.Length > 0)
                {
                    ProcessModulesToImport(initializedRunspace, foundModuleList, string.Empty, GetPublicCommands(), unresolvedCmdsToExpose);
                }
            }

            // Process dynamic variables if any are defined.
            if (DynamicVariablesToDefine.Count > 0)
            {
                ProcessDynamicVariables(initializedRunspace);
            }

            // Process command modifications if any are defined.
            if (CommandModifications.Count > 0)
            {
                ProcessCommandModifications(initializedRunspace);
            }

            // Process the 'User:' drive if 'UserDriveEnabled' is set.
            if (UserDriveEnabled)
            {
                Exception userDriveException = ProcessUserDrive(initializedRunspace);
                if (userDriveException != null)
                {
                    runspaceInitTracer.WriteLine(
                        "Runspace open failed while processing user drive with error {1}",
                        userDriveException);

                    Exception result = PSTraceSource.NewInvalidOperationException(userDriveException, RemotingErrorIdStrings.UserDriveProcessingThrewTerminatingError, userDriveException.Message);
                    return result;
                }
            }

            // Process startup scripts
            if (StartupScripts.Count > 0)
            {
                Exception startupScriptException = ProcessStartupScripts(initializedRunspace);
                if (startupScriptException != null)
                {
                    runspaceInitTracer.WriteLine(
                        "Runspace open failed while running startup script: First error {1}",
                        startupScriptException);

                    Exception result = PSTraceSource.NewInvalidOperationException(startupScriptException, RemotingErrorIdStrings.StartupScriptThrewTerminatingError, startupScriptException.Message);
                    return result;
                }
            }

            // Start transcribing
            if (!string.IsNullOrEmpty(TranscriptDirectory))
            {
                using (PowerShell psToInvoke = PowerShell.Create())
                {
                    psToInvoke.AddCommand(new Command("Start-Transcript")).AddParameter("OutputDirectory", TranscriptDirectory);
                    Exception exceptionToReturn = ProcessPowerShellCommand(psToInvoke, initializedRunspace);
                    if (exceptionToReturn != null)
                    {
                        // ThrowOnRunspaceOpenError handling is done by ProcessPowerShellCommand
                        return exceptionToReturn;
                    }
                }
            }

            return null;
        }

        private static string[] GetModulesForUnResolvedCommands(IEnumerable<string> unresolvedCommands, ExecutionContext context)
        {
            Collection<string> modulesToImport = new Collection<string>();
            HashSet<string> commandsToResolve = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var unresolvedCommand in unresolvedCommands)
            {
                string moduleName;
                string command = Utils.ParseCommandName(unresolvedCommand, out moduleName);
                if (!string.IsNullOrEmpty(moduleName))
                {
                    // Skip fully qualified module names since they are already processed.
                    continue;
                }

                if (WildcardPattern.ContainsWildcardCharacters(command))
                {
                    // Skip names with wild cards.
                    continue;
                }

                commandsToResolve.Add(command);
            }

            if (commandsToResolve.Count > 0)
            {
                Runspace restoreRunspace = Runspace.DefaultRunspace;
                try
                {
                    // Create a temporary default runspace for the analysis cache to use.
                    using (Runspace tempRunspace = RunspaceFactory.CreateRunspace())
                    {
                        tempRunspace.Open();
                        Runspace.DefaultRunspace = tempRunspace;

                        foreach (var unresolvedCommand in commandsToResolve)
                        {
                            // Use the analysis cache to find the first module containing the unresolved command.
                            foreach (string modulePath in ModuleUtils.GetDefaultAvailableModuleFiles(isForAutoDiscovery: true, context))
                            {
                                string expandedModulePath = IO.Path.GetFullPath(modulePath);
                                var exportedCommands = AnalysisCache.GetExportedCommands(expandedModulePath, false, context);

                                if (exportedCommands != null && exportedCommands.ContainsKey(unresolvedCommand))
                                {
                                    modulesToImport.Add(System.IO.Path.GetFileNameWithoutExtension(expandedModulePath));
                                    break;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Runspace.DefaultRunspace = restoreRunspace;
                }
            }

            return modulesToImport.ToArray<string>();
        }

        private void ProcessCommandModifications(Runspace initializedRunspace)
        {
            foreach (var pair in CommandModifications)
            {
                string commandName = pair.Key;
                Hashtable commandModification = pair.Value;

                CommandInfo existingCommand = initializedRunspace.SessionStateProxy.InvokeCommand.GetCommand(commandName, CommandTypes.Cmdlet | CommandTypes.Function);
                if (existingCommand == null)
                {
                    // Could not find the command - just continue, rather than generating an error. This could just be a missing module
                    // or something similar.
                    continue;
                }

                // If we are wrapping a function, rename it.
                FunctionInfo commandAsFunction = existingCommand as FunctionInfo;
                if (commandAsFunction != null)
                {
                    string newCommandName = commandAsFunction.Name + "_" + Guid.NewGuid().ToString("N");
                    commandAsFunction.Rename(newCommandName);
                    initializedRunspace.ExecutionContext.EngineSessionState.GlobalScope.FunctionTable.Add(newCommandName, commandAsFunction);
                    initializedRunspace.ExecutionContext.EngineSessionState.GlobalScope.FunctionTable.Remove(commandName);
                    existingCommand = initializedRunspace.SessionStateProxy.InvokeCommand.GetCommand(newCommandName, CommandTypes.Function);
                }

                CommandMetadata metadata = new CommandMetadata(existingCommand);
                List<string> unprocessedCommandModifications = new List<string>();
                foreach (string commandModificationParameter in commandModification.Keys)
                {
                    unprocessedCommandModifications.Add(commandModificationParameter);
                }

                // Visit all parameters of the command we're wrapping
                foreach (string existingParameter in metadata.Parameters.Keys.ToArray<string>())
                {
                    // If it's not allowed, remove it
                    if (!commandModification.ContainsKey(existingParameter))
                    {
                        metadata.Parameters.Remove(existingParameter);
                    }
                    else
                    {
                        // Remember that we've processed this parameter, so that we can add the remainder
                        // as virtual command modifications over (what we must assume to be) dynamic parameters.
                        unprocessedCommandModifications.Remove(existingParameter);

                        ProcessCommandModification(commandModification, metadata, existingParameter);
                    }
                }

                // Now, process the command modifications that the user requested (but there was no parameter
                // in the cmdlet that matched the requested parameter)
                foreach (string unprocessedCommandModification in unprocessedCommandModifications)
                {
                    ProcessCommandModification(commandModification, metadata, unprocessedCommandModification);
                }

                string proxyBody = ProxyCommand.Create(metadata, string.Empty, false);
                ScriptBlock proxyScriptBlock = ScriptBlock.Create(proxyBody);
                proxyScriptBlock.LanguageMode = PSLanguageMode.FullLanguage;

                initializedRunspace.ExecutionContext.EngineSessionState.GlobalScope.FunctionTable.Add(
                    commandName, new FunctionInfo(commandName, proxyScriptBlock, initializedRunspace.ExecutionContext));
            }
        }

        /// <summary>
        /// Process a command modification for a specific parameter.
        /// </summary>
        /// <param name="commandModification">The hashtable of command modifications for this command.</param>
        /// <param name="metadata">The metadata for the command being processed.</param>
        /// <param name="parameterName">The parameter being modified.</param>
        private static void ProcessCommandModification(Hashtable commandModification, CommandMetadata metadata, string parameterName)
        {
            // If the metadata doesn't actually contain the parameter, then we need to create one.
            if (!metadata.Parameters.ContainsKey(parameterName))
            {
                metadata.Parameters[parameterName] = new ParameterMetadata(parameterName);
            }

            // Add validation attributes
            Hashtable parameterValidations = (Hashtable)commandModification[parameterName];
            foreach (object parameterValidation in parameterValidations.Keys)
            {
                string[] parameterValidationValues =
                    ((HashSet<string>)parameterValidations[parameterValidation]).ToList<string>().ToArray();

                switch (parameterValidation.ToString())
                {
                    case "ValidateSet":
                        ValidateSetAttribute validateSet = new ValidateSetAttribute(parameterValidationValues);
                        metadata.Parameters[parameterName].Attributes.Add(validateSet);
                        break;

                    case "ValidatePattern":
                        string pattern = "^(" + string.Join('|', parameterValidationValues) + ")$";
                        ValidatePatternAttribute validatePattern = new ValidatePatternAttribute(pattern);
                        metadata.Parameters[parameterName].Attributes.Add(validatePattern);
                        break;
                }
            }
        }

        private Exception ProcessDynamicVariables(Runspace initializedRunspace)
        {
            foreach (Hashtable variable in DynamicVariablesToDefine)
            {
                if (variable.ContainsKey("Name"))
                {
                    string name = variable["Name"].ToString();
                    ScriptBlock sb = variable["Value"] as ScriptBlock;

                    if (!string.IsNullOrEmpty(name) && (sb != null))
                    {
                        sb.SessionStateInternal = initializedRunspace.ExecutionContext.EngineSessionState;

                        using (PowerShell psToInvoke = PowerShell.Create())
                        {
                            psToInvoke.AddCommand(new Command("Invoke-Command")).AddParameter("ScriptBlock", sb).AddParameter("NoNewScope");
                            psToInvoke.AddCommand(new Command("Set-Variable")).AddParameter("Name", name);

                            Exception exceptionToReturn = ProcessPowerShellCommand(psToInvoke, initializedRunspace);
                            if (exceptionToReturn != null)
                            {
                                // ThrowOnRunspaceOpenError handling is done by ProcessPowerShellCommand
                                return exceptionToReturn;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private Exception ProcessUserDrive(Runspace initializedRunspace)
        {
            Exception ex = null;
            try
            {
                List<ProviderInfo> fileSystemProviders = initializedRunspace.ExecutionContext.EngineSessionState.Providers["FileSystem"];
                if (fileSystemProviders.Count == 0)
                {
                    throw new PSInvalidOperationException(RemotingErrorIdStrings.UserDriveCannotGetFileSystemProvider);
                }

                // Create the User drive path directory in current user local appdata location:
                // SystemDrive\Users\[user]\AppData\Local\Microsoft\PowerShell\DriveRoots\[UserName]
                // Or for virtual accounts
                // WinDir\System32\Microsoft\PowerShell\DriveRoots\[UserName]
                string directoryName = MakeUserNamePath();
                string userDrivePath = Path.Combine(Platform.CacheDirectory, "DriveRoots", directoryName);

                // Create directory if it doesn't exist.
                if (!System.IO.Directory.Exists(userDrivePath))
                {
                    System.IO.Directory.CreateDirectory(userDrivePath);
                }

                // Create the PSDrive.
                var newDriveInfo = new PSDriveInfo(
                    "User",
                    fileSystemProviders[0],
                    userDrivePath,
                    null,
                    null);
                var userDriveInfo = initializedRunspace.ExecutionContext.SessionState.Drive.New(newDriveInfo, null);

                // Set User drive maximum size.  Default maximum size is 50MB
                userDriveInfo.MaximumSize = (UserDriveMaximumSize > 0) ? UserDriveMaximumSize : 50000000;
            }
            catch (ArgumentNullException e) { ex = e; }
            catch (ArgumentException e) { ex = e; }
            catch (NotSupportedException e) { ex = e; }
            catch (ProviderNotFoundException e) { ex = e; }
            catch (ProviderInvocationException e) { ex = e; }
            catch (KeyNotFoundException e) { ex = e; }
            catch (IOException e) { ex = e; }
            catch (UnauthorizedAccessException e) { ex = e; }

            return ex;
        }

        private string MakeUserNamePath()
        {
            // Use the user name passed to initial session state if available, or
            // otherwise use the current user name.
            var userName = !string.IsNullOrEmpty(this.UserDriveUserName)
                ? this.UserDriveUserName
                // domain\user on Windows, just user on Unix
#if UNIX
                : Environment.UserName;
#else
                : Environment.UserDomainName + "_" + Environment.UserName;
#endif

            // Ensure that user name contains no invalid path characters.
            // MSDN indicates that logon names cannot contain any of these invalid characters,
            // but this check will ensure safety.
            if (userName.IndexOfAny(System.IO.Path.GetInvalidPathChars()) > -1)
            {
                throw new PSInvalidOperationException(RemotingErrorIdStrings.InvalidUserDriveName);
            }

            return userName;
        }

        private Exception ProcessStartupScripts(Runspace initializedRunspace)
        {
            foreach (string startupScript in StartupScripts)
            {
                using (PowerShell psToInvoke = PowerShell.Create())
                {
                    psToInvoke.AddCommand(new Command(startupScript, false, false));

                    Exception exceptionToReturn = ProcessPowerShellCommand(psToInvoke, initializedRunspace);
                    if (exceptionToReturn != null)
                    {
                        // ThrowOnRunspaceOpenError handling is done by ProcessPowerShellCommand
                        return exceptionToReturn;
                    }
                }
            }

            return null;
        }

        private Exception ProcessPowerShellCommand(PowerShell psToInvoke, Runspace initializedRunspace)
        {
            PSLanguageMode originalLanguageMode = initializedRunspace.SessionStateProxy.LanguageMode;

            try
            {
                initializedRunspace.SessionStateProxy.LanguageMode = PSLanguageMode.FullLanguage;

                psToInvoke.Runspace = initializedRunspace;

                foreach (Command command in psToInvoke.Commands.Commands)
                {
                    command.CommandOrigin = CommandOrigin.Internal;
                }

                try
                {
                    psToInvoke.Invoke();
                }
                catch (Exception e)
                {
                    if (ThrowOnRunspaceOpenError)
                    {
                        return e;
                    }
                }
            }
            finally
            {
                // Restore the langauge mode, but not if it was altered by the startup script itself.
                if (initializedRunspace.SessionStateProxy.LanguageMode == PSLanguageMode.FullLanguage)
                {
                    initializedRunspace.SessionStateProxy.LanguageMode = originalLanguageMode;
                }
            }

            if (ThrowOnRunspaceOpenError)
            {
                // find out if there are any error records reported. If there is one, report the error..
                // this will result in the runspace getting closed/broken.
                ArrayList errorList = (ArrayList)initializedRunspace.GetExecutionContext.DollarErrorVariable;
                if (errorList.Count > 0)
                {
                    ErrorRecord lastErrorRecord = errorList[0] as ErrorRecord;
                    if (lastErrorRecord != null)
                    {
                        return new Exception(lastErrorRecord.ToString());
                    }
                    else
                    {
                        Exception lastException = errorList[0] as Exception;
                        if (lastException != null)
                        {
                            return lastException;
                        }
                        else
                        {
                            return new Exception(errorList[0].ToString());
                        }
                    }
                }
            }

            return null;
        }

        private RunspaceOpenModuleLoadException ProcessModulesToImport(
            Runspace initializedRunspace,
            IEnumerable moduleList,
            string path,
            HashSet<CommandInfo> publicCommands,
            HashSet<string> unresolvedCmdsToExpose)
        {
            RunspaceOpenModuleLoadException exceptionToReturn = null;
            List<PSModuleInfo> processedModules = new List<PSModuleInfo>();

            foreach (object module in moduleList)
            {
                string moduleName = module as string;
                if (moduleName != null)
                {
                    exceptionToReturn = ProcessOneModule(
                        initializedRunspace: initializedRunspace,
                        name: moduleName,
                        moduleInfoToLoad: null,
                        path: path,
                        publicCommands: publicCommands,
                        processedModules: processedModules);
                }
                else
                {
                    ModuleSpecification moduleSpecification = module as ModuleSpecification;
                    if (moduleSpecification != null)
                    {
                        if ((moduleSpecification.RequiredVersion == null) && (moduleSpecification.Version == null) && (moduleSpecification.MaximumVersion == null) && (moduleSpecification.Guid == null))
                        {
                            // if only name is specified in the module spec, just try import the module
                            // ie., don't take the performance overhead of calling GetModule.
                            exceptionToReturn = ProcessOneModule(
                                initializedRunspace: initializedRunspace,
                                name: moduleSpecification.Name,
                                moduleInfoToLoad: null,
                                path: path,
                                publicCommands: publicCommands,
                                processedModules: processedModules);
                        }
                        else
                        {
                            Collection<PSModuleInfo> moduleInfos = ModuleCmdletBase.GetModuleIfAvailable(moduleSpecification, initializedRunspace);

                            if (moduleInfos != null && moduleInfos.Count > 0)
                            {
                                exceptionToReturn = ProcessOneModule(
                                    initializedRunspace: initializedRunspace,
                                    name: moduleSpecification.Name,
                                    moduleInfoToLoad: moduleInfos[0],
                                    path: path,
                                    publicCommands: publicCommands,
                                    processedModules: processedModules);
                            }
                            else
                            {
                                var version = "0.0.0.0";

                                if (moduleSpecification.RequiredVersion != null)
                                {
                                    version = moduleSpecification.RequiredVersion.ToString();
                                }
                                else if (moduleSpecification.Version != null)
                                {
                                    version = moduleSpecification.Version.ToString();
                                    if (moduleSpecification.MaximumVersion != null)
                                    {
                                        version = version + " - " + moduleSpecification.MaximumVersion;
                                    }
                                }
                                else if (moduleSpecification.MaximumVersion != null)
                                {
                                    version = moduleSpecification.MaximumVersion;
                                }

                                string message = StringUtil.Format(
                                    global::Modules.RequiredModuleNotFoundWrongGuidVersion,
                                    moduleSpecification.Name,
                                    moduleSpecification.Guid,
                                    version);
                                RunspaceOpenModuleLoadException rome = new RunspaceOpenModuleLoadException(message);
                                exceptionToReturn = ValidateAndReturnRunspaceOpenModuleLoadException(null, moduleSpecification.Name, rome);
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(false, "ProcessImportModule can import a module using name or module specification.");
                    }
                }
            }

            if (exceptionToReturn == null)
            {
                // Now go through the list of commands not yet resolved to ensure they are public if requested
                foreach (string unresolvedCommand in unresolvedCmdsToExpose.ToArray<string>())
                {
                    string moduleName;
                    string commandToMakeVisible = Utils.ParseCommandName(unresolvedCommand, out moduleName);
                    bool found = false;

                    foreach (CommandInfo cmd in LookupCommands(
                        commandPattern: commandToMakeVisible,
                        moduleName: moduleName,
                        context: initializedRunspace.ExecutionContext,
                        processedModules: processedModules))
                    {
                        if (!found)
                        {
                            found = true;
                        }

                        try
                        {
                            // Special case for wild card lookups.
                            // "Import-Module" or "ipmo" cannot be visible when exposing commands via VisibleCmdlets, etc.
                            if ((cmd.Name.Equals("Import-Module", StringComparison.OrdinalIgnoreCase) &&
                                 (!string.IsNullOrEmpty(cmd.ModuleName) && cmd.ModuleName.Equals("Microsoft.PowerShell.Core", StringComparison.OrdinalIgnoreCase))) ||
                                 cmd.Name.Equals("ipmo", StringComparison.OrdinalIgnoreCase)
                                )
                            {
                                cmd.Visibility = SessionStateEntryVisibility.Private;
                            }
                            else
                            {
                                cmd.Visibility = SessionStateEntryVisibility.Public;
                                publicCommands.Add(cmd);
                            }
                        }
                        catch (PSNotImplementedException)
                        {
                            // Some CommandInfo derivations throw on the Visibility setter.
                        }
                    }

                    if (found && !WildcardPattern.ContainsWildcardCharacters(commandToMakeVisible))
                    {
                        unresolvedCmdsToExpose.Remove(unresolvedCommand);
                    }
                }
            }

            return exceptionToReturn;
        }

        /// <summary>
        /// Helper method to search for commands matching the provided commandPattern.
        /// Supports wild cards and if the commandPattern contains wildcard characters then multiple
        /// results can be returned.  Otherwise a single (and first) match will be returned.
        /// If a moduleName is provided then only commands associated with that module will be returned.
        /// Only public commands are searched to start with.  If no results are found then a search on
        /// internal commands is performed.
        /// </summary>
        /// <param name="commandPattern"></param>
        /// <param name="moduleName"></param>
        /// <param name="context"></param>
        /// <param name="processedModules"></param>
        /// <returns></returns>
        private static IEnumerable<CommandInfo> LookupCommands(
            string commandPattern,
            string moduleName,
            ExecutionContext context,
            List<PSModuleInfo> processedModules)
        {
            bool isWildCardPattern = WildcardPattern.ContainsWildcardCharacters(commandPattern);
            var searchOptions = isWildCardPattern ?
                SearchResolutionOptions.CommandNameIsPattern | SearchResolutionOptions.ResolveFunctionPatterns | SearchResolutionOptions.SearchAllScopes :
                SearchResolutionOptions.ResolveFunctionPatterns | SearchResolutionOptions.SearchAllScopes;

            bool found = false;
            bool haveModuleName = !string.IsNullOrEmpty(moduleName);

            // Start with public search
            CommandOrigin cmdOrigin = CommandOrigin.Runspace;
            while (true)
            {
                foreach (CommandInfo commandInfo in context.SessionState.InvokeCommand.GetCommands(
                    name: commandPattern,
                    commandTypes: CommandTypes.All,
                    options: searchOptions,
                    commandOrigin: cmdOrigin))
                {
                    // If module name is provided then use it to restrict returned results.
                    if (haveModuleName && !moduleName.Equals(commandInfo.ModuleName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!found)
                    {
                        found = true;
                    }

                    yield return commandInfo;

                    // Return first match unless a wild card pattern is submitted.
                    if (!isWildCardPattern)
                    {
                        break;
                    }
                }

                if (found || (cmdOrigin == CommandOrigin.Internal))
                {
                    break;
                }

                // Next try internal search.
                cmdOrigin = CommandOrigin.Internal;
            }

            // If the command is associated with a module, try finding the command in the imported module list.
            // The SessionState function table holds only one command name, and if two or more modules contain
            // a command with the same name, only one of them will appear in the function table search above.
            if (!found && haveModuleName)
            {
                var pattern = new WildcardPattern(commandPattern);

                foreach (PSModuleInfo moduleInfo in processedModules)
                {
                    if (moduleInfo.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var cmd in moduleInfo.ExportedCommands.Values)
                        {
                            if (pattern.IsMatch(cmd.Name))
                            {
                                yield return cmd;
                            }
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// If <paramref name="moduleInfoToLoad"/> is null, import module using <paramref name="name"/>. Otherwise,
        /// import module using <paramref name="moduleInfoToLoad"/>
        /// </summary>
        private RunspaceOpenModuleLoadException ProcessOneModule(
            Runspace initializedRunspace,
            string name,
            PSModuleInfo moduleInfoToLoad,
            string path,
            HashSet<CommandInfo> publicCommands,
            List<PSModuleInfo> processedModules)
        {
            using (PowerShell pse = PowerShell.Create())
            {
                CommandInfo c = new CmdletInfo("Import-Module", typeof(ImportModuleCommand), null, null, initializedRunspace.ExecutionContext);
                Command cmd = new Command(c);
                if (moduleInfoToLoad != null)
                {
                    cmd.Parameters.Add("ModuleInfo", moduleInfoToLoad);
                    name = moduleInfoToLoad.Name;
                }
                else
                {
                    // If FullyQualifiedPath is supplied then use it.
                    // In this scenario, the FullyQualifiedPath would
                    // refer to $pshome\Modules location where core
                    // modules are deployed.
                    if (!string.IsNullOrEmpty(path))
                    {
                        name = Path.Combine(path, name);
                    }

                    cmd.Parameters.Add("Name", name);
                }

                if (!ThrowOnRunspaceOpenError)
                {
                    cmd.MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
                }

                pse.AddCommand(cmd);

                if (!ThrowOnRunspaceOpenError)
                {
                    c = new CmdletInfo("Out-Default", typeof(OutDefaultCommand), null, null, initializedRunspace.ExecutionContext);
                    pse.AddCommand(new Command(c));
                }
                else
                {
                    // For runspace init module processing, pass back the PSModuleInfo to the output pipeline.
                    cmd.Parameters.Add("PassThru");
                }

                pse.Runspace = initializedRunspace;
                // Module import should be run in FullLanguage mode since it is running in
                // a trusted context.
                var savedLanguageMode = pse.Runspace.ExecutionContext.LanguageMode;
                pse.Runspace.ExecutionContext.LanguageMode = PSLanguageMode.FullLanguage;
                try
                {
                    // For runspace init module processing, collect the imported PSModuleInfo returned in the output pipeline.
                    // In other cases, this collection will be empty.
                    Collection<PSModuleInfo> moduleInfos = pse.Invoke<PSModuleInfo>();
                    processedModules.AddRange(moduleInfos);
                }
                finally
                {
                    pse.Runspace.ExecutionContext.LanguageMode = savedLanguageMode;
                }

                // Lock down the command visibility to respect default command visibility
                if (this.DefaultCommandVisibility != SessionStateEntryVisibility.Public)
                {
                    foreach (CommandInfo importedCommand in initializedRunspace.ExecutionContext.SessionState.InvokeCommand.GetCommands(
                        name: "*",
                        CommandTypes.Alias | CommandTypes.Function | CommandTypes.Filter | CommandTypes.Cmdlet,
                        true))
                    {
                        try
                        {
                            // All commands except for the initial session public commands should be made private.
                            if ((importedCommand.Visibility != this.DefaultCommandVisibility) &&
                                !publicCommands.Contains(importedCommand))
                            {
                                importedCommand.Visibility = this.DefaultCommandVisibility;
                            }
                        }
                        catch (PSNotImplementedException)
                        {
                            // Some CommandInfo derivations throw on the Visibility setter.
                        }
                    }
                }

                // Now see if there were any errors. Because the only way we have to
                // return an error at this point is a single exception, we'll take the first
                // error and throw it...
                return ValidateAndReturnRunspaceOpenModuleLoadException(pse, name, null);
            }
        }

        private RunspaceOpenModuleLoadException ValidateAndReturnRunspaceOpenModuleLoadException(PowerShell pse, string moduleName, RunspaceOpenModuleLoadException exception)
        {
            // Only throw the exception if ThrowOnRunspaceOpenError is set.
            if (ThrowOnRunspaceOpenError)
            {
                RunspaceOpenModuleLoadException rome = null;
                if (exception != null)
                {
                    rome = exception;
                }
                else if (pse.Streams.Error.Count > 0)
                {
                    ErrorRecord er;
                    Exception firstError;
                    // Merge errors from pse.Streams and errors
                    PSDataCollection<ErrorRecord> mergedErrors = new PSDataCollection<ErrorRecord>();
                    er = pse.Streams.Error[0];
                    firstError = er.Exception;
                    foreach (var e in pse.Streams.Error)
                    {
                        mergedErrors.Add(e);
                    }

                    rome = new RunspaceOpenModuleLoadException(moduleName, mergedErrors);
                }

                if (rome != null)
                {
                    return rome;
                }
            }

            return null;
        }

        /// <summary>
        /// Reinitializes elements of the associated runspace to their initial values.
        /// This allows for runspace reuse with minimal chance for contamination.
        /// </summary>
        /// <param name="context"></param>
        internal void ResetRunspaceState(ExecutionContext context)
        {
            lock (_syncObject)
            {
                SessionStateInternal ss = context.EngineSessionState;

                // Reset the global variable table
                ss.InitializeSessionStateInternalSpecialVariables(true);

                // Add the built-in variables
                foreach (SessionStateVariableEntry e in InitialSessionState.BuiltInVariables)
                {
                    PSVariable v = new PSVariable(
                        e.Name,
                        e.Value,
                        e.Options, e.Attributes,
                        e.Description)
                    { Visibility = e.Visibility };
                    ss.GlobalScope.SetVariable(e.Name, v, false, true, ss, fastPath: true);
                }

                ss.InitializeFixedVariables();

                // Then re-initialize it with variables to session state...
                foreach (SessionStateVariableEntry e in Variables)
                {
                    PSVariable v = new PSVariable(
                        e.Name,
                        e.Value,
                        e.Options,
                        e.Attributes,
                        e.Description)
                    { Visibility = e.Visibility };
                    ss.GlobalScope.SetVariable(e.Name, v, false, true, ss, fastPath: true);
                }

                InitialSessionState.CreateQuestionVariable(context);

                // Reset the path for this runspace.
                SetSessionStateDrive(context, true);

                // Reset the event, transaction and debug managers.
                context.ResetManagers();

                // Reset tracing/debugging to the off state.
                context.PSDebugTraceLevel = 0;
                context.PSDebugTraceStep = false;
            }
        }

        internal static void SetSessionStateDrive(ExecutionContext context, bool setLocation)
        {
            // Set the starting location to the current process working directory
            // Ignore any errors as the file system provider may not be loaded or
            // a drive with the same name as the real file system drive may not have
            // been mounted.
            try
            {
                bool proceedWithSetLocation = true;

                if (context.EngineSessionState.ProviderCount > 0)
                {
                    // NTRAID#Windows Out Of Band Releases-908481-2005/07/01-JeffJon
                    // Make sure we have a CurrentDrive set so that we can deal with
                    // UNC paths

                    if (context.EngineSessionState.CurrentDrive == null)
                    {
                        bool fsDriveSet = false;
                        try
                        {
                            // Set the current drive to the first FileSystem drive if it exists.
                            ProviderInfo fsProvider = context.EngineSessionState.GetSingleProvider(context.ProviderNames.FileSystem);

                            Collection<PSDriveInfo> fsDrives = fsProvider.Drives;
                            if (fsDrives != null && fsDrives.Count > 0)
                            {
                                context.EngineSessionState.CurrentDrive = fsDrives[0];
                                fsDriveSet = true;
                            }
                        }
                        catch (ProviderNotFoundException)
                        {
                        }

                        if (!fsDriveSet)
                        {
                            Collection<PSDriveInfo> allDrives = context.EngineSessionState.Drives(null);

                            if (allDrives != null && allDrives.Count > 0)
                            {
                                context.EngineSessionState.CurrentDrive = allDrives[0];
                            }
                            else
                            {
                                ItemNotFoundException itemNotFound =
                                    new ItemNotFoundException(Directory.GetCurrentDirectory(), "PathNotFound", SessionStateStrings.PathNotFound);

                                context.ReportEngineStartupError(itemNotFound);
                                proceedWithSetLocation = false;
                            }
                        }
                    }

                    if (proceedWithSetLocation && setLocation)
                    {
                        CmdletProviderContext providerContext = new CmdletProviderContext(context);

                        try
                        {
                            providerContext.SuppressWildcardExpansion = true;
                            context.EngineSessionState.SetLocation(Directory.GetCurrentDirectory(), providerContext);
                        }
                        catch (ItemNotFoundException)
                        {
                            // If we can't access the Environment.CurrentDirectory, we may be in an AppContainer. Set the
                            // default drive to $pshome
                            string defaultPath = System.IO.Path.GetDirectoryName(Environment.ProcessPath);
                            context.EngineSessionState.SetLocation(defaultPath, providerContext);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        internal static void CreateQuestionVariable(ExecutionContext context)
        {
            QuestionMarkVariable qv = new QuestionMarkVariable(context);
            context.EngineSessionState.SetVariableAtScope(qv, "global", true, CommandOrigin.Internal);
        }

        internal static void RemoveTypesAndFormats(ExecutionContext context, IList<string> formatFilesToRemove, IList<string> typeFilesToRemove)
        {
            // The formats and types tables are implemented in such a way that
            // we can't simply remove an entry. We need to edit the list, clear the
            // exiting composed table and then rebuild the entire table.
            if (formatFilesToRemove != null && formatFilesToRemove.Count > 0)
            {
                var newFormats = new InitialSessionStateEntryCollection<SessionStateFormatEntry>();
                HashSet<string> formatFilesToRemoveSet = new HashSet<string>(formatFilesToRemove, StringComparer.OrdinalIgnoreCase);
                foreach (SessionStateFormatEntry entry in context.InitialSessionState.Formats)
                {
                    if (!formatFilesToRemoveSet.Contains(entry.FileName))
                    {
                        newFormats.Add(entry);
                    }
                }

                context.InitialSessionState.Formats.Clear();
                context.InitialSessionState.Formats.Add(newFormats);
                context.InitialSessionState.UpdateFormats(context, false);
            }

            if (typeFilesToRemove != null && typeFilesToRemove.Count > 0)
            {
                // The types table has the same issue as the format table - we need to rebuild the entire table.
                var newTypes = new InitialSessionStateEntryCollection<SessionStateTypeEntry>();
                List<string> resolvedTypeFilesToRemove = new List<string>();
                foreach (var typeFile in typeFilesToRemove)
                {
                    resolvedTypeFilesToRemove.Add(ModuleCmdletBase.ResolveRootedFilePath(typeFile, context) ?? typeFile);
                }

                foreach (SessionStateTypeEntry entry in context.InitialSessionState.Types)
                {
                    if (entry.FileName == null)
                    {
                        // The entry is associated with a TypeData instance
                        newTypes.Add(entry);
                    }
                    else
                    {
                        // Resolving the file path because the path to the types file in module manifest is now specified as
                        // ..\..\types.ps1xml which expands to C:\Windows\System32\WindowsPowerShell\v1.0\Modules\Microsoft.PowerShell.Core\..\..\types.ps1xml
                        string filePath = ModuleCmdletBase.ResolveRootedFilePath(entry.FileName, context) ?? entry.FileName;
                        if (!resolvedTypeFilesToRemove.Contains(filePath))
                        {
                            newTypes.Add(entry);
                        }
                    }
                }

                // If there are any types that need to be added to the typetable, update them.
                // Else, clear the typetable
                if (newTypes.Count > 0)
                {
                    context.InitialSessionState.Types.Clear();
                    context.InitialSessionState.Types.Add(newTypes);
                    context.InitialSessionState.UpdateTypes(context, false);
                }
                else
                {
                    context.TypeTable.Clear();
                }
            }
        }

        /// <summary>
        /// Update the type metadata loaded into this runspace.
        /// </summary>
        /// <param name="context">The execution context for the runspace to update.</param>
        /// <param name="updateOnly">If true, re-initialize the metadata collection...</param>
        internal void UpdateTypes(ExecutionContext context, bool updateOnly)
        {
            if (Types.Count == 1)
            {
                TypeTable typeTable = Types[0].TypeTable;
                if (typeTable != null)
                {
                    // reuse the TypeTable instance specified in the sste.
                    // this essentially allows for TypeTable sharing across
                    // multiple runspaces.

                    context.TypeTable = typeTable;

                    Types.Clear();
                    Types.Add(typeTable.typesInfo);

                    return;
                }
            }

            if (!updateOnly)
            {
                context.TypeTable.Clear();
            }

            ConcurrentBag<string> errors = new ConcurrentBag<string>();
            // Use at most 3 locks (we don't expect contention on that many cores anyways,
            // and typically we'll be processing just 2 or 3 files anyway, hence capacity=3.
            ConcurrentDictionary<string, string> filesProcessed = new ConcurrentDictionary<string, string>(
                    concurrencyLevel: 3,
                    capacity: 3,
                    StringComparer.OrdinalIgnoreCase);
            Parallel.ForEach(
                Types,
                sste =>
            {
                // foreach (var sste in Types)
                if (sste.FileName != null)
                {
                    if (filesProcessed.TryAdd(sste.FileName, null))
                    {
                        string moduleName = string.Empty;
                        if (sste.PSSnapIn != null && !string.IsNullOrEmpty(sste.PSSnapIn.Name))
                        {
                            moduleName = sste.PSSnapIn.Name;
                        }

                        context.TypeTable.Update(moduleName, sste.FileName, errors, context.AuthorizationManager, context.EngineHostInterface, out _);
                    }
                }
                else if (sste.TypeTable != null)
                {
                    // We get here only if it's NOT updating the existing type table
                    // because we cannot do the update with a type table instance
                    errors.Add(TypesXmlStrings.TypeTableCannotCoExist);
                }
                else
                {
                    context.TypeTable.Update(sste.TypeData, errors, sste.IsRemove);
                }
            });

            context.TypeTable.ClearConsolidatedMembers();

            if (updateOnly)
            {
                // Put the SessionStateTypeEntry into the cache if we are updating the type table
                foreach (var sste in Types)
                {
                    context.InitialSessionState.Types.Add(sste);
                }
            }

            if (!errors.IsEmpty)
            {
                var allErrors = new StringBuilder();
                allErrors.Append('\n');
                foreach (string error in errors)
                {
                    if (!string.IsNullOrEmpty(error))
                    {
                        if (this.ThrowOnRunspaceOpenError || this.RefreshTypeAndFormatSetting)
                        {
                            allErrors.Append(error);
                            allErrors.Append('\n');
                        }
                        else
                        {
                            context.ReportEngineStartupError(ExtendedTypeSystem.TypesXmlError, error);
                        }
                    }
                }

                if (this.ThrowOnRunspaceOpenError)
                {
                    string resource = ExtendedTypeSystem.TypesXmlError;
                    ThrowTypeOrFormatErrors(resource, allErrors.ToString(), "ErrorsUpdatingTypes");
                }

                if (this.RefreshTypeAndFormatSetting)
                {
                    string resource = ExtendedTypeSystem.TypesXmlError;
                    ThrowTypeOrFormatErrors(resource, allErrors.ToString(), "ErrorsUpdatingTypes");
                }
            }
        }

        /// <summary>
        /// Update the formatting information for a runspace.
        /// </summary>
        /// <param name="context">The execution context for the runspace to be updated.</param>
        /// <param name="update">True if we only want to add stuff, false if we want to reinitialize.</param>
        internal void UpdateFormats(ExecutionContext context, bool update)
        {
            if (DisableFormatUpdates || this.Formats.Count == 0)
            {
                return;
            }

            Collection<PSSnapInTypeAndFormatErrors> entries = new Collection<PSSnapInTypeAndFormatErrors>();
            InitialSessionStateEntryCollection<SessionStateFormatEntry> formatsToLoad;

            // If we're just updating the current runspace, then we'll add our entries
            // to the current list otherwise, we'll build a new list...
            if (update && context.InitialSessionState != null)
            {
                formatsToLoad = context.InitialSessionState.Formats;
                formatsToLoad.Add(this.Formats);
            }
            else
            {
                formatsToLoad = this.Formats;
            }

            HashSet<string> filesProcessed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SessionStateFormatEntry ssfe in formatsToLoad)
            {
                string name = ssfe.FileName;
                PSSnapInInfo snapin = ssfe.PSSnapIn;
                if (snapin != null && !string.IsNullOrEmpty(snapin.Name))
                {
                    name = snapin.Name;
                }

                if (ssfe.Formattable != null)
                {
                    if (formatsToLoad.Count == 1)
                    {
                        context.FormatDBManager = ssfe.Formattable.FormatDBManager;
                    }
                    else
                    {
                        // if a SharedFormatTable is allowed then only one
                        // entry can be specified.
                        throw PSTraceSource.NewInvalidOperationException(FormatAndOutXmlLoadingStrings.FormatTableCannotCoExist);
                    }
                }
                else if (ssfe.FormatData != null)
                {
                    entries.Add(new PSSnapInTypeAndFormatErrors(name, ssfe.FormatData));
                }
                else
                {
                    if (!filesProcessed.Contains(ssfe.FileName))
                    {
                        filesProcessed.Add(ssfe.FileName);
                        entries.Add(new PSSnapInTypeAndFormatErrors(name, ssfe.FileName));
                    }
                }
            }

            if (entries.Count > 0)
            {
                context.FormatDBManager.UpdateDataBase(entries, context.AuthorizationManager, context.EngineHostInterface, true);

                var allErrors = new StringBuilder("\n");
                bool hasErrors = false;

                // Now see if there were any errors in the format files and report them
                // if this is the case...
                foreach (PSSnapInTypeAndFormatErrors entry in entries)
                {
                    if (entry.Errors != null && !entry.Errors.IsEmpty)
                    {
                        foreach (string error in entry.Errors)
                        {
                            if (!string.IsNullOrEmpty(error))
                            {
                                hasErrors = true;
                                if (this.ThrowOnRunspaceOpenError || this.RefreshTypeAndFormatSetting)
                                {
                                    allErrors.Append(error);
                                    allErrors.Append('\n');
                                }
                                else
                                {
                                    context.ReportEngineStartupError(FormatAndOutXmlLoadingStrings.FormatLoadingErrors, error);
                                }
                            }
                        }
                    }
                }

                if ((this.ThrowOnRunspaceOpenError || this.RefreshTypeAndFormatSetting) && hasErrors)
                {
                    string resource = FormatAndOutXmlLoadingStrings.FormatLoadingErrors;
                    ThrowTypeOrFormatErrors(resource, allErrors.ToString(), "ErrorsUpdatingFormats");
                }
            }
        }

        private static void ThrowTypeOrFormatErrors(string resourceString, string errorMsg, string errorId)
        {
            string message = StringUtil.Format(resourceString, errorMsg);
            var ex = new RuntimeException(message);
            ex.SetErrorId(errorId);
            throw ex;
        }

        /// <summary>
        /// Need to have SnapIn support till we move to modules.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="warning"></param>
        /// <returns></returns>
        [Obsolete("Custom PSSnapIn is deprecated. Please use a module instead.", true)]
        public PSSnapInInfo ImportPSSnapIn(string name, out PSSnapInException warning)
        {
            if (string.IsNullOrEmpty(name))
            {
                PSTraceSource.NewArgumentException(nameof(name));
            }

            // Check whether the mshsnapin is present in the registry.
            // TODO: Note the hard-coded version number here, this was part of the SingleShell
            // implementation and should be refactored.
            PSSnapInInfo newPSSnapIn = PSSnapInReader.Read("2", name);

            if (!PSVersionInfo.IsValidPSVersion(newPSSnapIn.PSVersion))
            {
                s_PSSnapInTracer.TraceError("MshSnapin {0} and current monad engine's versions don't match.", name);

                throw PSTraceSource.NewArgumentException(
                    "mshSnapInID",
                    ConsoleInfoErrorStrings.AddPSSnapInBadMonadVersion,
                    newPSSnapIn.PSVersion.ToString(),
                    "2.0");
            }

            // Now actually load the snapin...
            PSSnapInInfo snapin = ImportPSSnapIn(newPSSnapIn, out warning);

            return snapin;
        }

        internal PSSnapInInfo ImportCorePSSnapIn()
        {
            // Load Microsoft.PowerShell.Core as a snapin.
            PSSnapInInfo coreSnapin = PSSnapInReader.ReadCoreEngineSnapIn();
            ImportPSSnapIn(coreSnapin, out _);
            return coreSnapin;
        }

        internal PSSnapInInfo ImportPSSnapIn(PSSnapInInfo psSnapInInfo, out PSSnapInException warning)
        {
            ArgumentNullException.ThrowIfNull(psSnapInInfo);

            // See if the snapin is already loaded. If has been then there will be an entry in the
            // Assemblies list for it already...
            bool reload = true;

            foreach (SessionStateAssemblyEntry ae in this.Assemblies)
            {
                PSSnapInInfo loadedPSSnapInInfo = ae.PSSnapIn;
                if (loadedPSSnapInInfo != null)
                {
                    // See if the assembly-qualified names match and return the existing PSSnapInInfo
                    // if they do.
                    string loadedSnapInName = ae.PSSnapIn.AssemblyName;
                    if (!string.IsNullOrEmpty(loadedSnapInName)
                        && string.Equals(loadedSnapInName, psSnapInInfo.AssemblyName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        warning = null;

                        // the previous implementation used to return the
                        // same loaded snap-in value. This results in the
                        // commands/types/formats exposed in the snap-in
                        // to be not populated in the InitialSessionState
                        // object. This is being fixed
                        reload = false;
                        break;
                    }
                }
            }

            Dictionary<string, SessionStateCmdletEntry> cmdlets = null;
            Dictionary<string, List<SessionStateAliasEntry>> aliases = null;
            Dictionary<string, SessionStateProviderEntry> providers = null;

            Assembly assembly = null;
            string helpFile = null;

            if (reload)
            {
                s_PSSnapInTracer.WriteLine("Loading assembly for psSnapIn {0}", psSnapInInfo.Name);

                assembly = PSSnapInHelpers.LoadPSSnapInAssembly(psSnapInInfo);

                if (assembly == null)
                {
                    s_PSSnapInTracer.TraceError("Loading assembly for psSnapIn {0} failed", psSnapInInfo.Name);
                    warning = null;
                    return null; // BUGBUG - should add something to the warnings list here instead of quitting...
                }

                s_PSSnapInTracer.WriteLine("Loading assembly for psSnapIn {0} succeeded", psSnapInInfo.Name);

                PSSnapInHelpers.AnalyzePSSnapInAssembly(assembly, psSnapInInfo.Name, psSnapInInfo, moduleInfo: null, out cmdlets, out aliases, out providers, out helpFile);
            }

            // We skip checking if the file exists when it's in $PSHOME because of magic
            // where we have the former contents of those files built into the engine directly.
            var psHome = Utils.DefaultPowerShellAppBase;

            foreach (string file in psSnapInInfo.Types)
            {
                string path = Path.Combine(psSnapInInfo.ApplicationBase, file);

                if (!string.Equals(psHome, psSnapInInfo.ApplicationBase, StringComparison.OrdinalIgnoreCase)
                    && !File.Exists(path))
                {
                    // Remove the application base directory if assembly doesn't exist in it.
                    path = file;
                }

                SessionStateTypeEntry typeEntry = new SessionStateTypeEntry(path);
                typeEntry.SetPSSnapIn(psSnapInInfo);
                this.Types.Add(typeEntry);
            }

            foreach (string file in psSnapInInfo.Formats)
            {
                string path = Path.Combine(psSnapInInfo.ApplicationBase, file);

                if (!string.Equals(psHome, psSnapInInfo.ApplicationBase, StringComparison.OrdinalIgnoreCase)
                    && !File.Exists(path))
                {
                    path = file;
                }

                SessionStateFormatEntry formatEntry = new SessionStateFormatEntry(path);
                formatEntry.SetPSSnapIn(psSnapInInfo);
                this.Formats.Add(formatEntry);
            }

            var assemblyEntry = new SessionStateAssemblyEntry(psSnapInInfo.AssemblyName, psSnapInInfo.AbsoluteModulePath);
            assemblyEntry.SetPSSnapIn(psSnapInInfo);
            Assemblies.Add(assemblyEntry);

            if (cmdlets != null)
            {
                foreach (SessionStateCmdletEntry cmdlet in cmdlets.Values)
                {
                    SessionStateCmdletEntry newEntry = (SessionStateCmdletEntry)cmdlet.Clone();
                    newEntry.Visibility = this.DefaultCommandVisibility;

                    this.Commands.Add(newEntry);
                }
            }

            if (aliases != null)
            {
                foreach (var cmdletAliasesEntry in aliases.Values)
                {
                    foreach (var sessionStateAliasEntry in cmdletAliasesEntry)
                    {
                        sessionStateAliasEntry.Visibility = this.DefaultCommandVisibility;
                        this.Commands.Add(sessionStateAliasEntry);
                    }
                }
            }

            if (providers != null)
            {
                foreach (SessionStateProviderEntry provider in providers.Values)
                {
                    this.Providers.Add(provider);
                }
            }

            warning = null;

            // Add help file information for built-in functions
            if (psSnapInInfo.Name.Equals(CoreSnapin, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var f in BuiltInFunctions)
                {
                    Collection<SessionStateCommandEntry> funcList = Commands[f.Name];
                    foreach (var func in funcList)
                    {
                        if (func is SessionStateFunctionEntry)
                        {
                            ((SessionStateFunctionEntry)func).SetHelpFile(helpFile);
                        }
                    }
                }
            }

            ImportedSnapins.Add(psSnapInInfo.Name, psSnapInInfo);
            return psSnapInInfo;
        }

        internal PSSnapInInfo GetPSSnapIn(string psSnapinName)
        {
            if (ImportedSnapins.TryGetValue(psSnapinName, out PSSnapInInfo importedSnapin))
            {
                return importedSnapin;
            }

            return null;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        internal static Assembly LoadAssemblyFromFile(string fileName)
        {
            s_PSSnapInTracer.WriteLine("Loading assembly for psSnapIn {0}", fileName);

            Assembly assembly = Assembly.LoadFrom(fileName);
            if (assembly == null)
            {
                s_PSSnapInTracer.TraceError("Loading assembly for psSnapIn {0} failed", fileName);
                return null;
            }

            s_PSSnapInTracer.WriteLine("Loading assembly for psSnapIn {0} succeeded", fileName);

            return assembly;
        }

        internal void ImportCmdletsFromAssembly(Assembly assembly, PSModuleInfo module)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            string assemblyPath = assembly.Location;
            PSSnapInHelpers.AnalyzePSSnapInAssembly(
                assembly,
                assemblyPath,
                psSnapInInfo: null,
                module,
                out Dictionary<string, SessionStateCmdletEntry> cmdlets,
                out Dictionary<string, List<SessionStateAliasEntry>> aliases,
                out Dictionary<string, SessionStateProviderEntry> providers,
                helpFile: out _);

            if (cmdlets != null)
            {
                foreach (SessionStateCmdletEntry cmdlet in cmdlets.Values)
                {
                    this.Commands.Add(cmdlet);
                }
            }

            if (aliases != null)
            {
                foreach (var cmdletAliasesEntry in aliases.Values)
                {
                    foreach (var sessionStateAliasEntry in cmdletAliasesEntry)
                    {
                        this.Commands.Add(sessionStateAliasEntry);
                    }
                }
            }

            if (providers != null)
            {
                foreach (SessionStateProviderEntry provider in providers.Values)
                {
                    this.Providers.Add(provider);
                }
            }
        }

        // Now define a bunch of functions that describe the rest of the default session state...
        internal const string FormatEnumerationLimit = "FormatEnumerationLimit";
        internal const int DefaultFormatEnumerationLimit = 4;

        /// <summary>
        /// This is the default function to use for tab expansion.
        /// </summary>
        private static readonly string s_tabExpansionFunctionText = @"
<# Options include:
     RelativeFilePaths - [bool]
         Always resolve file paths using Resolve-Path -Relative.
         The default is to use some heuristics to guess if relative or absolute is better.

   To customize your own custom options, pass a hashtable to CompleteInput, e.g.
         return [System.Management.Automation.CommandCompletion]::CompleteInput($inputScript, $cursorColumn,
             @{ RelativeFilePaths=$false }
#>

[CmdletBinding(DefaultParameterSetName = 'ScriptInputSet')]
[OutputType([System.Management.Automation.CommandCompletion])]
Param(
    [Parameter(ParameterSetName = 'ScriptInputSet', Mandatory = $true, Position = 0)]
    [string] $inputScript,

    [Parameter(ParameterSetName = 'ScriptInputSet', Position = 1)]
    [int] $cursorColumn = $inputScript.Length,

    [Parameter(ParameterSetName = 'AstInputSet', Mandatory = $true, Position = 0)]
    [System.Management.Automation.Language.Ast] $ast,

    [Parameter(ParameterSetName = 'AstInputSet', Mandatory = $true, Position = 1)]
    [System.Management.Automation.Language.Token[]] $tokens,

    [Parameter(ParameterSetName = 'AstInputSet', Mandatory = $true, Position = 2)]
    [System.Management.Automation.Language.IScriptPosition] $positionOfCursor,

    [Parameter(ParameterSetName = 'ScriptInputSet', Position = 2)]
    [Parameter(ParameterSetName = 'AstInputSet', Position = 3)]
    [Hashtable] $options = $null
)

End
{
    if ($psCmdlet.ParameterSetName -eq 'ScriptInputSet')
    {
        return [System.Management.Automation.CommandCompletion]::CompleteInput(
            <#inputScript#>  $inputScript,
            <#cursorColumn#> $cursorColumn,
            <#options#>      $options)
    }
    else
    {
        return [System.Management.Automation.CommandCompletion]::CompleteInput(
            <#ast#>              $ast,
            <#tokens#>           $tokens,
            <#positionOfCursor#> $positionOfCursor,
            <#options#>          $options)
    }
}
        ";

        /// <summary>
        /// This is the default function to use for clear-host.
        /// </summary>
        internal static string GetClearHostFunctionText()
        {
            if (Platform.IsWindows)
            {
                // use $RawUI so this works over remoting where there isn't a physical console
                return @"
$RawUI = $Host.UI.RawUI
$RawUI.CursorPosition = @{X=0;Y=0}
$RawUI.SetBufferContents(
    @{Top = -1; Bottom = -1; Right = -1; Left = -1},
    @{Character = ' '; ForegroundColor = $rawui.ForegroundColor; BackgroundColor = $rawui.BackgroundColor})
# .Link
# https://go.microsoft.com/fwlink/?LinkID=2096480
# .ExternalHelp System.Management.Automation.dll-help.xml
";
            }
            else
            {
                // Porting note: non-Windows platforms use `clear`
                return @"
[Console]::Write((
    & (Get-Command -CommandType Application clear | Select-Object -First 1).Definition
))
# .Link
# https://go.microsoft.com/fwlink/?LinkID=2096480
# .ExternalHelp System.Management.Automation.dll-help.xml
";
            }
        }

#if UNIX
        internal static string GetExecFunctionText()
        {
            return @"
Switch-Process -WithCommand $args
";
        }
#endif

        /// <summary>
        /// This is the default function to use for man/help. It uses
        /// splatting to pass in the parameters.
        /// </summary>
        internal static string GetHelpPagingFunctionText()
        {
            // We used to generate the text for this function so you could add a parameter
            // to Get-Help and not worry about adding it here.  That was a little slow at
            // startup, so it's hard coded, with a test to make sure the parameters match.
            return @"
<#
.FORWARDHELPTARGETNAME Get-Help
.FORWARDHELPCATEGORY Cmdlet
#>
[CmdletBinding(DefaultParameterSetName='AllUsersView', HelpUri='https://go.microsoft.com/fwlink/?LinkID=113316')]
param(
    [Parameter(Position=0, ValueFromPipelineByPropertyName=$true)]
    [string]
    ${Name},

    [string]
    ${Path},

    [ValidateSet('Alias','Cmdlet','Provider','General','FAQ','Glossary','HelpFile','ScriptCommand','Function','Filter','ExternalScript','All','DefaultHelp','DscResource','Class','Configuration')]
    [string[]]
    ${Category},

    [Parameter(ParameterSetName='DetailedView', Mandatory=$true)]
    [switch]
    ${Detailed},

    [Parameter(ParameterSetName='AllUsersView')]
    [switch]
    ${Full},

    [Parameter(ParameterSetName='Examples', Mandatory=$true)]
    [switch]
    ${Examples},

    [Parameter(ParameterSetName='Parameters', Mandatory=$true)]
    [string[]]
    ${Parameter},

    [string[]]
    ${Component},

    [string[]]
    ${Functionality},

    [string[]]
    ${Role},

    [Parameter(ParameterSetName='Online', Mandatory=$true)]
    [switch]
    ${Online},

    [Parameter(ParameterSetName='ShowWindow', Mandatory=$true)]
    [switch]
    ${ShowWindow})

    # Display the full help topic by default but only for the AllUsersView parameter set.
    if (($psCmdlet.ParameterSetName -eq 'AllUsersView') -and !$Full) {
        $PSBoundParameters['Full'] = $true
    }

    # Nano needs to use Unicode, but Windows and Linux need the default
    $OutputEncoding = if ([System.Management.Automation.Platform]::IsNanoServer -or [System.Management.Automation.Platform]::IsIoT) {
        [System.Text.Encoding]::Unicode
    } else {
        [System.Console]::OutputEncoding
    }

    $help = Get-Help @PSBoundParameters

    # If a list of help is returned or AliasHelpInfo (because it is small), don't pipe to more
    $psTypeNames = ($help | Select-Object -First 1).PSTypeNames
    if ($psTypeNames -Contains 'HelpInfoShort' -Or $psTypeNames -Contains 'AliasHelpInfo')
    {
        $help
    }
    elseif ($help -ne $null)
    {
        # By default use more on Windows and less on Linux.
        if ($IsWindows) {
            $pagerCommand = 'more.com'
            $pagerArgs = $null
        }
        else {
            $pagerCommand = 'less'
            $pagerArgs = '-s','-P','Page %db?B of %D:.\. Press h for help or q to quit\.'
        }

        # Respect PAGER environment variable which allows user to specify a custom pager.
        # Ignore a pure whitespace PAGER value as that would cause the tokenizer to return 0 tokens.
        if (![string]::IsNullOrWhitespace($env:PAGER)) {
            if (Get-Command $env:PAGER -ErrorAction Ignore) {
                # Entire PAGER value corresponds to a single command.
                $pagerCommand = $env:PAGER
                $pagerArgs = $null
            }
            else {
                # PAGER value is not a valid command, check if PAGER command and arguments have been specified.
                # Tokenize the specified $env:PAGER value. Ignore tokenizing errors since any errors may be valid
                # argument syntax for the paging utility.
                $errs = $null
                $tokens = [System.Management.Automation.PSParser]::Tokenize($env:PAGER, [ref]$errs)

                $customPagerCommand = $tokens[0].Content
                if (!(Get-Command $customPagerCommand -ErrorAction Ignore)) {
                    # Custom pager command is invalid, issue a warning.
                    Write-Warning ""Custom-paging utility command not found. Ignoring command specified in `$env:PAGER: $env:PAGER""
                }
                else {
                    # This approach will preserve all the pagers args.
                    $pagerCommand = $customPagerCommand
                    $pagerArgs = if ($tokens.Count -gt 1) {$env:PAGER.Substring($tokens[1].Start)} else {$null}
                }
            }
        }

        $pagerCommandInfo = Get-Command -Name $pagerCommand -ErrorAction Ignore
        if ($pagerCommandInfo -eq $null) {
            $help
        }
        elseif ($pagerCommandInfo.CommandType -eq 'Application') {
            # If the pager is an application, format the output width before sending to the app.
            $consoleWidth = [System.Math]::Max([System.Console]::WindowWidth, 20)

            if ($pagerArgs) {
                $help | Out-String -Stream -Width ($consoleWidth - 1) | & $pagerCommand $pagerArgs
            }
            else {
                $help | Out-String -Stream -Width ($consoleWidth - 1) | & $pagerCommand
            }
        }
        else {
            # The pager command is a PowerShell function, script or alias, so pipe directly into it.
            $help | & $pagerCommand $pagerArgs
        }
    }
";
        }

        internal static string GetMkdirFunctionText()
        {
            return @"
<#
.FORWARDHELPTARGETNAME New-Item
.FORWARDHELPCATEGORY Cmdlet
#>

[CmdletBinding(DefaultParameterSetName='pathSet',
    SupportsShouldProcess=$true,
    SupportsTransactions=$true,
    ConfirmImpact='Medium')]
    [OutputType([System.IO.DirectoryInfo])]
param(
    [Parameter(ParameterSetName='nameSet', Position=0, ValueFromPipelineByPropertyName=$true)]
    [Parameter(ParameterSetName='pathSet', Mandatory=$true, Position=0, ValueFromPipelineByPropertyName=$true)]
    [System.String[]]
    ${Path},

    [Parameter(ParameterSetName='nameSet', Mandatory=$true, ValueFromPipelineByPropertyName=$true)]
    [AllowNull()]
    [AllowEmptyString()]
    [System.String]
    ${Name},

    [Parameter(ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
    [System.Object]
    ${Value},

    [Switch]
    ${Force},

    [Parameter(ValueFromPipelineByPropertyName=$true)]
    [System.Management.Automation.PSCredential]
    ${Credential}
)

begin {
    $wrappedCmd = $ExecutionContext.InvokeCommand.GetCommand('New-Item', [System.Management.Automation.CommandTypes]::Cmdlet)
    $scriptCmd = {& $wrappedCmd -Type Directory @PSBoundParameters }

    $steppablePipeline = $scriptCmd.GetSteppablePipeline()
    $steppablePipeline.Begin($PSCmdlet)
}

process {
    $steppablePipeline.Process($_)
}

end {
    $steppablePipeline.End()
}

";
        }

        internal static string GetOSTFunctionText()
        {
            return @"
[CmdletBinding()]
param(
    [ValidateRange(2, 2147483647)]
    [int]
    ${Width},

    [Parameter(ValueFromPipeline=$true)]
    [psobject]
    ${InputObject})

begin {
    $PSBoundParameters['Stream'] = $true
    $wrappedCmd = $ExecutionContext.InvokeCommand.GetCommand('Out-String',[System.Management.Automation.CommandTypes]::Cmdlet)
    $scriptCmd = {& $wrappedCmd @PSBoundParameters }

    $steppablePipeline = $scriptCmd.GetSteppablePipeline($myInvocation.CommandOrigin)
    $steppablePipeline.Begin($PSCmdlet)
}

process {
    $steppablePipeline.Process($_)
}

end {
    $steppablePipeline.End()
}
<#
.ForwardHelpTargetName Out-String
.ForwardHelpCategory Cmdlet
#>
";
        }

        internal const ActionPreference DefaultDebugPreference = ActionPreference.SilentlyContinue;
        internal const ActionPreference DefaultErrorActionPreference = ActionPreference.Continue;
        internal const ActionPreference DefaultProgressPreference = ActionPreference.Continue;
        internal const ActionPreference DefaultVerbosePreference = ActionPreference.SilentlyContinue;
        internal const ActionPreference DefaultWarningPreference = ActionPreference.Continue;
        internal const ActionPreference DefaultInformationPreference = ActionPreference.SilentlyContinue;

        internal const ErrorView DefaultErrorView = ErrorView.ConciseView;
        internal const bool DefaultWhatIfPreference = false;
        internal const ConfirmImpact DefaultConfirmPreference = ConfirmImpact.High;

        static InitialSessionState()
        {
            var builtinVariables = new List<SessionStateVariableEntry>()
            {
                // Engine variables that should be precreated before running profile
                // Bug fix for Win7:2202228 Engine halts if initial command fulls up variable table
                // Anytime a new variable that the engine depends on to run is added, this table
                // must be updated...
                new SessionStateVariableEntry(SpecialVariables.LastToken, null, string.Empty),
                new SessionStateVariableEntry(SpecialVariables.FirstToken, null, string.Empty),
                new SessionStateVariableEntry(SpecialVariables.StackTrace, null, string.Empty),

                // Variable which controls the output rendering
                new SessionStateVariableEntry(
                    SpecialVariables.PSStyle,
                    PSStyle.Instance,
                    RunspaceInit.PSStyleDescription,
                    ScopedItemOptions.Constant),

                // Variable which controls the encoding for piping data to a NativeCommand
                new SessionStateVariableEntry(
                    SpecialVariables.OutputEncoding,
                    Encoding.Default,
                    RunspaceInit.OutputEncodingDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(System.Text.Encoding))),

                // Preferences
                //
                // NTRAID#Windows Out Of Band Releases-931461-2006/03/13
                // ArgumentTypeConverterAttribute is applied to these variables,
                // but this only reaches the global variable.  If these are
                // redefined in script scope etc, the type conversion
                // is not applicable.
                //
                // Variables typed to ActionPreference
                new SessionStateVariableEntry(
                    SpecialVariables.ConfirmPreference,
                    DefaultConfirmPreference,
                    RunspaceInit.ConfirmPreferenceDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(ConfirmImpact))),
                new SessionStateVariableEntry(
                    SpecialVariables.DebugPreference,
                    DefaultDebugPreference,
                    RunspaceInit.DebugPreferenceDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(ActionPreference))),
                new SessionStateVariableEntry(
                    SpecialVariables.ErrorActionPreference,
                    DefaultErrorActionPreference,
                    RunspaceInit.ErrorActionPreferenceDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(ActionPreference))),
                new SessionStateVariableEntry(
                    SpecialVariables.ProgressPreference,
                    DefaultProgressPreference,
                    RunspaceInit.ProgressPreferenceDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(ActionPreference))),
                new SessionStateVariableEntry(
                    SpecialVariables.VerbosePreference,
                    DefaultVerbosePreference,
                    RunspaceInit.VerbosePreferenceDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(ActionPreference))),
                new SessionStateVariableEntry(
                    SpecialVariables.WarningPreference,
                    DefaultWarningPreference,
                    RunspaceInit.WarningPreferenceDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(ActionPreference))),
                new SessionStateVariableEntry(
                    SpecialVariables.InformationPreference,
                    DefaultInformationPreference,
                    RunspaceInit.InformationPreferenceDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(ActionPreference))),
                new SessionStateVariableEntry(
                    SpecialVariables.ErrorView,
                    DefaultErrorView,
                    RunspaceInit.ErrorViewDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(ErrorView))),
                new SessionStateVariableEntry(
                    SpecialVariables.NestedPromptLevel,
                    0,
                    RunspaceInit.NestedPromptLevelDescription),
                new SessionStateVariableEntry(
                    SpecialVariables.WhatIfPreference,
                    DefaultWhatIfPreference,
                    RunspaceInit.WhatIfPreferenceDescription),
                new SessionStateVariableEntry(
                    FormatEnumerationLimit,
                    DefaultFormatEnumerationLimit,
                    RunspaceInit.FormatEnumerationLimitDescription),

                // variable for PSEmailServer
                new SessionStateVariableEntry(
                    SpecialVariables.PSEmailServer,
                    string.Empty,
                    RunspaceInit.PSEmailServerDescription),

                // Start: Variables which control remoting behavior
                new SessionStateVariableEntry(
                    Microsoft.PowerShell.Commands.PSRemotingBaseCmdlet.DEFAULT_SESSION_OPTION,
                    new System.Management.Automation.Remoting.PSSessionOption(),
                    RemotingErrorIdStrings.PSDefaultSessionOptionDescription,
                    ScopedItemOptions.None),
                new SessionStateVariableEntry(
                    SpecialVariables.PSSessionConfigurationName,
                    "http://schemas.microsoft.com/powershell/Microsoft.PowerShell",
                    RemotingErrorIdStrings.PSSessionConfigurationName,
                    ScopedItemOptions.None),
                new SessionStateVariableEntry(
                    SpecialVariables.PSSessionApplicationName,
                    "wsman",
                    RemotingErrorIdStrings.PSSessionAppName,
                    ScopedItemOptions.None),
                // End: Variables which control remoting behavior

                #region Platform
                new SessionStateVariableEntry(
                    SpecialVariables.IsLinux,
                    Platform.IsLinux,
                    string.Empty,
                    ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope),

                new SessionStateVariableEntry(
                    SpecialVariables.IsMacOS,
                    Platform.IsMacOS,
                    string.Empty,
                    ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope),

                new SessionStateVariableEntry(
                    SpecialVariables.IsWindows,
                    Platform.IsWindows,
                    string.Empty,
                    ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope),

                new SessionStateVariableEntry(
                    SpecialVariables.IsCoreCLR,
                    Platform.IsCoreCLR,
                    string.Empty,
                    ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope),
                #endregion
            };

            if (ExperimentalFeature.IsEnabled(ExperimentalFeature.PSNativeCommandErrorActionPreferenceFeatureName))
            {
                builtinVariables.Add(
                    new SessionStateVariableEntry(
                        SpecialVariables.PSNativeCommandUseErrorActionPreference,
                        value: true,    // when this feature is changed to stable, this should default to `false`
                        RunspaceInit.PSNativeCommandUseErrorActionPreferenceDescription,
                        ScopedItemOptions.None,
                        new ArgumentTypeConverterAttribute(typeof(bool))));
            }

            builtinVariables.Add(
                new SessionStateVariableEntry(
                    SpecialVariables.NativeArgumentPassing,
                    Platform.IsWindows ? NativeArgumentPassingStyle.Windows : NativeArgumentPassingStyle.Standard,
                    RunspaceInit.NativeCommandArgumentPassingDescription,
                    ScopedItemOptions.None,
                    new ArgumentTypeConverterAttribute(typeof(NativeArgumentPassingStyle))));

            BuiltInVariables = builtinVariables.ToArray();
        }

        internal static readonly SessionStateVariableEntry[] BuiltInVariables;

        /// <summary>
        /// Returns a new array of alias entries everytime it's called. This
        /// can't be static because the elements may be mutated in different session
        /// state objects so each session state must have a copy of the entry.
        /// </summary>
        internal static SessionStateAliasEntry[] BuiltInAliases
        {
            get
            {
                // Too many AllScope entries hurts performance because an entry is
                // created in each new scope, so we limit the use of AllScope to the
                // most commonly used commands - primarily so command lookup is faster,
                // though if we speed up command lookup significantly, then removing
                // AllScope for all of these aliases makes sense.

                const ScopedItemOptions AllScope = ScopedItemOptions.AllScope;
                const ScopedItemOptions ReadOnly_AllScope = ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope;
                const ScopedItemOptions ReadOnly = ScopedItemOptions.ReadOnly;

                var builtInAliases = new List<SessionStateAliasEntry> {
                    new SessionStateAliasEntry("foreach", "ForEach-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("%", "ForEach-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("where", "Where-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("?", "Where-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("clc", "Clear-Content", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("cli", "Clear-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("clp", "Clear-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("clv", "Clear-Variable", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("cpi", "Copy-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("cvpa", "Convert-Path", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("dbp", "Disable-PSBreakpoint", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ebp", "Enable-PSBreakpoint", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("epal", "Export-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("epcsv", "Export-Csv", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("fl", "Format-List", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ft", "Format-Table", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("fw", "Format-Wide", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gal", "Get-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gbp", "Get-PSBreakpoint", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gc", "Get-Content", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gci", "Get-ChildItem", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gcm", "Get-Command", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gdr", "Get-PSDrive", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gcs", "Get-PSCallStack", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ghy", "Get-History", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gi", "Get-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gl", "Get-Location", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gm", "Get-Member", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gmo", "Get-Module", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gp", "Get-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gpv", "Get-ItemPropertyValue", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gps", "Get-Process", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("group", "Group-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gu", "Get-Unique", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gv", "Get-Variable", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("iex", "Invoke-Expression", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ihy", "Invoke-History", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ii", "Invoke-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ipmo", "Import-Module", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ipal", "Import-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ipcsv", "Import-Csv", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("measure", "Measure-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("mi", "Move-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("mp", "Move-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("nal", "New-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ndr", "New-PSDrive", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ni", "New-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("nv", "New-Variable", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("nmo", "New-Module", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("oh", "Out-Host", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rbp", "Remove-PSBreakpoint", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rdr", "Remove-PSDrive", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ri", "Remove-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rni", "Rename-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rnp", "Rename-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rp", "Remove-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rmo", "Remove-Module", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rv", "Remove-Variable", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gerr", "Get-Error", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rvpa", "Resolve-Path", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sal", "Set-Alias", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sbp", "Set-PSBreakpoint", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("select", "Select-Object", string.Empty, ReadOnly_AllScope),
                    new SessionStateAliasEntry("si", "Set-Item", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sl", "Set-Location", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sp", "Set-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("saps", "Start-Process", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("spps", "Stop-Process", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sv", "Set-Variable", string.Empty, ReadOnly),
                    // Web cmdlets aliases
                    new SessionStateAliasEntry("irm", "Invoke-RestMethod", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("iwr", "Invoke-WebRequest", string.Empty, ReadOnly),
// Porting note: #if !UNIX is used to disable aliases for cmdlets which conflict with Linux / macOS
#if !UNIX
                    // ac is a native command on macOS
                    new SessionStateAliasEntry("ac", "Add-Content", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("clear", "Clear-Host"),
                    new SessionStateAliasEntry("compare", "Compare-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("cpp", "Copy-ItemProperty", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("diff", "Compare-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("gsv", "Get-Service", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sleep", "Start-Sleep", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sort", "Sort-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("start", "Start-Process", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sasv", "Start-Service", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("spsv", "Stop-Service", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("tee", "Tee-Object", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("write", "Write-Output", string.Empty, ReadOnly),
                    // These were transferred from the "transferred from the profile" section
                    new SessionStateAliasEntry("cat", "Get-Content"),
                    new SessionStateAliasEntry("cp", "Copy-Item", string.Empty, AllScope),
                    new SessionStateAliasEntry("ls", "Get-ChildItem"),
                    new SessionStateAliasEntry("man", "help"),
                    new SessionStateAliasEntry("mount", "New-PSDrive"),
                    new SessionStateAliasEntry("mv", "Move-Item"),
                    new SessionStateAliasEntry("ps", "Get-Process"),
                    new SessionStateAliasEntry("rm", "Remove-Item"),
                    new SessionStateAliasEntry("rmdir", "Remove-Item"),
                    new SessionStateAliasEntry("cnsn", "Connect-PSSession", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("dnsn", "Disconnect-PSSession", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ogv", "Out-GridView", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("shcm", "Show-Command", string.Empty, ReadOnly),
#endif
                    // Bash built-ins we purposefully keep even if they override native commands
                    new SessionStateAliasEntry("cd", "Set-Location", string.Empty, AllScope),
                    new SessionStateAliasEntry("dir", "Get-ChildItem", string.Empty, AllScope),
                    new SessionStateAliasEntry("echo", "Write-Output", string.Empty, AllScope),
                    new SessionStateAliasEntry("fc", "Format-Custom", string.Empty, ReadOnly),
#if !UNIX
                    new SessionStateAliasEntry("kill", "Stop-Process"),
#endif
                    new SessionStateAliasEntry("pwd", "Get-Location"),
                    new SessionStateAliasEntry("type", "Get-Content"),
// #if !CORECLR is used to disable aliases for cmdlets which are not available on OneCore or not appropriate for PSCore6 due to conflicts
#if !CORECLR
                    new SessionStateAliasEntry("gwmi", "Get-WmiObject", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("iwmi", "Invoke-WMIMethod", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ise", "powershell_ise.exe", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("rwmi", "Remove-WMIObject", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("sc", "Set-Content", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("swmi", "Set-WMIInstance", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("trcm", "Trace-Command", string.Empty, ReadOnly),
#endif
                    // Aliases transferred from the profile
                    new SessionStateAliasEntry("h", "Get-History"),
                    new SessionStateAliasEntry("history", "Get-History"),
                    new SessionStateAliasEntry("md", "mkdir", string.Empty, AllScope),
                    new SessionStateAliasEntry("popd", "Pop-Location", string.Empty, AllScope),
                    new SessionStateAliasEntry("pushd", "Push-Location", string.Empty, AllScope),
                    new SessionStateAliasEntry("r", "Invoke-History"),
                    new SessionStateAliasEntry("cls", "Clear-Host"),
                    new SessionStateAliasEntry("chdir", "Set-Location"),
                    new SessionStateAliasEntry("copy", "Copy-Item", string.Empty, AllScope),
                    new SessionStateAliasEntry("del", "Remove-Item", string.Empty, AllScope),
                    new SessionStateAliasEntry("erase", "Remove-Item"),
                    new SessionStateAliasEntry("move", "Move-Item", string.Empty, AllScope),
                    new SessionStateAliasEntry("rd", "Remove-Item"),
                    new SessionStateAliasEntry("ren", "Rename-Item"),
                    new SessionStateAliasEntry("set", "Set-Variable"),
                    new SessionStateAliasEntry("icm", "Invoke-Command"),
                    new SessionStateAliasEntry("clhy", "Clear-History", string.Empty, ReadOnly),
                    // Job Specific aliases
                    new SessionStateAliasEntry("gjb", "Get-Job"),
                    new SessionStateAliasEntry("rcjb", "Receive-Job"),
                    new SessionStateAliasEntry("rjb", "Remove-Job"),
                    new SessionStateAliasEntry("sajb", "Start-Job"),
                    new SessionStateAliasEntry("spjb", "Stop-Job"),
                    new SessionStateAliasEntry("wjb", "Wait-Job"),
#if !CORECLR
                    new SessionStateAliasEntry("sujb", "Suspend-Job"),
                    new SessionStateAliasEntry("rujb", "Resume-Job"),
                    // Remoting Cmdlets Specific aliases
                    new SessionStateAliasEntry("npssc", "New-PSSessionConfigurationFile", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("ipsn", "Import-PSSession"),
                    new SessionStateAliasEntry("epsn", "Export-PSSession"),
#endif
                    new SessionStateAliasEntry("nsn", "New-PSSession"),
                    new SessionStateAliasEntry("gsn", "Get-PSSession"),
                    new SessionStateAliasEntry("rsn", "Remove-PSSession"),
                    new SessionStateAliasEntry("etsn", "Enter-PSSession"),
                    new SessionStateAliasEntry("rcsn", "Receive-PSSession", string.Empty, ReadOnly),
                    new SessionStateAliasEntry("exsn", "Exit-PSSession"),
                    // Win8: 121662/169179 Add "sls" alias for Select-String cmdlet
                    //   - do not use AllScope - this causes errors in profiles that set this somewhat commonly used alias.
                    new SessionStateAliasEntry("sls", "Select-String"),
                };

                return builtInAliases.ToArray();
            }
        }

        internal const string DefaultPromptFunctionText = @"
""PS $($executionContext.SessionState.Path.CurrentLocation)$('>' * ($nestedPromptLevel + 1)) "";
# .Link
# https://go.microsoft.com/fwlink/?LinkID=225750
# .ExternalHelp System.Management.Automation.dll-help.xml
";

        internal const string DefaultSetDriveFunctionText = "Set-Location $MyInvocation.MyCommand.Name";

        internal static readonly ScriptBlock SetDriveScriptBlock = ScriptBlock.CreateDelayParsedScriptBlock(DefaultSetDriveFunctionText, isProductCode: true);

        private static readonly PSLanguageMode systemLanguageMode = (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce) ? PSLanguageMode.ConstrainedLanguage : PSLanguageMode.FullLanguage;

        internal static readonly SessionStateFunctionEntry[] BuiltInFunctions = new SessionStateFunctionEntry[]
        {
           // Functions that don't require full language mode
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("cd..", "Set-Location ..", isProductCode: true, languageMode: systemLanguageMode),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("cd\\", "Set-Location \\", isProductCode: true, languageMode: systemLanguageMode),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("cd~", "Set-Location ~", isProductCode: true, languageMode: systemLanguageMode),
            // Win8: 320909. Retaining the original definition to ensure backward compatability.
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("Pause",
                string.Concat("$null = Read-Host '", CodeGeneration.EscapeSingleQuotedStringContent(RunspaceInit.PauseDefinitionString), "'"), isProductCode: true, languageMode: systemLanguageMode),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("help", GetHelpPagingFunctionText(), isProductCode: true, languageMode: systemLanguageMode),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("prompt", DefaultPromptFunctionText, isProductCode: true, languageMode: systemLanguageMode),

#if UNIX
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("exec", GetExecFunctionText(), isProductCode: true, languageMode: systemLanguageMode),
#endif

            // Functions that require full language mode and are trusted
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("Clear-Host", GetClearHostFunctionText(), isProductCode: true, languageMode: PSLanguageMode.FullLanguage),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("TabExpansion2", s_tabExpansionFunctionText, isProductCode: true, languageMode: PSLanguageMode.FullLanguage),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("oss", GetOSTFunctionText(), isProductCode: true, languageMode: PSLanguageMode.FullLanguage),
#if !UNIX
            // Porting note: we remove mkdir on Linux because of a conflict
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("mkdir", GetMkdirFunctionText(), isProductCode: true, languageMode: PSLanguageMode.FullLanguage),
#endif
#if !UNIX
            // Porting note: we remove the drive functions from Linux because they make no sense in that environment
            // Default drives
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("A:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("B:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("C:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("D:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("E:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("F:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("G:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("H:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("I:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("J:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("K:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("L:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("M:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("N:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("O:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("P:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("Q:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("R:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("S:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("T:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("U:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("V:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("W:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("X:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("Y:", DefaultSetDriveFunctionText, SetDriveScriptBlock),
            SessionStateFunctionEntry.GetDelayParsedFunctionEntry("Z:", DefaultSetDriveFunctionText, SetDriveScriptBlock)
#endif
        };

        internal static void RemoveAllDrivesForProvider(ProviderInfo pi, SessionStateInternal ssi)
        {
            foreach (PSDriveInfo di in ssi.GetDrivesForProvider(pi.FullName))
            {
                try
                {
                    ssi.RemoveDrive(di, true, null);
                }
                catch (Exception)
                {
                }
            }
        }

        private static readonly PSTraceSource s_PSSnapInTracer = PSTraceSource.GetTracer("PSSnapInLoadUnload", "Loading and unloading mshsnapins", false);

        internal static readonly string CoreSnapin = "Microsoft.PowerShell.Core";
        internal static readonly string CoreModule = "Microsoft.PowerShell.Core";

        // The list of engine modules to create warnings when you try to remove them
        internal static readonly HashSet<string> EngineModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Microsoft.PowerShell.Utility",
                "Microsoft.PowerShell.Management",
                "Microsoft.PowerShell.Diagnostics",
                "Microsoft.PowerShell.Host",
                "Microsoft.PowerShell.Security",
                "Microsoft.WSMan.Management"
            };

        internal static readonly HashSet<string> NestedEngineModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Microsoft.PowerShell.Commands.Utility",
                "Microsoft.PowerShell.Commands.Management",
                "Microsoft.PowerShell.Commands.Diagnostics",
                "Microsoft.PowerShell.ConsoleHost"
            };

        internal static readonly Dictionary<string, string> EngineModuleNestedModuleMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Microsoft.PowerShell.Utility", "Microsoft.PowerShell.Commands.Utility"},
                { "Microsoft.PowerShell.Management", "Microsoft.PowerShell.Commands.Management"},
                { "Microsoft.PowerShell.Diagnostics", "Microsoft.PowerShell.Commands.Diagnostics"},
                { "Microsoft.PowerShell.Host", "Microsoft.PowerShell.ConsoleHost"},
            };

        internal static readonly Dictionary<string, string> NestedModuleEngineModuleMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Microsoft.PowerShell.Commands.Utility", "Microsoft.PowerShell.Utility"},
                { "Microsoft.PowerShell.Commands.Management", "Microsoft.PowerShell.Management"},
                { "Microsoft.PowerShell.Commands.Diagnostics", "Microsoft.PowerShell.Diagnostics"},
                { "Microsoft.PowerShell.ConsoleHost", "Microsoft.PowerShell.Host"},
                { "Microsoft.PowerShell.Security", "Microsoft.PowerShell.Security"},
                { "Microsoft.WSMan.Management", "Microsoft.WSMan.Management"},
            };

        // The list of engine modules that we will not allow users to remove
        internal static readonly HashSet<string> ConstantEngineModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                CoreModule,
            };

        // The list of nested engine modules that we will not allow users to remove
        internal static readonly HashSet<string> ConstantEngineNestedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "System.Management.Automation",
            };

        internal static string GetNestedModuleDllName(string moduleName)
        {
            string result = null;

            if (!EngineModuleNestedModuleMapping.TryGetValue(moduleName, out result))
            {
                result = string.Empty;
            }

            return result;
        }
    }

    /// <summary>
    /// Set of helper methods fro loading assemblies containing cmdlets...
    /// </summary>
    internal static class PSSnapInHelpers
    {
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom")]
        internal static Assembly LoadPSSnapInAssembly(PSSnapInInfo psSnapInInfo)
        {
            Assembly assembly = null;
            s_PSSnapInTracer.WriteLine("Loading assembly from GAC. Assembly Name: {0}", psSnapInInfo.AssemblyName);

            try
            {
                assembly = Assembly.Load(new AssemblyName(psSnapInInfo.AssemblyName));
            }
            catch (BadImageFormatException e)
            {
                s_PSSnapInTracer.TraceWarning("Not able to load assembly {0}: {1}", psSnapInInfo.AssemblyName, e.Message);
            }
            catch (FileNotFoundException e)
            {
                s_PSSnapInTracer.TraceWarning("Not able to load assembly {0}: {1}", psSnapInInfo.AssemblyName, e.Message);
            }
            catch (FileLoadException e)
            {
                s_PSSnapInTracer.TraceWarning("Not able to load assembly {0}: {1}", psSnapInInfo.AssemblyName, e.Message);
            }

            if (assembly != null)
            {
                return assembly;
            }

            s_PSSnapInTracer.WriteLine("Loading assembly from path: {0}", psSnapInInfo.AssemblyName);

            try
            {
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(psSnapInInfo.AbsoluteModulePath);

                if (!string.Equals(assemblyName.FullName, psSnapInInfo.AssemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    string message = StringUtil.Format(ConsoleInfoErrorStrings.PSSnapInAssemblyNameMismatch, psSnapInInfo.AbsoluteModulePath, psSnapInInfo.AssemblyName);
                    s_PSSnapInTracer.TraceError(message);
                    throw new PSSnapInException(psSnapInInfo.Name, message);
                }

                assembly = Assembly.LoadFrom(psSnapInInfo.AbsoluteModulePath);
            }
            catch (FileLoadException e)
            {
                s_PSSnapInTracer.TraceError("Not able to load assembly {0}: {1}", psSnapInInfo.AssemblyName, e.Message);
                throw new PSSnapInException(psSnapInInfo.Name, e.Message);
            }
            catch (BadImageFormatException e)
            {
                s_PSSnapInTracer.TraceError("Not able to load assembly {0}: {1}", psSnapInInfo.AssemblyName, e.Message);
                throw new PSSnapInException(psSnapInInfo.Name, e.Message);
            }
            catch (FileNotFoundException e)
            {
                s_PSSnapInTracer.TraceError("Not able to load assembly {0}: {1}", psSnapInInfo.AssemblyName, e.Message);
                throw new PSSnapInException(psSnapInInfo.Name, e.Message);
            }

            return assembly;
        }

        private static bool TryGetCustomAttribute<T>(Type decoratedType, out T attribute) where T : Attribute
        {
            var attributes = decoratedType.GetCustomAttributes<T>(inherit: false);
            attribute = attributes.FirstOrDefault();
            return attribute != null;
        }

        internal static void AnalyzePSSnapInAssembly(
            Assembly assembly,
            string name,
            PSSnapInInfo psSnapInInfo,
            PSModuleInfo moduleInfo,
            out Dictionary<string, SessionStateCmdletEntry> cmdlets,
            out Dictionary<string, List<SessionStateAliasEntry>> aliases,
            out Dictionary<string, SessionStateProviderEntry> providers,
            out string helpFile)
        {
            helpFile = null;

            ArgumentNullException.ThrowIfNull(assembly);

            cmdlets = null;
            aliases = null;
            providers = null;

            // See if this assembly has already been scanned...

            Dictionary<string, Tuple<SessionStateCmdletEntry, List<SessionStateAliasEntry>>> cachedCmdlets;
            if (s_cmdletCache.Value.TryGetValue(assembly, out cachedCmdlets))
            {
                cmdlets = new Dictionary<string, SessionStateCmdletEntry>(cachedCmdlets.Count, StringComparer.OrdinalIgnoreCase);
                aliases = new Dictionary<string, List<SessionStateAliasEntry>>(cachedCmdlets.Count, StringComparer.OrdinalIgnoreCase);

                foreach (var pair in cachedCmdlets)
                {
                    var key = pair.Key;
                    var entry = pair.Value;
                    if (entry.Item1.PSSnapIn == null && psSnapInInfo != null)
                    {
                        entry.Item1.SetPSSnapIn(psSnapInInfo);
                    }

                    var newEntry = (SessionStateCmdletEntry)entry.Item1.Clone();
                    if (newEntry.PSSnapIn != null && psSnapInInfo == null)
                    {
                        newEntry.SetPSSnapIn(null);
                    }

                    cmdlets[key] = newEntry;

                    if (entry.Item2 != null)
                    {
                        var aliasList = new List<SessionStateAliasEntry>();
                        foreach (var alias in entry.Item2)
                        {
                            if (alias.PSSnapIn == null && psSnapInInfo != null)
                            {
                                alias.SetPSSnapIn(psSnapInInfo);
                            }

                            var newAliasEntry = (SessionStateAliasEntry)alias.Clone();
                            if (newAliasEntry.PSSnapIn != null && psSnapInInfo == null)
                            {
                                newAliasEntry.SetPSSnapIn(null);
                            }

                            aliasList.Add(newAliasEntry);
                        }

                        aliases[key] = aliasList;
                    }
                }
            }

            Dictionary<string, SessionStateProviderEntry> cachedProviders;
            if (s_providerCache.Value.TryGetValue(assembly, out cachedProviders))
            {
                providers = new Dictionary<string, SessionStateProviderEntry>(s_providerCache.Value.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var pair in cachedProviders)
                {
                    var key = pair.Key;
                    var entry = pair.Value;
                    if (entry.PSSnapIn == null && psSnapInInfo != null)
                    {
                        entry.SetPSSnapIn(psSnapInInfo);
                    }

                    var newEntry = (SessionStateProviderEntry)entry.Clone();
                    if (newEntry.PSSnapIn != null && psSnapInInfo == null)
                    {
                        newEntry.SetPSSnapIn(null);
                    }

                    providers[key] = newEntry;
                }
            }

            string assemblyPath = assembly.Location;
            if (cmdlets != null || providers != null)
            {
                if (!s_assembliesWithModuleInitializerCache.Value.ContainsKey(assembly))
                {
                    s_PSSnapInTracer.WriteLine("Returning cached cmdlet and provider entries for {0}", assemblyPath);
                    return;
                }
                else
                {
                    s_PSSnapInTracer.WriteLine("Executing IModuleAssemblyInitializer.Import for {0}", assemblyPath);
                    var assemblyTypes = GetAssemblyTypes(assembly, name);
                    ExecuteModuleInitializer(assembly, assemblyTypes);
                    return;
                }
            }

            s_PSSnapInTracer.WriteLine("Analyzing assembly {0} for cmdlet and providers", assemblyPath);
            helpFile = GetHelpFile(assemblyPath);

            if (psSnapInInfo != null && psSnapInInfo.Name.Equals(InitialSessionState.CoreSnapin, StringComparison.OrdinalIgnoreCase))
            {
                InitializeCoreCmdletsAndProviders(psSnapInInfo, out cmdlets, out providers, helpFile);

#if DEBUG
                // Make sure the pre-built cmdlet and provider tables match what reflection finds.  This will help
                // avoid issues where you add a cmdlet but forget to update the table in InitializeCoreCmdletsAndProviders.

                Dictionary<string, SessionStateCmdletEntry> cmdletsCheck = null;
                Dictionary<string, SessionStateProviderEntry> providersCheck = null;
                Dictionary<string, List<SessionStateAliasEntry>> aliasesCheck = null;
                AnalyzeModuleAssemblyWithReflection(assembly, name, psSnapInInfo, moduleInfo, helpFile, ref cmdletsCheck, ref aliasesCheck, ref providersCheck);

                Diagnostics.Assert(aliasesCheck == null, "InitializeCoreCmdletsAndProviders assumes no aliases are defined in System.Management.Automation.dll");
                Diagnostics.Assert(providersCheck.Count == providers.Count, "new Provider added to System.Management.Automation.dll - update InitializeCoreCmdletsAndProviders");
                foreach (var pair in providersCheck)
                {
                    SessionStateProviderEntry other;
                    if (providers.TryGetValue(pair.Key, out other))
                    {
                        Diagnostics.Assert((object)pair.Value.HelpFileName == (object)other.HelpFileName, "Pre-generated Provider help file incorrect");
                        Diagnostics.Assert(pair.Value.ImplementingType == other.ImplementingType, "Pre-generated Provider implementing type incorrect");
                        Diagnostics.Assert(string.Equals(pair.Value.Name, other.Name, StringComparison.Ordinal), "Pre-generated Provider name incorrect");
                        Diagnostics.Assert(pair.Value.PSSnapIn == other.PSSnapIn, "Pre-generated Provider snapin type incorrect");
                        Diagnostics.Assert(pair.Value.Module == other.Module, "Pre-generated Provider module incorrect");
                        Diagnostics.Assert(pair.Value.Visibility == other.Visibility, "Pre-generated Provider visibility incorrect");
                    }
                    else
                    {
                        Diagnostics.Assert(false, "Missing provider: " + pair.Key);
                    }
                }

                Diagnostics.Assert(cmdletsCheck.Count == cmdlets.Count, "new Cmdlet added to System.Management.Automation.dll - update InitializeCoreCmdletsAndProviders");

                foreach (var pair in cmdletsCheck)
                {
                    SessionStateCmdletEntry other;
                    if (cmdlets.TryGetValue(pair.Key, out other))
                    {
                        Diagnostics.Assert((object)pair.Value.HelpFileName == (object)other.HelpFileName, "Pre-generated Provider help file incorrect");
                        Diagnostics.Assert(pair.Value.ImplementingType == other.ImplementingType, "Pre-generated Provider implementing type incorrect");
                        Diagnostics.Assert(string.Equals(pair.Value.Name, other.Name, StringComparison.Ordinal), "Pre-generated Provider name incorrect");
                        Diagnostics.Assert(pair.Value.PSSnapIn == other.PSSnapIn, "Pre-generated Provider snapin type incorrect");
                        Diagnostics.Assert(pair.Value.Module == other.Module, "Pre-generated Provider module incorrect");
                        Diagnostics.Assert(pair.Value.Visibility == other.Visibility, "Pre-generated Provider visibility incorrect");
                    }
                    else
                    {
                        Diagnostics.Assert(false, "Pre-generated Cmdlet missing: " + pair.Key);
                    }
                }
#endif
            }
            else
            {
                AnalyzeModuleAssemblyWithReflection(assembly, name, psSnapInInfo, moduleInfo, helpFile, ref cmdlets, ref aliases, ref providers);
            }

            // Cache the cmdlet and provider info for this assembly...
            // We need to cache a clone of this data *before* the
            // module info is set on it since module info can't be shared
            // across runspaces. When these entries are hit in the cache,
            // copies will be returned to ensure that the cache is never tied to a runspace.

            if (cmdlets != null)
            {
                var clone = new Dictionary<string, Tuple<SessionStateCmdletEntry, List<SessionStateAliasEntry>>>(cmdlets.Count, StringComparer.OrdinalIgnoreCase);
                List<SessionStateAliasEntry> aliasesCloneList = null;

                foreach (var entry in cmdlets)
                {
                    List<SessionStateAliasEntry> aliasEntries;
                    if (aliases != null && aliases.TryGetValue(entry.Key, out aliasEntries))
                    {
                        aliasesCloneList = new List<SessionStateAliasEntry>(aliases.Count);
                        foreach (var aliasEntry in aliasEntries)
                        {
                            aliasesCloneList.Add((SessionStateAliasEntry)aliasEntry.Clone());
                        }
                    }

                    clone[entry.Key] = new Tuple<SessionStateCmdletEntry, List<SessionStateAliasEntry>>((SessionStateCmdletEntry)entry.Value.Clone(), aliasesCloneList);
                }

                s_cmdletCache.Value[assembly] = clone;
            }

            if (providers != null)
            {
                var clone = new Dictionary<string, SessionStateProviderEntry>(providers.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var entry in providers)
                {
                    clone[entry.Key] = (SessionStateProviderEntry)entry.Value.Clone();
                }

                s_providerCache.Value[assembly] = clone;
            }
        }

        private static void AnalyzeModuleAssemblyWithReflection(
            Assembly assembly,
            string name,
            PSSnapInInfo psSnapInInfo,
            PSModuleInfo moduleInfo,
            string helpFile,
            ref Dictionary<string, SessionStateCmdletEntry> cmdlets,
            ref Dictionary<string, List<SessionStateAliasEntry>> aliases,
            ref Dictionary<string, SessionStateProviderEntry> providers)
        {
            var assemblyTypes = GetAssemblyTypes(assembly, name);
            ExecuteModuleInitializer(assembly, assemblyTypes);

            foreach (Type type in assemblyTypes)
            {
                if (!HasDefaultConstructor(type))
                {
                    continue;
                }

                // Check for cmdlets
                if (IsCmdletClass(type) && TryGetCustomAttribute(type, out CmdletAttribute cmdletAttribute))
                {
                    if (TryGetCustomAttribute(type, out ExperimentalAttribute expAttribute) && expAttribute.ToHide)
                    {
                        // If 'ExperimentalAttribute' is specified on the cmdlet type and the
                        // effective action at run time is 'Hide', then we ignore the type.
                        continue;
                    }

                    string cmdletName = cmdletAttribute.VerbName + "-" + cmdletAttribute.NounName;
                    if (cmdlets != null && cmdlets.ContainsKey(cmdletName))
                    {
                        string message = StringUtil.Format(ConsoleInfoErrorStrings.PSSnapInDuplicateCmdlets, cmdletName, name);
                        s_PSSnapInTracer.TraceError(message);
                        throw new PSSnapInException(name, message);
                    }

                    SessionStateCmdletEntry cmdlet = new SessionStateCmdletEntry(cmdletName, type, helpFile);
                    if (psSnapInInfo != null)
                    {
                        cmdlet.SetPSSnapIn(psSnapInInfo);
                    }

                    if (moduleInfo != null)
                    {
                        cmdlet.SetModule(moduleInfo);
                    }

                    cmdlets ??= new Dictionary<string, SessionStateCmdletEntry>(StringComparer.OrdinalIgnoreCase);
                    cmdlets.Add(cmdletName, cmdlet);

                    if (TryGetCustomAttribute(type, out AliasAttribute aliasAttribute))
                    {
                        aliases ??= new Dictionary<string, List<SessionStateAliasEntry>>(StringComparer.OrdinalIgnoreCase);

                        var aliasList = new List<SessionStateAliasEntry>();
                        foreach (var alias in aliasAttribute.AliasNames)
                        {
                            // Alias declared by 'AliasAttribute' is set with the option 'ScopedItemOptions.None', because we believe
                            // the users of the cmdlet, instead of the author, should have control of what options applied to an alias
                            // ('ScopedItemOptions.ReadOnly' and/or 'ScopedItemOptions.AllScopes').
                            var aliasEntry = new SessionStateAliasEntry(alias, cmdletName, description: string.Empty, ScopedItemOptions.None);
                            if (psSnapInInfo != null) { aliasEntry.SetPSSnapIn(psSnapInInfo); }

                            if (moduleInfo != null)
                            {
                                aliasEntry.SetModule(moduleInfo);
                            }

                            aliasList.Add(aliasEntry);
                        }

                        aliases.Add(cmdletName, aliasList);
                    }

                    s_PSSnapInTracer.WriteLine("{0} from type {1} is added as a cmdlet. ", cmdletName, type.FullName);
                }
                // Check for providers
                else if (IsProviderClass(type) && TryGetCustomAttribute(type, out CmdletProviderAttribute providerAttribute))
                {
                    if (TryGetCustomAttribute(type, out ExperimentalAttribute expAttribute) && expAttribute.ToHide)
                    {
                        // If 'ExperimentalAttribute' is specified on the provider type and
                        // the effective action at run time is 'Hide', then we ignore the type.
                        continue;
                    }

                    string providerName = providerAttribute.ProviderName;
                    if (providers != null && providers.ContainsKey(providerName))
                    {
                        string message = StringUtil.Format(ConsoleInfoErrorStrings.PSSnapInDuplicateProviders, providerName, psSnapInInfo.Name);
                        s_PSSnapInTracer.TraceError(message);
                        throw new PSSnapInException(psSnapInInfo.Name, message);
                    }

                    SessionStateProviderEntry provider = new SessionStateProviderEntry(providerName, type, helpFile);
                    if (psSnapInInfo != null)
                    {
                        provider.SetPSSnapIn(psSnapInInfo);
                    }

                    if (moduleInfo != null)
                    {
                        provider.SetModule(moduleInfo);
                    }

                    providers ??= new Dictionary<string, SessionStateProviderEntry>(StringComparer.OrdinalIgnoreCase);
                    providers.Add(providerName, provider);

                    s_PSSnapInTracer.WriteLine("{0} from type {1} is added as a provider. ", providerName, type.FullName);
                }
            }
        }

        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1001:CommasMustBeSpacedCorrectly", Justification = "Reviewed.")]
        private static void InitializeCoreCmdletsAndProviders(
            PSSnapInInfo psSnapInInfo,
            out Dictionary<string, SessionStateCmdletEntry> cmdlets,
            out Dictionary<string, SessionStateProviderEntry> providers,
            string helpFile)
        {
            cmdlets = new Dictionary<string, SessionStateCmdletEntry>(StringComparer.OrdinalIgnoreCase)
            {
                { "Add-History",                       new SessionStateCmdletEntry("Add-History", typeof(AddHistoryCommand), helpFile) },
                { "Clear-History",                     new SessionStateCmdletEntry("Clear-History", typeof(ClearHistoryCommand), helpFile) },
                { "Debug-Job",                         new SessionStateCmdletEntry("Debug-Job", typeof(DebugJobCommand), helpFile) },
#if !UNIX
                { "Disable-PSRemoting",                new SessionStateCmdletEntry("Disable-PSRemoting", typeof(DisablePSRemotingCommand), helpFile) },
                { "Enable-PSRemoting",                 new SessionStateCmdletEntry("Enable-PSRemoting", typeof(EnablePSRemotingCommand), helpFile) },
                { "Disable-PSSessionConfiguration",    new SessionStateCmdletEntry("Disable-PSSessionConfiguration", typeof(DisablePSSessionConfigurationCommand), helpFile) },
                { "Enable-PSSessionConfiguration",     new SessionStateCmdletEntry("Enable-PSSessionConfiguration", typeof(EnablePSSessionConfigurationCommand), helpFile) },
                { "Get-PSSessionCapability",           new SessionStateCmdletEntry("Get-PSSessionCapability", typeof(GetPSSessionCapabilityCommand), helpFile) },
                { "Get-PSSessionConfiguration",        new SessionStateCmdletEntry("Get-PSSessionConfiguration", typeof(GetPSSessionConfigurationCommand), helpFile) },
                { "Receive-PSSession",                 new SessionStateCmdletEntry("Receive-PSSession", typeof(ReceivePSSessionCommand), helpFile) },
                { "Register-PSSessionConfiguration",   new SessionStateCmdletEntry("Register-PSSessionConfiguration", typeof(RegisterPSSessionConfigurationCommand), helpFile) },
                { "Unregister-PSSessionConfiguration", new SessionStateCmdletEntry("Unregister-PSSessionConfiguration", typeof(UnregisterPSSessionConfigurationCommand), helpFile) },
                { "Set-PSSessionConfiguration",        new SessionStateCmdletEntry("Set-PSSessionConfiguration", typeof(SetPSSessionConfigurationCommand), helpFile) },
                { "Test-PSSessionConfigurationFile",   new SessionStateCmdletEntry("Test-PSSessionConfigurationFile", typeof(TestPSSessionConfigurationFileCommand), helpFile) },
                { "Connect-PSSession",                 new SessionStateCmdletEntry("Connect-PSSession", typeof(ConnectPSSessionCommand), helpFile) },
                { "Disconnect-PSSession",              new SessionStateCmdletEntry("Disconnect-PSSession", typeof(DisconnectPSSessionCommand), helpFile) },
#endif
                { "Disable-ExperimentalFeature",       new SessionStateCmdletEntry("Disable-ExperimentalFeature", typeof(DisableExperimentalFeatureCommand), helpFile) },
                { "Enable-ExperimentalFeature",        new SessionStateCmdletEntry("Enable-ExperimentalFeature", typeof(EnableExperimentalFeatureCommand), helpFile) },
                { "Enter-PSHostProcess",               new SessionStateCmdletEntry("Enter-PSHostProcess", typeof(EnterPSHostProcessCommand), helpFile) },
                { "Enter-PSSession",                   new SessionStateCmdletEntry("Enter-PSSession", typeof(EnterPSSessionCommand), helpFile) },
                { "Exit-PSHostProcess",                new SessionStateCmdletEntry("Exit-PSHostProcess", typeof(ExitPSHostProcessCommand), helpFile) },
                { "Exit-PSSession",                    new SessionStateCmdletEntry("Exit-PSSession", typeof(ExitPSSessionCommand), helpFile) },
                { "Export-ModuleMember",               new SessionStateCmdletEntry("Export-ModuleMember", typeof(ExportModuleMemberCommand), helpFile) },
                { "ForEach-Object",                    new SessionStateCmdletEntry("ForEach-Object", typeof(ForEachObjectCommand), helpFile) },
                { "Get-Command",                       new SessionStateCmdletEntry("Get-Command", typeof(GetCommandCommand), helpFile) },
                { "Get-ExperimentalFeature",           new SessionStateCmdletEntry("Get-ExperimentalFeature", typeof(GetExperimentalFeatureCommand), helpFile) },
                { "Get-Help",                          new SessionStateCmdletEntry("Get-Help", typeof(GetHelpCommand), helpFile) },
                { "Get-History",                       new SessionStateCmdletEntry("Get-History", typeof(GetHistoryCommand), helpFile) },
                { "Get-Job",                           new SessionStateCmdletEntry("Get-Job", typeof(GetJobCommand), helpFile) },
                { "Get-Module",                        new SessionStateCmdletEntry("Get-Module", typeof(GetModuleCommand), helpFile) },
                { "Get-PSHostProcessInfo",             new SessionStateCmdletEntry("Get-PSHostProcessInfo", typeof(GetPSHostProcessInfoCommand), helpFile) },
                { "Get-PSSession",                     new SessionStateCmdletEntry("Get-PSSession", typeof(GetPSSessionCommand), helpFile) },
                { "Import-Module",                     new SessionStateCmdletEntry("Import-Module", typeof(ImportModuleCommand), helpFile) },
                { "Invoke-Command",                    new SessionStateCmdletEntry("Invoke-Command", typeof(InvokeCommandCommand), helpFile) },
                { "Invoke-History",                    new SessionStateCmdletEntry("Invoke-History", typeof(InvokeHistoryCommand), helpFile) },
                { "New-Module",                        new SessionStateCmdletEntry("New-Module", typeof(NewModuleCommand), helpFile) },
                { "New-ModuleManifest",                new SessionStateCmdletEntry("New-ModuleManifest", typeof(NewModuleManifestCommand), helpFile) },
                { "New-PSRoleCapabilityFile",          new SessionStateCmdletEntry("New-PSRoleCapabilityFile", typeof(NewPSRoleCapabilityFileCommand), helpFile) },
                { "New-PSSession",                     new SessionStateCmdletEntry("New-PSSession", typeof(NewPSSessionCommand), helpFile) },
                { "New-PSSessionConfigurationFile",    new SessionStateCmdletEntry("New-PSSessionConfigurationFile", typeof(NewPSSessionConfigurationFileCommand), helpFile) },
                { "New-PSSessionOption",               new SessionStateCmdletEntry("New-PSSessionOption", typeof(NewPSSessionOptionCommand), helpFile) },
                { "New-PSTransportOption",             new SessionStateCmdletEntry("New-PSTransportOption", typeof(NewPSTransportOptionCommand), helpFile) },
                { "Out-Default",                       new SessionStateCmdletEntry("Out-Default", typeof(OutDefaultCommand), helpFile) },
                { "Out-Host",                          new SessionStateCmdletEntry("Out-Host", typeof(OutHostCommand), helpFile) },
                { "Out-Null",                          new SessionStateCmdletEntry("Out-Null", typeof(OutNullCommand), helpFile) },
                { "Receive-Job",                       new SessionStateCmdletEntry("Receive-Job", typeof(ReceiveJobCommand), helpFile) },
                { "Register-ArgumentCompleter",        new SessionStateCmdletEntry("Register-ArgumentCompleter", typeof(RegisterArgumentCompleterCommand), helpFile) },
                { "Remove-Job",                        new SessionStateCmdletEntry("Remove-Job", typeof(RemoveJobCommand), helpFile) },
                { "Remove-Module",                     new SessionStateCmdletEntry("Remove-Module", typeof(RemoveModuleCommand), helpFile) },
                { "Remove-PSSession",                  new SessionStateCmdletEntry("Remove-PSSession", typeof(RemovePSSessionCommand), helpFile) },
                { "Save-Help",                         new SessionStateCmdletEntry("Save-Help", typeof(SaveHelpCommand), helpFile) },
                { "Set-PSDebug",                       new SessionStateCmdletEntry("Set-PSDebug", typeof(SetPSDebugCommand), helpFile) },
                { "Set-StrictMode",                    new SessionStateCmdletEntry("Set-StrictMode", typeof(SetStrictModeCommand), helpFile) },
                { "Start-Job",                         new SessionStateCmdletEntry("Start-Job", typeof(StartJobCommand), helpFile) },
                { "Stop-Job",                          new SessionStateCmdletEntry("Stop-Job", typeof(StopJobCommand), helpFile) },
                { "Test-ModuleManifest",               new SessionStateCmdletEntry("Test-ModuleManifest", typeof(TestModuleManifestCommand), helpFile) },
                { "Update-Help",                       new SessionStateCmdletEntry("Update-Help", typeof(UpdateHelpCommand), helpFile) },
                { "Wait-Job",                          new SessionStateCmdletEntry("Wait-Job", typeof(WaitJobCommand), helpFile) },
                { "Where-Object",                      new SessionStateCmdletEntry("Where-Object", typeof(WhereObjectCommand), helpFile) },
#if !CORECLR
                { "Add-PSSnapin",                      new SessionStateCmdletEntry("Add-PSSnapin", typeof(AddPSSnapinCommand), helpFile) },
                { "Export-Console",                    new SessionStateCmdletEntry("Export-Console", typeof(ExportConsoleCommand), helpFile) },
                { "Get-PSSnapin",                      new SessionStateCmdletEntry("Get-PSSnapin", typeof(GetPSSnapinCommand), helpFile) },
                { "Remove-PSSnapin",                   new SessionStateCmdletEntry("Remove-PSSnapin", typeof(RemovePSSnapinCommand), helpFile) },
                { "Resume-Job",                        new SessionStateCmdletEntry("Resume-Job", typeof(ResumeJobCommand), helpFile) },
                { "Suspend-Job",                       new SessionStateCmdletEntry("Suspend-Job", typeof(SuspendJobCommand), helpFile) },
#endif
                // Not exported, but are added via reflection so added here as well, though maybe they shouldn't be
                { "Out-LineOutput",                    new SessionStateCmdletEntry("Out-LineOutput", typeof(OutLineOutputCommand), helpFile) },
                { "Format-Default",                    new SessionStateCmdletEntry("Format-Default", typeof(FormatDefaultCommand), helpFile) },
            };

            if (ExperimentalFeature.IsEnabled("PSSubsystemPluginModel"))
            {
                cmdlets.Add("Get-PSSubsystem", new SessionStateCmdletEntry("Get-PSSubsystem", typeof(Subsystem.GetPSSubsystemCommand), helpFile));
            }

#if UNIX
            cmdlets.Add("Switch-Process", new SessionStateCmdletEntry("Switch-Process", typeof(SwitchProcessCommand), helpFile));
#endif

            foreach (var val in cmdlets.Values)
            {
                val.SetPSSnapIn(psSnapInInfo);
            }

            providers = new Dictionary<string, SessionStateProviderEntry>(StringComparer.OrdinalIgnoreCase)
            {
#if !UNIX
                { "Registry",    new SessionStateProviderEntry("Registry", typeof(RegistryProvider), helpFile) },
#endif
                { "Alias",       new SessionStateProviderEntry("Alias", typeof(AliasProvider), helpFile) },
                { "Environment", new SessionStateProviderEntry("Environment", typeof(EnvironmentProvider), helpFile) },
                { "FileSystem" , new SessionStateProviderEntry("FileSystem", typeof(FileSystemProvider), helpFile) },
                { "Function",    new SessionStateProviderEntry("Function", typeof(FunctionProvider), helpFile) },
                { "Variable",    new SessionStateProviderEntry("Variable", typeof(VariableProvider), helpFile) },
            };

            foreach (var val in providers.Values)
            {
                val.SetPSSnapIn(psSnapInInfo);
            }
        }

        private static void ExecuteModuleInitializer(Assembly assembly, IEnumerable<Type> assemblyTypes)
        {
            foreach (Type type in assemblyTypes)
            {
                if (typeof(IModuleAssemblyInitializer).IsAssignableFrom(type))
                {
                    s_assembliesWithModuleInitializerCache.Value[assembly] = true;
                    var moduleInitializer = (IModuleAssemblyInitializer)Activator.CreateInstance(type, true);
                    moduleInitializer.OnImport();
                }
            }
        }

        internal static IEnumerable<Type> GetAssemblyTypes(Assembly assembly, string name)
        {
            try
            {
                // Return types that are public, non-abstract, non-interface and non-valueType.
                return assembly.ExportedTypes.Where(static t => !t.IsAbstract && !t.IsInterface && !t.IsValueType);
            }
            catch (ReflectionTypeLoadException e)
            {
                string message = e.Message + "\nLoader Exceptions: \n";
                if (e.LoaderExceptions != null)
                {
                    foreach (Exception exception in e.LoaderExceptions)
                    {
                        message += "\n" + exception.Message;
                    }
                }

                s_PSSnapInTracer.TraceError(message);
                throw new PSSnapInException(name, message);
            }
        }

        // cmdletCache holds the list of cmdlets along with its aliases per each assembly.
        private static readonly Lazy<ConcurrentDictionary<Assembly, Dictionary<string, Tuple<SessionStateCmdletEntry, List<SessionStateAliasEntry>>>>> s_cmdletCache =
            new Lazy<ConcurrentDictionary<Assembly, Dictionary<string, Tuple<SessionStateCmdletEntry, List<SessionStateAliasEntry>>>>>();

        private static readonly Lazy<ConcurrentDictionary<Assembly, Dictionary<string, SessionStateProviderEntry>>> s_providerCache =
            new Lazy<ConcurrentDictionary<Assembly, Dictionary<string, SessionStateProviderEntry>>>();

        // Using a ConcurrentDictionary for this so that we can avoid having a private lock variable. We use only the keys for checking.
        private static readonly Lazy<ConcurrentDictionary<Assembly, bool>> s_assembliesWithModuleInitializerCache = new Lazy<ConcurrentDictionary<Assembly, bool>>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsCmdletClass(Type type)
        {
            return type.IsSubclassOf(typeof(System.Management.Automation.Cmdlet));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsProviderClass(Type type)
        {
            return type.IsSubclassOf(typeof(System.Management.Automation.Provider.CmdletProvider));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasDefaultConstructor(Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) is not null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetHelpFile(string assemblyPath)
        {
            // Help files exist only for original module assemblies, not for generated Ngen binaries
            return Path.GetFileName(assemblyPath).Replace(".ni.dll", ".dll") + StringLiterals.HelpFileExtension;
        }

        private static readonly PSTraceSource s_PSSnapInTracer = PSTraceSource.GetTracer("PSSnapInLoadUnload", "Loading and unloading mshsnapins", false);
    }

    // Guid is {15d4c170-2f29-5689-a0e2-d95b0c7b4ea0}

    [EventSource(Name = "Microsoft-PowerShell-Runspaces")]
    internal class RunspaceEventSource : EventSource
    {
        internal static readonly RunspaceEventSource Log = new RunspaceEventSource();

        public void OpenRunspaceStart() { WriteEvent(1); }

        public void OpenRunspaceStop() { WriteEvent(2); }

        public void LoadAssembliesStart() { WriteEvent(3); }

        public void LoadAssembliesStop() { WriteEvent(4); }

        public void UpdateFormatTableStart() { WriteEvent(5); }

        public void UpdateFormatTableStop() { WriteEvent(6); }

        public void UpdateTypeTableStart() { WriteEvent(7); }

        public void UpdateTypeTableStop() { WriteEvent(8); }

        public void LoadProvidersStart() { WriteEvent(9); }

        public void LoadProvidersStop() { WriteEvent(10); }

        public void LoadCommandsStart() { WriteEvent(11); }

        public void LoadCommandsStop() { WriteEvent(12); }

        public void LoadVariablesStart() { WriteEvent(13); }

        public void LoadVariablesStop() { WriteEvent(14); }

        public void LoadEnvironmentVariablesStart() { WriteEvent(15); }

        public void LoadEnvironmentVariablesStop() { WriteEvent(16); }

        public void LoadAssemblyStart(string Name, string FileName) { WriteEvent(17, Name, FileName); }

        public void LoadAssemblyStop(string Name, string FileName) { WriteEvent(18, Name, FileName); }

        public void ProcessFormatFileStart(string FileName) { WriteEvent(19, FileName); }

        public void ProcessFormatFileStop(string FileName) { WriteEvent(20, FileName); }

        public void ProcessTypeFileStart(string FileName) { WriteEvent(21, FileName); }

        public void ProcessTypeFileStop(string FileName) { WriteEvent(22, FileName); }

        public void LoadProviderStart(string Name) { WriteEvent(23, Name); }

        public void LoadProviderStop(string Name) { WriteEvent(24, Name); }

        public void LoadCommandStart(string Name) { WriteEvent(25, Name); }

        public void LoadCommandStop(string Name) { WriteEvent(26, Name); }
    }
}
