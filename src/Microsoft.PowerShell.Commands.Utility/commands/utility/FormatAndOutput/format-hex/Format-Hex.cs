// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text;
using System.Security;
using System.Management.Automation;
using System.Collections.Generic;
using System.Management.Automation.Internal;
// Once Serialization is available on CoreCLR: using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Displays the hexidecimal equivalent of the input data.
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "Hex", SupportsShouldProcess = true, HelpUri = "https://go.microsoft.com/fwlink/?LinkId=526919")]
    [OutputType(typeof(Microsoft.PowerShell.Commands.ByteCollection))]
    [Alias("fhx")]
    public sealed class FormatHex : PSCmdlet
    {
        private const int BUFFERSIZE = 16;

        #region Parameters

        /// <summary>
        /// Path of file(s) to process.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty()]
        public string[] Path { get; set; }

        /// <summary>
        /// Literal path of file to process.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LiteralPath")]
        [ValidateNotNullOrEmpty()]
        [Alias("PSPath", "LP")]
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
        [ArgumentToEncodingTransformationAttribute()]
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
            if (String.Equals(this.ParameterSetName, "ByInputObject", StringComparison.OrdinalIgnoreCase))
            {
                ProcessObjectContent(InputObject);
            }
            else
            {
                List<string> pathsToProcess = String.Equals(this.ParameterSetName, "LiteralPath", StringComparison.OrdinalIgnoreCase) ?
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
                            WriteHexidecimal(buffer.Slice(0, bytesRead), path, offset);
                            break;
                        }

                        WriteHexidecimal(buffer.Slice(0, bytesRead), path, offset);

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
            Object obj = inputObject.BaseObject;
            byte[] inputBytes = null;

            switch (obj)
            {
                case System.IO.FileSystemInfo fsi:
                    string[] path = { fsi.FullName };
                    List<string> pathsToProcess = ResolvePaths(path, true);
                    ProcessPath(pathsToProcess);
                    return;
                case string str:
                    inputBytes = Encoding.GetBytes(str);
                    break;
                case byte b:
                    inputBytes = new byte[] { b };
                    break;
                case byte[] byteArray:
                    inputBytes = byteArray;
                    break;
                case Int32 iInt32:
                    inputBytes = BitConverter.GetBytes(iInt32);
                    break;
                case Int32[] i32s:
                    int i32 = 0;
                    inputBytes = new byte[sizeof(Int32) * i32s.Length];
                    Span<byte> inputStreamArray32 = inputBytes;

                    foreach (Int32 value in i32s)
                    {
                        BitConverter.TryWriteBytes(inputStreamArray32.Slice(i32), value);
                        i32 += sizeof(Int32);
                    }

                    break;
                case Int64 iInt64:
                    inputBytes = BitConverter.GetBytes(iInt64);
                    break;
                case Int64[] inputInt64s:
                    int i64 = 0;
                    inputBytes = new byte[sizeof(Int64) * inputInt64s.Length];
                    Span<byte> inputStreamArray64 = inputBytes;

                    foreach (Int64 value in inputInt64s)
                    {
                        BitConverter.TryWriteBytes(inputStreamArray64.Slice(i64), value);
                        i64 += sizeof(Int64);
                    }

                    break;

                // If the object type is not supported, throw an error. Once Serialization is
                // available on CoreCLR, other types will be supported.
                default:
                {
                    string errorMessage = StringUtil.Format(UtilityCommonStrings.FormatHexTypeNotSupported, obj.GetType());
                    ErrorRecord errorRecord = new ErrorRecord(new ArgumentException(errorMessage),
                                                                "FormatHexTypeNotSupported",
                                                                ErrorCategory.InvalidArgument,
                                                                obj.GetType());
                    WriteError(errorRecord);
                    break;
                }
            }

            if (inputBytes != null)
            {
                int offset = Math.Min(inputBytes.Length, Offset < (long)int.MaxValue ? (int)Offset : int.MaxValue);
                int count = Math.Min(inputBytes.Length - offset, Count < (long)int.MaxValue ? (int)Count : int.MaxValue);
                if (offset != 0 || count != inputBytes.Length)
                {
                    WriteHexidecimal(inputBytes.AsSpan().Slice(offset, count), null, 0);
                }
                else
                {
                    WriteHexidecimal(inputBytes, null, 0);
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
        private void WriteHexidecimal(Span<byte> inputBytes, string path, Int64 offset)
        {
            ByteCollection byteCollectionObject = new ByteCollection((UInt64)offset, inputBytes.ToArray(), path);
            WriteObject(byteCollectionObject);
        }

        private void WriteHexidecimal(byte[] inputBytes, string path, Int64 offset)
        {
            ByteCollection byteCollectionObject = new ByteCollection((UInt64)offset, inputBytes, path);
            WriteObject(byteCollectionObject);
        }

        #endregion
    }
}
