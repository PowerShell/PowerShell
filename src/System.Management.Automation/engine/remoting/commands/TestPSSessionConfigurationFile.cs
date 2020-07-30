// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Test-PSSessionConfigurationFile command implementation
    ///
    /// See Declarative Initial Session Config (DISC)
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "PSSessionConfigurationFile", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096797")]
    [OutputType(typeof(bool))]
    public class TestPSSessionConfigurationFileCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// The output path for the generated file...
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string Path
        {
            get { return _path; }

            set { _path = value; }
        }

        private string _path;

        #endregion

        #region Overrides

        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            ProviderInfo provider = null;
            Collection<string> filePaths;

            try
            {
                if (this.Context.EngineSessionState.IsProviderLoaded(Context.ProviderNames.FileSystem))
                {
                    filePaths = SessionState.Path.GetResolvedProviderPathFromPSPath(_path, out provider);
                }
                else
                {
                    filePaths = new Collection<string>();
                    filePaths.Add(_path);
                }
            }
            catch (ItemNotFoundException)
            {
                string message = StringUtil.Format(RemotingErrorIdStrings.PSSessionConfigurationFileNotFound, _path);
                FileNotFoundException fnf = new FileNotFoundException(message);
                ErrorRecord er = new ErrorRecord(fnf, "PSSessionConfigurationFileNotFound",
                    ErrorCategory.ResourceUnavailable, _path);
                WriteError(er);
                return;
            }

            // Make sure that the path is in the file system - that's all we can handle currently...
            if (!provider.NameEquals(this.Context.ProviderNames.FileSystem))
            {
                // "The current provider ({0}) cannot open a file"
                throw InterpreterError.NewInterpreterException(_path, typeof(RuntimeException),
                    null, "FileOpenError", ParserStrings.FileOpenError, provider.FullName);
            }

            // Make sure at least one file was found...
            if (filePaths == null || filePaths.Count < 1)
            {
                string message = StringUtil.Format(RemotingErrorIdStrings.PSSessionConfigurationFileNotFound, _path);
                FileNotFoundException fnf = new FileNotFoundException(message);
                ErrorRecord er = new ErrorRecord(fnf, "PSSessionConfigurationFileNotFound",
                    ErrorCategory.ResourceUnavailable, _path);
                WriteError(er);
                return;
            }

            if (filePaths.Count > 1)
            {
                // "The path resolved to more than one file; can only process one file at a time."
                throw InterpreterError.NewInterpreterException(filePaths, typeof(RuntimeException),
                    null, "AmbiguousPath", ParserStrings.AmbiguousPath);
            }

            string filePath = filePaths[0];
            ExternalScriptInfo scriptInfo = null;
            string ext = System.IO.Path.GetExtension(filePath);
            if (ext.Equals(StringLiterals.PowerShellDISCFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                // Create a script info for loading the file...
                string scriptName;
                scriptInfo = DISCUtils.GetScriptInfoForFile(this.Context, filePath, out scriptName);

                Hashtable configTable = null;

                try
                {
                    configTable = DISCUtils.LoadConfigFile(this.Context, scriptInfo);
                }
                catch (RuntimeException e)
                {
                    WriteVerbose(StringUtil.Format(RemotingErrorIdStrings.DISCErrorParsingConfigFile, filePath, e.Message));
                    WriteObject(false);
                    return;
                }

                if (configTable == null)
                {
                    WriteObject(false);
                    return;
                }

                DISCUtils.ExecutionPolicyType = typeof(ExecutionPolicy);
                WriteObject(DISCUtils.VerifyConfigTable(configTable, this, filePath));
            }
            else
            {
                string message = StringUtil.Format(RemotingErrorIdStrings.InvalidPSSessionConfigurationFilePath, filePath);
                InvalidOperationException ioe = new InvalidOperationException(message);
                ErrorRecord er = new ErrorRecord(ioe, "InvalidPSSessionConfigurationFilePath",
                    ErrorCategory.InvalidArgument, _path);
                ThrowTerminatingError(er);
            }
        }

        #endregion
    }
}
