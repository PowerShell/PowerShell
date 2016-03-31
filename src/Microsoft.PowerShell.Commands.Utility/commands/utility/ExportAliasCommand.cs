/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;

#if CORECLR
// Some APIs are missing from System.Environment. We use System.Management.Automation.Environment as a proxy type:
//  - for missing APIs, System.Management.Automation.Environment has extension implementation.
//  - for existing APIs, System.Management.Automation.Environment redirect the call to System.Environment.
using Environment = System.Management.Automation.Environment;
#endif

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The formats that export-alias supports
    /// </summary>
    public enum ExportAliasFormat
    {
        /// <summary>
        /// Aliases will be exported to a CSV file
        /// </summary>
        Csv,

        /// <summary>
        /// Aliases will be exported as an MSH script
        /// </summary>
        Script
    }

    /// <summary>
    /// The implementation of the "export-alias" cmdlet
    /// </summary>
    /// 
    [Cmdlet(VerbsData.Export, "Alias", SupportsShouldProcess = true, DefaultParameterSetName = "ByPath", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113296")]
    [OutputType(typeof(AliasInfo))]
    public class ExportAliasCommand : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// The Path of the file to export the aliases to.
        /// </summary>
        /// 
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByPath")]
        public string Path
        {
            get
            {
                return path;
            }
            set
            {
                if (value == null)
                {
                    path = ".";
                }
                else
                {
                    path = value;
                }
            }
        }
        private string path = ".";

        /// <summary>
        /// The literal path of the file to export the aliases to.
        /// </summary>
        /// 
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "ByLiteralPath")]
        [Alias("PSPath")]
        public string LiteralPath
        {
            get
            {
                return path;
            }
            set
            {
                if (value == null)
                {
                    path = ".";
                }
                else
                {
                    path = value;
                    isLiteralPath = true;
                }
            }
        }
        private bool isLiteralPath = false;

        /// <summary>
        /// The Name parameter for the command
        /// </summary>
        /// 
        [Parameter (Position = 1, ValueFromPipelineByPropertyName = true)]
        public string[] Name
        {
            get
            {
                return names;
            }
            set
            {
                if (value == null)
                {
                    names = new string[] { "*" };
                }
                else
                {
                    names = value;
                }
            }
        }
        private string[] names = new string[] { "*" };

        /// <summary>
        /// If set to true, the alias that is set is passed to the
        /// pipeline.
        /// </summary>
        /// 
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return passThru;
            }

            set
            {
                passThru = value;
            }
        }
        private bool passThru;

        /// <summary>
        /// Parameter that determines the format of the file created.
        /// </summary>
        ///
        [Parameter]
        public ExportAliasFormat As
        {
            get
            {
                return asFormat;
            }
            set
            {
                asFormat = value;
            }
        }
        private ExportAliasFormat asFormat = ExportAliasFormat.Csv;

        /// <summary>
        /// Property that sets append parameter.
        /// </summary>
        [Parameter()]
        public SwitchParameter Append
        {
            get
            {
                return append;
            }
            set
            {
                append = value;
            }
        }
        private bool append;

        /// <summary>
        /// Property that sets force parameter.
        /// </summary>
        [Parameter()]
        public SwitchParameter Force
        {
            get
            {
                return force;
            }
            set
            {
                force = value;
            }
        }
        private bool force;

        /// <summary>
        /// Property that prevents file overwrite.
        /// </summary>
        [Parameter()]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber
        {
            get
            {
                return noclobber;
            }
            set
            {
                noclobber = value;
            }
        }
        private bool noclobber;

        /// <summary>
        /// The description that gets added to the file as a comment
        /// </summary>
        /// <value></value>
        [Parameter]
        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
            }
        }
        private string description;

        /// <summary>
        /// The scope parameter for the command determines
        /// which scope the aliases are retrieved from.
        /// </summary>
        /// 
        [Parameter]
        public string Scope
        {
            get
            {
                return scope;
            }

            set
            {
                scope = value;
            }
        }
        private string scope;
        #endregion Parameters

        #region Command code

        /// <summary>
        /// The main processing loop of the command.
        /// </summary>
        /// 
        protected override void ProcessRecord()
        {
            // First get the alias table (from the proper scope if necessary)
            IDictionary<string, AliasInfo> aliasTable = null;

            if (!String.IsNullOrEmpty(scope))
            {
                // This can throw PSArgumentException and PSArgumentOutOfRangeException
                // but just let them go as this is terminal for the pipeline and the
                // exceptions are already properly adorned with an ErrorRecord.

                aliasTable = SessionState.Internal.GetAliasTableAtScope(scope);
            }
            else
            {
                aliasTable = SessionState.Internal.GetAliasTable();
            }

            foreach (string aliasName in names)
            {
                bool resultFound = false;

                // Create the name pattern

                WildcardPattern namePattern =
                    WildcardPattern.Get(
                        aliasName,
                        WildcardOptions.IgnoreCase);

                // Now loop through the table and write out any aliases that
                // match the name and don't match the exclude filters and are
                // visible to the caller...
                CommandOrigin origin = MyInvocation.CommandOrigin;
                foreach (KeyValuePair<string, AliasInfo> tableEntry in aliasTable)
                {
                    if (!namePattern.IsMatch(tableEntry.Key))
                    {
                        continue;
                    }

                    if (SessionState.IsVisible(origin, tableEntry.Value))
                    {
                        resultFound = true;
                        matchingAliases.Add(tableEntry.Value);
                    }
                }

                if (!resultFound &&
                    !WildcardPattern.ContainsWildcardCharacters(aliasName))
                {
                    // Need to write an error if the user tries to get an alias
                    // that doesn't exist and they are not globbing.

                    ItemNotFoundException itemNotFound =
                        new ItemNotFoundException(
                            aliasName,
                            "AliasNotFound",
                            SessionStateStrings.AliasNotFound);

                    WriteError(
                        new ErrorRecord(
                            itemNotFound.ErrorRecord,
                            itemNotFound));
                }
            }
        } // ProcessRecord


        /// <summary>
        /// Writes the aliases to the file
        /// </summary>
        protected override void EndProcessing()
        {
            StreamWriter writer = null;
            FileInfo readOnlyFileInfo = null;
            try
            {
                if (ShouldProcess(Path))
                {
                    writer = OpenFile(out readOnlyFileInfo);
                }

                if (null != writer)
                    WriteHeader(writer);

                // Now write out the aliases

                foreach (AliasInfo alias in matchingAliases)
                {
                    string line = null;
                    if (this.As == ExportAliasFormat.Csv)
                    {
                        line = GetAliasLine(alias, "\"{0}\",\"{1}\",\"{2}\",\"{3}\"");
                    }
                    else
                    {
                        line = GetAliasLine(alias, "set-alias -Name:\"{0}\" -Value:\"{1}\" -Description:\"{2}\" -Option:\"{3}\"");
                    }

                    if (null != writer)
                        writer.WriteLine(line);

                    if (PassThru)
                    {
                        WriteObject(alias);
                    }
                }

            }
            finally
            {
                if (null != writer)
                    writer.Dispose();
                // reset the read-only attribute
                if (null != readOnlyFileInfo)
                    readOnlyFileInfo.Attributes |= FileAttributes.ReadOnly;
            }
        }

        /// <summary>
        /// Holds all the matching aliases for writing to the file
        /// </summary>
        private Collection<AliasInfo> matchingAliases = new Collection<AliasInfo>();

        private static string GetAliasLine(AliasInfo alias, string formatString)
        {
            // Using the invariant culture here because we don't want the
            // file to vary based on locale.

            string result =
                String.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    formatString,
                    alias.Name,
                    alias.Definition,
                    alias.Description,
                    alias.Options);

            return result;
        }


        private void WriteHeader(StreamWriter writer)
        {
            WriteFormattedResourceString(writer, AliasCommandStrings.ExportAliasHeaderTitle);

            string user = Environment.UserName;
            WriteFormattedResourceString(writer, AliasCommandStrings.ExportAliasHeaderUser, user);

            DateTime now = DateTime.Now;
            WriteFormattedResourceString(writer, AliasCommandStrings.ExportAliasHeaderDate, now);

            string machine = Environment.MachineName;
            WriteFormattedResourceString(writer, AliasCommandStrings.ExportAliasHeaderMachine, machine);

            // Now write the description if there is one

            if (description != null)
            {
                // First we need to break up the description on newlines and add a
                // # for each line.

                description = description.Replace("\n", "\n# ");

                // Now write out the description
                writer.WriteLine("#");
                writer.Write("# ");
                writer.WriteLine(description);
            }
        }

        private static void WriteFormattedResourceString(
            StreamWriter writer,
            string resourceId,
            params object[] args)
        {
            string line = StringUtil.Format(resourceId, args);

            writer.Write("# ");

            writer.WriteLine(line);
        }

        /// <summary>
        /// Open the file to which aliases should be exported
        /// </summary>
        /// <param name="readOnlyFileInfo">
        /// If not null, this is the file whose read-only attribute
        /// was cleared (due to the -Force parameter).  The attribute
        /// should be reset.
        /// </param>
        /// <returns></returns>
        private StreamWriter OpenFile(out FileInfo readOnlyFileInfo)
        {
            StreamWriter result = null;
            FileStream file = null;
            readOnlyFileInfo = null;

            PathUtils.MasterStreamOpen(
                this,
                this.Path,
                EncodingConversion.Unicode,
                false, // defaultEncoding
                Append,
                Force,
                NoClobber,
                out file,
                out result,
                out readOnlyFileInfo,
                isLiteralPath
                );

            return result;
        }

        private void ThrowFileOpenError(Exception e, string pathWithError)
        {
            string message = StringUtil.Format(AliasCommandStrings.ExportAliasFileOpenFailed, pathWithError, e.Message);

            ErrorRecord errorRecord = new ErrorRecord(
                e,
                "FileOpenFailure",
                ErrorCategory.OpenError,
                pathWithError);

            errorRecord.ErrorDetails = new ErrorDetails(message);
            this.ThrowTerminatingError(errorRecord);
        }

        #endregion Command code
    } // class ExportAliasCommand
}//Microsoft.PowerShell.Commands

