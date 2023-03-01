// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// The command information for cmdlets that are directly executable by PowerShell.
    /// </summary>
    public class CmdletInfo : CommandInfo
    {
        #region ctor

        /// <summary>
        /// Constructs a CmdletInfo object from the raw cmdlet data.  This should only
        /// be used for Intrinsic commands.
        /// </summary>
        /// <param name="name">
        /// The name of the cmdlet.
        /// </param>
        /// <param name="implementingType">
        /// The type information about the class that implements the cmdlet.
        /// </param>
        /// <param name="helpFile">
        /// The name of the help file associated with the cmdlet
        /// </param>
        /// <param name="PSSnapin">
        /// The PSSnapInInfo of the PSSnapin the cmdlet comes from.
        /// </param>
        /// <param name="context">
        /// The current engine context.
        /// </param>
        internal CmdletInfo(
            string name,
            Type implementingType,
            string helpFile,
            PSSnapInInfo PSSnapin,
            ExecutionContext context)
            : base(name, CommandTypes.Cmdlet, context)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            // Get the verb and noun from the name
            if (!SplitCmdletName(name, out _verb, out _noun))
            {
                throw
                    PSTraceSource.NewArgumentException(
                        nameof(name),
                        DiscoveryExceptions.InvalidCmdletNameFormat,
                        name);
            }

            _implementingType = implementingType;
            _helpFilePath = helpFile;
            _PSSnapin = PSSnapin;
            _options = ScopedItemOptions.ReadOnly;

            // CmdletInfo represents cmdlets exposed from assemblies.  On a locked down system, only trusted
            // assemblies will be loaded.  Therefore, a CmdletInfo instance will always be trusted.
            this.DefiningLanguageMode = PSLanguageMode.FullLanguage;
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal CmdletInfo(CmdletInfo other)
            : base(other)
        {
            _verb = other._verb;
            _noun = other._noun;
            _implementingType = other._implementingType;
            _helpFilePath = other._helpFilePath;
            _PSSnapin = other._PSSnapin;
            _options = ScopedItemOptions.ReadOnly;
        }

        /// <summary>
        /// Create a copy of commandInfo for GetCommandCommand so that we can generate parameter
        /// sets based on an argument list (so we can get the dynamic parameters.)
        /// </summary>
        internal override CommandInfo CreateGetCommandCopy(object[] arguments)
        {
            CmdletInfo copy = new CmdletInfo(this);
            copy.IsGetCommandCopy = true;
            copy.Arguments = arguments;
            return copy;
        }

        /// <summary>
        /// Directly create a cmdlet object from a type. This allows
        /// unregistered commands to be called. It also allows the overhead of
        /// command discovery to be bypassed.
        /// </summary>
        /// <param name="name">The name to use for the cmdlet, must be in the form Noun-Verb.</param>
        /// <param name="implementingType">The .NET class implementing this cmdlet.</param>
        public CmdletInfo(string name, Type implementingType)
            : base(name, CommandTypes.Cmdlet, null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (implementingType == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (!typeof(Cmdlet).IsAssignableFrom(implementingType))
            {
                throw PSTraceSource.NewInvalidOperationException(DiscoveryExceptions.CmdletDoesNotDeriveFromCmdletType, "implementingType", implementingType.FullName);
            }

            // Get the verb and noun from the name
            if (!SplitCmdletName(name, out _verb, out _noun))
            {
                throw
                    PSTraceSource.NewArgumentException(
                        nameof(name),
                        DiscoveryExceptions.InvalidCmdletNameFormat,
                        name);
            }

            _implementingType = implementingType;
            _helpFilePath = string.Empty;
            _PSSnapin = null;
            _options = ScopedItemOptions.ReadOnly;
        }

        #endregion ctor

        #region public members

        /// <summary>
        /// Gets the verb of the cmdlet.
        /// </summary>
        public string Verb
        {
            get
            {
                return _verb;
            }
        }

        private readonly string _verb = string.Empty;

        /// <summary>
        /// Gets the noun of the cmdlet.
        /// </summary>
        public string Noun
        {
            get
            {
                return _noun;
            }
        }

        private readonly string _noun = string.Empty;

        internal static bool SplitCmdletName(string name, out string verb, out string noun)
        {
            noun = verb = string.Empty;
            if (string.IsNullOrEmpty(name))
                return false;

            int index = 0;
            for (int i = 0; i < name.Length; i++)
            {
                if (CharExtensions.IsDash(name[i]))
                {
                    index = i;
                    break;
                }
            }

            if (index > 0)
            {
                verb = name.Substring(0, index);
                noun = name.Substring(index + 1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the help file path for the cmdlet.
        /// </summary>
        public string HelpFile
        {
            get
            {
                return _helpFilePath;
            }

            internal set
            {
                _helpFilePath = value;
            }
        }

        private string _helpFilePath = string.Empty;

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.Cmdlet; }
        }

        /// <summary>
        /// Gets the Snap-in in which the cmdlet is implemented.
        /// </summary>
        public PSSnapInInfo PSSnapIn
        {
            get
            {
                return _PSSnapin;
            }
        }

        private readonly PSSnapInInfo _PSSnapin;

        /// <summary>
        /// Gets the name of the PSSnapin the cmdlet is implemented in.
        /// </summary>
        internal string PSSnapInName
        {
            get
            {
                string result = null;
                if (_PSSnapin != null)
                {
                    result = _PSSnapin.Name;
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the source version.
        /// </summary>
        public override Version Version
        {
            get
            {
                if (_version == null)
                {
                    if (Module != null)
                    {
                        _version = base.Version;
                    }
                    else if (_PSSnapin != null)
                    {
                        _version = _PSSnapin.Version;
                    }
                }

                return _version;
            }
        }

        private Version _version;

        /// <summary>
        /// Gets the type that implements the cmdlet.
        /// </summary>
        public Type ImplementingType
        {
            get
            {
                return _implementingType;
            }
        }

        private readonly Type _implementingType = null;

        /// <summary>
        /// Gets the synopsis of the cmdlet.
        /// </summary>
        public override string Definition
        {
            get
            {
                StringBuilder synopsis = new StringBuilder();

                if (this.ImplementingType != null)
                {
                    foreach (CommandParameterSetInfo parameterSet in ParameterSets)
                    {
                        synopsis.AppendLine();
                        synopsis.AppendLine(
                            string.Format(
                                System.Globalization.CultureInfo.CurrentCulture,
                                "{0}{1}{2} {3}",
                                _verb,
                                StringLiterals.CommandVerbNounSeparator,
                                _noun,
                                parameterSet.ToString()));
                    }
                }
                else
                {
                    // Skip the synopsis documentation if the cmdlet hasn't been loaded yet.
                    synopsis.AppendLine(
                        string.Format(
                            System.Globalization.CultureInfo.CurrentCulture,
                            "{0}{1}{2}",
                            _verb,
                            StringLiterals.CommandVerbNounSeparator,
                            _noun));
                }

                return synopsis.ToString();
            }
        }

        /// <summary>
        /// Gets the name of the default parameter set.
        /// </summary>
        public string DefaultParameterSet
        {
            get
            {
                return this.CommandMetadata.DefaultParameterSetName;
            }
        }

        /// <summary>
        /// Return the output types specified on the cmdlet.
        /// </summary>
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get
            {
                if (_outputType == null)
                {
                    _outputType = new List<PSTypeName>();

                    if (ImplementingType != null)
                    {
                        foreach (object o in ImplementingType.GetCustomAttributes(typeof(OutputTypeAttribute), false))
                        {
                            OutputTypeAttribute attr = (OutputTypeAttribute)o;
                            _outputType.AddRange(attr.Type);
                        }
                    }
                }

                List<PSTypeName> providerTypes = new List<PSTypeName>();

                if (Context != null)
                {
                    ProviderInfo provider = null;
                    if (Arguments != null)
                    {
                        // See if we have a path argument - we only consider named arguments -Path and -LiteralPath,
                        // and only if they are fully specified (no prefixes allowed, so we don't need to deal with
                        // ambiguities that the parameter binder would resolve for us.

                        for (int i = 0; i < Arguments.Length - 1; i++)
                        {
                            var arg = Arguments[i] as string;
                            if (arg != null &&
                                (arg.Equals("-Path", StringComparison.OrdinalIgnoreCase) ||
                                (arg.Equals("-LiteralPath", StringComparison.OrdinalIgnoreCase))))
                            {
                                var path = Arguments[i + 1] as string;
                                if (path != null)
                                {
                                    Context.SessionState.Path.GetResolvedProviderPathFromPSPath(path, true, out provider);
                                }
                            }
                        }
                    }

                    // If no path argument, just use the current path to choose the provider.
                    provider ??= Context.SessionState.Path.CurrentLocation.Provider;

                    provider.GetOutputTypes(Name, providerTypes);
                    if (providerTypes.Count > 0)
                    {
                        providerTypes.InsertRange(0, _outputType);
                        return new ReadOnlyCollection<PSTypeName>(providerTypes);
                    }
                }

                return new ReadOnlyCollection<PSTypeName>(_outputType);
            }
        }

        private List<PSTypeName> _outputType = null;

        /// <summary>
        /// Gets or sets the scope options for the alias.
        /// </summary>
        /// <exception cref="System.Management.Automation.SessionStateUnauthorizedAccessException">
        /// If the trying to set an cmdlet that is constant or
        ///     if the value trying to be set is ScopedItemOptions.Constant
        /// </exception>
        public ScopedItemOptions Options
        {
            get
            {
                return _options;
            }

            set
            {
                SetOptions(value, false);
            }
        }

        private ScopedItemOptions _options = ScopedItemOptions.None;

        /// <summary>
        /// Sets the options for the cmdlet and allows changes ReadOnly options only if force is specified.
        /// </summary>
        /// <param name="newOptions">
        /// The new options value.
        /// </param>
        /// <param name="force">
        /// If true the change to the options will happen even if the existing options are read-only.
        /// </param>
        internal void SetOptions(ScopedItemOptions newOptions, bool force)
        {
            // Check to see if the cmdlet is readonly, if so
            // throw an exception because the options cannot be changed.

            if ((_options & ScopedItemOptions.ReadOnly) != 0)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Cmdlet,
                            "CmdletIsReadOnly",
                            SessionStateStrings.CmdletIsReadOnly);

                throw e;
            }

            _options = newOptions;
        }

        #endregion public members

        #region internal/private members

        /// <summary>
        /// Gets the full name of the cmdlet including the PSSnapin name.
        /// </summary>
        private static string GetFullName(string moduleName, string cmdletName)
        {
            System.Diagnostics.Debug.Assert(cmdletName != null, "cmdletName != null");
            string result = cmdletName;
            if (!string.IsNullOrEmpty(moduleName))
            {
                result = moduleName + '\\' + result;
            }

            return result;
        }

        /// <summary>
        /// Gets the full name of the cmdlet including the PSSnapin name.
        /// </summary>
        private static string GetFullName(CmdletInfo cmdletInfo)
        {
            return GetFullName(cmdletInfo.ModuleName, cmdletInfo.Name);
        }

        /// <summary>
        /// Gets the full name of the cmdlet including the PSSnapin name.
        /// </summary>
        internal static string GetFullName(PSObject psObject)
        {
            // If this is a high-fidelity object then extract full-name normally.
            if (psObject.BaseObject is CmdletInfo)
            {
                CmdletInfo cmdletInfo = (CmdletInfo)psObject.BaseObject;
                return GetFullName(cmdletInfo);
            }

            // Otherwise, it is a PSCustomObject shredded in a remote call: extract name as a property.
            else
            {
                // Handle the case in one or both of the properties might not be defined.
                PSPropertyInfo nameProperty = psObject.Properties["Name"];
                PSPropertyInfo psSnapInProperty = psObject.Properties["PSSnapIn"];
                string nameString = nameProperty == null ? string.Empty : (string)nameProperty.Value;
                string psSnapInString = psSnapInProperty == null ? string.Empty : (string)psSnapInProperty.Value;
                return GetFullName(psSnapInString, nameString);
            }
        }

        /// <summary>
        /// Gets the full name of the cmdlet including the PSSnapin name.
        /// </summary>
        internal string FullName
        {
            get
            {
                return GetFullName(this);
            }
        }

        /// <summary>
        /// Gets the CommandMetadata for this cmdlet.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The type name is invalid or the length of the type name
        /// exceeds 1024 characters.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// The caller does not have the required permission to load the assembly
        /// or create the type.
        /// </exception>
        /// <exception cref="ParsingMetadataException">
        /// If more than int.MaxValue parameter-sets are defined for the command.
        /// </exception>
        /// <exception cref="MetadataException">
        /// If a parameter defines the same parameter-set name multiple times.
        /// If the attributes could not be read from a property or field.
        /// </exception>
        internal override CommandMetadata CommandMetadata
        {
            get
            {
                return _cmdletMetadata ??= CommandMetadata.Get(this.Name, this.ImplementingType, Context);
            }
        }

        private CommandMetadata _cmdletMetadata;

        internal override bool ImplementsDynamicParameters
        {
            get
            {
                if (ImplementingType != null)
                {
                    return (ImplementingType.GetInterface(nameof(IDynamicParameters), true) != null);
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion internal/private members
    }
}
