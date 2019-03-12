// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.IO;
using System.Text;

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

            var reader = new StringReader(csv);
            StringBuilder bld = new StringBuilder();

            while (reader.Peek() != -1) 
            {
                char nextChar = (char)reader.Read();

                // if next character was delimiter, add string to builder and clear builder
                // else if next character was quote, perform reading untill next quote and add it to builder
                // else read and add it to builder
                if (nextChar == Delimiter) 
                {
                    result.Add(bld.ToString());
                    bld.Clear();
                } 
                else if (nextChar == Quote) 
                {
                    bool isinQuotes = true;
                    while (reader.Peek() != -1 && isinQuotes) 
                    {
                        nextChar = (char)reader.Read();
                        
                        if (nextChar == Quote) {
                            isinQuotes = false;
                        } 
                        else {
                            bld.Append(nextChar);
                        }
                    }
                } else {
                    bld.Append(nextChar);
                }
            }

            // add last word if remainder is not empty
            if (bld.ToString() != string.Empty) {
                result.Add(bld.ToString());
            }

            reader.Close();    
            return result;           
        }
    }
}
