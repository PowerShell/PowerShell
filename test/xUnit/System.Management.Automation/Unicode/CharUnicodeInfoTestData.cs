// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace System.Management.Automation.Unicode.Tests
{
    // The tests come from CoreFX tests: src\System.Globalization\tests\CharUnicodeInfo\

    public static class CharUnicodeInfoTestData
    {
        private static readonly Lazy<List<CharUnicodeInfoTestCase>> s_testCases = new Lazy<List<CharUnicodeInfoTestCase>>(() =>
        {
            List<CharUnicodeInfoTestCase> testCases = new List<CharUnicodeInfoTestCase>();
            string fileName =
                CharUnicodeInfo.GetUnicodeCategory('\u10D0') == UnicodeCategory.LowercaseLetter  ? "UnicodeData.11.0.txt" :
                CharUnicodeInfo.GetUnicodeCategory('\u037f') == UnicodeCategory.OtherNotAssigned ? "UnicodeData6.3.txt"   : "UnicodeData.8.0.txt";
            Stream stream = typeof(CharTests).GetTypeInfo().Assembly.GetManifestResourceStream(fileName);
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    Parse(testCases, reader.ReadLine());
                }
            }
            return testCases;
        });

        public static List<CharUnicodeInfoTestCase> TestCases => s_testCases.Value;

        private static int s_rangeMinCodePoint;
        private static void Parse(List<CharUnicodeInfoTestCase> testCases, string line)
        {
            // Data is in the format:
            // code-value;
            // character-name;
            // general-category;
            // canonical-combining-classes; (ignored)
            // bidirecional-category; (ignored)
            // character-decomposition-mapping; (ignored)
            // decimal-digit-value; (ignored)
            // digit-value; (ignoed)
            // number-value;
            string[] data = line.Split(';');
            string charValueString = data[0];
            string charName = data[1];
            string charCategoryString = data[2];
            string numericValueString = data[8];

            int codePoint = int.Parse(charValueString, NumberStyles.HexNumber);
            Parse(testCases, codePoint, charCategoryString, numericValueString);

            if (charName.EndsWith("First>"))
            {
                s_rangeMinCodePoint = codePoint;
            }
            else if (charName.EndsWith("Last>"))
            {
                // Assumes that we have already found a range start
                for (int rangeCodePoint = s_rangeMinCodePoint + 1; rangeCodePoint < codePoint; rangeCodePoint++)
                {
                    // Assumes that all code points in the range have the same numeric value
                    // and general category
                    Parse(testCases, rangeCodePoint, charCategoryString, numericValueString);
                }
            }
        }

        private static Dictionary<string, UnicodeCategory> s_unicodeCategories = new Dictionary<string, UnicodeCategory>
        {
            ["Pe"] = UnicodeCategory.ClosePunctuation,
            ["Pc"] = UnicodeCategory.ConnectorPunctuation,
            ["Cc"] = UnicodeCategory.Control,
            ["Sc"] = UnicodeCategory.CurrencySymbol,
            ["Pd"] = UnicodeCategory.DashPunctuation,
            ["Nd"] = UnicodeCategory.DecimalDigitNumber,
            ["Me"] = UnicodeCategory.EnclosingMark,
            ["Pf"] = UnicodeCategory.FinalQuotePunctuation,
            ["Cf"] = UnicodeCategory.Format,
            ["Pi"] = UnicodeCategory.InitialQuotePunctuation,
            ["Nl"] = UnicodeCategory.LetterNumber,
            ["Zl"] = UnicodeCategory.LineSeparator,
            ["Ll"] = UnicodeCategory.LowercaseLetter,
            ["Sm"] = UnicodeCategory.MathSymbol,
            ["Lm"] = UnicodeCategory.ModifierLetter,
            ["Sk"] = UnicodeCategory.ModifierSymbol,
            ["Mn"] = UnicodeCategory.NonSpacingMark,
            ["Ps"] = UnicodeCategory.OpenPunctuation,
            ["Lo"] = UnicodeCategory.OtherLetter,
            ["Cn"] = UnicodeCategory.OtherNotAssigned,
            ["No"] = UnicodeCategory.OtherNumber,
            ["Po"] = UnicodeCategory.OtherPunctuation,
            ["So"] = UnicodeCategory.OtherSymbol,
            ["Po"] = UnicodeCategory.OtherPunctuation,
            ["Zp"] = UnicodeCategory.ParagraphSeparator,
            ["Co"] = UnicodeCategory.PrivateUse,
            ["Zs"] = UnicodeCategory.SpaceSeparator,
            ["Mc"] = UnicodeCategory.SpacingCombiningMark,
            ["Cs"] = UnicodeCategory.Surrogate,
            ["Lt"] = UnicodeCategory.TitlecaseLetter,
            ["Lu"] = UnicodeCategory.UppercaseLetter
        };

        private static void Parse(List<CharUnicodeInfoTestCase> testCases, int codePoint, string charCategoryString, string numericValueString)
        {
            string codeValueRepresentation = codePoint > char.MaxValue ? char.ConvertFromUtf32(codePoint) : ((char)codePoint).ToString();
            double numericValue = ParseNumericValueString(numericValueString);
            UnicodeCategory generalCategory = s_unicodeCategories[charCategoryString];

            testCases.Add(new CharUnicodeInfoTestCase()
            {
                Utf32CodeValue = codeValueRepresentation,
                GeneralCategory = generalCategory,
                NumericValue = numericValue,
                CodePoint = codePoint
            });
        }

        private static double ParseNumericValueString(string numericValueString)
        {
            if (numericValueString.Length == 0)
            {
                // Parsing empty string (no numeric value)
                return -1;
            }

            int fractionDelimeterIndex = numericValueString.IndexOf("/");
            if (fractionDelimeterIndex == -1)
            {
                // Parsing basic number
                return double.Parse(numericValueString);
            }

            // Unicode datasets display fractions not decimals (e.g. 1/4 instead of 0.25),
            // so we should parse them as such
            string numeratorString = numericValueString.Substring(0, fractionDelimeterIndex);
            double numerator = double.Parse(numeratorString);

            string denominatorString = numericValueString.Substring(fractionDelimeterIndex + 1);
            double denominator = double.Parse(denominatorString);

            return numerator / denominator;
        }
    }

    public class CharUnicodeInfoTestCase
    {
        public string Utf32CodeValue { get; set; }
        public int CodePoint { get; set; }
        public UnicodeCategory GeneralCategory { get; set; }
        public double NumericValue { get; set; }
    }
}
