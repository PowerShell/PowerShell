// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace System.Management.Automation
{
    internal static class FuzzyMatcher
    {
        public const int MinimumDistance = 5;

        /// <summary>
        /// Determine if the two strings are considered similar.
        /// </summary>
        /// <param name="string1">The first string to compare.</param>
        /// <param name="string2">The second string to compare.</param>
        /// <returns>True if the two strings have a distance <= MinimumDistance.</returns>
        public static bool IsFuzzyMatch(string string1, string string2)
        {
            return GetDamerauLevenshteinDistance(string1, string2) <= MinimumDistance;
        }


        /// <summary>
        /// Compute the distance between two strings.
        /// Based off https://www.csharpstar.com/csharp-string-distance-algorithm/.
        /// </summary>
        /// <param name="string1">The first string to compare.</param>
        /// <param name="string2">The second string to compare.</param>
        /// <returns>The distance value where the lower the value the shorter the distance between the two strings representing a closer match.</returns>
        public static int GetDamerauLevenshteinDistance(string string1, string string2)
        {
            var bounds = new { Height = string1.Length + 1, Width = string2.Length + 1 };

            int[,] matrix = new int[bounds.Height, bounds.Width];

            for (int height = 0; height < bounds.Height; height++) { matrix[height, 0] = height; };
            for (int width = 0; width < bounds.Width; width++) { matrix[0, width] = width; };

            for (int height = 1; height < bounds.Height; height++)
            {
                for (int width = 1; width < bounds.Width; width++)
                {
                    int cost = (string1[height - 1] == string2[width - 1]) ? 0 : 1;
                    int insertion = matrix[height, width - 1] + 1;
                    int deletion = matrix[height - 1, width] + 1;
                    int substitution = matrix[height - 1, width - 1] + cost;

                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));

                    if (height > 1 && width > 1 && string1[height - 1] == string2[width - 2] && string1[height - 2] == string2[width - 1])
                    {
                        distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
                    }

                    matrix[height, width] = distance;
                }
            }

            return matrix[bounds.Height - 1, bounds.Width - 1];
        }
    }
}
