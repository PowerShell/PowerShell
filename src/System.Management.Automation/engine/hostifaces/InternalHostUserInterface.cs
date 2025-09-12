// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Security;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal.Host
{
    internal partial
    class InternalHostUserInterface : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
    {
        internal
        InternalHostUserInterface(PSHostUserInterface externalUI, InternalHost parentHost)
        {
            // externalUI may be null

            _externalUI = externalUI;

            // parent may not be null, however

            Dbg.Assert(parentHost != null, "parent may not be null");
            if (parentHost == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(parentHost));
            }

            _parent = parentHost;

            PSHostRawUserInterface rawui = null;

            if (externalUI != null)
            {
                rawui = externalUI.RawUI;
            }

            _internalRawUI = new InternalHostRawUserInterface(rawui, _parent);
        }

        private
        void
        ThrowNotInteractive()
        {
            _internalRawUI.ThrowNotInteractive();
        }

        private static void
        ThrowPromptNotInteractive(string promptMessage)
        {
            string message = StringUtil.Format(HostInterfaceExceptionsStrings.HostFunctionPromptNotImplemented, promptMessage);
            HostException e = new HostException(
                message,
                null,
                "HostFunctionNotImplemented",
                ErrorCategory.NotImplemented);
            throw e;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <value></value>
        /// <exception/>
        public override
        System.Management.Automation.Host.PSHostRawUserInterface
        RawUI
        {
            get
            {
                return _internalRawUI;
            }
        }

        public override bool SupportsVirtualTerminal
        {
            get { return _externalUI != null && _externalUI.SupportsVirtualTerminal; }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <exception cref="HostException">
        /// if the UI property of the external host is null, possibly because the PSHostUserInterface is not
        /// implemented by the external host.
        /// </exception>
        public override
        string
        ReadLine()
        {
            if (_externalUI == null)
            {
                ThrowNotInteractive();
            }

            string result = null;
            try
            {
                result = _externalUI.ReadLine();
            }
            catch (PipelineStoppedException)
            {
                // PipelineStoppedException is thrown by host when it wants
                // to stop the pipeline.
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_parent.Context.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                {
                    throw;
                }

                lpl.Stopper.Stop();
            }

            return result;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <exception cref="HostException">
        /// if the UI property of the external host is null, possibly because the PSHostUserInterface is not
        /// implemented by the external host.
        /// </exception>
        public override
        SecureString
        ReadLineAsSecureString()
        {
            if (_externalUI == null)
            {
                ThrowNotInteractive();
            }

            SecureString result = null;

            try
            {
                result = _externalUI.ReadLineAsSecureString();
            }
            catch (PipelineStoppedException)
            {
                // PipelineStoppedException is thrown by host when it wants
                // to stop the pipeline.
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_parent.Context.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                {
                    throw;
                }

                lpl.Stopper.Stop();
            }

            return result;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="value">
        /// </param>
        /// <exception cref="HostException">
        /// if <paramref name="value"/> is not null and the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override
        void
        Write(string value)
        {
            if (value == null)
            {
                return;
            }

            if (_externalUI == null)
            {
                return;
            }

            _externalUI.Write(value);
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="foregroundColor">
        /// </param>
        /// <param name="backgroundColor">
        /// </param>
        /// <param name="value">
        /// </param>
        /// <exception cref="HostException">
        /// if <paramref name="value"/> is not null and the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override
        void
        Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            if (value == null)
            {
                return;
            }

            if (_externalUI == null)
            {
                return;
            }

            if (PSStyle.Instance.OutputRendering == OutputRendering.PlainText)
            {
                _externalUI.Write(value);
            }
            else
            {
                _externalUI.Write(foregroundColor, backgroundColor, value);
            }
        }

        /// <summary>
        /// See base class
        /// <seealso cref="Write(string)"/>
        /// <seealso cref="WriteLine(string)"/>
        /// </summary>
        /// <exception cref="HostException">
        /// if the UI property of the external host is null, possibly because the PSHostUserInterface is not
        ///     implemented by the external host
        /// </exception>
        public override
        void
        WriteLine()
        {
            if (_externalUI == null)
            {
                return;
            }

            _externalUI.WriteLine();
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="value">
        /// </param>
        /// <exception cref="HostException">
        /// if <paramref name="value"/> is not null and the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override
        void
        WriteLine(string value)
        {
            if (value == null)
            {
                return;
            }

            if (_externalUI == null)
            {
                return;
            }

            _externalUI.WriteLine(value);
        }

        public override
        void
        WriteErrorLine(string value)
        {
            if (value == null)
            {
                return;
            }

            if (_externalUI == null)
            {
                return;
            }

            _externalUI.WriteErrorLine(value);
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="foregroundColor">
        /// </param>
        /// <param name="backgroundColor">
        /// </param>
        /// <param name="value">
        /// </param>
        /// <exception cref="HostException">
        /// if <paramref name="value"/> is not null and the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override
        void
        WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            if (value == null)
            {
                return;
            }

            if (_externalUI == null)
            {
                return;
            }

            if (PSStyle.Instance.OutputRendering == OutputRendering.PlainText)
            {
                _externalUI.WriteLine(value);
            }
            else
            {
                _externalUI.WriteLine(foregroundColor, backgroundColor, value);
            }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <exception cref="HostException">
        /// if <paramref name="message"/> is not null and the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override
        void
        WriteDebugLine(string message)
        {
            WriteDebugLineHelper(message);
        }

        /// <summary>
        /// </summary>
        internal void WriteDebugRecord(DebugRecord record)
        {
            WriteDebugInfoBuffers(record);

            if (_externalUI == null)
            {
                return;
            }

            _externalUI.WriteDebugLine(record.Message);
        }

        /// <summary>
        /// Writes the DebugRecord to informational buffers.
        /// </summary>
        /// <param name="record">DebugRecord.</param>
        internal void WriteDebugInfoBuffers(DebugRecord record) => _informationalBuffers?.AddDebug(record);

        /// <summary>
        /// Helper function for WriteDebugLine.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="preference"></param>
        /// <exception cref="ActionPreferenceStopException">
        /// If the debug preference is set to ActionPreference.Stop
        /// </exception>
        /// <exception cref="ActionPreferenceStopException">
        /// If the debug preference is set to ActionPreference.Inquire and user requests to stop execution.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If the debug preference is not a valid ActionPreference value.
        /// </exception>
        internal
        void
        WriteDebugLine(string message, ref ActionPreference preference)
        {
            string errorMsg = null;
            ErrorRecord errorRecord = null;
            switch (preference)
            {
                case ActionPreference.Continue:
                case ActionPreference.Break:
                    WriteDebugLineHelper(message);
                    break;
                case ActionPreference.SilentlyContinue:
                case ActionPreference.Ignore:
                    break;
                case ActionPreference.Inquire:
                    if (!DebugShouldContinue(message, ref preference))
                    {
                        // user asked to exit with an error

                        errorMsg = InternalHostUserInterfaceStrings.WriteDebugLineStoppedError;
                        errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(errorMsg),
                            "UserStopRequest", ErrorCategory.OperationStopped, null);
                        ActionPreferenceStopException e = new ActionPreferenceStopException(errorRecord);
                        // We cannot call ThrowTerminatingError since this is not a cmdlet or provider
                        throw e;
                    }
                    else
                    {
                        WriteDebugLineHelper(message);
                    }

                    break;
                case ActionPreference.Stop:
                    WriteDebugLineHelper(message);

                    errorMsg = InternalHostUserInterfaceStrings.WriteDebugLineStoppedError;
                    errorRecord = new ErrorRecord(new ParentContainsErrorRecordException(errorMsg),
                        "ActionPreferenceStop", ErrorCategory.OperationStopped, null);
                    ActionPreferenceStopException ense = new ActionPreferenceStopException(errorRecord);
                    // We cannot call ThrowTerminatingError since this is not a cmdlet or provider
                    throw ense;
                default:
                    Dbg.Assert(false, "all preferences should be checked");
                    throw PSTraceSource.NewArgumentException(nameof(preference),
                        InternalHostUserInterfaceStrings.UnsupportedPreferenceError, preference);
                    // break;
            }
        }

        /// <summary>
        /// If informationBuffers is not null, the respective messages will also
        /// be written to the buffers along with external host.
        /// </summary>
        /// <param name="informationalBuffers">
        /// Buffers to which Debug, Verbose, Warning, Progress, Information messages
        /// will be written to.
        /// </param>
        /// <remarks>
        /// This method is not thread safe. Caller should make sure of the
        /// associated risks.
        /// </remarks>
        internal void SetInformationalMessageBuffers(PSInformationalBuffers informationalBuffers)
        {
            _informationalBuffers = informationalBuffers;
        }

        /// <summary>
        /// Gets the informational message buffers of the host.
        /// </summary>
        /// <returns>Informational message buffers.</returns>
        internal PSInformationalBuffers GetInformationalMessageBuffers()
        {
            return _informationalBuffers;
        }

        private
        void
        WriteDebugLineHelper(string message)
        {
            if (message == null)
            {
                return;
            }

            WriteDebugRecord(new DebugRecord(message));
        }

        /// <summary>
        /// Ask the user whether to continue/stop or break to a nested prompt.
        /// </summary>
        /// <param name="message">
        /// Message to display to the user. This routine will append the text "Continue" to ensure that people know what question
        /// they are answering.
        /// </param>
        /// <param name="actionPreference">
        /// Preference setting which determines the behaviour.  This is by-ref and will be modified based upon what the user
        /// types. (e.g. YesToAll will change Inquire => NotifyContinue)
        /// </param>
        private
        bool
        DebugShouldContinue(string message, ref ActionPreference actionPreference)
        {
            Dbg.Assert(actionPreference == ActionPreference.Inquire, "Why are you inquiring if your preference is not to?");

            bool shouldContinue = false;

            Collection<ChoiceDescription> choices = new Collection<ChoiceDescription>();

            choices.Add(new ChoiceDescription(InternalHostUserInterfaceStrings.ShouldContinueYesLabel, InternalHostUserInterfaceStrings.ShouldContinueYesHelp));
            choices.Add(new ChoiceDescription(InternalHostUserInterfaceStrings.ShouldContinueYesToAllLabel, InternalHostUserInterfaceStrings.ShouldContinueYesToAllHelp));
            choices.Add(new ChoiceDescription(InternalHostUserInterfaceStrings.ShouldContinueNoLabel, InternalHostUserInterfaceStrings.ShouldContinueNoHelp));
            choices.Add(new ChoiceDescription(InternalHostUserInterfaceStrings.ShouldContinueNoToAllLabel, InternalHostUserInterfaceStrings.ShouldContinueNoToAllHelp));
            choices.Add(new ChoiceDescription(InternalHostUserInterfaceStrings.ShouldContinueSuspendLabel, InternalHostUserInterfaceStrings.ShouldContinueSuspendHelp));

            bool endLoop = true;
            do
            {
                endLoop = true;

                switch (
                    PromptForChoice(
                        InternalHostUserInterfaceStrings.ShouldContinuePromptMessage,
                        message,
                        choices,
                        0))
                {
                    case 0:
                        shouldContinue = true;
                        break;

                    case 1:
                        actionPreference = ActionPreference.Continue;
                        shouldContinue = true;
                        break;

                    case 2:
                        shouldContinue = false;
                        break;

                    case 3:
                        // No to All means that we want to stop every time WriteDebug is called. Since No throws an error, I
                        // think that ordinarily, the caller will terminate.  So I don't think the caller will ever get back
                        // calling WriteDebug again, and thus "No to All" might not be a useful option to have.

                        actionPreference = ActionPreference.Stop;
                        shouldContinue = false;
                        break;

                    case 4:
                        // This call returns when the user exits the nested prompt.

                        _parent.EnterNestedPrompt();
                        endLoop = false;
                        break;
                }
            } while (!endLoop);

            return shouldContinue;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <exception cref="HostException">
        /// if <paramref name="record"/> is not null and the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override
        void
        WriteProgress(long sourceId, ProgressRecord record)
        {
            if (record == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(record));
            }

            // Write to Information Buffers
            _informationalBuffers?.AddProgress(record);

            if (_externalUI == null)
            {
                return;
            }

            _externalUI.WriteProgress(sourceId, record);
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <exception cref="HostException">
        /// if <paramref name="message"/> is not null and the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override
        void
        WriteVerboseLine(string message)
        {
            if (message == null)
            {
                return;
            }

            WriteVerboseRecord(new VerboseRecord(message));
        }

        /// <summary>
        /// </summary>
        internal void WriteVerboseRecord(VerboseRecord record)
        {
            WriteVerboseInfoBuffers(record);

            if (_externalUI == null)
            {
                return;
            }

            _externalUI.WriteVerboseLine(record.Message);
        }

        /// <summary>
        /// Writes the VerboseRecord to informational buffers.
        /// </summary>
        /// <param name="record">VerboseRecord.</param>
        internal void WriteVerboseInfoBuffers(VerboseRecord record) => _informationalBuffers?.AddVerbose(record);

        /// <summary>
        /// See base class.
        /// </summary>
        /// <exception cref="HostException">
        /// if <paramref name="message"/> is not null and the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override void WriteWarningLine(string message)
        {
            if (message == null)
            {
                return;
            }

            WriteWarningRecord(new WarningRecord(message));
        }

        /// <summary>
        /// </summary>
        internal void WriteWarningRecord(WarningRecord record)
        {
            WriteWarningInfoBuffers(record);

            if (_externalUI == null)
            {
                return;
            }

            _externalUI.WriteWarningLine(record.Message);
        }

        /// <summary>
        /// Writes the WarningRecord to informational buffers.
        /// </summary>
        /// <param name="record">WarningRecord.</param>
        internal void WriteWarningInfoBuffers(WarningRecord record) => _informationalBuffers?.AddWarning(record);

        /// <summary>
        /// </summary>
        internal void WriteInformationRecord(InformationRecord record)
        {
            WriteInformationInfoBuffers(record);

            if (_externalUI == null)
            {
                return;
            }

            _externalUI.WriteInformation(record);
        }

        /// <summary>
        /// Writes the InformationRecord to informational buffers.
        /// </summary>
        /// <param name="record">WarningRecord.</param>
        internal void WriteInformationInfoBuffers(InformationRecord record) => _informationalBuffers?.AddInformation(record);

        internal static Type GetFieldType(FieldDescription field)
        {
            Type result;
            if (TypeResolver.TryResolveType(field.ParameterAssemblyFullName, out result) ||
                TypeResolver.TryResolveType(field.ParameterTypeFullName, out result))
            {
                return result;
            }

            return null;
        }

        internal static bool IsSecuritySensitiveType(string typeName)
        {
            if (typeName.Equals(nameof(PSCredential), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (typeName.Equals(nameof(SecureString), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="caption">
        /// </param>
        /// <param name="message">
        /// </param>
        /// <param name="descriptions">
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="descriptions"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="descriptions"/>.Count is less than 1.
        /// </exception>
        /// <exception cref="HostException">
        /// if the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override
        Dictionary<string, PSObject>
        Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            if (descriptions == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(descriptions));
            }

            if (descriptions.Count < 1)
            {
                throw PSTraceSource.NewArgumentException(nameof(descriptions), InternalHostUserInterfaceStrings.PromptEmptyDescriptionsError, "descriptions");
            }

            if (_externalUI == null)
            {
                ThrowPromptNotInteractive(message);
            }

            Dictionary<string, PSObject> result = null;

            try
            {
                result = _externalUI.Prompt(caption, message, descriptions);
            }
            catch (PipelineStoppedException)
            {
                // PipelineStoppedException is thrown by host when it wants
                // to stop the pipeline.
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_parent.Context.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                {
                    throw;
                }

                lpl.Stopper.Stop();
            }

            return result;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="message"></param>
        /// <param name="choices"></param>
        /// <param name="defaultChoice">
        /// </param>
        /// <exception cref="HostException">
        /// if the UI property of the external host is null,
        ///     possibly because the PSHostUserInterface is not implemented by the external host
        /// </exception>
        public override
        int
        PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            if (_externalUI == null)
            {
                ThrowPromptNotInteractive(message);
            }

            int result = -1;
            try
            {
                result = _externalUI.PromptForChoice(caption, message, choices, defaultChoice);
            }
            catch (PipelineStoppedException)
            {
                // PipelineStoppedException is thrown by host when it wants
                // to stop the pipeline.
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_parent.Context.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                {
                    throw;
                }

                lpl.Stopper.Stop();
            }

            return result;
        }

        /// <summary>
        /// Presents a dialog allowing the user to choose options from a set of options.
        /// </summary>
        /// <param name="caption">
        /// Caption to precede or title the prompt.  E.g. "Parameters for get-foo (instance 1 of 2)"
        /// </param>
        /// <param name="message">
        /// A message that describes what the choice is for.
        /// </param>
        /// <param name="choices">
        /// An Collection of ChoiceDescription objects that describe each choice.
        /// </param>
        /// <param name="defaultChoices">
        /// The index of the labels in the choices collection element to be presented to the user as
        /// the default choice(s).
        /// </param>
        /// <returns>
        /// The indices of the choice elements that corresponds to the options selected.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface.PromptForChoice"/>
        public Collection<int> PromptForChoice(string caption,
            string message,
            Collection<ChoiceDescription> choices,
            IEnumerable<int> defaultChoices)
        {
            if (_externalUI == null)
            {
                ThrowPromptNotInteractive(message);
            }

            IHostUISupportsMultipleChoiceSelection hostForMultipleChoices =
                _externalUI as IHostUISupportsMultipleChoiceSelection;

            Collection<int> result = null;
            try
            {
                if (hostForMultipleChoices == null)
                {
                    // host did not implement this new interface..
                    // so work with V1 host API to get the behavior..
                    // this will allow Hosts that were developed with
                    // V1 API to interact with PowerShell V2.
                    result = EmulatePromptForMultipleChoice(caption, message, choices, defaultChoices);
                }
                else
                {
                    result = hostForMultipleChoices.PromptForChoice(caption, message, choices, defaultChoices);
                }
            }
            catch (PipelineStoppedException)
            {
                // PipelineStoppedException is thrown by host when it wants
                // to stop the pipeline.
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_parent.Context.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                {
                    throw;
                }

                lpl.Stopper.Stop();
            }

            return result;
        }

        /// <summary>
        /// This method is added to be backward compatible with V1 hosts w.r.t
        /// new PromptForChoice method added in PowerShell V2.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="message"></param>
        /// <param name="choices"></param>
        /// <param name="defaultChoices"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// 1. Choices is null.
        /// 2. Choices.Count = 0
        /// 3. DefaultChoice is either less than 0 or greater than Choices.Count
        /// </exception>
        private Collection<int> EmulatePromptForMultipleChoice(string caption,
            string message,
            Collection<ChoiceDescription> choices,
            IEnumerable<int> defaultChoices)
        {
            Dbg.Assert(_externalUI != null, "externalUI cannot be null.");

            if (choices == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(choices));
            }

            if (choices.Count == 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(choices),
                    InternalHostUserInterfaceStrings.EmptyChoicesError, "choices");
            }

            Dictionary<int, bool> defaultChoiceKeys = new Dictionary<int, bool>();
            if (defaultChoices != null)
            {
                foreach (int defaultChoice in defaultChoices)
                {
                    if ((defaultChoice < 0) || (defaultChoice >= choices.Count))
                    {
                        throw PSTraceSource.NewArgumentOutOfRangeException("defaultChoice", defaultChoice,
                            InternalHostUserInterfaceStrings.InvalidDefaultChoiceForMultipleSelection,
                            "defaultChoice",
                            "choices",
                            defaultChoice);
                    }

                    defaultChoiceKeys.TryAdd(defaultChoice, true);
                }
            }

            // Construct the caption + message + list of choices + default choices
            Text.StringBuilder choicesMessage = new Text.StringBuilder();
            const char newLine = '\n';
            if (!string.IsNullOrEmpty(caption))
            {
                choicesMessage.Append(caption);
                choicesMessage.Append(newLine);
            }

            if (!string.IsNullOrEmpty(message))
            {
                choicesMessage.Append(message);
                choicesMessage.Append(newLine);
            }

            string[,] hotkeysAndPlainLabels = null;
            HostUIHelperMethods.BuildHotkeysAndPlainLabels(choices, out hotkeysAndPlainLabels);

            const string choiceTemplate = "[{0}] {1}  ";
            for (int i = 0; i < hotkeysAndPlainLabels.GetLength(1); ++i)
            {
                string choice =
                    string.Format(
                        Globalization.CultureInfo.InvariantCulture,
                        choiceTemplate,
                        hotkeysAndPlainLabels[0, i],
                        hotkeysAndPlainLabels[1, i]);
                choicesMessage.Append(choice);
                choicesMessage.Append(newLine);
            }

            // default choices
            string defaultPrompt = string.Empty;
            if (defaultChoiceKeys.Count > 0)
            {
                string prepend = string.Empty;
                Text.StringBuilder defaultChoicesBuilder = new Text.StringBuilder();
                foreach (int defaultChoice in defaultChoiceKeys.Keys)
                {
                    string defaultStr = hotkeysAndPlainLabels[0, defaultChoice];
                    if (string.IsNullOrEmpty(defaultStr))
                    {
                        defaultStr = hotkeysAndPlainLabels[1, defaultChoice];
                    }

                    defaultChoicesBuilder.Append(Globalization.CultureInfo.InvariantCulture, $"{prepend}{defaultStr}");
                    prepend = ",";
                }

                string defaultChoicesStr = defaultChoicesBuilder.ToString();

                if (defaultChoiceKeys.Count == 1)
                {
                    defaultPrompt = StringUtil.Format(InternalHostUserInterfaceStrings.DefaultChoice, defaultChoicesStr);
                }
                else
                {
                    defaultPrompt = StringUtil.Format(InternalHostUserInterfaceStrings.DefaultChoicesForMultipleChoices,
                        defaultChoicesStr);
                }
            }

            string messageToBeDisplayed = choicesMessage.ToString() + defaultPrompt + newLine;
            // read choices from the user
            Collection<int> result = new Collection<int>();
            int choicesSelected = 0;
            while (true)
            {
                string choiceMsg = StringUtil.Format(InternalHostUserInterfaceStrings.ChoiceMessage, choicesSelected);
                messageToBeDisplayed += choiceMsg;
                _externalUI.WriteLine(messageToBeDisplayed);
                string response = _externalUI.ReadLine();

                // they just hit enter
                if (response.Length == 0)
                {
                    // this may happen when
                    // 1. user wants to go with the defaults
                    // 2. user selected some choices and wanted those
                    // choices to be picked.

                    // user did not pick up any choices..choose the default
                    if (result.Count == 0)
                    {
                        // if there's a default, pick that one.
                        foreach (int defaultChoice in defaultChoiceKeys.Keys)
                        {
                            result.Add(defaultChoice);
                        }
                    }
                    // allow for no choice selection.
                    break;
                }

                int choicePicked = HostUIHelperMethods.DetermineChoicePicked(response.Trim(), choices, hotkeysAndPlainLabels);

                if (choicePicked >= 0)
                {
                    result.Add(choicePicked);
                    choicesSelected++;
                }
                // reset messageToBeDisplayed
                messageToBeDisplayed = string.Empty;
            }

            return result;
        }

        private readonly PSHostUserInterface _externalUI = null;
        private readonly InternalHostRawUserInterface _internalRawUI = null;
        private readonly InternalHost _parent = null;
        private PSInformationalBuffers _informationalBuffers = null;
    }
}
