// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    using System;

    /// <summary>
    /// Possible types of CompletionResults.
    /// </summary>
    public enum CompletionResultType
    {
        /// <summary> An unknown result type, kept as text only.</summary>
        Text = 0,

        /// <summary>A history result type like the items out of get-history.</summary>
        History = 1,

        /// <summary>A command result type like the items out of get-command.</summary>
        Command = 2,

        /// <summary>A provider item.</summary>
        ProviderItem = 3,

        /// <summary>A provider container.</summary>
        ProviderContainer = 4,

        /// <summary>A property result type like the property items out of get-member.</summary>
        Property = 5,

        /// <summary>A method result type like the method items out of get-member.</summary>
        Method = 6,

        /// <summary>A parameter name result type like the Parameters property out of get-command items.</summary>
        ParameterName = 7,

        /// <summary>A parameter value result type.</summary>
        ParameterValue = 8,

        /// <summary>A variable result type like the items out of get-childitem variable.</summary>
        Variable = 9,

        /// <summary>A namespace.</summary>
        Namespace = 10,

        /// <summary>A type name.</summary>
        Type = 11,

        /// <summary>A keyword.</summary>
        Keyword = 12,

        /// <summary>A dynamic keyword.</summary>
        DynamicKeyword = 13,

        // If a new enum is added, there is a range test that uses DynamicKeyword for parameter validation
        // that needs to be updated to use the new enum.
        // We can't use a "MaxValue" enum because it's value would preclude ever adding a new enum.
    }

    /// <summary>
    /// Class used to store a tab completion or Intellisense result.
    /// </summary>
    public class CompletionResult
    {
        /// <summary>
        /// Text to be used as the auto completion result.
        /// </summary>
        private string _completionText;

        /// <summary>
        /// Text to be displayed in a list.
        /// </summary>
        private string _listItemText;

        /// <summary>
        /// The text for the tooltip with details to be displayed about the object.
        /// </summary>
        private string _toolTip;

        /// <summary>
        /// Type of completion result.
        /// </summary>
        private CompletionResultType _resultType;

        /// <summary>
        /// Private member for null instance.
        /// </summary>
        private static readonly CompletionResult s_nullInstance = new CompletionResult();

        /// <summary>
        /// Gets the text to be used as the auto completion result.
        /// </summary>
        public string CompletionText
        {
            get
            {
                if (this == s_nullInstance)
                {
                    throw PSTraceSource.NewInvalidOperationException(TabCompletionStrings.NoAccessToProperties);
                }

                return _completionText;
            }
        }

        /// <summary>
        /// Gets the text to be displayed in a list.
        /// </summary>
        public string ListItemText
        {
            get
            {
                if (this == s_nullInstance)
                {
                    throw PSTraceSource.NewInvalidOperationException(TabCompletionStrings.NoAccessToProperties);
                }

                return _listItemText;
            }
        }

        /// <summary>
        /// Gets the type of completion result.
        /// </summary>
        public CompletionResultType ResultType
        {
            get
            {
                if (this == s_nullInstance)
                {
                    throw PSTraceSource.NewInvalidOperationException(TabCompletionStrings.NoAccessToProperties);
                }

                return _resultType;
            }
        }

        /// <summary>
        /// Gets the text for the tooltip with details to be displayed about the object.
        /// </summary>
        public string ToolTip
        {
            get
            {
                if (this == s_nullInstance)
                {
                    throw PSTraceSource.NewInvalidOperationException(TabCompletionStrings.NoAccessToProperties);
                }

                return _toolTip;
            }
        }

        /// <summary>
        /// Gets the null instance of type CompletionResult.
        /// </summary>
        internal static CompletionResult Null
        {
            get { return s_nullInstance; }
        }

        /// <summary>
        /// Initializes a new instance of the CompletionResult class.
        /// </summary>
        /// <param name="completionText">The text to be used as the auto completion result.</param>
        /// <param name="listItemText">The text to be displayed in a list.</param>
        /// <param name="resultType">The type of completion result.</param>
        /// <param name="toolTip">The text for the tooltip with details to be displayed about the object.</param>
        public CompletionResult(string completionText, string listItemText, CompletionResultType resultType, string toolTip)
        {
            if (string.IsNullOrEmpty(completionText))
            {
                throw PSTraceSource.NewArgumentNullException("completionText");
            }

            if (string.IsNullOrEmpty(listItemText))
            {
                throw PSTraceSource.NewArgumentNullException("listItemText");
            }

            if (resultType < CompletionResultType.Text || resultType > CompletionResultType.DynamicKeyword)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException("resultType", resultType);
            }

            if (string.IsNullOrEmpty(toolTip))
            {
                throw PSTraceSource.NewArgumentNullException("toolTip");
            }

            _completionText = completionText;
            _listItemText = listItemText;
            _toolTip = toolTip;
            _resultType = resultType;
        }

        /// <summary>
        /// Initializes a new instance of this class internally if the result out of TabExpansion is a string.
        /// </summary>
        /// <param name="completionText">Completion text.</param>
        public CompletionResult(string completionText)
            : this(completionText, completionText, CompletionResultType.Text, completionText)
        {
        }

        /// <summary>
        /// An null instance of CompletionResult.
        /// </summary>
        /// <remarks>
        /// This can be used in argument completion, to indicate that the completion attempt has gone through the
        /// native command argument completion methods.
        /// </remarks>
        private CompletionResult() { }
    }
}
