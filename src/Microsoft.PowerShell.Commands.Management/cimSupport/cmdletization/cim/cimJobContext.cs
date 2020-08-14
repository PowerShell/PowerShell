// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Management.Automation;

using Microsoft.Management.Infrastructure;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    internal class CimJobContext
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

        internal CimCmdletInvocationContext CmdletInvocationContext { get; private set; }

        internal CimSession Session { get; private set; }

        internal object TargetObject { get; private set; }

        internal string ClassName
        {
            get
            {
                return GetCimClassName(this.CmdletInvocationContext.CmdletDefinitionContext.CmdletizationClassName);
            }
        }

        internal string ClassNameOrNullIfResourceUriIsUsed
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

        internal string Namespace
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

        internal InvocationInfo CmdletInvocationInfo
        {
            get { return this.CmdletInvocationContext.CmdletInvocationInfo; }
        }

        internal string CmdletizationClassName
        {
            get { return this.CmdletInvocationContext.CmdletDefinitionContext.CmdletizationClassName; }
        }

        internal Version CmdletizationModuleVersion
        {
            get { return this.CmdletInvocationContext.CmdletDefinitionContext.CmdletizationModuleVersion; }
        }

        internal ActionPreference ErrorActionPreference
        {
            get { return this.CmdletInvocationContext.ErrorActionPreference; }
        }

        internal ActionPreference WarningActionPreference
        {
            get { return this.CmdletInvocationContext.WarningActionPreference; }
        }

        internal ActionPreference VerboseActionPreference
        {
            get { return this.CmdletInvocationContext.VerboseActionPreference; }
        }

        internal ActionPreference DebugActionPreference
        {
            get { return this.CmdletInvocationContext.DebugActionPreference; }
        }

        internal bool IsRunningInBackground
        {
            get { return this.CmdletInvocationContext.IsRunningInBackground; }
        }

        internal MshCommandRuntime.ShouldProcessPossibleOptimization ShouldProcessOptimization
        {
            get { return this.CmdletInvocationContext.ShouldProcessOptimization; }
        }

        internal bool ShowComputerName
        {
            get { return this.CmdletInvocationContext.ShowComputerName; }
        }

        internal bool SupportsShouldProcess
        {
            get { return this.CmdletInvocationContext.CmdletDefinitionContext.SupportsShouldProcess; }
        }
    }
}
