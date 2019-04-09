// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A cmdlet that sets the properties of the TraceSwitch instances that are instantiated in the process.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "TraceSource", DefaultParameterSetName = "optionsSet", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113400")]
    [OutputType(typeof(PSTraceSource))]
    public class SetTraceSourceCommand : TraceListenerCommandBase
    {
        #region Parameters

        /// <summary>
        /// The TraceSource parameter determines which TraceSource categories the
        /// operation will take place on.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Name
        {
            get { return base.NameInternal; }

            set { base.NameInternal = value; }
        }

        /// <summary>
        /// The flags to be set on the TraceSource.
        /// </summary>
        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true, ParameterSetName = "optionsSet")]
        public PSTraceSourceOptions Option
        {
            get { return base.OptionsInternal; }

            set
            {
                base.OptionsInternal = value;
            }
        }

        /// <summary>
        /// The parameter which determines the options for output from the trace listeners.
        /// </summary>
        [Parameter(ParameterSetName = "optionsSet")]
        public TraceOptions ListenerOption
        {
            get { return base.ListenerOptionsInternal; }

            set
            {
                base.ListenerOptionsInternal = value;
            }
        }

        /// <summary>
        /// Adds the file trace listener using the specified file.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "optionsSet")]
        [Alias("PSPath", "Path")]
        public string FilePath
        {
            get { return base.FileListener; }

            set { base.FileListener = value; }
        }

        /// <summary>
        /// Force parameter to control read-only files.
        /// </summary>
        [Parameter(ParameterSetName = "optionsSet")]
        public SwitchParameter Force
        {
            get { return base.ForceWrite; }

            set { base.ForceWrite = value; }
        }

        /// <summary>
        /// If this parameter is specified the Debugger trace listener will be added.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "optionsSet")]
        public SwitchParameter Debugger
        {
            get { return base.DebuggerListener; }

            set { base.DebuggerListener = value; }
        }

        /// <summary>
        /// If this parameter is specified the Msh Host trace listener will be added.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "optionsSet")]
        public SwitchParameter PSHost
        {
            get { return base.PSHostListener; }

            set { base.PSHostListener = value; }
        }

        /// <summary>
        /// If set, the specified listeners will be removed regardless of their type.
        /// </summary>
        [Parameter(ParameterSetName = "removeAllListenersSet")]
        [ValidateNotNullOrEmpty]
        public string[] RemoveListener { get; set; } = new string[] { "*" };

        /// <summary>
        /// If set, the specified file trace listeners will be removed.
        /// </summary>
        [Parameter(ParameterSetName = "removeFileListenersSet")]
        [ValidateNotNullOrEmpty]
        public string[] RemoveFileListener { get; set; } = new string[] { "*" };

        /// <summary>
        /// Determines if the modified PSTraceSource should be written out.
        /// Default is false.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "optionsSet")]
        public SwitchParameter PassThru
        {
            get { return _passThru; }

            set { _passThru = value; }
        }

        private bool _passThru;

        #endregion Parameters

        #region Cmdlet code

        /// <summary>
        /// Sets the TraceSource properties.
        /// </summary>
        protected override void ProcessRecord()
        {
            Collection<PSTraceSource> matchingSources = null;

            switch (ParameterSetName)
            {
                case "optionsSet":
                    Collection<PSTraceSource> preconfiguredTraceSources = null;
                    matchingSources = ConfigureTraceSource(Name, true, out preconfiguredTraceSources);

                    if (PassThru)
                    {
                        WriteObject(matchingSources, true);
                        WriteObject(preconfiguredTraceSources, true);
                    }

                    break;

                case "removeAllListenersSet":
                    matchingSources = GetMatchingTraceSource(Name, true);
                    RemoveListenersByName(matchingSources, RemoveListener, false);
                    break;

                case "removeFileListenersSet":
                    matchingSources = GetMatchingTraceSource(Name, true);
                    RemoveListenersByName(matchingSources, RemoveFileListener, true);
                    break;
            }
        }

        #endregion Cmdlet code
    }
}
