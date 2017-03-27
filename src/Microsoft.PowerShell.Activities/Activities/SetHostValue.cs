using System;
using System.Reflection;
using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.ComponentModel;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Activity to set a host value in a Workflow.
    /// </summary>
    public sealed class SetPSWorkflowData : NativeActivity
    {
        /// <summary>
        /// The variable to set, if not included in the PSWorkflowRuntimeVariable enum.
        /// </summary>
        [DefaultValue(null)]
        public InArgument<string> OtherVariableName
        {
            get;
            set;
        }

        /// <summary>
        ///
        /// </summary>
        [DefaultValue(null)]
        public InArgument<Object> Value
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the remoting behavior to use when invoking this activity.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(RemotingBehavior.PowerShell)]
        public InArgument<RemotingBehavior> PSRemotingBehavior { get; set; }

        /// <summary>
        /// Defines the number of retries that the activity will make to connect to a remote
        /// machine when it encounters an error. The default is to not retry.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSConnectionRetryCount
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the delay, in seconds, between connection retry attempts.
        /// The default is one second.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSConnectionRetryIntervalSec
        {
            get;
            set;
        }

        /// <summary>
        /// The Input stream for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<PSDataCollection<PSObject>> Input
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to connect the input stream for this activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(false)]
        public bool UseDefaultInput
        {
            get;
            set;
        }

        /// <summary>
        /// The output stream from the activity
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<PSObject>> Result
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to append output to Result.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public bool? AppendOutput
        {
            get;
            set;
        }

        /// <summary>
        /// The Error stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<ErrorRecord>> PSError
        {
            get;
            set;
        }

        /// <summary>
        /// The Progress stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<ProgressRecord>> PSProgress
        {
            get;
            set;
        }

        /// <summary>
        /// The Verbose stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<VerboseRecord>> PSVerbose
        {
            get;
            set;
        }

        /// <summary>
        /// The Debug stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<DebugRecord>> PSDebug
        {
            get;
            set;
        }

        /// <summary>
        /// The Warning stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<WarningRecord>> PSWarning
        {
            get;
            set;
        }

        /// <summary>
        /// The Information stream / collection for the activity.
        /// </summary>
        [InputAndOutputCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InOutArgument<PSDataCollection<InformationRecord>> PSInformation
        {
            get;
            set;
        }

        /// <summary>
        /// The computer name to invoke this activity on.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<string[]> PSComputerName
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the credential to use in the remote connection.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<PSCredential> PSCredential
        {
            get;
            set;
        }

        /// <summary>
        /// Forces the activity to return non-serialized objects. Resulting objects
        /// have functional methods and properties (as opposed to serialized versions
        /// of them), but will not survive persistence when the Workflow crashes or is
        /// persisted.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> PSDisableSerialization
        {
            get;
            set;
        }

        /// <summary>
        /// Forces the activity to not call the persist functionality, which will be responsible for
        /// persisting the workflow state onto the disk.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> PSPersist
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether to merge error data to the output stream
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> MergeErrorToOutput
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the maximum amount of time, in seconds, that this activity may run.
        /// The default is unlimited.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSActionRunningTimeoutSec
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the maximum amount of time that the workflow engine should wait for a bookmark
        /// to be resumed.
        /// The default is unlimited.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSBookmarkTimeoutSec
        {
            get;
            set;
        }

        /// <summary>
        /// This the list of module names (or paths) that are required to run this Activity successfully.
        /// The default is null.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<string[]> PSRequiredModules
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the number of retries that the activity will make when it encounters
        /// an error during execution of its action. The default is to not retry.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSActionRetryCount
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the delay, in seconds, between action retry attempts.
        /// The default is one second.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSActionRetryIntervalSec
        {
            get;
            set;
        }

        /// <summary>
        /// The port to use in a remote connection attempt. The default is:
        /// HTTP: 5985, HTTPS: 5986.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSPort { get; set; }

        /// <summary>
        /// Determines whether to use SSL in the connection attempt. The default is false.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> PSUseSsl { get; set; }

        /// <summary>
        /// Determines whether to allow redirection by the remote computer. The default is false.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<bool?> PSAllowRedirection { get; set; }

        /// <summary>
        /// Defines the remote application name to connect to. The default is "wsman".
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<string> PSApplicationName { get; set; }

        /// <summary>
        /// Defines the remote configuration name to connect to. The default is "Microsoft.PowerShell".
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<string> PSConfigurationName { get; set; }

        /// <summary>
        /// Defines the fully-qualified remote URI to connect to. When specified, the PSComputerName,
        /// PSApplicationName, PSConfigurationName, and PSPort are not used.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<string[]> PSConnectionUri { get; set; }

        /// <summary>
        /// Defines the authentication type to be used in the remote connection.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<AuthenticationMechanism?> PSAuthentication { get; set; }

        /// <summary>
        /// Defines the certificate thumbprint to be used in the remote connection.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<string> PSCertificateThumbprint { get; set; }

        /// <summary>
        /// Defines any session options to be used in the remote connection.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
        public InArgument<PSSessionOption> PSSessionOption { get; set; }


        /// <summary>
        /// Execute the logic for this activity...
        /// </summary>
        /// <param name="context"></param>
        protected override void Execute(NativeActivityContext context)
        {
            // Retrieve our host overrides
            HostParameterDefaults hostValues = context.GetExtension<HostParameterDefaults>();
            SetHostValuesByVariableName(context, hostValues);
            SetHostValuesByProperty(context, hostValues);
        }

        private void SetHostValuesByProperty(NativeActivityContext context, HostParameterDefaults hostValues)
        {
            Type currentType = this.GetType();

            // Populate any of our parameters into the host defaults.
            foreach (PropertyInfo field in currentType.GetProperties())
            {
                // See if it's an argument
                if (typeof(Argument).IsAssignableFrom(field.PropertyType))
                {
                    // Skip the ones that are specific to this activity
                    if (String.Equals("VariableToSet", field.Name, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals("OtherVariableName", field.Name, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals("Value", field.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Handle Bookmark timeouts, but don't set them as a host default.
                    if (String.Equals("PSBookmarkTimeoutSec", field.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // See if this is trying to change the bookmark timeout
                        if (PSBookmarkTimeoutSec.Get(context).HasValue)
                        {
                            SafelySetResumeBookmarkTimeout(TimeSpan.FromSeconds(PSBookmarkTimeoutSec.Get(context).Value));
                        }
                        else
                        {
                            SafelySetResumeBookmarkTimeout(TimeSpan.FromSeconds(0));
                        }

                        continue;
                    }

                    // Get the argument
                    Argument currentArgument = (Argument)field.GetValue(this, null);
                    if (currentArgument.Expression != null)
                    {
                        if (currentArgument.Get(context) == null)
                        {
                            hostValues.Parameters.Remove(field.Name);
                        }
                        else
                        {
                            hostValues.Parameters[field.Name] = currentArgument.Get(context);
                        }
                    }
                }
            }
        }

        private void SetHostValuesByVariableName(NativeActivityContext context, HostParameterDefaults hostValues)
        {
            // Set the Command / host metadata
            string variableName = null;

            if (OtherVariableName.Get(context) != null)
            {
                if (OtherVariableName.Expression != null)
                {
                    string value = OtherVariableName.Get(context);

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        variableName = value;
                    }
                }

                if (String.Equals(variableName, "Position", StringComparison.OrdinalIgnoreCase))
                {
                    HostSettingCommandMetadata metadata = hostValues.HostCommandMetadata;

                    // The position should come in as line:column:command
                    string positionMessage = (string)Value.Get(context);
                    string[] positionElements = positionMessage.Split(new char[] { ':' }, 3);

                    string line = positionElements[0].Trim();
                    string column = positionElements[1].Trim();
                    string commandName = positionElements[2].Trim();

                    if (!String.IsNullOrEmpty(line))
                    {
                        metadata.StartLineNumber = Int32.Parse(line, CultureInfo.InvariantCulture);
                    }

                    if (!String.IsNullOrEmpty(column))
                    {
                        metadata.StartColumnNumber = Int32.Parse(line, CultureInfo.InvariantCulture);
                    }

                    if (!String.IsNullOrEmpty(commandName))
                    {
                        metadata.CommandName = commandName;
                    }
                }
                else
                {
                    if (Value.Get(context) == null)
                    {
                        hostValues.Parameters.Remove(variableName);
                    }
                    else
                    {
                        hostValues.Parameters[variableName] = Value.Get(context);
                    }
                }
            }
        }

        /// <summary>
        /// Internal reflection call to set the ResumeBookmarkTimeout property, which
        /// controls how much time Workflow allows an activity to run while a bookmark is
        /// being resumed. The workflow default is 30 seconds, which can be exceeded
        /// on heavily loaded systems especially on parallel workflows.
        /// There is not a public property for this value, so the implementation below is
        /// the one recommended by the workflow team.
        /// </summary>
        /// <param name="timeout">How long to wait.</param>
        private static void SafelySetResumeBookmarkTimeout(TimeSpan timeout)
        {
            Type activityDefaults = Type.GetType("System.Activities.ActivityDefaults, System.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            if (activityDefaults != null)
            {
                FieldInfo resumeBookmarkTimeout = activityDefaults.GetField("ResumeBookmarkTimeout");
                if (resumeBookmarkTimeout != null)
                {
                    // This is an attempt to reset the workflow default.
                    if (timeout.TotalSeconds == 0)
                    {
                        // First see if it's been explicitly set. If so, don't reset it.
                        TimeSpan currentTimeout = (TimeSpan)resumeBookmarkTimeout.GetValue(null);
                        if (currentTimeout.TotalSeconds == 30)
                        {
                            resumeBookmarkTimeout.SetValue(null, TimeSpan.MaxValue);
                        }
                    }
                    else
                    {
                        // They've specified a value. Use it.
                        resumeBookmarkTimeout.SetValue(null, timeout);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Fail("Could not find ResumeBookmarkTimeout property");
                }
            }
            else
            {
                System.Diagnostics.Debug.Fail("Could not find ResumeBookmarkTimeout type");
            }
        }
    }
}
