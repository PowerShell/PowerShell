// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Host
{
    /// <summary>
    /// Provides a description of a choice for use by <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>.
    /// <!--Used by the engine to describe cmdlet parameters.-->
    /// </summary>
    public sealed
    class ChoiceDescription
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell compatibility with Windows PowerShell

        private readonly string label = null;
        private string helpMessage = string.Empty;

        #endregion

        /// <summary>
        /// Initializes an new instance of ChoiceDescription and defines the Label value.
        /// </summary>
        /// <param name="label">
        /// The label to identify this field description
        /// </param>
        /// <exception cref="System.Management.Automation.PSArgumentException">
        /// <paramref name="label"/> is null or empty.
        /// </exception>
        public
        ChoiceDescription(string label)
        {
            // the only required parameter is label.

            if (string.IsNullOrEmpty(label))
            {
                // "label" is not localizable
                throw PSTraceSource.NewArgumentException(nameof(label), DescriptionsStrings.NullOrEmptyErrorTemplate, "label");
            }

            this.label = label;
        }

        /// <summary>
        /// Initializes an new instance of ChoiceDescription and defines the Label and HelpMessage values.
        /// </summary>
        /// <param name="label">
        /// The label to identify this field description.
        /// </param>
        /// <param name="helpMessage">
        /// The help message for this field.
        /// </param>
        /// <exception cref="System.Management.Automation.PSArgumentException">
        /// <paramref name="label"/> is null or empty.
        /// </exception>
        /// <exception cref="System.Management.Automation.PSArgumentNullException">
        /// <paramref name="helpMessage"/> is null.
        /// </exception>
        public
        ChoiceDescription(string label, string helpMessage)
        {
            // the only required parameter is label.

            if (string.IsNullOrEmpty(label))
            {
                // "label" is not localizable
                throw PSTraceSource.NewArgumentException(nameof(label), DescriptionsStrings.NullOrEmptyErrorTemplate, "label");
            }

            if (helpMessage == null)
            {
                // "helpMessage" is not localizable
                throw PSTraceSource.NewArgumentNullException(nameof(helpMessage));
            }

            this.label = label;
            this.helpMessage = helpMessage;
        }

        /// <summary>
        /// Gets a short, human-presentable message to describe and identify the choice.  Think Button label.
        /// </summary>
        /// <remarks>
        /// Note that the special character &amp; (ampersand) may be embedded in the label string to identify the next character in the label
        /// as a "hot key" (aka "keyboard accelerator") that the Console.PromptForChoice implementation may use to allow the user to
        /// quickly set input focus to this choice.  The implementation of <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>
        /// is responsible for parsing the label string for this special character and rendering it accordingly.
        ///
        /// For examples, a choice named "Yes to All" might have "Yes to &amp;All" as it's label.
        /// </remarks>
        public
        string
        Label
        {
            get
            {
                Dbg.Assert(this.label != null, "label should not be null");

                return this.label;
            }
        }

        /// <summary>
        /// Gets and sets the help message for this field.
        /// </summary>
        /// <exception cref="System.Management.Automation.PSArgumentNullException">
        /// Set to null.
        /// </exception>
        /// <remarks>
        /// This should be a few sentences to describe the field, suitable for presentation as a tool tip.
        /// Avoid placing including formatting characters such as newline and tab.
        /// </remarks>
        public
        string
        HelpMessage
        {
            get
            {
                Dbg.Assert(this.helpMessage != null, "helpMessage should not be null");

                return this.helpMessage;
            }

            set
            {
                if (value == null)
                {
                    throw PSTraceSource.NewArgumentNullException("value");
                }

                this.helpMessage = value;
            }
        }
    }
}
