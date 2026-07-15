// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Management.Automation;

using Microsoft.Management.Infrastructure;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    internal sealed class CimJobContext
    {
        internal CimJobContext(
            CimCmdletInvocationContext cmdletInvocationContext,
            CimSession session,
            object targetObject)
        {
            this.CmdletInvocationContext = cmdletInvocationContext;

            this.Session = session;
            this.TargetObject = targetObject ?? this.ClassName;
        }

        public CimCmdletInvocationContext CmdletInvocationContext { get; }

        public CimSession Session { get; }

        public object TargetObject { get; }

        public string ClassName
        {
            get
            {
                return GetCimClassName(this.CmdletInvocationContext.CmdletDefinitionContext.CmdletizationClassName);
            }
        }

        public string ClassNameOrNullIfResourceUriIsUsed
        {
            get
            {
                if (this.CmdletInvocationContext.CmdletDefinitionContext.ResourceUri != null)
                {
                    return null;
                }

                return this.ClassName;
            }
        }

        public string Namespace
        {
            get
            {
                if (!string.IsNullOrEmpty(this.CmdletInvocationContext.NamespaceOverride))
                {
                    return this.CmdletInvocationContext.NamespaceOverride;
                }

                return GetCimNamespace(this.CmdletInvocationContext.CmdletDefinitionContext.CmdletizationClassName);
            }
        }

        private static void ExtractCimNamespaceAndClassName(string cmdletizationClassName, out string cimNamespace, out string cimClassName)
        {
            int indexOfLastBackslash = cmdletizationClassName.LastIndexOf('\\');
            int indexOfLastForwardSlash = cmdletizationClassName.LastIndexOf('/');
            int indexOfLastSeparator = Math.Max(indexOfLastBackslash, indexOfLastForwardSlash);
            if (indexOfLastSeparator != (-1))
            {
                cimNamespace = cmdletizationClassName.Substring(0, indexOfLastSeparator);
                cimClassName = cmdletizationClassName.Substring(indexOfLastSeparator + 1, cmdletizationClassName.Length - indexOfLastSeparator - 1);
            }
            else
            {
                cimNamespace = null;
                cimClassName = cmdletizationClassName;
            }
        }

        private static string GetCimClassName(string cmdletizationClassName)
        {
            string throwAway;
            string cimClassName;
            ExtractCimNamespaceAndClassName(cmdletizationClassName, out throwAway, out cimClassName);
            return cimClassName;
        }

        private static string GetCimNamespace(string cmdletizationClassName)
        {
            string cimNamespace;
            string throwAway;
            ExtractCimNamespaceAndClassName(cmdletizationClassName, out cimNamespace, out throwAway);
            return cimNamespace;
        }

        internal string PrependComputerNameToMessage(string message)
        {
            string computerName = this.Session.ComputerName;
            if (computerName == null)
            {
                return message;
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                CmdletizationResources.CimJob_ComputerNameConcatenationTemplate,
                computerName,
                message);
        }

        public InvocationInfo CmdletInvocationInfo
        {
            get { return this.CmdletInvocationContext.CmdletInvocationInfo; }
        }

        public string CmdletizationClassName
        {
            get { return this.CmdletInvocationContext.CmdletDefinitionContext.CmdletizationClassName; }
        }

        public Version CmdletizationModuleVersion
        {
            get { return this.CmdletInvocationContext.CmdletDefinitionContext.CmdletizationModuleVersion; }
        }

        public ActionPreference ErrorActionPreference
        {
            get { return this.CmdletInvocationContext.ErrorActionPreference; }
        }

        public ActionPreference WarningActionPreference
        {
            get { return this.CmdletInvocationContext.WarningActionPreference; }
        }

        public ActionPreference VerboseActionPreference
        {
            get { return this.CmdletInvocationContext.VerboseActionPreference; }
        }

        public ActionPreference DebugActionPreference
        {
            get { return this.CmdletInvocationContext.DebugActionPreference; }
        }

        public bool IsRunningInBackground
        {
            get { return this.CmdletInvocationContext.IsRunningInBackground; }
        }

        public MshCommandRuntime.ShouldProcessPossibleOptimization ShouldProcessOptimization
        {
            get { return this.CmdletInvocationContext.ShouldProcessOptimization; }
        }

        public bool ShowComputerName
        {
            get { return this.CmdletInvocationContext.ShowComputerName; }
        }

        public bool SupportsShouldProcess
        {
            get { return this.CmdletInvocationContext.CmdletDefinitionContext.SupportsShouldProcess; }
        }
    }
}
