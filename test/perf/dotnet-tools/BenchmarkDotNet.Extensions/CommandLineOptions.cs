using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Extensions
{
    public class CommandLineOptions
    {
        // Find and parse given parameter with expected int value, then remove it and its value from the list of arguments to then pass to BenchmarkDotNet
        // Throws ArgumentException if the parameter does not have a value or that value is not parsable as an int
        public static List<string> ParseAndRemoveIntParameter(List<string> argsList, string parameter, out int? parameterValue)
        {
            int parameterIndex = argsList.IndexOf(parameter);
            parameterValue = null;

            if (parameterIndex != -1)
            {
                if (parameterIndex + 1 < argsList.Count && Int32.TryParse(argsList[parameterIndex+1], out int parsedParameterValue))
                {
                    // remove --partition-count args
                    parameterValue = parsedParameterValue;
                    argsList.RemoveAt(parameterIndex+1);
                    argsList.RemoveAt(parameterIndex);
                }
                else
                {
                    throw new ArgumentException(String.Format($"{parameter} must be followed by an integer"));
                }
            }

            return argsList;
        }

        public static List<string> ParseAndRemoveStringsParameter(List<string> argsList, string parameter, out List<string> parameterValue)
        {
            int parameterIndex = argsList.IndexOf(parameter);
            parameterValue = new List<string>();

            if (parameterIndex + 1 < argsList.Count)
            {
                while (parameterIndex + 1 < argsList.Count && !argsList[parameterIndex + 1].StartsWith("-"))
                {
                    // remove each filter string and stop when we get to the next argument flag
                    parameterValue.Add(argsList[parameterIndex + 1]);
                    argsList.RemoveAt(parameterIndex + 1);
                }
            }
            //We only want to remove the --exclusion-filter if it exists
            if (parameterIndex != -1)
            {
                argsList.RemoveAt(parameterIndex);
            }

            return argsList;
        }

        public static void ParseAndRemoveBooleanParameter(List<string> argsList, string parameter, out bool parameterValue)
        {
            int parameterIndex = argsList.IndexOf(parameter);

            if (parameterIndex != -1)
            {
                argsList.RemoveAt(parameterIndex);

                parameterValue = true;
            }
            else
            {
                parameterValue = false;
            }
        }

        public static void ValidatePartitionParameters(int? count, int? index)
        {
            // Either count and index must both be specified or neither specified
            if (!(count.HasValue == index.HasValue))
            {
                throw new ArgumentException("If either --partition-count or --partition-index is specified, both must be specified");
            }
            // Check values of count and index parameters
            else if (count.HasValue && index.HasValue)
            {
                if (count < 2)
                {
                    throw new ArgumentException("When specified, value of --partition-count must be greater than 1");
                }
                else if (!(index < count))
                {
                    throw new ArgumentException("Value of --partition-index must be less than --partition-count");
                }
                else if (index < 0)
                {
                    throw new ArgumentException("Value of --partition-index must be greater than or equal to 0");
                }
            }
        }
    }
}
