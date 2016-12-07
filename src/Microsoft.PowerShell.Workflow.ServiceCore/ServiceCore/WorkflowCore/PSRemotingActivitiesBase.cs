using System;
using System.Activities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.PowerShell.Workflow;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Base class for PowerShell-based workflow activities
    /// </summary>
    public abstract class PSRemotingActivity : PSActivity, IImplementsConnectionRetry
    {
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
        /// Defines the remoting behavior to use when invoking this activity.
        /// </summary>
        [ConnectivityCategory]
        [DefaultValue(null)]
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
        public InArgument<uint?> PSConnectionRetryCount { get; set; }

        /// <summary>
        /// Defines the delay, in seconds, between connection retry attempts.
        /// The default is one second.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<uint?> PSConnectionRetryIntervalSec { get; set; }

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
        /// Declares whether this command supports its own custom remoting.
        /// Commands that support their own custom remoting should return TRUE
        /// from this property, and use the PSComputerName parameter as required
        /// when the 'PSRemotingBehavior' argument is set to 'Custom'.
        /// </summary>
        protected virtual bool SupportsCustomRemoting { get { return false; } }

        /// <summary>
        /// Returns TRUE if the PSComputerName argument has been specified, and
        /// contains at least one target.
        /// </summary>
        /// <param name="context">The workflow NativeActivityContext</param>
        /// <returns></returns>
        protected bool GetIsComputerNameSpecified(ActivityContext context)
        {
            return ((PSComputerName.Get(context) != null) &&
                (PSComputerName.Get(context).Length > 0));
        }

        // Prepare the commands
        /// <summary>
        /// Prepare commands that use PSRP for remoting...
        /// </summary>
        /// <param name="context">The activity context to use</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected override List<ActivityImplementationContext> GetImplementation(NativeActivityContext context)
        {
            string[] computernames = PSComputerName.Get(context);
            string[] connectionUris = PSConnectionUri.Get(context);
            PSSessionOption sessionOptions = PSSessionOption.Get(context);
            List<ActivityImplementationContext> commands = new List<ActivityImplementationContext>();

            // Configure the remote connectivity options
            RemotingBehavior remotingBehavior = PSRemotingBehavior.Get(context);
            if (PSRemotingBehavior.Expression == null)
            {
                remotingBehavior = RemotingBehavior.PowerShell;
            }

            // If they've specified the 'Custom' remoting behavior, ensure the activity
            // supports it.
            if ((remotingBehavior == RemotingBehavior.Custom) && (!SupportsCustomRemoting))
            {
                throw new ArgumentException(Resources.CustomRemotingNotSupported);
            }

            if (PSCredential.Get(context) != null && PSAuthentication.Get(context) == AuthenticationMechanism.NegotiateWithImplicitCredential)
            {
                throw new ArgumentException(Resources.CredentialParameterCannotBeSpecifiedWithNegotiateWithImplicitAuthentication);
            }

            // we need connection info to be populated even for the custom remoting case.
            // This is because the ComputerName is picked up from the connection info field
            if ((remotingBehavior == RemotingBehavior.PowerShell || (IsActivityInlineScript(this) && RunWithCustomRemoting(context))) &&
                (GetIsComputerNameSpecified(context) || (connectionUris != null && connectionUris.Length > 0)))
            {
                List<WSManConnectionInfo> connectionInfo = ActivityUtils.GetConnectionInfo(
                    computernames,
                    connectionUris, PSCertificateThumbprint.Get(context), PSConfigurationName.Get(context),
                    PSUseSsl.Get(context), PSPort.Get(context), PSApplicationName.Get(context),
                    PSCredential.Get(context), PSAuthentication.Get(context).GetValueOrDefault(AuthenticationMechanism.Default),
                    PSAllowRedirection.Get(context).GetValueOrDefault(false),
                    sessionOptions);

                foreach (WSManConnectionInfo connection in connectionInfo)
                {
                    CreatePowerShellInstance(context, connection, commands);
                }
            }
            // Configure the local invocation options
            else
            {
                CreatePowerShellInstance(context, null, commands);
            }

            return commands;
        }

        /// <summary>
        /// Creates Powershell instance and adds the command to it
        /// </summary>
        /// <param name="context">The activity context to use</param>
        /// <param name="connection">The wsman connection to use</param>
        /// <param name="commands">The list of commands</param>
        private void CreatePowerShellInstance(NativeActivityContext context, WSManConnectionInfo connection,
            List<ActivityImplementationContext> commands)
        {
            // Create the PowerShell instance, and add the command to it.
            ActivityImplementationContext implementationContext = GetPowerShell(context);
#if true
            Runspace runspace;
            if (connection != null)
            {
                implementationContext.ConnectionInfo = connection;
                runspace = RunspaceFactory.CreateRunspace(connection);
                implementationContext.PowerShellInstance.Runspace = runspace;
            }
            else
            {
                // if PSComputerName is "" or $null than connection is NULL
                UpdateImplementationContextForLocalExecution(implementationContext, context);
            }

#endif
            // Add it to the queue of commands to execute.
            commands.Add(implementationContext);
        }
    }
}