// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
//using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO.TextFieldParser;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class is used to parse CSV text.
    /// </summary>
    internal class CSVHelper
    {
        internal CSVHelper(char delimiter)
        {
            Delimiter = delimiter;
        }

        /// <summary>
        /// Gets or sets the delimiter that separates the values.
        /// </summary>
        internal char Delimiter { get; } = ',';

        /// <summary>
        /// Parse a CSV string.
        /// </summary>
        /// <param name="csv">
        /// String to be parsed.
        /// </param>
        internal Collection<string> ParseCsv(string csv)
        {        
            csv = csv.Trim(); 
            Collection<string> result = new Collection<string>();
            if (csv.Length == 0 || csv[0] == '#')
            {
                return result;
            }


            var reader = new System.IO.StringReader(sampleText);
            using (var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(reader))
            {
                parser.Delimiters = new string[] { "," };
                parser.HasFieldsEnclosedInQuotes = true; // <--- !!!
                string[] fields;
                while ((fields = parser.ReadFields()) != null)
                {
                    result.Add(fields);
                }
            }
            
            // regex expression, inspiration from
            // https://stackoverflow.com/questions/31118964/delimit-a-string-by-character-unless-within-quotation-marks-c-sharp
            // string[] elements = Regex.Split(csv, Delimiter + "|(\"[^\"]*\")"); 
            // todo: exclude " in split
            // Collection<string> result = new Collection<string>(elements);          
            return result;           
        }
    }
}
