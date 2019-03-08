// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

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
            // regex expression, inspiration from
            // https://stackoverflow.com/questions/31118964/delimit-a-string-by-character-unless-within-quotation-marks-c-sharp
            string[] elements = Regex.Split(csv, Delimiter + "|(\"[^\"]*\")").Where(exp => !String.IsNullOrEmpty(exp)).ToArray();          
            Collection<string> result = new Collection<string>(elements);          
            return result;           
        }
    }
}
