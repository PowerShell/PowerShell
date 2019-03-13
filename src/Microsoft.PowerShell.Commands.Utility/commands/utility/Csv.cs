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
            StringBuilder wordBuffer = new StringBuilder();

            while (reader.Peek() != -1) 
            {
                char nextChar = (char)reader.Read();

                // if next character was delimiter or we are at the end, add string to result and clear builder
                // else if next character was quote, perform reading untill next quote and add it to builder
                // else read and add it to builder
                if (nextChar == Delimiter) 
                {
                    result.Add(wordBuffer.ToString());
                    wordBuffer.Clear();
                } 
                else if (nextChar == '"') 
                {
                    bool betweenQuotes = true;
                    while (reader.Peek() != -1 && betweenQuotes) 
                    {
                        nextChar = (char)reader.Read();
                        
                        if (nextChar == '"') 
                        {
                            betweenQuotes = false;
                        } 
                        else 
                        {
                            wordBuffer.Append(nextChar);
                        }
                    }
                } 
                else 
                {
                    wordBuffer.Append(nextChar);
                }
            }

            string lastWord = wordBuffer.ToString();
            if (lastWord != string.Empty) {
                result.Add(lastWord);
            }

            reader.Close();    
            return result;           
        }
    }
}
