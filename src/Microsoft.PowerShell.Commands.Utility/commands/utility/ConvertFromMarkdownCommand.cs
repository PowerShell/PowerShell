// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Security;
using System.Threading.Tasks;

using Microsoft.PowerShell.MarkdownRender;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Converts a Markdown string to a MarkdownInfo object.
    /// The conversion can be done into a HTML text or VT100 encoding string.
    /// </summary>
    [Cmdlet(
        VerbsData.ConvertFrom, "Markdown",
        DefaultParameterSetName = PathParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?linkid=2006503")]
    [OutputType(typeof(Microsoft.PowerShell.MarkdownRender.MarkdownInfo))]
    public class ConvertFromMarkdownCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets path to the file to convert from Markdown to MarkdownInfo.
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = PathParameterSet, Mandatory = true, Position = 0)]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the path to the file to convert from Markdown to MarkdownInfo.
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Alias("PSPath", "LP")]
        [Parameter(ParameterSetName = LiteralPathParameterSet, Mandatory = true)]
        public string[] LiteralPath { get; set; }

        /// <summary>
        /// Gets or sets the InputObject of type System.IO.FileInfo or string with content to convert from Markdown to MarkdownInfo.
        /// </summary>
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = InputObjParamSet, Mandatory = true, ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Gets or sets if the Markdown document should be converted to a VT100 encoded string.
        /// </summary>
        [Parameter]
        public SwitchParameter AsVT100EncodedString { get; set; }

        private const string PathParameterSet = "PathParamSet";
        private const string LiteralPathParameterSet = "LiteralParamSet";
        private const string InputObjParamSet = "InputObjParamSet";

        private MarkdownConversionType _conversionType = MarkdownConversionType.HTML;
        private PSMarkdownOptionInfo _mdOption = null;

        /// <summary>
        /// Read the PSMarkdownOptionInfo set in SessionState.
        /// </summary>
        protected override void BeginProcessing()
        {
            _mdOption = PSMarkdownOptionInfoCache.Get(this.CommandInfo);

            bool? supportsVT100 = this.Host?.UI.SupportsVirtualTerminal;

            // supportsVT100 == null if the host is null.
            // supportsVT100 == false if host does not support VT100.
            if (supportsVT100 != true)
            {
                _mdOption.EnableVT100Encoding = false;
            }

            if (AsVT100EncodedString)
            {
                _conversionType = MarkdownConversionType.VT100;
            }
        }

        /// <summary>
        /// Override ProcessRecord.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case InputObjParamSet:
                    object baseObj = InputObject.BaseObject;

                    if (baseObj is FileInfo fileInfo)
                    {
                        WriteObject(
                            MarkdownConverter.Convert(
                                ReadContentFromFile(fileInfo.FullName)?.Result,
                                _conversionType,
                                _mdOption));
                    }
                    else if (baseObj is string inpObj)
                    {
                        WriteObject(MarkdownConverter.Convert(inpObj, _conversionType, _mdOption));
                    }
                    else
                    {
                        string errorMessage = StringUtil.Format(ConvertMarkdownStrings.InvalidInputObjectType, baseObj.GetType());
                        ErrorRecord errorRecord = new(
                            new InvalidDataException(errorMessage),
                            "InvalidInputObject",
                            ErrorCategory.InvalidData,
                            InputObject);

                        WriteError(errorRecord);
                    }

                    break;

                case PathParameterSet:
                    ConvertEachFile(Path, _conversionType, isLiteral: false, optionInfo: _mdOption);
                    break;

                case LiteralPathParameterSet:
                    ConvertEachFile(LiteralPath, _conversionType, isLiteral: true, optionInfo: _mdOption);
                    break;
            }
        }

        private void ConvertEachFile(IEnumerable<string> paths, MarkdownConversionType conversionType, bool isLiteral, PSMarkdownOptionInfo optionInfo)
        {
            foreach (var path in paths)
            {
                var resolvedPaths = ResolvePath(path, isLiteral);

                foreach (var resolvedPath in resolvedPaths)
                {
                    WriteObject(
                            MarkdownConverter.Convert(
                                ReadContentFromFile(resolvedPath)?.Result,
                                conversionType,
                                optionInfo));
                }
            }
        }

        private async Task<string> ReadContentFromFile(string filePath)
        {
            ErrorRecord errorRecord = null;

            try
            {
                using (StreamReader reader = new(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    string mdContent = await reader.ReadToEndAsync();
                    return mdContent;
                }
            }
            catch (FileNotFoundException fnfe)
            {
                errorRecord = new ErrorRecord(
                    fnfe,
                    "FileNotFound",
                    ErrorCategory.ResourceUnavailable,
                    filePath);
            }
            catch (SecurityException se)
            {
                errorRecord = new ErrorRecord(
                    se,
                    "FileSecurityError",
                    ErrorCategory.SecurityError,
                    filePath);
            }
            catch (UnauthorizedAccessException uae)
            {
                errorRecord = new ErrorRecord(
                    uae,
                    "FileUnauthorizedAccess",
                    ErrorCategory.SecurityError,
                    filePath);
            }

            WriteError(errorRecord);
            return null;
        }

        private List<string> ResolvePath(string path, bool isLiteral)
        {
            ProviderInfo provider = null;
            PSDriveInfo drive = null;
            List<string> resolvedPaths = new();

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
                ErrorRecord errorRecord = new(
                    new ArgumentException(),
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
