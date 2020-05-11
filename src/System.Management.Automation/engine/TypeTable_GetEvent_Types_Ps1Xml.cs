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
        private void Process_GetEvent_Types_Ps1Xml(string filePath, ConcurrentBag<string> errors)
        {
            typesInfo.Add(new SessionStateTypeEntry(filePath));

            string typeName = null;
            PSMemberInfoInternalCollection<PSMemberInfo> typeMembers = null;
            PSMemberInfoInternalCollection<PSMemberInfo> memberSetMembers = null;
            HashSet<string> newMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            #region System.Diagnostics.Eventing.Reader.EventLogConfiguration

            typeName = @"System.Diagnostics.Eventing.Reader.EventLogConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "LogName", "MaximumSizeInBytes", "RecordCount", "LogMode" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Diagnostics.Eventing.Reader.EventLogConfiguration

            #region System.Diagnostics.Eventing.Reader.EventLogRecord

            typeName = @"System.Diagnostics.Eventing.Reader.EventLogRecord";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "TimeCreated", "ProviderName", "Id", "Message" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Diagnostics.Eventing.Reader.EventLogRecord

            #region System.Diagnostics.Eventing.Reader.ProviderMetadata

            typeName = @"System.Diagnostics.Eventing.Reader.ProviderMetadata";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"ProviderName");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"ProviderName", @"Name", conversionType: null),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Name", "LogLinks" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Diagnostics.Eventing.Reader.ProviderMetadata

#if !CORECLR
            #region Microsoft.PowerShell.Commands.GetCounter.CounterSet

            typeName = @"Microsoft.PowerShell.Commands.GetCounter.CounterSet";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"Counter");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Counter", @"Paths", conversionType: null),
                typeMembers,
                isOverride: false);

            #endregion Microsoft.PowerShell.Commands.GetCounter.CounterSet

            #region Microsoft.PowerShell.Commands.GetCounter.PerformanceCounterSample

            typeName = @"Microsoft.PowerShell.Commands.GetCounter.PerformanceCounterSample";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Path", "InstanceName", "CookedValue" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.PowerShell.Commands.GetCounter.PerformanceCounterSample

            #region Microsoft.PowerShell.Commands.GetCounter.PerformanceCounterSampleSet

            typeName = @"Microsoft.PowerShell.Commands.GetCounter.PerformanceCounterSampleSet";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"Readings");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Readings",
                    GetScriptBlock(@"$strPaths = """"
          foreach ($ctr in $this.CounterSamples)
          {
              $strPaths += ($ctr.Path + "" :"" + ""`n"")
              $strPaths += ($ctr.CookedValue.ToString() + ""`n`n"")
          }

          return $strPaths"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Timestamp", "Readings" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.PowerShell.Commands.GetCounter.PerformanceCounterSampleSet
#endif

            // Update binder version for newly added members.
            foreach (string memberName in newMembers)
            {
                PSGetMemberBinder.TypeTableMemberAdded(memberName);
            }
        }
    }
}
