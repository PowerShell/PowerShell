/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using System.Management.Automation.Internal;

namespace System.Management.Automation
{

    internal static class EncodingConversion
    {
        internal const string Unknown = "unknown";
        internal const string String = "string";
        internal const string Unicode = "unicode";
        internal const string BigEndianUnicode = "bigendianunicode";
        internal const string Ascii = "ascii";
        internal const string Utf8 = "utf8";
        internal const string Utf8NoBom = "utf8NoBOM";
        internal const string Utf8Bom = "utf8BOM";
        internal const string Utf7 = "utf7";
        internal const string Utf32 = "utf32";
        internal const string Default = "default";
        internal const string OEM = "oem";
        internal const string Byte = "byte"; // This will return unicode, but 'byte' is special as it uses unicode encoding without a BOM
        // We're using this custom ByteEncoding so the *-Content cmdlets can provide `byte` as an encoding, which has the 
        // behavior of [io.file]::ReadAllBytes(<file>). When we changed the type of the parameter to Encoding
        // and created the ArgumentTransformationAttribute to convert strings to encodings to be more usable, it means that
        // we have to check to se
        internal static readonly ByteEncoding byteEncoding = new ByteEncoding();
        // We will provide a partial list for tab completion we will convert to an encoding.
        // If the user has an encoding object, we will cheerfully use it, but we can't use
        // it as a validation set, as a user may have an specialty encoding which we have
        // no idea about.
        // We can't use Encoding.GetEncoding() as a validate set because we need to support
        // unreal encodings like 'Byte' and 'OEM'
        internal static readonly string[] TabCompletionResults = {
                Ascii, BigEndianUnicode, Byte, OEM, Unicode, Utf7, Utf8, Utf8Bom, Utf8NoBom, Utf32
            };

        internal static Dictionary<string, Encoding> encodingMap = new Dictionary<string,Encoding>(StringComparer.OrdinalIgnoreCase)
        {
            { Ascii, System.Text.Encoding.ASCII },
            { BigEndianUnicode, System.Text.Encoding.BigEndianUnicode },
            { Byte, byteEncoding },
            { Default, ClrFacade.GetDefaultEncoding() },
            { OEM, ClrFacade.GetOEMEncoding() },
            { Unicode, System.Text.Encoding.Unicode },
            { Utf7, System.Text.Encoding.UTF7 },
            { Utf8, ClrFacade.GetDefaultEncoding() },
            { Utf8Bom, System.Text.Encoding.UTF8 },
            { Utf8NoBom, ClrFacade.GetDefaultEncoding() },
            { Utf32, System.Text.Encoding.UTF32 },
            { String, System.Text.Encoding.Unicode },
            { Unknown, System.Text.Encoding.Unicode },
        };

        /// <summary>
        /// retrieve the encoding parameter from the command line
        /// it throws if the encoding does not match the known ones
        /// </summary>
        /// <returns>a System.Text.Encoding object (null if no encoding specified)</returns>
        internal static Encoding Convert(Cmdlet cmdlet, string encoding)
        {
            if (string.IsNullOrEmpty(encoding))
            {
                // no parameter passed, default to UTF8
                return ClrFacade.GetDefaultEncoding();
            }

            Encoding foundEncoding;
            if (encodingMap.TryGetValue(encoding, out foundEncoding))
            {
                return foundEncoding;
            }

            // error condition: unknown encoding value
            string validEncodingValues = string.Join(", ", TabCompletionResults);
            string msg = StringUtil.Format(PathUtilsStrings.OutFile_WriteToFileEncodingUnknown, encoding, validEncodingValues);

            ErrorRecord errorRecord = new ErrorRecord(
                PSTraceSource.NewArgumentException("Encoding"),
                "WriteToFileEncodingUnknown",
                ErrorCategory.InvalidArgument,
                null);

            errorRecord.ErrorDetails = new ErrorDetails(msg);
            cmdlet.ThrowTerminatingError(errorRecord);

            return null;
        }

        // We need a way to provide an encoding and also
        // a way to notify that the user wants "byte" encoding which is just a stream of bytes
        // Because we've changed the parameter type to Encoding, we need a way
        // to continue to support the *-Content cmdlets use of -Encoding Byte, so we need
        // to have the ArgumentTransformationAttribute actually return an encoding
        // moreover, we need a way to notify the -Content cmdlets that the user requested `byte`
        // so we can't just return the actual encoding we want, thus this artificial encoding
        internal class ByteEncoding : System.Text.Encoding
        {
            // the encoding for this is not bigendian and not BOM
            public Encoding ActualEncoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
            // redirect all the calls to the ActualEncoding
            public override unsafe int GetByteCount(char* chars, int count) { return ActualEncoding.GetByteCount(chars, count); }
            public override int GetByteCount(char[] chars, int index, int count) { return ActualEncoding.GetByteCount(chars, index, count); }
            public override int GetByteCount(string s) { return ActualEncoding.GetByteCount(s); }
            public override int GetByteCount(char[] chars) { return ActualEncoding.GetByteCount(chars); }
            public override byte[] GetBytes(char[] chars) { return ActualEncoding.GetBytes(chars); }
            public override byte[] GetBytes(char[] chars, int index, int count) { return ActualEncoding.GetBytes(chars, index, count); }
            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { return ActualEncoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex); }
            public override byte[] GetBytes(string s) { return ActualEncoding.GetBytes(s); }
            public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { return ActualEncoding.GetBytes(chars, charCount, bytes, byteCount); }
            public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { return ActualEncoding.GetBytes(s, charIndex, charCount, bytes, byteIndex); }
            public override int GetCharCount(byte[] bytes) { return ActualEncoding.GetCharCount(bytes); }
            public override int GetCharCount(byte[] bytes, int index, int count) { return ActualEncoding.GetCharCount(bytes, index, count); }
            public override unsafe int GetCharCount(byte* bytes, int count) { return ActualEncoding.GetCharCount(bytes, count); }
            public override char[] GetChars(byte[] bytes, int index, int count) { return ActualEncoding.GetChars(bytes, index, count); }
            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { return ActualEncoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex); }
            public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { return ActualEncoding.GetChars(bytes, byteCount, chars, charCount); }
            public override char[] GetChars(byte[] bytes) { return ActualEncoding.GetChars(bytes); }
            public override Decoder GetDecoder() { return ActualEncoding.GetDecoder(); }
            public override Encoder GetEncoder() { return ActualEncoding.GetEncoder(); }
            public override int GetHashCode() { return ActualEncoding.GetHashCode(); }
            public override int GetMaxByteCount(int charCount) { return ActualEncoding.GetMaxByteCount(charCount); }
            public override int GetMaxCharCount(int byteCount) { return ActualEncoding.GetMaxCharCount(byteCount); }
            public override byte[] GetPreamble() { return ActualEncoding.GetPreamble(); }
            public override string GetString(byte[] bytes) { return ActualEncoding.GetString(bytes); }
            public override string GetString(byte[] bytes, int index, int count) { return ActualEncoding.GetString(bytes, index, count); }
            public override bool IsAlwaysNormalized(NormalizationForm normalizationForm) { return ActualEncoding.IsAlwaysNormalized(normalizationForm); }
        }
    }

    /// <summary>
    /// To make it easier to specify -Encoding parameter, we add an ArgumentTransformationAttribute here.
    /// When the input data is of type string and is valid to be converted to System.Text.Encoding, we do
    /// the conversion and return the converted value. Otherwise, we just return the input data.
    /// </summary>
    internal sealed class ArgumentToEncodingTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            string encodingName;
            if (LanguagePrimitives.TryConvertTo<string>(inputData, out encodingName))
            {
                Encoding foundEncoding;
                if (EncodingConversion.encodingMap.TryGetValue(encodingName, out foundEncoding))
                {
                    return foundEncoding;
                }
            }
            return inputData;
        }

    }

    internal class EncodingArgumentCompleter : IArgumentCompleter
    {
        /// <summary>
        ///
        /// </summary>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            System.Management.Automation.Language.CommandAst commandAst,
            System.Collections.IDictionary fakeBoundParameters)
        {
            List<CompletionResult> encodings = new List<CompletionResult>();
            foreach(string encoding in EncodingConversion.TabCompletionResults)
            {
                if (string.IsNullOrEmpty(wordToComplete))
                {
                    encodings.Add(new CompletionResult(encoding, encoding, CompletionResultType.Text, encoding));
                }
                else if (encoding.IndexOf(wordToComplete, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    encodings.Add(new CompletionResult(encoding, encoding, CompletionResultType.Text, encoding));
                }
            }
            return encodings;
        }
    }
}
