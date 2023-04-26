// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Text;

[module: SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type", Target = "~T:Microsoft.PowerShell.Commands.ByteCollection")]

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Don't use! The API is obsolete!.
    /// </summary>
    [Obsolete("This class is included in this SDK for completeness only. The members of this class cannot be used directly, nor should this class be used to derive other classes.", true)]
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
        /// Big Endian UTF32 encoding.
        /// </summary>
        BigEndianUTF32,

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
    /// Utility class to contain resources for the Microsoft.PowerShell.Utility module.
    /// </summary>
    [Obsolete("This class is obsolete", true)]
    public static class UtilityResources
    {
        /// <summary>
        /// </summary>
        public static string PathDoesNotExist { get { return UtilityCommonStrings.PathDoesNotExist; } }

        /// <summary>
        /// </summary>
        public static string FileReadError { get { return UtilityCommonStrings.FileReadError; } }

        /// <summary>
        /// The resource string used to indicate 'PATH:' in the formatting header.
        /// </summary>
        public static string FormatHexPathPrefix { get { return UtilityCommonStrings.FormatHexPathPrefix; } }

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
        /// Initializes a new instance of the <see cref="ByteCollection"/> class.
        /// </summary>
        /// <param name="offset">The Offset address to be used while displaying the bytes in the collection.</param>
        /// <param name="value">Underlying bytes stored in the collection.</param>
        /// <param name="path">Indicates the path of the file whose contents are wrapped in the ByteCollection.</param>
        [Obsolete("The constructor is deprecated.", true)]
        public ByteCollection(uint offset, byte[] value, string path)
            : this((ulong)offset, value, path)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteCollection"/> class.
        /// </summary>
        /// <param name="offset">The Offset address to be used while displaying the bytes in the collection.</param>
        /// <param name="value">Underlying bytes stored in the collection.</param>
        /// <param name="path">Indicates the path of the file whose contents are wrapped in the ByteCollection.</param>
        public ByteCollection(ulong offset, byte[] value, string path)
        {
            if (value == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(value));
            }

            Offset64 = offset;
            Bytes = value;
            Path = path;
            Label = path;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteCollection"/> class.
        /// </summary>
        /// <param name="offset">The Offset address to be used while displaying the bytes in the collection.</param>
        /// <param name="value">Underlying bytes stored in the collection.</param>
        [Obsolete("The constructor is deprecated.", true)]
        public ByteCollection(uint offset, byte[] value)
            : this((ulong)offset, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteCollection"/> class.
        /// </summary>
        /// <param name="offset">The Offset address to be used while displaying the bytes in the collection.</param>
        /// <param name="value">Underlying bytes stored in the collection.</param>
        public ByteCollection(ulong offset, byte[] value)
        {
            if (value == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(value));
            }

            Offset64 = offset;
            Bytes = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteCollection"/> class.
        /// </summary>
        /// <param name="offset">The Offset address to be used while displaying the bytes in the collection.</param>
        /// <param name="label">
        /// The label for the byte group. This may be a file path or a formatted identifying string for the group.
        /// </param>
        /// <param name="value">Underlying bytes stored in the collection.</param>
        public ByteCollection(ulong offset, string label, byte[] value)
            : this(offset, value)
        {
            Label = label;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteCollection"/> class.
        /// </summary>
        /// <param name="value">Underlying bytes stored in the collection.</param>
        public ByteCollection(byte[] value)
        {
            if (value == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(value));
            }

            Bytes = value;
        }

        /// <summary>
        /// Gets the Offset address to be used while displaying the bytes in the collection.
        /// </summary>
        [Obsolete("The property is deprecated, please use Offset64 instead.", true)]
        public uint Offset
        {
            get
            {
                return (uint)Offset64;
            }

            private set
            {
                Offset64 = value;
            }
        }

        /// <summary>
        /// Gets the Offset address to be used while displaying the bytes in the collection.
        /// </summary>
        public ulong Offset64 { get; private set; }

        /// <summary>
        /// Gets underlying bytes stored in the collection.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Bytes { get; }

        /// <summary>
        /// Gets the path of the file whose contents are wrapped in the ByteCollection.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the hexadecimal representation of the <see cref="Offset64"/> value.
        /// </summary>
        public string HexOffset => string.Create(CultureInfo.CurrentCulture, $"{Offset64:X16}");

        /// <summary>
        /// Gets the type of the input objects used to create the <see cref="ByteCollection"/>.
        /// </summary>
        public string Label { get; }

        private const int BytesPerLine = 16;

        private string _hexBytes = string.Empty;

        /// <summary>
        /// Gets a space-delimited string of the <see cref="Bytes"/> in this <see cref="ByteCollection"/>
        /// in hexadecimal format.
        /// </summary>
        public string HexBytes
        {
            get
            {
                if (_hexBytes == string.Empty)
                {
                    StringBuilder line = new(BytesPerLine * 3);

                    foreach (var currentByte in Bytes)
                    {
                        line.AppendFormat(CultureInfo.CurrentCulture, "{0:X2} ", currentByte);
                    }

                    _hexBytes = line.ToString().Trim();
                }

                return _hexBytes;
            }
        }

        private string _ascii = string.Empty;

        /// <summary>
        /// Gets the ASCII string representation of the <see cref="Bytes"/> in this <see cref="ByteCollection"/>.
        /// </summary>
        /// <value></value>
        public string Ascii
        {
            get
            {
                if (_ascii == string.Empty)
                {
                    StringBuilder ascii = new(BytesPerLine);

                    foreach (var currentByte in Bytes)
                    {
                        var currentChar = (char)currentByte;
                        if (currentChar == 0x0)
                        {
                            ascii.Append(' ');
                        }
                        else if (char.IsControl(currentChar))
                        {
                            ascii.Append((char)0xFFFD);
                        }
                        else
                        {
                            ascii.Append(currentChar);
                        }
                    }

                    _ascii = ascii.ToString();
                }

                return _ascii;
            }
        }

        /// <summary>
        /// Displays the hexadecimal format of the bytes stored in the collection.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            const int BytesPerLine = 16;
            const string LineFormat = "{0:X16}   ";

            // '16 + 3' comes from format "{0:X16}   ".
            // '16' comes from '[Uint64]::MaxValue.ToString("X").Length'.
            StringBuilder nextLine = new(16 + 3 + (BytesPerLine * 3));
            StringBuilder asciiEnd = new(BytesPerLine);

            // '+1' comes from 'result.Append(nextLine.ToString() + " " + asciiEnd.ToString());' below.
            StringBuilder result = new(nextLine.Capacity + asciiEnd.Capacity + 1);

            if (Bytes.Length > 0)
            {
                long charCounter = 0;

                var currentOffset = Offset64;

                nextLine.AppendFormat(CultureInfo.InvariantCulture, LineFormat, currentOffset);

                foreach (byte currentByte in Bytes)
                {
                    // Display each byte, in 2-digit hexadecimal, and add that to the left-hand side.
                    nextLine.AppendFormat("{0:X2} ", currentByte);

                    // If the character is printable, add its ascii representation to
                    // the right-hand side.  Otherwise, add a dot to the right hand side.
                    var currentChar = (char)currentByte;
                    if (currentChar == 0x0)
                    {
                        asciiEnd.Append(' ');
                    }
                    else if (char.IsControl(currentChar))
                    {
                        asciiEnd.Append((char)0xFFFD);
                    }
                    else
                    {
                        asciiEnd.Append(currentChar);
                    }

                    charCounter++;

                    // If we've hit the end of a line, combine the right half with the
                    // left half, and start a new line.
                    if ((charCounter % BytesPerLine) == 0)
                    {
                        result.Append(nextLine).Append(' ').Append(asciiEnd);
                        nextLine.Clear();
                        asciiEnd.Clear();
                        currentOffset += BytesPerLine;
                        nextLine.AppendFormat(CultureInfo.InvariantCulture, LineFormat, currentOffset);

                        // Adding a newline to support long inputs strings flowing through InputObject parameterset.
                        if ((charCounter <= Bytes.Length) && string.IsNullOrEmpty(Path))
                        {
                            result.AppendLine();
                        }
                    }
                }

                // At the end of the file, we might not have had the chance to output
                // the end of the line yet. Only do this if we didn't exit on the 16-byte
                // boundary, though.
                if ((charCounter % 16) != 0)
                {
                    while ((charCounter % 16) != 0)
                    {
                        nextLine.Append(' ', 3);
                        asciiEnd.Append(' ');
                        charCounter++;
                    }

                    result.Append(nextLine).Append(' ').Append(asciiEnd);
                }
            }

            return result.ToString();
        }
    }
}
