// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Security;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "import-localizeddata" cmdlet.
    /// </summary>
    [Cmdlet(VerbsData.Import, "LocalizedData", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096710")]
    public sealed class ImportLocalizedData : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// The path from which to import the aliases.
        /// </summary>
        [Parameter(Position = 0)]
        [Alias("Variable")]
        [ValidateNotNullOrEmpty]
        public string BindingVariable
        {
            get
            {
                return _bindingVariable;
            }

            set
            {
                _bindingVariable = value;
            }
        }

        private string _bindingVariable;

        /// <summary>
        /// The scope to import the aliases to.
        /// </summary>
        [Parameter(Position = 1)]
        public string UICulture
        {
            get
            {
                return _uiculture;
            }

            set
            {
                _uiculture = value;
            }
        }

        private string _uiculture;

        /// <summary>
        /// The scope to import the aliases to.
        /// </summary>
        [Parameter]
        public string BaseDirectory
        {
            get
            {
                return _baseDirectory;
            }

            set
            {
                _baseDirectory = value;
            }
        }

        private string _baseDirectory;

        /// <summary>
        /// The scope to import the aliases to.
        /// </summary>
        [Parameter]
        public string FileName
        {
            get
            {
                return _fileName;
            }

            set
            {
                _fileName = value;
            }
        }

        private string _fileName;

        /// <summary>
        /// The command allowed in the data file.  If unspecified, then ConvertFrom-StringData is allowed.
        /// </summary>
        [Parameter]
        [ValidateTrustedData]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        public string[] SupportedCommand
        {
            get
            {
                return _commandsAllowed;
            }

            set
            {
                _setSupportedCommand = true;
                _commandsAllowed = value;
            }
        }

        private string[] _commandsAllowed = new string[] { "ConvertFrom-StringData" };
        private bool _setSupportedCommand = false;

        #endregion Parameters

        #region Command code

        /// <summary>
        /// The main processing loop of the command.
        /// </summary>
        protected override void ProcessRecord()
        {
            string path = GetFilePath();

            if (path == null)
            {
                return;
            }

            if (!File.Exists(path))
            {
                InvalidOperationException ioe =
                    PSTraceSource.NewInvalidOperationException(
                        ImportLocalizedDataStrings.FileNotExist,
                        path);
                WriteError(new ErrorRecord(ioe, "ImportLocalizedData", ErrorCategory.ObjectNotFound, path));
                return;
            }

            // Prevent additional commands in ConstrainedLanguage mode
            if (_setSupportedCommand && Context.LanguageMode == PSLanguageMode.ConstrainedLanguage)
            {
                if (SystemPolicy.GetSystemLockdownPolicy() != SystemEnforcementMode.Audit)
                {
                    NotSupportedException nse =
                        PSTraceSource.NewNotSupportedException(
                            ImportLocalizedDataStrings.CannotDefineSupportedCommand);
                    ThrowTerminatingError(
                        new ErrorRecord(nse, "CannotDefineSupportedCommand", ErrorCategory.PermissionDenied, null));
                }
                
                SystemPolicy.LogWDACAuditMessage(
                    context: Context,
                    title: ImportLocalizedDataStrings.WDACLogTitle,
                    message: ImportLocalizedDataStrings.WDACLogMessage,
                    fqid: "SupportedCommandsDisabled",
                    dropIntoDebugger: true);
            }

            string script = GetScript(path);
            if (script == null)
            {
                return;
            }

            try
            {
                var scriptBlock = Context.Engine.ParseScriptBlock(script, false);
                scriptBlock.CheckRestrictedLanguage(SupportedCommand, null, false);
                object result;
                PSLanguageMode oldLanguageMode = Context.LanguageMode;
                Context.LanguageMode = PSLanguageMode.RestrictedLanguage;
                try
                {
                    result = scriptBlock.InvokeReturnAsIs();
                    if (result == AutomationNull.Value)
                    {
                        result = null;
                    }
                }
                finally
                {
                    Context.LanguageMode = oldLanguageMode;
                }

                if (_bindingVariable != null)
                {
                    VariablePath variablePath = new(_bindingVariable);
                    if (variablePath.IsUnscopedVariable)
                    {
                        variablePath = variablePath.CloneAndSetLocal();
                    }

                    if (string.IsNullOrEmpty(variablePath.UnqualifiedPath))
                    {
                        InvalidOperationException ioe = PSTraceSource.NewInvalidOperationException(
                            ImportLocalizedDataStrings.IncorrectVariableName, _bindingVariable);
                        WriteError(new ErrorRecord(ioe, "ImportLocalizedData", ErrorCategory.InvalidArgument,
                                                   _bindingVariable));
                        return;
                    }

                    SessionStateScope scope = null;
                    PSVariable variable = SessionState.Internal.GetVariableItem(variablePath, out scope);

                    if (variable == null)
                    {
                        variable = new PSVariable(variablePath.UnqualifiedPath, result, ScopedItemOptions.None);
                        Context.EngineSessionState.SetVariable(variablePath, variable, false, CommandOrigin.Internal);
                    }
                    else
                    {
                        variable.Value = result;

                        if (Context.LanguageMode == PSLanguageMode.ConstrainedLanguage)
                        {
                            // Mark untrusted values for assignments to 'Global:' variables, and 'Script:' variables in
                            // a module scope, if it's necessary.
                            ExecutionContext.MarkObjectAsUntrustedForVariableAssignment(variable, scope, Context.EngineSessionState);
                        }
                    }
                }

                // If binding variable is null, write the object to stream
                else
                {
                    WriteObject(result);
                }
            }
            catch (RuntimeException e)
            {
                PSInvalidOperationException ioe = PSTraceSource.NewInvalidOperationException(e,
                    ImportLocalizedDataStrings.ErrorLoadingDataFile,
                    path,
                    e.Message);

                throw ioe;
            }

            return;
        }

        private string GetFilePath()
        {
            if (string.IsNullOrEmpty(_fileName))
            {
                if (InvocationExtent == null || string.IsNullOrEmpty(InvocationExtent.File))
                {
                    throw PSTraceSource.NewInvalidOperationException(ImportLocalizedDataStrings.NotCalledFromAScriptFile);
                }
            }

            string dir = _baseDirectory;

            if (string.IsNullOrEmpty(dir))
            {
                if (InvocationExtent != null && !string.IsNullOrEmpty(InvocationExtent.File))
                {
                    dir = Path.GetDirectoryName(InvocationExtent.File);
                }
                else
                {
                    dir = ".";
                }
            }

            dir = PathUtils.ResolveFilePath(dir, this);

            string fileName = _fileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = InvocationExtent.File;
            }
            else
            {
                if (!string.IsNullOrEmpty(Path.GetDirectoryName(fileName)))
                {
                    throw PSTraceSource.NewInvalidOperationException(ImportLocalizedDataStrings.FileNameParameterCannotHavePath);
                }
            }

            fileName = Path.GetFileNameWithoutExtension(fileName);

            CultureInfo culture = null;
            if (_uiculture == null)
            {
                culture = CultureInfo.CurrentUICulture;
            }
            else
            {
                try
                {
                    culture = CultureInfo.GetCultureInfo(_uiculture);
                }
                catch (ArgumentException)
                {
                    throw PSTraceSource.NewArgumentException("Culture");
                }
            }

            CultureInfo currentCulture = culture;
            string filePath;
            string fullFileName = fileName + ".psd1";
            while (currentCulture != null && !string.IsNullOrEmpty(currentCulture.Name))
            {
                filePath = Path.Combine(dir, currentCulture.Name, fullFileName);

                if (File.Exists(filePath))
                {
                    return filePath;
                }

                currentCulture = currentCulture.Parent;
            }

            filePath = Path.Combine(dir, fullFileName);

            if (File.Exists(filePath))
            {
                return filePath;
            }

            InvalidOperationException ioe =
                PSTraceSource.NewInvalidOperationException(
                                        ImportLocalizedDataStrings.CannotFindPsd1File,
                                        fullFileName,
                                        Path.Combine(dir, culture.Name)
                                        );
            WriteError(new ErrorRecord(ioe, "ImportLocalizedData", ErrorCategory.ObjectNotFound,
                                       Path.Combine(dir, culture.Name, fullFileName)));
            return null;
        }

        private string GetScript(string filePath)
        {
            InvalidOperationException ioe = null;
            try
            {
                // 197751: WR BUG BASH: Powershell: localized text display as garbage
                // leaving the encoding to be decided by the StreamReader. StreamReader
                // will read the preamble and decide proper encoding.
                using (FileStream scriptStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (StreamReader scriptReader = new(scriptStream))
                {
                    return scriptReader.ReadToEnd();
                }
            }
            catch (ArgumentException e)
            {
                ioe = PSTraceSource.NewInvalidOperationException(
                                            ImportLocalizedDataStrings.ErrorOpeningFile,
                                            filePath,
                                            e.Message);
            }
            catch (IOException e)
            {
                ioe = PSTraceSource.NewInvalidOperationException(
                                            ImportLocalizedDataStrings.ErrorOpeningFile,
                                            filePath,
                                            e.Message);
            }
            catch (NotSupportedException e)
            {
                ioe = PSTraceSource.NewInvalidOperationException(
                                            ImportLocalizedDataStrings.ErrorOpeningFile,
                                            filePath,
                                            e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                ioe = PSTraceSource.NewInvalidOperationException(
                                            ImportLocalizedDataStrings.ErrorOpeningFile,
                                            filePath,
                                            e.Message);
            }

            WriteError(new ErrorRecord(ioe, "ImportLocalizedData", ErrorCategory.OpenError, filePath));
            return null;
        }

        #endregion Command code
    }
}
