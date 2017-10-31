/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
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
        internal static readonly string[] TabCompletionResults = {
                Ascii, BigEndianUnicode, OEM, Unicode, Utf7, Utf8, Utf8Bom, Utf8NoBom, Utf32
            };

        internal static Dictionary<string, Encoding> encodingMap = new Dictionary<string,Encoding>(StringComparer.OrdinalIgnoreCase)
        {
            { Ascii, System.Text.Encoding.ASCII },
            { BigEndianUnicode, System.Text.Encoding.BigEndianUnicode },
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
            string encodingName = inputData as String;
            Encoding foundEncoding;
            if (encodingName != null && EncodingConversion.encodingMap.TryGetValue(encodingName, out foundEncoding))
            {
                return foundEncoding;
            }
            return inputData;
        }

    }

}
