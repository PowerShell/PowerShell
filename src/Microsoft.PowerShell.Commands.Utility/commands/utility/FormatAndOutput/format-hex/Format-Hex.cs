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
    [Cmdlet(VerbsCommon.Format, "Hex", SupportsShouldProcess = true, HelpUri ="https://go.microsoft.com/fwlink/?LinkId=526919")]
    [OutputType(typeof(Microsoft.PowerShell.Commands.ByteCollection))]
    [Alias ("fhx")]
    public sealed class FormatHex : PSCmdlet
    {
        private const int BUFFERSIZE = 16;

        #region Parameters

        /// <summary>
        /// Path of file(s) to process
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty()]
        public string[] Path { get; set; }

        /// <summary>
        /// Literal path of file to process
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "LiteralPath")]
        [ValidateNotNullOrEmpty()]
        [Alias("PSPath","LP")]
        public string[] LiteralPath { get; set; }

        /// <summary>
        /// Ojbect to process
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ByInputObject", ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Type of character encoding for InputObject
        /// </summary>
        [Parameter(ParameterSetName = "ByInputObject")]
        [ArgumentToEncodingTransformationAttribute()]
        [ArgumentCompletions(
            EncodingConversion.Ascii,
            EncodingConversion.BigEndianUnicode,
            EncodingConversion.OEM,
            EncodingConversion.Unicode,
            EncodingConversion.Utf7,
            EncodingConversion.Utf8,
            EncodingConversion.Utf8Bom,
            EncodingConversion.Utf8NoBom,
            EncodingConversion.Utf32
            )]
        [ValidateNotNullOrEmpty]
        public Encoding Encoding { get; set; } = ClrFacade.GetDefaultEncoding();

        /// <summary>
        /// This parameter is no-op
        /// </summary>
        [Parameter(ParameterSetName = "ByInputObject")]
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
        /// passes a copy of that array on to the ConvertToHexidecimal method to output.
        /// </summary>
        /// <param name="path"></param>
        private void ProcessFileContent(string path)
        {
            byte[] buffer = new byte[BUFFERSIZE];

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)))
                {
                    UInt32 offset = 0;
                    Int32 bytesRead = 0;

                    while ((bytesRead = reader.Read(buffer, 0, BUFFERSIZE)) > 0)
                    {
                        if (bytesRead == BUFFERSIZE)
                        {
                            // We are reusing the same buffer so if we save the output to a variable, the variable
                            // will just contain multiple references to the same buffer memory space (containing only the
                            // last bytes of the file read).  Copying the buffer allows us to pass the values on without
                            // overwriting previous values.
                            byte[] copyOfBuffer = new byte[16];
                            Array.Copy(buffer, 0, copyOfBuffer, 0, bytesRead);
                            ConvertToHexidecimal(copyOfBuffer, path, offset);
                        }
                        else
                        {
                            // Handle the case of a partial (and probably last) buffer.  Copies the bytes read into a new,
                            // shorter array so we do not have the extra bytes from the previous pass through at the end.
                            byte[] remainingBytes = new byte[bytesRead];
                            Array.Copy(buffer, 0, remainingBytes, 0, bytesRead);
                            ConvertToHexidecimal(remainingBytes, path, offset);
                        }
                        // Update offset value.
                        offset += (UInt32)bytesRead;
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
        /// that array on to the ConvertToHexidecimal method to output.
        /// </summary>
        /// <param name="inputObject"></param>
        private void ProcessObjectContent(PSObject inputObject)
        {
            Object obj = inputObject.BaseObject;
            byte[] inputBytes = null;
            if (obj is System.IO.FileSystemInfo)
            {
                string[] path = { ((FileSystemInfo)obj).FullName };
                List<string> pathsToProcess = ResolvePaths(path, true);
                ProcessPath(pathsToProcess);
            }

            else if (obj is string)
            {
                string inputString = obj.ToString();
                inputBytes = Encoding.GetBytes(inputString);
            }

            else if (obj is byte)
            {
                inputBytes = new byte[] { (byte)obj };
            }

            else if (obj is byte[])
            {
                inputBytes = ((byte[])obj);
            }

            else if (obj is Int32)
            {
                inputBytes = BitConverter.GetBytes((Int32)obj);
            }

            else if (obj is Int32[])
            {
                List<byte> inputStreamArray = new List<byte>();
                Int32[] inputInts = (Int32[])obj;
                foreach (Int32 value in inputInts)
                {
                    byte[] tempBytes = BitConverter.GetBytes(value);
                    inputStreamArray.AddRange(tempBytes);
                }
                inputBytes = inputStreamArray.ToArray();
            }

            else if (obj is Int64)
            {
                inputBytes = BitConverter.GetBytes((Int64)obj);
            }

            else if (obj is Int64[])
            {
                List<byte> inputStreamArray = new List<byte>();
                Int64[] inputInts = (Int64[])obj;
                foreach (Int64 value in inputInts)
                {
                    byte[] tempBytes = BitConverter.GetBytes(value);
                    inputStreamArray.AddRange(tempBytes);
                }
                inputBytes = inputStreamArray.ToArray();
            }

            // If the object type is not supported, throw an error. Once Serialization is
            // available on CoreCLR, other types will be supported.
            else
            {
                string errorMessage = StringUtil.Format(UtilityCommonStrings.FormatHexTypeNotSupported, obj.GetType());
                ErrorRecord errorRecord = new ErrorRecord(new ArgumentException(errorMessage),
                                                            "FormatHexTypeNotSupported",
                                                            ErrorCategory.InvalidArgument,
                                                            obj.GetType());
                WriteError(errorRecord);
            }

            if (inputBytes != null)
            {
                ConvertToHexidecimal(inputBytes, null, 0);
            }
        }

        #endregion

        #region Output

        /// <summary>
        /// Outputs the hexadecimial representaion of the of the input data.
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        private void ConvertToHexidecimal(byte[] inputBytes, string path, UInt32 offset)
        {
            if (inputBytes != null)
            {
                ByteCollection byteCollectionObject = new ByteCollection(offset, inputBytes, path);
                WriteObject(byteCollectionObject);
            }
        }

        #endregion
    }
}
