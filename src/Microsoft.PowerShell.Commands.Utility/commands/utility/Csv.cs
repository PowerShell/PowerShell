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
            Collection<string> result = new Collection<string>();

            /////////new code with regex/////
            
            csv = csv.Trim();

            //regex expression:
            //https://stackoverflow.com/questions/31118964/delimit-a-string-by-character-unless-within-quotation-marks-c-sharp
            string[] pieces = Regex.Split(csv, ",|(\"[^\"]*\")").Where(exp => !String.IsNullOrEmpty(exp)).ToArray();

            foreach (string piece in pieces)
            {
                result.add(piece);
            }
            
            return result;
            /* 
            //using a reader//
            if (csv.Length == 0 || csv[0] == '#')
            {
                return result;
            }

            using (StringReader reader = new StringReader(csv.ToString()))
                {
                    while ((readText = await reader.ReadLineAsync()) != null)
                    {
                        String line = readText;
                        //split line


                    }
                }

            ////////old code below////////

            string tempString = string.Empty;
            csv = csv.Trim();
            if (csv.Length == 0 || csv[0] == '#')
            {
                return result;
            }

            bool inQuote = false;

            // for each character in the file (use stream instead of for loop for speed)
            // if the character is a delimiter,

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];
                if (c == Delimiter)
                {
                    if (!inQuote)
                    {
                        result.Add(tempString);
                        tempString = string.Empty;
                    }
                    else
                    {
                        tempString += c;
                    }
                }
                else
                {
                    switch (c)
                    {
                        case '"':
                            if (inQuote)
                            {
                                // If we are at the end of the string or the end of the segment, create a new value
                                // Otherwise we have an error
                                if (i == csv.Length - 1)
                                {
                                    result.Add(tempString);
                                    tempString = string.Empty;
                                    inQuote = false;
                                    break;
                                }

                                if (csv[i + 1] == Delimiter)
                                {
                                    result.Add(tempString);
                                    tempString = string.Empty;
                                    inQuote = false;
                                    i++;
                                }
                                else if (csv[i + 1] == '"')
                                {
                                    tempString += '"';
                                    i++;
                                }
                                else
                                {
                                    inQuote = false;
                                }
                            }
                            else
                            {
                                inQuote = true;
                            }

                            break;

                        default:
                            tempString += c;
                            break;
                    }
                }
            }

            if (tempString.Length > 0)
            {
                result.Add(tempString);
            }

            return result;
            */
        }
    }
}
