/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;


[module: SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type", Target = "Microsoft.PowerShell.Commands.ByteCollection")]

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The base class for all command processor classes. It provides
    /// abstract methods to execute a command.
    /// </summary>
    internal static class UtilityCommon
    {
#if CORECLR
        // AccessViolationException/StackOverflowException Not In CoreCLR.
        // The CoreCLR team told us to not check for these exceptions because they
        // usually won't be caught.
        internal static void CheckForSevereException(PSCmdlet cmdlet, Exception e) { }
#else
        // Keep in sync:
        // S.M.A.CommandProcessorBase.CheckForSevereException
        // S.M.A.Internal.ConsoleHost.CheckForSevereException
        // S.M.A.Commands.CommandsCommon.CheckForSevereException
        // S.M.A.Commands.UtilityCommon.CheckForSevereException
        /// <summary>
        /// Checks whether the exception is a severe exception which should
        /// cause immediate process failure.
        /// </summary>
        /// <param name="cmdlet">can be null</param>
        /// <param name="e"></param>
        /// <remarks>
        /// CB says 02/23/2005: I personally would err on the side
        /// of treating OOM like an application exception, rather than
        /// a critical system failure.I think this will be easier to justify
        /// in Orcas, if we tease apart the two cases of OOM better.
        /// But even in Whidbey, how likely is it that we couldnt JIT
        /// some backout code?  At that point, the process or possibly
        /// the machine is likely to stop executing soon no matter
        /// what you do in this routine.  So I would just consider
        /// AccessViolationException.  (I understand why you have SO here,
        /// at least temporarily).
        /// </remarks>
        internal static void CheckForSevereException(PSCmdlet cmdlet, Exception e)
        {
            if (e is AccessViolationException || e is StackOverflowException)
            {
                try
                {
                    if (!alreadyFailing)
                    {
                        alreadyFailing = true;

                        // Get the ExecutionContext from the thread.
                        ExecutionContext context =
                            (null != cmdlet)
                                ? cmdlet.Context
                                : LocalPipeline.GetExecutionContextFromTLS();

                        // Log a command health event for this critical error.
                        MshLog.LogCommandHealthEvent(context, e, Severity.Critical);
                    }
                }
                finally
                {
                    if (!designForTestability_SkipFailFast)
                        WindowsErrorReporting.FailFast(e);
                }
            }
        }
        private static bool alreadyFailing = false;
        // 2005/04/22-JonN Adding this capability so that we can
        // get some test coverage on this function
        private static bool designForTestability_SkipFailFast = false;
#endif
        /// <summary>
        /// Converts the textencodingtype enum value to the corresponding encoding
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal static Encoding GetEncodingFromEnum(TextEncodingType type)
        {
            Encoding encoding = Encoding.ASCII;

            switch (type)
            {
                case TextEncodingType.String:
                    encoding = Encoding.Unicode;
                    break;

                case TextEncodingType.Unicode:
                    encoding = Encoding.Unicode;
                    break;

                case TextEncodingType.BigEndianUnicode:
                    encoding = Encoding.BigEndianUnicode;
                    break;

                case TextEncodingType.Utf8:
                    encoding = Encoding.UTF8;
                    break;

                case TextEncodingType.Utf7:
                    encoding = Encoding.UTF7;
                    break;

                case TextEncodingType.Ascii:
                    encoding = Encoding.ASCII;
                    break;

                default:
                    // Default to unicode encoding
                    encoding = Encoding.ASCII;
                    break;
            }

            return encoding;
        }
    } // class UtilityCommon
    /// <summary>
    /// abc
    /// </summary>
    public enum TextEncodingType
    {
        /// <summary>
        /// No encoding.
        /// </summary>
        Unknown,

        /// <summary>
        /// Unicode encoding.
        /// </summary>
        String,

        /// <summary>
        /// Unicode encoding.
        /// </summary>
        Unicode,

        /// <summary>
        /// Byte encoding.
        /// </summary>
        Byte,

        /// <summary>
        /// Big Endian Unicode encoding.
        /// </summary>
        BigEndianUnicode,

        /// <summary>
        /// UTF8 encoding.
        /// </summary>
        Utf8,

        /// <summary>
        /// UTF7 encoding.
        /// </summary>
        Utf7,

        /// <summary>
        /// ASCII encoding.
        /// </summary>
        Ascii,
    }

    /// <summary>
    /// Utility class to contain resources for the Microsoft.PowerShell.Utility module
    /// </summary>
    public static class UtilityResources
    {
        /// <summary>
        /// </summary>
        public static string PathDoesNotExist { get { return UtilityCommonStrings.PathDoesNotExist; } }

        /// <summary>
        /// </summary>
        public static string FileReadError { get { return UtilityCommonStrings.FileReadError; } }

        /// <summary>
        /// Error message to indicate that Format-Hex cmdlet does not directly support the type provided as input.
        /// </summary>
        public static string FormatHexTypeNotSupported { get { return UtilityCommonStrings.FormatHexTypeNotSupported; } }

        /// <summary>
        /// Error message to indicate that Format-Hex cmdlet does not directly support the type provided as input.
        /// </summary>
        public static string FormatHexResolvePathError { get { return UtilityCommonStrings.FormatHexResolvePathError; } }

        /// <summary>
        /// The resource string used to indicate 'PATH:' in the formating header.
        /// </summary>
        public static string FormatHexPathPrefix { get { return UtilityCommonStrings.FormatHexPathPrefix; } }

        /// <summary>
        /// Error message to indicate that requested algorithm  is not supported on the target platform.
        /// </summary>
        public static string AlgorithmTypeNotSupported { get { return UtilityCommonStrings.AlgorithmTypeNotSupported; } }

        /// <summary>
        /// The file '{0}' could not be parsed as a PowerShell Data File.
        /// </summary>
        public static string CouldNotParseAsPowerShellDataFile { get { return UtilityCommonStrings.CouldNotParseAsPowerShellDataFile; } }
    }

    /// <summary>
    /// ByteCollection is used as a wrapper class for the collection of bytes.
    /// </summary>
    public class ByteCollection
    {
        /// <summary>
        /// ByteCollection constructor.
        /// </summary>
        /// <param name="offset">The Offset address to be used while displaying the bytes in the collection.</param>
        /// <param name="value">Underlying bytes stored in the collection.</param>
        /// <param name="path">Indicates the path of the file whose contents are wrapped in the ByteCollection.</param>
        public ByteCollection(UInt32 offset, Byte[] value, string path)
        {
            this.Offset = offset;
            _initialOffSet = offset;
            this.Bytes = value;
            this.Path = path;
        }

        /// <summary>
        /// ByteCollection constructor.
        /// </summary>
        /// <param name="offset">The Offset address to be used while displaying the bytes in the collection.</param>
        /// <param name="value">Underlying bytes stored in the collection.</param>
        public ByteCollection(UInt32 offset, Byte[] value)
        {
            this.Offset = offset;
            _initialOffSet = offset;
            this.Bytes = value;
        }

        /// <summary>
        /// ByteCollection constructor.
        /// </summary>
        /// <param name="value">Underlying bytes stored in the collection.</param>
        public ByteCollection(Byte[] value)
        {
            this.Bytes = value;
        }

        /// <summary>
        /// The Offset address to be used while displaying the bytes in the collection.
        /// </summary>
        public UInt32 Offset { get; private set; }
        private UInt32 _initialOffSet = 0;

        /// <summary>
        /// Underlying bytes stored in the collection.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Byte[] Bytes { get; private set; }

        /// <summary>
        /// Indicates the path of the file whose contents are wrapped in the ByteCollection.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Displays the hexadecimal format of the bytes stored in the collection.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            StringBuilder nextLine = new StringBuilder();
            StringBuilder asciiEnd = new StringBuilder();

            if (Bytes.Length > 0)
            {
                UInt32 charCounter = 0;

                // ToString() in invoked thrice by the F&O for the same content. 
                // Hence making sure that Offset is not getting incremented thrice for the same bytes being displayed.
                Offset = _initialOffSet;

                nextLine.AppendFormat("{0:X2}   ", CultureInfo.InvariantCulture.TextInfo.ToUpper(Convert.ToString(Offset, 16)).PadLeft(8, '0'));
                foreach (Byte currentByte in Bytes)
                {
                    // Display each byte, in 2-digit hexadecimal, and add that to the left-hand side.
                    nextLine.AppendFormat("{0:X2} ", currentByte);

                    // If the character is printable, add its ascii representation to
                    // the right-hand side.  Otherwise, addeter  a dot to the right hand side.
                    if ((currentByte >= 0x20) && (currentByte <= 0xFE))
                    {
                        asciiEnd.Append((char)currentByte);
                    }
                    else
                    {
                        asciiEnd.Append('.');
                    }
                    charCounter++;

                    // If we've hit the end of a line, combine the right half with the
                    // left half, and start a new line.
                    if ((charCounter % 16) == 0)
                    {
                        result.Append(nextLine.ToString() + " " + asciiEnd.ToString());
                        nextLine.Clear();
                        asciiEnd.Clear();
                        Offset += 0x10;
                        nextLine.AppendFormat("{0:X2}   ", CultureInfo.InvariantCulture.TextInfo.ToUpper(Convert.ToString(Offset, 16)).PadLeft(8, '0'));

                        // Adding a newline to support long inputs strings flowing through InputObject parameterset.
                        if ((charCounter <= Bytes.Length) && string.IsNullOrEmpty(this.Path))
                        {
                            result.Append("\r\n");
                        }
                    }
                }

                // At the end of the file, we might not have had the chance to output
                // the end of the line yet.  Only do this if we didn't exit on the 16-byte
                // boundary, though.
                if ((charCounter % 16) != 0)
                {
                    while ((charCounter % 16) != 0)
                    {
                        nextLine.Append("   ");
                        asciiEnd.Append(' ');
                        charCounter++;
                    }
                    result.Append(nextLine.ToString() + " " + asciiEnd.ToString());
                }
            }

            return result.ToString();
        }
    }
} // namespace Microsoft.PowerShell.Commands

