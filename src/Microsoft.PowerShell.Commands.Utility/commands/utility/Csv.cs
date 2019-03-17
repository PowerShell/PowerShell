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

                // if next character was delimiter or we are at the end, add string to result and clear wordBuffer
                // else if next character was quote, perform reading until next quote and add it to wordBuffer
                // else read and add it to wordBuffer
                if (nextChar == Delimiter) 
                {
                    result.Add(wordBuffer.ToString());
                    wordBuffer.Clear();
                } 
                else if (nextChar == '"') 
                {
                    bool inQuotes = true;
                    
                    // if we are within a quote section, read and append to wordBuffer until we find a next quote that is not followed by another quote
                    // if it is a single quote, escape the quote section
                    // if the quote is followed by an other quote, do not escape and add a quote character to wordBuffer
                    while (reader.Peek() != -1 && inQuotes) 
                    {
                        nextChar = (char)reader.Read();
                        
                        if (nextChar == '"') 
                        {
                            if ((char)reader.Peek() == '"')
                            {
                                wordBuffer.Append(nextChar);
                                reader.Read();
                            } 
                            else 
                            {
                                inQuotes = false;
                            }
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
            if (lastWord != string.Empty)
            {
                result.Add(lastWord);
            }

            reader.Close();    
            return result;           
        }
    }
}
