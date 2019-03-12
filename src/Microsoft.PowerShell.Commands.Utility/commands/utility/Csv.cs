// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
//using System.Text.RegularExpressions;
//using System.IO;

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
        internal char Quote { get; } = '"';

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

            var reader = new System.IO.StringReader(csv);
            var tempString = string.Empty;

            // old implementation but now using the reader class
            while(reader.Peek() != -1) {
                char nextChar = (char)reader.Read();
                if(nextChar == Delimiter) {
                    //next character was delimiter found, so add string to collection
                    result.Add(tempString);
                    tempString = string.Empty;
                } else if(nextChar == Quote) {
                    //next character was quote, so perform reading untill next quote and add it to tempString
                    bool inQuote = true;
                    while(reader.Peek() != -1 && inQuote) {
                        nextChar = (char)reader.Read();
                        if(nextChar == Quote) {
                            //exit quote found
                            inQuote = false;
                        } else {
                            // add to emptyString
                            tempString += nextChar;
                        }
                    }
                }
            }
            // add last word if toAdd is not empty
            if(tempString != string.Empty) {
                result.Add(tempString);
            }

            //return new Collection<string>();      
            return result;           
        }
    }
}
