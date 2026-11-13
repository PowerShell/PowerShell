// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Net.Mime;
using System.Reflection;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Provides argument completion for ContentType parameter.
    /// </summary>
    public class ContentTypeArgumentCompleter : IArgumentCompleter
    {
        /// <summary>
        /// Returns completion results for ContentType parameter.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="commandAst">The command AST.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>List of Completion Results.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
                => CompletionCompleters.GetMatchingResults(
                    wordToComplete,
                    possibleCompletionValues: EnumerateMediaTypeFieldValues(
                        typeof(MediaTypeNames.Application),
                        typeof(MediaTypeNames.Text)),
                    resultType: CompletionResultType.ParameterValue);

        /// <summary>
        /// Enumerate media type field values.
        /// This uses reflection to extract the public field string values from an array of media types.
        /// </summary>
        /// <param name="mediaTypes">The array of media types.</param>
        /// <returns>Enumerator of media type field value strings.</returns>
        private static IEnumerable<string> EnumerateMediaTypeFieldValues(params Type[] mediaTypes)
        {
            foreach (Type mediaType in mediaTypes)
            {
                foreach (FieldInfo field in mediaType.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.FieldType == typeof(string))
                    {
                        string fieldValue = (string)field.GetValue(null);
                        if (!string.IsNullOrEmpty(fieldValue))
                        {
                            yield return fieldValue;
                        }
                    }
                }
            }
        }
    }
}
