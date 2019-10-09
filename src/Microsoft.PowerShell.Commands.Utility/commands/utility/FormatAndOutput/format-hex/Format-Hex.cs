// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
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
    public sealed class FormatHex : PSCmdlet
    {
        private const int BUFFERSIZE = 16;

        #region Parameters

        /// <summary>
        /// Gets or sets the path of file(s) to process.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty()]
        public string[] Path { get; set; }

        /// <summary>
        /// Gets or sets the literal path of file to process.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LiteralPath")]
        [ValidateNotNullOrEmpty()]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath { get; set; }

        /// <summary>
        /// Gets or sets the object to process.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ByInputObject", ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Gets or sets the type of character encoding for InputObject.
        /// </summary>
        [Parameter(ParameterSetName = "ByInputObject")]
        [ArgumentToEncodingTransformationAttribute()]
        [ArgumentEncodingCompletionsAttribute]
        [ValidateNotNullOrEmpty]
        public Encoding Encoding { get; set; } = ClrFacade.GetDefaultEncoding();

        /// <summary>
        /// Gets or sets count of bytes to read from the input stream.
        /// </summary>
        [Parameter]
        [ValidateRange(ValidateRangeKind.Positive)]
        public long Count { get; set; } = long.MaxValue;

        /// <summary>
        /// Gets or sets offset of bytes to start reading the input stream from.
        /// </summary>
        [Parameter]
        [ValidateRange(ValidateRangeKind.NonNegative)]
        public long Offset { get; set; }

        /// <summary>
        /// Gets or sets whether the file input should be swallowed as is. This parameter is no-op, deprecated.
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

        #endregion

        #region Paths

        /// <summary>
        /// Validate each path provided and if valid, add to array of paths to process.
        /// If path is a literal path it is added to the array to process; we cannot validate them until we
        /// try to process file contents.
        /// </summary>
        /// <param name="path">The file path to resolve.</param>
        /// <param name="literalPath">The paths to process.</param>
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
                    ErrorRecord errorRecord = new ErrorRecord(
                        new ArgumentException(errorMessage),
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
        /// <param name="pathsToProcess">The paths to process.</param>
        private void ProcessPath(List<string> pathsToProcess)
        {
            foreach (string path in pathsToProcess)
            {
                ProcessFileContent(path);
            }
        }

        /// <summary>
        /// Creates a binary reader that reads the file content into a buffer (byte[]) 16 bytes at a time, and
        /// passes a copy of that array on to the WriteHexadecimal method to output.
        /// </summary>
        /// <param name="path">The file path to retrieve content from for processing.</param>
        private void ProcessFileContent(string path)
        {
            Span<byte> buffer = stackalloc byte[BUFFERSIZE];

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    long offset = Offset;
                    int bytesRead = 0;
                    long count = 0;

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
        /// that array on to the WriteHexadecimal method to output.
        /// </summary>
        /// <param name="inputObject">The pipeline input object being processed.</param>
        private void ProcessObjectContent(PSObject inputObject)
        {
            object obj = inputObject.BaseObject;

            if (obj is System.IO.FileSystemInfo fsi)
            {
                string[] path = { fsi.FullName };
                List<string> pathsToProcess = ResolvePaths(path, true);
                ProcessPath(pathsToProcess);
                return;
            }

            byte[] inputBytes = ConvertToByteArray(obj);

            if (inputBytes != null)
            {
                int offset = Math.Min(inputBytes.Length, Offset < (long)int.MaxValue ? (int)Offset : int.MaxValue);
                int count = Math.Min(inputBytes.Length - offset, Count < (long)int.MaxValue ? (int)Count : int.MaxValue);
                if (offset != 0 || count != inputBytes.Length)
                {
                    WriteHexadecimal(inputBytes.AsSpan().Slice(offset, count), null, 0);
                }
                else
                {
                    WriteHexadecimal(inputBytes, null, 0);
                }
            }
            else
            {
                string errorMessage = StringUtil.Format(UtilityCommonStrings.FormatHexTypeNotSupported, obj.GetType());
                ErrorRecord errorRecord = new ErrorRecord(
                    new ArgumentException(errorMessage),
                    "FormatHexTypeNotSupported",
                    ErrorCategory.InvalidArgument,
                    obj.GetType());
                WriteError(errorRecord);
            }
        }

        /// <summary>
        /// Converts the input object to a byte array based on the underlying type for basic value types and strings,
        /// as well as enum values or arrays.
        /// </summary>
        /// <param name="inputObject">The object to convert.</param>
        /// <returns>Returns a byte array of the input values, or null if there is no available conversion path.</returns>
        private byte[] ConvertToByteArray(object inputObject)
        {
            if (inputObject is string str)
            {
                return Encoding.GetBytes(str);
            }

            var baseType = inputObject.GetType();
            byte[] result = null;
            int elements = 1;
            bool isArray = false;
            bool isBool = false;
            bool isEnum = false;
            if (baseType.IsArray)
            {
                baseType = baseType.GetElementType();
                dynamic dynamicObject = inputObject;
                elements = (int)dynamicObject.Length;
                isArray = true;
            }

            if (baseType.IsEnum)
            {
                baseType = baseType.GetEnumUnderlyingType();
                isEnum = true;
            }

            if (baseType.IsPrimitive && elements > 0)
            {
                if (baseType == typeof(bool))
                {
                    isBool = true;
                }

                var elementSize = Marshal.SizeOf(baseType);
                result = new byte[elementSize * elements];
                if (!isArray)
                {
                    inputObject = new object[] { inputObject };
                }

                int index = 0;
                foreach (dynamic obj in (Array)inputObject)
                {
                    if (elementSize == 1)
                    {
                        result[index] = (byte)obj;
                    }
                    else
                    {
                        dynamic toBytes;
                        if (isEnum)
                        {
                            toBytes = Convert.ChangeType(obj, baseType);
                        }
                        else if (isBool)
                        {
                            // bool is 1 byte apparently
                            toBytes = Convert.ToByte(obj);
                        }
                        else
                        {
                            toBytes = obj;
                        }

                        var bytes = BitConverter.GetBytes(toBytes);
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            result[i + index] = bytes[i];
                        }
                    }

                    index += elementSize;
                }
            }

            return result;
        }

        #endregion

        #region Output

        /// <summary>
        /// Outputs the hexadecimial representation of the input data.
        /// </summary>
        /// <param name="inputBytes">Bytes for the hexadecimial representation.</param>
        /// <param name="path">File path.</param>
        /// <param name="offset">Offset in the file.</param>
        private void WriteHexadecimal(Span<byte> inputBytes, string path, long offset)
        {
            ByteCollection byteCollectionObject = new ByteCollection((ulong)offset, inputBytes.ToArray(), path);
            WriteObject(byteCollectionObject);
        }

        private void WriteHexadecimal(byte[] inputBytes, string path, long offset)
        {
            ByteCollection byteCollectionObject = new ByteCollection((ulong)offset, inputBytes, path);
            WriteObject(byteCollectionObject);
        }

        #endregion
    }
}
