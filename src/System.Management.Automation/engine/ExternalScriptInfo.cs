// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Security;
using System.Text;

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation
{
    /// <summary>
    /// Provides information for scripts that are directly executable by PowerShell
    /// but are not built into the runspace configuration.
    /// </summary>
    public class ExternalScriptInfo : CommandInfo, IScriptCommandInfo
    {
        #region ctor

        /// <summary>
        /// Creates an instance of the ExternalScriptInfo class with the specified name, and path.
        /// </summary>
        /// <param name="name">
        /// The name of the script.
        /// </param>
        /// <param name="path">
        /// The path to the script
        /// </param>
        /// <param name="context">
        /// The context of the currently running engine.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="context"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="path"/> is null or empty.
        /// </exception>
        internal ExternalScriptInfo(string name, string path, ExecutionContext context)
            : base(name, CommandTypes.ExternalScript, context)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            Diagnostics.Assert(IO.Path.IsPathRooted(path), "Caller makes sure that 'path' is already resolved.");

            // Path might contain short-name syntax such as 'DOCUME~1'. Use Path.GetFullPath to expand the short name
            _path = IO.Path.GetFullPath(path);
            CommonInitialization();
        }

        /// <summary>
        /// Creates an instance of ExternalScriptInfo that has no ExecutionContext.
        /// This is used exclusively to pass it to the AuthorizationManager that just uses the path parameter.
        /// </summary>
        /// <param name="name">
        /// The name of the script.
        /// </param>
        /// <param name="path">
        /// The path to the script
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="path"/> is null or empty.
        /// </exception>
        internal ExternalScriptInfo(string name, string path) : base(name, CommandTypes.ExternalScript)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            Diagnostics.Assert(IO.Path.IsPathRooted(path), "Caller makes sure that 'path' is already resolved.");

            // Path might contain short-name syntax such as 'DOCUME~1'. Use Path.GetFullPath to expand the short name
            _path = IO.Path.GetFullPath(path);
            CommonInitialization();
        }

        /// <summary>
        /// This is a copy constructor, used primarily for get-command.
        /// </summary>
        internal ExternalScriptInfo(ExternalScriptInfo other)
            : base(other)
        {
            _path = other._path;
            CommonInitialization();
        }

        /// <summary>
        /// Common initialization for all constructors.
        /// </summary>
        private void CommonInitialization()
        {
            // Assume external scripts are untrusted by default (for Get-Command, etc)
            // until we've actually parsed their script block.
            if (SystemPolicy.GetSystemLockdownPolicy() != SystemEnforcementMode.None)
            {
                // Get the lock down policy with no handle. This only impacts command discovery,
                // as the real language mode assignment will be done when we read the script
                // contents.
                switch (SystemPolicy.GetLockdownPolicy(_path, null))
                {
                    case SystemEnforcementMode.None:
                        DefiningLanguageMode = PSLanguageMode.FullLanguage;
                        break;

                    case SystemEnforcementMode.Audit:
                        DefiningLanguageMode = PSLanguageMode.ConstrainedLanguageAudit;
                        break;

                    case SystemEnforcementMode.Enforce:
                        DefiningLanguageMode = PSLanguageMode.ConstrainedLanguage;
                        break;
                }
            }
        }

        /// <summary>
        /// Create a copy of commandInfo for GetCommandCommand so that we can generate parameter
        /// sets based on an argument list (so we can get the dynamic parameters.)
        /// </summary>
        internal override CommandInfo CreateGetCommandCopy(object[] argumentList)
        {
            ExternalScriptInfo copy = new ExternalScriptInfo(this) { IsGetCommandCopy = true, Arguments = argumentList };
            return copy;
        }

        #endregion ctor

        internal override HelpCategory HelpCategory
        {
            get { return HelpCategory.ExternalScript; }
        }

        /// <summary>
        /// Gets the path to the script file.
        /// </summary>
        public string Path
        {
            get { return _path; }
        }

        private readonly string _path = string.Empty;

        /// <summary>
        /// Gets the path to the script file.
        /// </summary>
        public override string Definition
        {
            get { return Path; }
        }

        /// <summary>
        /// Gets the source of this command.
        /// </summary>
        public override string Source
        {
            get { return this.Definition; }
        }

        /// <summary>
        /// Returns the syntax of a command.
        /// </summary>
        internal override string Syntax
        {
            get
            {
                StringBuilder synopsis = new StringBuilder();

                foreach (CommandParameterSetInfo parameterSet in ParameterSets)
                {
                    synopsis.AppendLine(
                        string.Format(
                            Globalization.CultureInfo.CurrentCulture,
                            "{0} {1}",
                            Name,
                            parameterSet));
                }

                return synopsis.ToString();
            }
        }

        /// <summary>
        /// Determine the visibility for this script...
        /// </summary>
        public override SessionStateEntryVisibility Visibility
        {
            get
            {
                if (Context == null)
                {
                    return SessionStateEntryVisibility.Public;
                }

                return Context.EngineSessionState.CheckScriptVisibility(_path);
            }

            set
            {
                throw PSTraceSource.NewNotImplementedException();
            }
        }

        /// <summary>
        /// The script block that represents the external script.
        /// </summary>
        public ScriptBlock ScriptBlock
        {
            get
            {
                if (_scriptBlock == null)
                {
                    // Skip ShouldRun check for .psd1 files.
                    // Use ValidateScriptInfo() for explicitly validating the checkpolicy for psd1 file.
                    //
                    if (!_path.EndsWith(".psd1", StringComparison.OrdinalIgnoreCase))
                    {
                        ValidateScriptInfo(null);
                    }

                    // parse the script into an expression tree...
                    ScriptBlock newScriptBlock = ParseScriptContents(new Parser(), _path, ScriptContents, DefiningLanguageMode);
                    this.ScriptBlock = newScriptBlock;
                }

                return _scriptBlock;
            }

            private set
            {
                _scriptBlock = value;
                if (value != null)
                {
                    _scriptBlock.LanguageMode = this.DefiningLanguageMode;
                }
            }
        }

        private ScriptBlock _scriptBlock;
        private ScriptBlockAst _scriptBlockAst;

        private static ScriptBlock ParseScriptContents(Parser parser, string fileName, string fileContents, PSLanguageMode? definingLanguageMode)
        {
            // If we are in ConstrainedLanguage mode but the defining language mode is FullLanguage, then we need
            // to parse the script contents in FullLanguage mode context.  Otherwise we will get bogus parsing errors
            // such as "Configuration keyword not allowed".
            if (definingLanguageMode.HasValue && (definingLanguageMode == PSLanguageMode.FullLanguage))
            {
                var context = LocalPipeline.GetExecutionContextFromTLS();
                if ((context != null) && (context.LanguageMode == PSLanguageMode.ConstrainedLanguage))
                {
                    context.LanguageMode = PSLanguageMode.FullLanguage;
                    try
                    {
                        return ScriptBlock.Create(parser, fileName, fileContents);
                    }
                    finally
                    {
                        context.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                    }
                }
            }

            return ScriptBlock.Create(parser, fileName, fileContents);
        }

        internal ScriptBlockAst GetScriptBlockAst()
        {
            var scriptContents = ScriptContents;
            if (_scriptBlock == null)
            {
                this.ScriptBlock = ScriptBlock.TryGetCachedScriptBlock(_path, scriptContents);
            }

            if (_scriptBlock != null)
            {
                return (ScriptBlockAst)_scriptBlock.Ast;
            }

            if (_scriptBlockAst == null)
            {
                ParseError[] errors;
                Parser parser = new Parser();

                // If we are in ConstrainedLanguage mode but the defining language mode is FullLanguage, then we need
                // to parse the script contents in FullLanguage mode context.  Otherwise we will get bogus parsing errors
                // such as "Configuration or Class keyword not allowed".
                var context = LocalPipeline.GetExecutionContextFromTLS();
                if (context != null && context.LanguageMode == PSLanguageMode.ConstrainedLanguage &&
                    DefiningLanguageMode == PSLanguageMode.FullLanguage)
                {
                    context.LanguageMode = PSLanguageMode.FullLanguage;
                    try
                    {
                        _scriptBlockAst = parser.Parse(_path, ScriptContents, null, out errors, ParseMode.Default);
                    }
                    finally
                    {
                        context.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                    }
                }
                else
                {
                    _scriptBlockAst = parser.Parse(_path, ScriptContents, null, out errors, ParseMode.Default);
                }

                if (errors.Length == 0)
                {
                    this.ScriptBlock = new ScriptBlock(_scriptBlockAst, isFilter: false);
                    ScriptBlock.CacheScriptBlock(_scriptBlock.Clone(), _path, scriptContents);
                }
            }

            return _scriptBlockAst;
        }

        /// <summary>
        /// Validates the external script info.
        /// </summary>
        /// <param name="host"></param>
        public void ValidateScriptInfo(Host.PSHost host)
        {
            if (!_signatureChecked)
            {
                ExecutionContext context = Context ?? LocalPipeline.GetExecutionContextFromTLS();

                ReadScriptContents();

                // We have no way to check the signature w/o context because we don't have
                // an AuthorizationManager.  This can happen during initialization when trying
                // to get the CommandMetadata for a script (either to prepopulate the metadata
                // or creating a proxy).  If context can be null under any other circumstances,
                // we need to make sure it's acceptable if the parser is invoked on unsigned scripts.
                if (context != null)
                {
                    CommandDiscovery.ShouldRun(context, host, this, CommandOrigin.Internal);
                    _signatureChecked = true;
                }
            }
        }

        /// <summary>
        /// The output type(s) is specified in the script block.
        /// </summary>
        public override ReadOnlyCollection<PSTypeName> OutputType
        {
            get { return ScriptBlock.OutputType; }
        }

        internal bool SignatureChecked
        {
            set { _signatureChecked = value; }
        }

        private bool _signatureChecked;

        #region Internal

        /// <summary>
        /// The command metadata for the script.
        /// </summary>
        internal override CommandMetadata CommandMetadata
        {
            get
            {
                return _commandMetadata ??=
                    new CommandMetadata(this.ScriptBlock, this.Name, LocalPipeline.GetExecutionContextFromTLS());
            }
        }

        private CommandMetadata _commandMetadata;

        /// <summary>
        /// True if the command has dynamic parameters, false otherwise.
        /// </summary>
        internal override bool ImplementsDynamicParameters
        {
            get
            {
                try
                {
                    return ScriptBlock.HasDynamicParameters;
                }
                catch (ParseException) { }
                catch (ScriptRequiresException) { }

                // If we got here, there was some sort of parsing exception.  We'll just
                // ignore it and assume the script does not implement dynamic parameters.
                // Furthermore, we'll clear out the fields so that the next attempt to
                // access ScriptBlock will result in an exception that doesn't get ignored.
                _scriptBlock = null;
                _scriptContents = null;

                return false;
            }
        }

        #endregion Internal

        private ScriptRequirements GetRequiresData()
        {
            return GetScriptBlockAst().ScriptRequirements;
        }

        internal string RequiresApplicationID
        {
            get
            {
                var data = GetRequiresData();
                return data?.RequiredApplicationId;
            }
        }

        internal uint ApplicationIDLineNumber
        {
            get { return 0; }
        }

        internal Version RequiresPSVersion
        {
            get
            {
                var data = GetRequiresData();
                return data?.RequiredPSVersion;
            }
        }

        internal IEnumerable<string> RequiresPSEditions
        {
            get
            {
                var data = GetRequiresData();
                return data?.RequiredPSEditions;
            }
        }

        internal IEnumerable<ModuleSpecification> RequiresModules
        {
            get
            {
                var data = GetRequiresData();
                return data?.RequiredModules;
            }
        }

        internal bool RequiresElevation
        {
            get
            {
                var data = GetRequiresData();
                return data != null && data.IsElevationRequired;
            }
        }

        internal uint PSVersionLineNumber
        {
            get { return 0; }
        }

        internal IEnumerable<PSSnapInSpecification> RequiresPSSnapIns
        {
            get
            {
                var data = GetRequiresData();
                return data?.RequiresPSSnapIns;
            }
        }

        /// <summary>
        /// Gets the original contents of the script.
        /// </summary>
        public string ScriptContents
        {
            get
            {
                if (_scriptContents == null)
                {
                    ReadScriptContents();
                }

                return _scriptContents;
            }
        }

        private string _scriptContents;

        /// <summary>
        /// Gets the original encoding of the script.
        /// </summary>
        public Encoding OriginalEncoding
        {
            get
            {
                if (_scriptContents == null)
                {
                    ReadScriptContents();
                }

                return _originalEncoding;
            }
        }

        private Encoding _originalEncoding;

        private void ReadScriptContents()
        {
            if (_scriptContents == null)
            {
                // make sure we can actually load the script and that it's non-empty
                // before we call it.

                // Note, although we are passing ASCII as the encoding, the StreamReader
                // class still obeys the byte order marks at the beginning of the file
                // if present. If not present, then ASCII is used as the default encoding.

                try
                {
                    using (FileStream readerStream = new FileStream(_path, FileMode.Open, FileAccess.Read))
                    {
                        using (StreamReader scriptReader = new StreamReader(readerStream, Encoding.Default))
                        {
                            _scriptContents = scriptReader.ReadToEnd();
                            _originalEncoding = scriptReader.CurrentEncoding;

                            // Check this file against any system wide enforcement policies.
                            SystemScriptFileEnforcement filePolicyEnforcement = SystemPolicy.GetFilePolicyEnforcement(_path, readerStream);
                            switch (filePolicyEnforcement)
                            {
                                case SystemScriptFileEnforcement.None:
                                    if (Context != null)
                                    {
                                        DefiningLanguageMode = Context.LanguageMode;
                                    }
                                    
                                    break;

                                case SystemScriptFileEnforcement.Allow:
                                    DefiningLanguageMode = PSLanguageMode.FullLanguage;
                                    break;

                                case SystemScriptFileEnforcement.AllowConstrained:
                                    DefiningLanguageMode = PSLanguageMode.ConstrainedLanguage;
                                    break;

                                case SystemScriptFileEnforcement.AllowConstrainedAudit:
                                    SystemPolicy.LogWDACAuditMessage(
                                        title: "Script File Read",
                                        message: $"Script file {_path} is not trusted by policy and would run in Constrained Language mode under policy enforcement.",
                                        fqid: "ScriptFileNotTrustedByPolicy");
                                    DefiningLanguageMode = PSLanguageMode.ConstrainedLanguageAudit;
                                    break;

                                case SystemScriptFileEnforcement.Block:
                                    throw new PSSecurityException(
                                        string.Format(
                                            Globalization.CultureInfo.CurrentUICulture,
                                            SecuritySupportStrings.ScriptFileBlockedBySystemPolicy,
                                            _path));

                                default:
                                    throw new PSSecurityException(
                                        string.Format(
                                            Globalization.CultureInfo.CurrentUICulture,
                                            SecuritySupportStrings.UnknownSystemScriptFileEnforcement,
                                            filePolicyEnforcement));
                            }
                        }
                    }
                }
                catch (ArgumentException e)
                {
                    // This catches PSArgumentException as well.
                    ThrowCommandNotFoundException(e);
                }
                catch (IOException e)
                {
                    ThrowCommandNotFoundException(e);
                }
                catch (NotSupportedException e)
                {
                    ThrowCommandNotFoundException(e);
                }
                catch (UnauthorizedAccessException e)
                {
                    // this is unadvertised exception thrown by the StreamReader ctor when
                    // no permission to read the script file
                    ThrowCommandNotFoundException(e);
                }
            }
        }

        private static void ThrowCommandNotFoundException(Exception innerException)
        {
            CommandNotFoundException cmdE = new CommandNotFoundException(innerException.Message, innerException);
            throw cmdE;
        }
    }

    /// <summary>
    /// Thrown when fail to parse #requires statements. Caught by CommandDiscovery.
    /// </summary>
    internal class ScriptRequiresSyntaxException : ScriptRequiresException
    {
        internal ScriptRequiresSyntaxException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Defines the name and version tuple of a PSSnapin.
    /// </summary>
    [Serializable]
    public class PSSnapInSpecification
    {
        internal PSSnapInSpecification(string psSnapinName)
        {
            PSSnapInInfo.VerifyPSSnapInFormatThrowIfError(psSnapinName);
            Name = psSnapinName;
            Version = null;
        }

        /// <summary>
        /// The name of the snapin.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The version of the snapin.
        /// </summary>
        public Version Version { get; internal set; }
    }
}
