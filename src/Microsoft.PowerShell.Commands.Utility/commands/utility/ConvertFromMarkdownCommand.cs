// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using Microsoft.PowerShell.MarkdownRender;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Converts a markdown string to a MarkdownInfo object.
    /// The conversion can be done into a HTML text or VT100 encoding string.
    /// </summary>
    [Cmdlet(
        VerbsData.ConvertFrom, "Markdown",
        DefaultParameterSetName = PathParamSet,
        HelpUri = "TBD"
    )]
    [OutputType(typeof(Microsoft.PowerShell.MarkdownRender.MarkdownInfo))]
    public class ConvertFromMarkdownCommand : PSCmdlet
    {
        /// <summary>
        /// Path to the file to convert from Markdown to MarkdownInfo
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = PathParamSet, Mandatory = true)]
        public string[] Path { get; set; }

        /// <summary>
        /// Path to the file to convert from Markdown to MarkdownInfo
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = LitPathParamSet, Mandatory = true)]
        public string[] LiteralPath { get; set; }

        /// <summary>
        /// InputObject of type System.IO.FileInfo or string with content to convert from Markdown to MarkdownInfo
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = InputObjParamSet, Mandatory = true, ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// The Markdown document should be converted to a VT100 encoded string.
        /// </summary>
        [Parameter()]
        public SwitchParameter AsVT100EncodedString { get; set; }

        private const string PathParamSet = "PathParamSet";
        private const string LitPathParamSet = "LiteralParamSet";

        private const string InputObjParamSet = "InputObjParamSet";

        /// <summary>
        /// Override ProcessRecord
        /// </summary>
        protected override void ProcessRecord()
        {
            var conversionType = MarkdownConversionType.HTML;

            var mdOption = (SessionState.PSVariable.GetValue("MarkdownOptionInfo", new MarkdownOptionInfo())) as MarkdownOptionInfo;

            if(mdOption == null)
            {
                throw new InvalidOperationException();
            }

            if (AsVT100EncodedString)
            {
                conversionType = MarkdownConversionType.VT100;
            }

            switch (ParameterSetName)
            {
                case InputObjParamSet:
                    Object baseObj = InputObject.BaseObject;

                    var fileInfo = baseObj as FileInfo;
                    if (fileInfo != null)
                    {
                        WriteObject(
                            MarkdownConverter.Convert(
                                ReadContentFromFile(fileInfo.FullName).Result,
                                conversionType,
                                mdOption
                            )
                        );
                    }
                    else
                    {
                        var inpObj = baseObj as string;
                        if (inpObj != null)
                        {
                            WriteObject(MarkdownConverter.Convert(inpObj, conversionType, mdOption));
                        }
                        else
                        {
                            string errorMessage = StringUtil.Format(ConvertMarkdownStrings.InvalidInputObjectType, baseObj.GetType());
                            ErrorRecord errorRecord = new ErrorRecord(
                                new InvalidDataException(errorMessage),
                                "InvalidInputObject",
                                ErrorCategory.InvalidData,
                                InputObject);

                            WriteError(errorRecord);
                        }
                    }

                    break;

                case PathParamSet:
                    ConvertEachFile(Path, conversionType, isLiteral: false, optionInfo: mdOption);
                    break;

                case LitPathParamSet:
                    ConvertEachFile(LiteralPath, conversionType, isLiteral: true, optionInfo: mdOption);
                    break;
            }
        }

        private void ConvertEachFile(IEnumerable<string> paths, MarkdownConversionType conversionType, bool isLiteral, MarkdownOptionInfo optionInfo)
        {
            foreach (var path in paths)
            {
                // ResolvePath checks for file existence.
                var resolvedPaths = ResolvePath(path, isLiteral);

                foreach (var resolvedPath in resolvedPaths)
                {
                    WriteObject(
                            MarkdownConverter.Convert(
                                ReadContentFromFile(resolvedPath).Result,
                                conversionType,
                                optionInfo)
                        );
                }
            }
        }

        private async Task<string> ReadContentFromFile(string filePath)
        {
            Dbg.Diagnostics.Assert(File.Exists(filePath), "Caller should make sure the file exists.");

            using (StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                string mdContent = await reader.ReadToEndAsync();
                return mdContent;
            }

        }

        private List<string> ResolvePath(string path, bool isLiteral)
        {
            ProviderInfo provider = null;
            PSDriveInfo drive = null;
            List<string> resolvedPaths = new List<string>();

            try
            {
                if (isLiteral)
                {
                    resolvedPaths.Add(Context.SessionState.Path.GetUnresolvedProviderPathFromPSPath(path, out provider, out drive));
                }
                else
                {
                    resolvedPaths.AddRange(Context.SessionState.Path.GetResolvedProviderPathFromPSPath(path, out provider));
                }
            }
            catch (ItemNotFoundException infe)
            {
                string errorMessage = StringUtil.Format(ConvertMarkdownStrings.InputFileNotFound, path);
                var errorRecord = new ErrorRecord(
                    infe,
                    "FileNotFound",
                    ErrorCategory.ResourceUnavailable,
                    path);

                WriteError(errorRecord);
            }

            if (!provider.Name.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
            {
                string errorMessage = StringUtil.Format(ConvertMarkdownStrings.FileSystemPathsOnly, path);
                ErrorRecord errorRecord = new ErrorRecord(new ArgumentException(),
                                                                "OnlyFileSystemPathsSupported",
                                                                ErrorCategory.InvalidArgument,
                                                                path);
                WriteError(errorRecord);

                return null;
            }

            return resolvedPaths;
        }
    }
}
