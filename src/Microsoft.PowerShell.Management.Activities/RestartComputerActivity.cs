//
//    Copyright (C) Microsoft.  All rights reserved.
//
using System;
using System.Activities;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime;
using System.Activities.Statements;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.ComponentModel;
using Microsoft.PowerShell.Activities;
using System.Management.Automation.Tracing;
using System.Globalization;

namespace Microsoft.PowerShell.Management.Activities
{
    /// <summary>
    /// RestartActivityContext
    /// </summary>
    [Serializable]
    public class RestartActivityContext {
        /// <summary>
        /// Indicates whether a self restart is needed.
        /// </summary>
        [NonSerialized]
        internal bool NeedsRestart = false;
    }

    /// <summary>
    /// RestartComputerActivity
    /// </summary>
    public sealed class RestartComputer : PSActivity
    {
        /// <summary>
        /// Gets the display name of the command invoked by this activity.
        /// </summary>
        public RestartComputer()
        {
            this.DisplayName = "Restart-Computer";
        }

        private static Tracer _structuredTracer = new Tracer();

        /// <summary>
        /// If true, them the workflow will not checkpoint and resume after
        /// the computer is restarted.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public static bool DisableSelfRestart
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the fully qualified name of the command invoked by this activity.
        /// </summary>
        public override string PSCommandName
        {
            get
            {
                return "Microsoft.PowerShell.Management\\Restart-Computer";
            }
        }

        // Arguments

        /// <summary>
        /// Provides access to the Authentication parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public InArgument<System.Management.AuthenticationLevel> DcomAuthentication
        {
            get;
            set;
        }

        /// <summary>
        /// Provides access to the Impersonation parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.ImpersonationLevel> Impersonation
        {
            get;
            set;
        }

        /// <summary>
        /// Provides access to the WsmanAuthentication parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public InArgument<System.String> WsmanAuthentication { get; set; }

        /// <summary>
        /// Provides access to the Protocol parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.String> Protocol { get; set; }

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
        /// Provides access to the Force parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Force
        {
            get;
            set;
        }

        /// <summary>
        /// Provides access to the ThrottleLimit parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> ThrottleLimit
        {
            get;
            set;
        }

        /// <summary>
        /// Provides access to the Wait parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Management.Automation.SwitchParameter> Wait
        {
            get;
            set;
        }

        /// <summary>
        /// Provides access to the Timeout parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int32> Timeout
        {
            get;
            set;
        }

        /// <summary>
        /// Provides access to the For parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<Microsoft.PowerShell.Commands.WaitForServiceTypes> For
        {
            get;
            set;
        }

        /// <summary>
        /// Provides access to the Delay parameter.
        /// </summary>
        [ParameterSpecificCategory]
        [DefaultValue(null)]
        public InArgument<System.Int16> Delay
        {
            get;
            set;
        }

        private Variable<RestartActivityContext> restartActivityContext = new Variable<RestartActivityContext>("RestartActivityContext");

        private readonly PSPersist persistActivity = new PSPersist();

        /// <summary>
        /// Returns a configured instance of System.Management.Automation.PowerShell, pre-populated with the command to run.
        /// </summary>
        /// <param name="context">The NativeActivityContext for the currently running activity.</param>
        /// <returns>A populated instance of System.Management.Automation.PowerShell</returns>
        /// <remarks>The infrastructure takes responsibility for closing and disposing the PowerShell instance returned.</remarks>
        protected override ActivityImplementationContext GetPowerShell(NativeActivityContext context)
        {
            System.Management.Automation.PowerShell invoker = global::System.Management.Automation.PowerShell.Create();
            System.Management.Automation.PowerShell targetCommand = invoker.AddCommand(PSCommandName);

            // Get the host default values we should use if no explicit parameter was provided.
            var hostExtension = context.GetExtension<HostParameterDefaults>();
            Dictionary<string, object> parameterDefaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (hostExtension != null)
            {
                Dictionary<string, object> incomingArguments = hostExtension.Parameters;
                foreach (KeyValuePair<string, object> parameterDefault in incomingArguments)
                {
                    parameterDefaults[parameterDefault.Key] = parameterDefault.Value;
                }
            }


            if (DcomAuthentication.Expression != null)
            {
                targetCommand.AddParameter("DcomAuthentication", DcomAuthentication.Get(context));

            }

            if (Impersonation.Expression != null)
            {
                targetCommand.AddParameter("Impersonation", Impersonation.Get(context));
            }

            // See if the DCOM protocol is to be used
            bool usingDcom = false;
            if (Protocol.Expression != null)
            {
                string protocol = Protocol.Get(context);
                targetCommand.AddParameter("Protocol", protocol);
                if (string.Equals(protocol, "DCOM", StringComparison.OrdinalIgnoreCase))
                {
                    usingDcom = true;
                }
            }

            // Get the WSMan authentication mechanism to use. If no expression was specified,
            // and DCOM is not being used, get the default from the host.
            if (WsmanAuthentication.Expression != null)
            {
                targetCommand.AddParameter("WsmanAuthentication", WsmanAuthentication.Get(context));
            }
            else
            {
                if (!usingDcom)
                {
                    if (parameterDefaults.ContainsKey("PSAuthentication"))
                    {
                        string authString = parameterDefaults["PSAuthentication"].ToString();
                        // Note: the underlying cmdlet does support NegotiateWithImplicitCredential so it is expected
                        // that passing this in as-is will result in an (appropriate) error being emitted in that case.
                        targetCommand.AddParameter("WsmanAuthentication", authString);
                    }
                }
            }

            // Map PSComputerName to the underlying cmdlet name only if the computername is not empty
            string[] computerName = GetPSComputerName(context);
            if ((computerName != null) && (computerName.Length != 0))
            {
                targetCommand.AddParameter("ComputerName", computerName);
            }

            // Map PSCredential to credential. If no expression was provided, then use the default.
            if (PSCredential.Expression != null)
            {
                targetCommand.AddParameter("Credential", PSCredential.Get(context));
            }
            else
            {
                if (parameterDefaults.ContainsKey("PSCredential"))
                {
                    targetCommand.AddParameter("Credential", parameterDefaults["PSCredential"]);
                }
            }

            if (Force.Expression != null)
            {
                targetCommand.AddParameter("Force", Force.Get(context));
            }

            if (ThrottleLimit.Expression != null)
            {
                targetCommand.AddParameter("ThrottleLimit", ThrottleLimit.Get(context));
            }

            // Ignore the -Wait parameter in the self-restart case.
            if (!IsSelfRestart(context))
            {
                if (Wait.Expression != null)
                {
                    targetCommand.AddParameter("Wait", Wait.Get(context));
                }

                if (For.Expression != null)
                {
                    targetCommand.AddParameter("For", For.Get(context));
                }

                if (Delay.Expression != null)
                {
                    targetCommand.AddParameter("Delay", Delay.Get(context));
                }
            }

            if (Timeout.Expression != null)
            {
                targetCommand.AddParameter("Timeout", Timeout.Get(context));
            }

            return new ActivityImplementationContext()
            {
                PowerShellInstance = invoker
            };
        }

        /// <summary>
        /// Execute this command for this activity.
        /// </summary>
        /// <param name="context"></param>
        protected override void Execute(NativeActivityContext context)
        {
            if (!IsSelfRestart(context))
            {
                if (_structuredTracer.IsEnabled)
                {
                    string[] computerNames = new string[] { };
                    computerNames = PSComputerName.Get(context);
                    _structuredTracer.DebugMessage("Executing activity '" + this.DisplayName + "', restarting managed nodes: [" + string.Join(", ", computerNames, ", ") + "]");
                }
                base.Execute(context);
            }
            else
            {
                _structuredTracer.DebugMessage("Executing activity '" + this.DisplayName + "',doing self-restart" );
                PersistAndRestart(context);
            }
        }

        /// <summary>
        /// Test to see if we're restarting the machine we're running on.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>true if we are restarting the local machine.</returns>
        private bool IsSelfRestart(NativeActivityContext context)
        {
            string[] computerName = GetPSComputerName(context);

            if (computerName == null || computerName.Length == 0)
                return true;

            // NOTE: this is known to be problematic as the number of ways of saying "me" in the various
            // protocols is potentially unbounded. This should be reviewed periodically.
            foreach (string s in computerName)
            {
                string cn = s.Trim();
                if (string.Equals(cn, "localhost", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cn, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cn, "0:0:0:0:0:0:0:1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cn, "::1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cn, ".", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(cn, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the PSComputerName argument from the context
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private string[] GetPSComputerName(NativeActivityContext context)
        {
            if (PSComputerName.Expression != null)
            {
                return PSComputerName.Get(context);
            }

            // Retrieve our host overrides
            var hostValues = context.GetExtension<HostParameterDefaults>();

            if (hostValues != null)
            {
                Dictionary<string, object> incomingArguments = hostValues.Parameters;
                if (incomingArguments.ContainsKey("PSComputerName"))
                {
                    return incomingArguments["PSComputerName"] as string[];
                }
            }

            return null;
        }

        /// <summary>
        /// CacheMetadata
        /// </summary>
        /// <param name="metadata"></param>
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            metadata.AddImplementationChild(this.persistActivity);
            metadata.AddImplementationVariable(restartActivityContext);
        }

        private void PersistAndRestart(NativeActivityContext executionContext)
        {
            if (DisableSelfRestart)
            {
                _structuredTracer.DebugMessage("Executing activity '" + this.DisplayName + "', resume of workflow after restart was disabled.");
                return;
            }

            _structuredTracer.DebugMessage("Executing activity '" + this.DisplayName + "' scheduling persistence for workflow resumption after restart.");
            RestartActivityContext c = new RestartActivityContext();
            c.NeedsRestart = true;
            restartActivityContext.Set(executionContext, c);
            
            executionContext.ScheduleActivity(persistActivity, SelfRestart);
        }

        private void SelfRestart(NativeActivityContext context, ActivityInstance target)
        {
            RestartActivityContext restartContext = restartActivityContext.Get(context);
            if (!restartContext.NeedsRestart)
            {
                _structuredTracer.DebugMessage("Executing activity '" + this.DisplayName + "', local machine (" + Environment.MachineName + ") has been restarted.");
                return;
            }

            _structuredTracer.DebugMessage("Executing activity '" + this.DisplayName + "', local machine (" + Environment.MachineName + ") is self-restarting");
            base.Execute(context);
        }
    }
}
