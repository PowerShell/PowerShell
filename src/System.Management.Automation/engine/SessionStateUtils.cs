// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.PowerShell.Commands;

namespace System.Management.Automation
{
    /// <summary>
    /// This class holds the integer constants used in Session State.
    /// </summary>
    internal static class SessionStateConstants
    {
        /// <summary>
        /// The default maximum for the number of variables.
        /// </summary>
        internal const int DefaultVariableCapacity = 4096;

        /// <summary>
        /// Max # of variables allowed in a scope in Session State.
        /// </summary>
        internal const int MaxVariablesCapacity = 32768;

        /// <summary>
        /// Min # of variables allows in a scope in Session State.
        /// </summary>
        internal const int MinVariablesCapacity = 1024;

        /// <summary>
        /// The default maximum for the number of aliases.
        /// </summary>
        internal const int DefaultAliasCapacity = 4096;

        /// <summary>
        /// Max # of aliases allowed in a scope in Session State.
        /// </summary>
        internal const int MaxAliasCapacity = 32768;

        /// <summary>
        /// Min # of aliases allowed in a scope in Session State.
        /// </summary>
        internal const int MinAliasCapacity = 1024;

        /// <summary>
        /// The default maximum for the number of functions.
        /// </summary>
        internal const int DefaultFunctionCapacity = 4096;

        /// <summary>
        /// Max # of functions allowed in a scope in Session State.
        /// </summary>
        internal const int MaxFunctionCapacity = 32768;

        /// <summary>
        /// Min # of functions allowed in a scope in Session State.
        /// </summary>
        internal const int MinFunctionCapacity = 1024;

        /// <summary>
        /// The default maximum for the number of drives.
        /// </summary>
        internal const int DefaultDriveCapacity = 4096;

        /// <summary>
        /// Max # of drives allowed in a scope in Session State.
        /// </summary>
        internal const int MaxDriveCapacity = 32768;

        /// <summary>
        /// Min # of drives allowed in a scope in Session State.
        /// </summary>
        internal const int MinDriveCapacity = 1024;

        /// <summary>
        /// The default maximum for the number of errors.
        /// </summary>
        internal const int DefaultErrorCapacity = 256;

        /// <summary>
        /// Max # of errors allowed in a scope in Session State.
        /// </summary>
        internal const int MaxErrorCapacity = 32768;

        /// <summary>
        /// Min # of errors allowed in a scope in Session State.
        /// </summary>
        internal const int MinErrorCapacity = 256;

        /// <summary>
        /// The default capacity for a Dictionary store.
        /// </summary>
        internal const int DefaultDictionaryCapacity = 100;

        /// <summary>
        /// Default load factor on a hash table.
        /// </summary>
        internal const float DefaultHashTableLoadFactor = 0.25F;
    }

    /// <summary>
    /// This class has static methods that are used in Session State.
    /// </summary>
    internal static class SessionStateUtilities
    {
        /// <summary>
        /// Converts the specified array into a collection of the specified type.
        /// </summary>
        /// <param name="array">
        /// The array to be converted.
        /// </param>
        /// <returns>
        /// A collection of the elements that were in the array.
        /// </returns>
        internal static Collection<T> ConvertArrayToCollection<T>(T[] array)
        {
            Collection<T> result = new Collection<T>();
            if (array != null)
            {
                foreach (T element in array)
                {
                    result.Add(element);
                }
            }

            return result;
        }

        /// <summary>
        /// Compares the elements in the specified collection with value specified. If
        /// the string comparer is specified it is used for the comparison, else the
        /// .Equals method is used.
        /// </summary>
        /// <param name="collection">
        /// The collection to check for the value.
        /// </param>
        /// <param name="value">
        /// The value to check for.
        /// </param>
        /// <param name="comparer">
        /// If specified the comparer will be used instead of .Equals.
        /// </param>
        /// <returns>
        /// true if the value is contained in the collection or false otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="collection"/> is null.
        /// </exception>
        internal static bool CollectionContainsValue(IEnumerable collection, object value, IComparer comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            bool result = false;

            foreach (object item in collection)
            {
                if (comparer != null)
                {
                    if (comparer.Compare(item, value) == 0)
                    {
                        result = true;
                        break;
                    }
                }
                else
                {
                    if (item.Equals(value))
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Constructs a collection of WildcardPatterns for the specified
        /// string collection.
        /// </summary>
        /// <param name="globPatterns">
        /// The string patterns to construct the WildcardPatterns for.
        /// </param>
        /// <param name="options">
        /// The options to create the WildcardPatterns with.
        /// </param>
        /// <returns>
        /// A collection of WildcardPatterns that represent the string patterns
        /// that were passed.
        /// </returns>
        internal static Collection<WildcardPattern> CreateWildcardsFromStrings(
            IEnumerable<string> globPatterns,
            WildcardOptions options)
        {
            Collection<WildcardPattern> result = new Collection<WildcardPattern>();

            if (globPatterns != null)
            {
                // Loop through the patterns and construct a wildcard pattern for each one

                foreach (string pattern in globPatterns)
                {
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        result.Add(
                            WildcardPattern.Get(
                                pattern,
                                options));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if the specified text matches any of the patterns.
        /// </summary>
        /// <param name="text">
        /// The text to check against the wildcard pattern.
        /// </param>
        /// <param name="patterns">
        /// An array of wildcard patterns. If the array is empty or null the text is deemed
        /// to be a match.
        /// </param>
        /// <param name="defaultValue">
        /// The default value that should be returned if <paramref name="patterns"/>
        /// is empty or null.
        /// </param>
        /// <returns>
        /// True if the text matches any of the patterns OR if patterns is null or empty and defaultValue is True.
        /// </returns>
        internal static bool MatchesAnyWildcardPattern(
            string text,
            IEnumerable<WildcardPattern> patterns,
            bool defaultValue)
        {
            bool result = false;
            bool patternsNonEmpty = false;

            if (patterns != null)
            {
                // Loop through each of the patterns until a match is found
                foreach (WildcardPattern pattern in patterns)
                {
                    patternsNonEmpty = true;
                    if (pattern.IsMatch(text))
                    {
                        result = true;
                        break;
                    }
                }
            }

            if (!patternsNonEmpty)
            {
                // Since no pattern was specified return the default value
                result = defaultValue;
            }

            return result;
        }

        /// <summary>
        /// Converts an OpenMode enum value to a FileMode.
        /// </summary>
        /// <param name="openMode">
        /// The OpenMode value to be converted.
        /// </param>
        /// <returns>
        /// The FileMode representation of the OpenMode.
        /// </returns>
        internal static FileMode GetFileModeFromOpenMode(OpenMode openMode)
        {
            FileMode result = FileMode.Create;

            switch (openMode)
            {
                case OpenMode.Add:
                    result = FileMode.Append;
                    break;

                case OpenMode.New:
                    result = FileMode.CreateNew;
                    break;

                case OpenMode.Overwrite:
                    result = FileMode.Create;
                    break;
            }

            return result;
        }
    }
}

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The enum used by commands to allow the user to specify how
    /// a file (or other item) should be opened.
    /// </summary>
    public enum OpenMode
    {
        /// <summary>
        /// This opens the file for appending (similar to FileMode.Append)
        /// </summary>
        Add,

        /// <summary>
        /// The file must be created new. If the file exists it is an error (similar to FileMode.CreateNew)
        /// </summary>
        New,

        /// <summary>
        /// Creates a new file, if the file already exists it is overwritten (similar to FileMode.Create)
        /// </summary>
        Overwrite
    }
}

