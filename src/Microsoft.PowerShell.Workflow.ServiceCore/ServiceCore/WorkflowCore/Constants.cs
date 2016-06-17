/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.PowerShell.Workflow
{
    internal static class Constants
    {

        internal const string AsJob = "AsJob";

        // Remoting-related constants
        internal const string ComputerName = "PSComputerName";
        internal const string Credential = "PSCredential";
        internal const string Port = "PSPort";
        internal const string UseSSL = "PSUseSSL";
        internal const string ConfigurationName = "PSConfigurationName";
        internal const string ApplicationName = "PSApplicationName";
        internal const string ConnectionURI = "PSConnectionURI";
        internal const string AllowRedirection = "PSAllowRedirection";
        internal const string SessionOption = "PSSessionOption";
        internal const string Authentication = "PSAuthentication";
        internal const string AuthenticationLevel = "PSAuthenticationLevel";
        internal const string CertificateThumbprint = "PSCertificateThumbprint";
        internal const string PSParameterCollection = "PSParameterCollection";
        internal const string PSInputCollection = "PSInputCollection";
        internal const string InputObject = "InputObject";
        internal const string PSSenderInfo = "PSSenderInfo";
        internal const string PSCurrentDirectory = "PSCurrentDirectory";
        internal const string PSSuspendOnError = "PSSuspendOnError";

        // PowerShell-common constants
        internal const string Verbose = "Verbose";
        internal const string Debug = "Debug";
        internal const string ErrorAction = "ErrorAction";
        internal const string WarningAction = "WarningAction";
        internal const string InformationAction = "InformationAction";
        internal const string ErrorVariable = "ErrorVariable";
        internal const string WarningVariable = "WarningVariable";
        internal const string InformationVariable = "InformationVariable";
        internal const string OutVariable = "OutVariable";
        internal const string OutBuffer = "OutBuffer";
        internal const string PipelineVariable = "PipelineVariable";

        // Retry policy constants
        internal const string ConnectionRetryCount = "PSConnectionRetryCount";
        internal const string ActionRetryCount = "PSActionRetryCount";
        internal const string ConnectionRetryIntervalSec = "PSConnectionRetryIntervalSec";
        internal const string ActionRetryIntervalSec = "PSActionRetryIntervalSec";

        internal const string PrivateMetadata = "PSPrivateMetadata";
        internal const string WorkflowTakesPrivateMetadata = "WorkflowTakesPrivateMetadata";
        internal const string WorkflowJobCreationContext = "WorkflowJobCreationContext";

        // Timers
        internal const string PSRunningTime = "PSRunningTimeoutSec";
        internal const string PSElapsedTime = "PSElapsedTimeoutSec";
        internal const string Int32MaxValueDivideByThousand = "2147483";

        internal const string ModulePath = "PSWorkflowRoot";
        internal const string JobName = "JobName";
        internal const string DefaultComputerName = "localhost";

        // Job Metadata constants
        internal const string JobMetadataInstanceId = "InstanceId";
        internal const string JobMetadataSessionId = "Id";
        internal const string JobMetadataName = "Name";
        internal const string JobMetadataCommand = "Command";
        internal const string JobMetadataStateReason = "Reason";
        internal const string JobMetadataParentName = "ParentName";
        internal const string JobMetadataParentCommand = "ParentCommand";
        internal const string JobMetadataParentInstanceId = "ParentInstanceId";
        internal const string JobMetadataParentSessionId = "ParentSessionId";
        internal const string JobMetadataLocation = "Location";
        internal const string JobMetadataStatusMessage = "StatusMessage";
        internal const string JobMetadataUserName = "UserName";
        internal const string JobMetadataPid = "ProcessId";
        internal const string JobMetadataFilterState = "State";

        internal const string Persist = "PSPersist";
        internal const string PSRequiredModules = "PSRequiredModules";

        internal const string PSWorkflowErrorAction = "PSWorkflowErrorAction";
        internal const int MaxAllowedPersistencePathLength = 120;
    }
}
