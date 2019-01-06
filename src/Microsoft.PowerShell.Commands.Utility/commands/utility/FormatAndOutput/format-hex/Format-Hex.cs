// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
// Once Serialization is available on CoreCLR: using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Displays the hexadecimal equivalent of the input data.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Hex", HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526919")]
    [OutputType(typeof(Microsoft.PowerShell.Commands.ByteCollection))]
    [Alias("fhx")]
    public sealed class FormatHexCommand : PSCmdlet
    {
        private const int BUFFERSIZE = 16;

        /// <summary>
        /// For cases where a homogenous collection of bytes or other items are directly piped in, we collect all the
        /// bytes in a List&lt;byte&gt; and then output the formatted result all at once in EndProcessing()
        /// </summary>
        private List<byte> _inputBytes;

        /// <summary>
        /// If the input is determined to be heterogenous piped input or each input object turns out to be a complete
        /// array of items, we output each item as we receive it to avoid squashing output together in strange ways.
        /// </summary>
        private bool _isHeterogenousPipedInput = false;

        /// <summary>
        /// Keep track of prior input types to determine if we're given a heterogenous collection.
        /// </summary>
        private Type _lastInputType;

        #region Parameters

        /// <summary>
        /// Path of file(s) to process.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string[] Path { get; set; }

        /// <summary>
        /// Literal path of file to process.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LiteralPath", ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("PSPath", "LP", "FullName")]
        public string[] LiteralPath { get; set; }

        /// <summary>
        /// Object to process.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ByInputObject", ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Type of character encoding for InputObject.
        /// </summary>
        [Parameter(ParameterSetName = "ByInputObject")]
        [ArgumentToEncodingTransformationAttribute]
        [ArgumentEncodingCompletionsAttribute]
        [ValidateNotNullOrEmpty]
        public Encoding Encoding { get; set; } = ClrFacade.GetDefaultEncoding();

        /// <summary>
        /// Gets or sets count of bytes to read from the input stream.
        /// </summary>
        [Parameter]
        [ValidateRange(ValidateRangeKind.Positive)]
        public Int64 Count { get; set; } = Int64.MaxValue;

        /// <summary>
        /// Gets or sets offset of bytes to start reading the input stream from.
        /// </summary>
        [Parameter]
        [ValidateRange(ValidateRangeKind.NonNegative)]
        public Int64 Offset { get; set; }

        /// <summary>
        /// This parameter is no-op.
        /// </summary>
        [Parameter(ParameterSetName = "ByInputObject", DontShow = true)]
        [Obsolete("Raw parameter is deprecated.", true)]
        public SwitchParameter Raw { get; set; }

        #endregion

        #region Overrides

        /// <summary>
        /// Implements the ProcessRecord method for the FormatHex command.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (string.Equals(this.ParameterSetName, "ByInputObject", StringComparison.OrdinalIgnoreCase))
            {
                ProcessObjectContent(InputObject);
            }
            else
            {
                List<string> pathsToProcess = string.Equals(this.ParameterSetName, "LiteralPath", StringComparison.OrdinalIgnoreCase) ?
                                              ResolvePaths(LiteralPath, true) : ResolvePaths(Path, false);

                ProcessPath(pathsToProcess);
            }
        }

        /// <summary>
        /// Implements the EndProcessing method for the FormatHex command
        /// </summary>
        protected override void EndProcessing()
        {
            if (_inputBytes != null)
            {
                int offset = Math.Min(_inputBytes.Count, Offset < (long)int.MaxValue ? (int)Offset : int.MaxValue);
                int count = Math.Min(_inputBytes.Count - offset, Count < (long)int.MaxValue ? (int)Count : int.MaxValue);
                if (offset != 0 || count != _inputBytes.Count)
                {
                    WriteHexadecimal(_inputBytes.GetRange(offset, count).ToArray(), null, 0);
                }
                else
                {
                    WriteHexadecimal(_inputBytes.ToArray(), null, 0);
                }
            }
        }

        #endregion

        #region Paths

        /// <summary>
        /// Validate each path provided and if valid, add to array of paths to process.
        /// If path is a literal path it is added to the array to process; we cannot validate them until we
        /// try to process file contents.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="literalPath"></param>
        /// <returns></returns>
        private List<string> ResolvePaths(string[] path, bool literalPath)
        {
            List<string> pathsToProcess = new List<string>();
            ProviderInfo provider = null;
            PSDriveInfo drive = null;

            foreach (string currentPath in path)
            {
                List<string> newPaths = new List<string>();

                if (literalPath)
                {
                    newPaths.Add(Context.SessionState.Path.GetUnresolvedProviderPathFromPSPath(currentPath, out provider, out drive));
                }
                else
                {
                    try
                    {
                        newPaths.AddRange(Context.SessionState.Path.GetResolvedProviderPathFromPSPath(currentPath, out provider));
                    }
                    catch (ItemNotFoundException e)
                    {
                        if (!WildcardPattern.ContainsWildcardCharacters(currentPath))
                        {
                            ErrorRecord errorRecord = new ErrorRecord(e, "FileNotFound", ErrorCategory.ObjectNotFound, path);
                            WriteError(errorRecord);
                            continue;
                        }
                    }
                }

                if (!provider.Name.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
                {
                    // Write a non-terminating error message indicating that path specified is not supported.
                    string errorMessage = StringUtil.Format(UtilityCommonStrings.FormatHexOnlySupportsFileSystemPaths, currentPath);
                    ErrorRecord errorRecord = new ErrorRecord(new ArgumentException(errorMessage),
                                                                "FormatHexOnlySupportsFileSystemPaths",
                                                                ErrorCategory.InvalidArgument,
                                                                currentPath);
                    WriteError(errorRecord);
                    continue;
                }

                pathsToProcess.AddRange(newPaths);
            }

            return pathsToProcess;
        }

        /// <summary>
        /// Pass each valid path on to process its contents.
        /// </summary>
        /// <param name="pathsToProcess"></param>
        private void ProcessPath(List<string> pathsToProcess)
        {
            foreach (string path in pathsToProcess)
            {
                ProcessFileContent(path);
            }
        }

        /// <summary>
        /// Creates a binary reader that reads the file content into a buffer (byte[]) 16 bytes at a time, and
        /// passes a copy of that array on to the WriteHexidecimal method to output.
        /// </summary>
        /// <param name="path"></param>
        private void ProcessFileContent(string path)
        {
            Span<byte> buffer = stackalloc byte[BUFFERSIZE];

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    Int64 offset = Offset;
                    Int32 bytesRead = 0;
                    Int64 count = 0;

                    reader.BaseStream.Position = Offset;

                    while ((bytesRead = reader.Read(buffer)) > 0)
                    {
                        count += bytesRead;
                        if (count > Count)
                        {
                            bytesRead -= (int)(count - Count);
                            WriteHexadecimal(buffer.Slice(0, bytesRead), path, offset);
                            break;
                        }

                        WriteHexadecimal(buffer.Slice(0, bytesRead), path, offset);

                        offset += bytesRead;
                    }
                }
            }
            catch (IOException ioException)
            {
                // IOException takes care of FileNotFoundException, DirectoryNotFoundException, and PathTooLongException
                WriteError(new ErrorRecord(ioException, "FormatHexIOError", ErrorCategory.WriteError, path));
            }
            catch (ArgumentException argException)
            {
                WriteError(new ErrorRecord(argException, "FormatHexArgumentError", ErrorCategory.WriteError, path));
            }
            catch (NotSupportedException notSupportedException)
            {
                WriteError(new ErrorRecord(notSupportedException, "FormatHexPathRefersToANonFileDevice", ErrorCategory.InvalidArgument, path));
            }
            catch (SecurityException securityException)
            {
                WriteError(new ErrorRecord(securityException, "FormatHexUnauthorizedAccessError", ErrorCategory.PermissionDenied, path));
            }
        }

        #endregion

        #region InputObjects

        /// <summary>
        /// Creates a byte array from the object passed to the cmdlet (based on type) and passes
        /// that array on to the WriteHexidecimal method to output.
        /// </summary>
        /// <param name="inputObject"></param>
        private void ProcessObjectContent(PSObject inputObject)
        {
            dynamic baseObject = inputObject.BaseObject;
            Type baseType = baseObject.GetType();
            int elements = 1;
            bool isArray = false;
            bool isBool = false;
            bool isEnum = false;

            byte[] processResult = null;
            if (baseType.IsArray)
            {
                baseType = baseType.GetElementType();
                elements = (int)baseObject.Length;
                isArray = true;
                _isHeterogenousPipedInput = true;
            }

            if (baseType == typeof(FileInfo))
            {
                List<string> paths = new List<string>();
                if (!isArray)
                {
                    paths.Add(baseObject.FullName);
                }
                else
                {
                    foreach (FileInfo file in baseObject)
                    {
                        paths.Add(file.FullName);
                    }
                }

                List<string> pathsToProcess = new List<string>(ResolvePaths(paths.ToArray(), true));
                ProcessPath(pathsToProcess);
                return;
            }

            if (baseType == typeof(string))
            {
                _isHeterogenousPipedInput = true;

                if (!isArray)
                {
                    baseObject = new string[] { baseObject };
                }

                foreach (string str in (Array)baseObject)
                {
                    processResult = Encoding.GetBytes(str);
                    int offset = Math.Min(processResult.Length, Offset < (long)int.MaxValue ? (int)Offset : int.MaxValue);
                    int count = Math.Min(processResult.Length - offset, Count < (long)int.MaxValue ? (int)Count : int.MaxValue);
                    if (offset != 0 || count != processResult.Length)
                    {
                        WriteHexadecimal(processResult.AsSpan().Slice(offset, count), null, 0);
                    }
                    else
                    {
                        WriteHexadecimal(processResult, null, 0);
                    }
                }

                return;
            }

            if (baseType.IsEnum)
            {
                baseType = baseType.GetEnumUnderlyingType();
                isEnum = true;
            }

            if (!_isHeterogenousPipedInput)
            {
                if (_lastInputType != null && baseType != _lastInputType)
                {
                    _isHeterogenousPipedInput = true;
                }
                else
                {
                    _lastInputType = baseType;
                }
            }

            if (baseType.IsPrimitive && elements > 0)
            {
                if (baseType == typeof(bool))
                {
                    isBool = true;
                }

                var elementSize = Marshal.SizeOf(baseType);
                processResult = new byte[elementSize * elements];
                if (!isArray)
                {
                    baseObject = new object[1] { baseObject };
                }

                int index = 0;
                foreach (dynamic item in (Array)baseObject)
                {
                    if (elementSize == 1)
                    {
                        processResult[index] = (byte)item;
                    }
                    else
                    {
                        // bool is 4 bytes, apparently -- @lzybkr
                        dynamic byteConverterInput;
                        if (isEnum)
                        {
                            byteConverterInput = Convert.ChangeType(item, baseType);
                        }
                        else if (isBool)
                        {
                            byteConverterInput = Convert.ToInt32(item);
                        }
                        else
                        {
                            byteConverterInput = item;
                        }

                        byte[] bytes = BitConverter.GetBytes(byteConverterInput);
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            processResult[i + index] = bytes[i];
                        }
                    }

                    index += elementSize;
                }
            }
            else
            {
                // Type is neither any kind of primitive, enum, string, nor file, so we write an error
                string errorMessage = StringUtil.Format(UtilityCommonStrings.FormatHexTypeNotSupported, baseObject.GetType());
                ErrorRecord errorRecord = new ErrorRecord(
                    new ArgumentException(errorMessage),
                    "FormatHexTypeNotSupported",
                    ErrorCategory.InvalidArgument,
                    baseObject.GetType());
                WriteError(errorRecord);
                return;
            }

            if (_isHeterogenousPipedInput)
            {
                if (_inputBytes != null)
                {
                    // If we've been collecting individual bytes now, and some other input has been detected,
                    // we revert to heterogenous behaviour
                    foreach (byte b in _inputBytes)
                    {
                        WriteHexadecimal(new byte[] { b }, null, 0);
                    }

                    _inputBytes = null;
                }

                if (processResult != null)
                {
                    int offset = Math.Min(processResult.Length, Offset < (long)int.MaxValue ? (int)Offset : int.MaxValue);
                    int count = Math.Min(processResult.Length - offset, Count < (long)int.MaxValue ? (int)Count : int.MaxValue);
                    if (offset != 0 || count != processResult.Length)
                    {
                        WriteHexadecimal(processResult.AsSpan().Slice(offset, count), null, 0);
                    }
                    else
                    {
                        WriteHexadecimal(processResult, null, 0);
                    }
                }
            }
            else
            {
                if (_inputBytes == null)
                {
                    _inputBytes = new List<byte>(processResult);
                }
                else
                {
                    _inputBytes.AddRange(processResult);
                }
            }
        }

        #endregion

        #region Output

        /// <summary>
        /// Outputs the hexadecimial representaion of the input data.
        /// </summary>
        /// <param name="inputBytes">Bytes for the hexadecimial representaion.</param>
        /// <param name="path">File path.</param>
        /// <param name="offset">Offset in the file.</param>
        private void WriteHexadecimal(Span<byte> inputBytes, string path, Int64 offset)
        {
            ByteCollection byteCollectionObject = new ByteCollection((UInt64)offset, inputBytes.ToArray(), path);
            WriteObject(byteCollectionObject);
        }

        private void WriteHexadecimal(byte[] inputBytes, string path, Int64 offset)
        {
            ByteCollection byteCollectionObject = new ByteCollection((UInt64)offset, inputBytes, path);
            WriteObject(byteCollectionObject);
        }

        #endregion
    }
}
