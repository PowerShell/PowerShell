// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Reflection;

namespace System.Management.Automation.Runspaces
{
    public sealed partial class TypeTable
    {
        private void Process_TypesV3_Ps1Xml(string filePath, ConcurrentBag<string> errors)
        {
            typesInfo.Add(new SessionStateTypeEntry(filePath));

            string typeName = null;
            PSMemberInfoInternalCollection<PSMemberInfo> typeMembers = null;
            PSMemberInfoInternalCollection<PSMemberInfo> memberSetMembers = null;
            HashSet<string> newMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            #region System.Security.Cryptography.X509Certificates.X509Certificate2

            typeName = @"System.Security.Cryptography.X509Certificates.X509Certificate2";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"EnhancedKeyUsageList");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"EnhancedKeyUsageList",
                    GetScriptBlock(@",(new-object Microsoft.Powershell.Commands.EnhancedKeyUsageProperty -argumentlist $this).EnhancedKeyUsageList;"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"DnsNameList");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"DnsNameList",
                    GetScriptBlock(@",(new-object Microsoft.Powershell.Commands.DnsNameProperty -argumentlist $this).DnsNameList;"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"SendAsTrustedIssuer");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"SendAsTrustedIssuer",
                    GetScriptBlock(@"[Microsoft.Powershell.Commands.SendAsTrustedIssuerProperty]::ReadSendAsTrustedIssuerProperty($this)"),
                    GetScriptBlock(@"$sendAsTrustedIssuer = $args[0]
                    [Microsoft.Powershell.Commands.SendAsTrustedIssuerProperty]::WriteSendAsTrustedIssuerProperty($this,$this.PsPath,$sendAsTrustedIssuer)"),
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Security.Cryptography.X509Certificates.X509Certificate2

            #region System.Management.Automation.Remoting.PSSenderInfo

            typeName = @"System.Management.Automation.Remoting.PSSenderInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"ConnectedUser");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"ConnectedUser",
                    GetScriptBlock(@"$this.UserInfo.Identity.Name"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"RunAsUser");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"RunAsUser",
                    GetScriptBlock(@"if($null -ne $this.UserInfo.WindowsIdentity)
            {
                $this.UserInfo.WindowsIdentity.Name
            }"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.Remoting.PSSenderInfo

            #region System.Management.Automation.CompletionResult

            typeName = @"System.Management.Automation.CompletionResult";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.CompletionResult

            #region Deserialized.System.Management.Automation.CompletionResult

            typeName = @"Deserialized.System.Management.Automation.CompletionResult";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"TargetTypeForDeserialization", typeof(Microsoft.PowerShell.DeserializingTypeConverter)),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Deserialized.System.Management.Automation.CompletionResult

            #region System.Management.Automation.CommandCompletion

            typeName = @"System.Management.Automation.CommandCompletion";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.CommandCompletion

            #region Deserialized.System.Management.Automation.CommandCompletion

            typeName = @"Deserialized.System.Management.Automation.CommandCompletion";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"TargetTypeForDeserialization", typeof(Microsoft.PowerShell.DeserializingTypeConverter)),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Deserialized.System.Management.Automation.CommandCompletion

            #region Microsoft.PowerShell.Commands.ModuleSpecification

            typeName = @"Microsoft.PowerShell.Commands.ModuleSpecification";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.PowerShell.Commands.ModuleSpecification

            #region Deserialized.Microsoft.PowerShell.Commands.ModuleSpecification

            typeName = @"Deserialized.Microsoft.PowerShell.Commands.ModuleSpecification";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"TargetTypeForDeserialization", typeof(Microsoft.PowerShell.DeserializingTypeConverter)),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Deserialized.Microsoft.PowerShell.Commands.ModuleSpecification

            #region System.Management.Automation.JobStateEventArgs

            typeName = @"System.Management.Automation.JobStateEventArgs";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationDepth", 2),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.JobStateEventArgs

            #region Deserialized.System.Management.Automation.JobStateEventArgs

            typeName = @"Deserialized.System.Management.Automation.JobStateEventArgs";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"TargetTypeForDeserialization", typeof(Microsoft.PowerShell.DeserializingTypeConverter)),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Deserialized.System.Management.Automation.JobStateEventArgs

            #region System.Exception

            typeName = @"System.Exception";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Exception

            #region System.Management.Automation.Remoting.PSSessionOption

            typeName = @"System.Management.Automation.Remoting.PSSessionOption";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.Remoting.PSSessionOption

            #region Deserialized.System.Management.Automation.Remoting.PSSessionOption

            typeName = @"Deserialized.System.Management.Automation.Remoting.PSSessionOption";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"TargetTypeForDeserialization", typeof(Microsoft.PowerShell.DeserializingTypeConverter)),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Deserialized.System.Management.Automation.Remoting.PSSessionOption

            #region System.Management.Automation.DebuggerStopEventArgs

            typeName = @"System.Management.Automation.DebuggerStopEventArgs";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"SerializedInvocationInfo");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"SerializedInvocationInfo",
                    GetMethodInfo(typeof(Microsoft.PowerShell.DeserializingTypeConverter), @"GetInvocationInfo"),
                    setterCodeReference: null)
                { IsHidden = true },
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 3);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationMethod", @"SpecificProperties"),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationDepth", 2),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PropertySerializationSet",
                    new List<string> { "Breakpoints", "ResumeAction", "SerializedInvocationInfo" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.DebuggerStopEventArgs

            #region Deserialized.System.Management.Automation.DebuggerStopEventArgs

            typeName = @"Deserialized.System.Management.Automation.DebuggerStopEventArgs";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"TargetTypeForDeserialization", typeof(Microsoft.PowerShell.DeserializingTypeConverter)),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Deserialized.System.Management.Automation.DebuggerStopEventArgs

            // Update binder version for newly added members.
            foreach (string memberName in newMembers)
            {
                PSGetMemberBinder.TypeTableMemberAdded(memberName);
            }
        }
    }
}
