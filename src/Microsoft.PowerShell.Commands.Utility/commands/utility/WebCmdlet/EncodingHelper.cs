// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    internal static class EncodingHelper
    {
        private const int UTF8CodePage = 65001;
        private const int UTF8PreambleLength = 3;
        private const byte UTF8PreambleByte0 = 0xEF;
        private const byte UTF8PreambleByte1 = 0xBB;
        private const byte UTF8PreambleByte2 = 0xBF;
        private const int UTF8PreambleFirst2Bytes = 0xEFBB;

        private const int UTF32CodePage = 12000;
        private const int UTF32PreambleLength = 4;
        private const byte UTF32PreambleByte0 = 0xFF;
        private const byte UTF32PreambleByte1 = 0xFE;
        private const byte UTF32PreambleByte2 = 0x00;
        private const byte UTF32PreambleByte3 = 0x00;
        private const int UTF32OrUnicodePreambleFirst2Bytes = 0xFFFE;

        private const int BigEndianUTF32CodePage = 12001;
        private const int BigEndianUTF32PreambleLength = 4;
        private const byte BigEndianUTF32PreambleByte0 = 0x00;
        private const byte BigEndianUTF32PreambleByte1 = 0x00;
        private const byte BigEndianUTF32PreambleByte2 = 0xFE;
        private const byte BigEndianUTF32PreambleByte3 = 0xFF;
        private const int BigEndianUTF32PreambleFirst2Bytes = 0x0000;

        private const int UnicodeCodePage = 1200;
        private const int UnicodePreambleLength = 2;
        private const byte UnicodePreambleByte0 = 0xFF;
        private const byte UnicodePreambleByte1 = 0xFE;

        private const int BigEndianUnicodeCodePage = 1201;
        private const int BigEndianUnicodePreambleLength = 2;
        private const byte BigEndianUnicodePreambleByte0 = 0xFE;
        private const byte BigEndianUnicodePreambleByte1 = 0xFF;
        private const int BigEndianUnicodePreambleFirst2Bytes = 0xFEFF;

        internal static bool TryDetectEncodingFromBom(byte[] buffer, [NotNullWhen(true)] out Encoding? encoding, out int preambleLength)
        {
            int first2Bytes = buffer[0] << 8 | buffer[1];

            switch (first2Bytes)
            {
                case UTF8PreambleFirst2Bytes:
                    if (buffer[2] == UTF8PreambleByte2)
                    {
                        encoding = Encoding.UTF8;
                        preambleLength = UTF8PreambleLength;
                        return true;
                    }
                    break;

                case UTF32OrUnicodePreambleFirst2Bytes:
                    // UTF32 not supported on Phone
                    if (buffer[2] == UTF32PreambleByte2 && buffer[3] == UTF32PreambleByte3)
                    {
                        encoding = Encoding.UTF32;
                        preambleLength = UTF32PreambleLength;
                    }
                    else
                    {
                        encoding = Encoding.Unicode;
                        preambleLength = UnicodePreambleLength;
                    }
                    return true;

                case BigEndianUnicodePreambleFirst2Bytes:
                    encoding = Encoding.BigEndianUnicode;
                    preambleLength = BigEndianUnicodePreambleLength;
                    return true;
                
                case BigEndianUTF32PreambleFirst2Bytes:
                    if (buffer[2] == BigEndianUTF32PreambleByte2 && buffer[3] == BigEndianUTF32PreambleByte3)
                    {
                        encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
                        preambleLength = BigEndianUTF32PreambleLength;
                        return true;
                    }
                    break;
            }
        
            encoding = null;
            preambleLength = 0;
            return false;
        }
    }
}
