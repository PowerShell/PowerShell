// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Reflection;
using System.Threading;

namespace System.Management.Automation.Runspaces
{
    public sealed partial class TypeTable
    {
        private const int ValueFactoryCacheCount = 6;

        private static readonly Func<string, PSMemberInfoInternalCollection<PSMemberInfo>>[] s_valueFactoryCache;

        private static Func<string, PSMemberInfoInternalCollection<PSMemberInfo>> GetValueFactoryBasedOnInitCapacity(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

            if (capacity > ValueFactoryCacheCount)
            {
                return CreateValueFactory(capacity);
            }

            int cacheIndex = capacity - 1;
            if (s_valueFactoryCache[cacheIndex] == null)
            {
                Interlocked.CompareExchange(
                    ref s_valueFactoryCache[cacheIndex],
                    CreateValueFactory(capacity),
                    comparand: null);
            }

            return s_valueFactoryCache[cacheIndex];

            // Local helper function to avoid creating an instance of the generated delegate helper class
            // every time 'GetValueFactoryBasedOnInitCapacity' is invoked.
            static Func<string, PSMemberInfoInternalCollection<PSMemberInfo>> CreateValueFactory(int capacity)
            {
                return key => new PSMemberInfoInternalCollection<PSMemberInfo>(capacity);
            }
        }

        private static MethodInfo GetMethodInfo(Type type, string method)
        {
            return type.GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
        }

        private static ScriptBlock GetScriptBlock(string s)
        {
            var sb = ScriptBlock.CreateDelayParsedScriptBlock(s, isProductCode: true);
            sb.LanguageMode = PSLanguageMode.FullLanguage;
            return sb;
        }

        private void Process_Types_Ps1Xml(string filePath, ConcurrentBag<string> errors)
        {
            typesInfo.Add(new SessionStateTypeEntry(filePath));

            string typeName = null;
            PSMemberInfoInternalCollection<PSMemberInfo> typeMembers = null;
            PSMemberInfoInternalCollection<PSMemberInfo> memberSetMembers = null;
            HashSet<string> newMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            #region System.Xml.XmlNode

            typeName = @"System.Xml.XmlNode";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"ToString");
            AddMember(
                errors,
                typeName,
                new PSCodeMethod(
                    @"ToString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.ToStringCodeMethods), @"XmlNode")),
                typeMembers,
                isOverride: false);

            #endregion System.Xml.XmlNode

            #region System.Xml.XmlNodeList

            typeName = @"System.Xml.XmlNodeList";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"ToString");
            AddMember(
                errors,
                typeName,
                new PSCodeMethod(
                    @"ToString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.ToStringCodeMethods), @"XmlNodeList")),
                typeMembers,
                isOverride: false);

            #endregion System.Xml.XmlNodeList

            #region System.Management.Automation.PSDriveInfo

            typeName = @"System.Management.Automation.PSDriveInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"Used");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Used",
                    GetScriptBlock(@"## Ensure that this is a FileSystem drive
          if($this.Provider.ImplementingType -eq
          [Microsoft.PowerShell.Commands.FileSystemProvider])
          {
          $driveInfo = [System.IO.DriveInfo]::New($this.Root)
          if ( $driveInfo.IsReady ) { $driveInfo.TotalSize - $driveInfo.AvailableFreeSpace }
          }"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Free");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Free",
                    GetScriptBlock(@"## Ensure that this is a FileSystem drive
          if($this.Provider.ImplementingType -eq
          [Microsoft.PowerShell.Commands.FileSystemProvider])
          {
          [System.IO.DriveInfo]::New($this.Root).AvailableFreeSpace
          }"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.PSDriveInfo

            #region System.DirectoryServices.PropertyValueCollection
#if !UNIX
            typeName = @"System.DirectoryServices.PropertyValueCollection";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"ToString");
            AddMember(
                errors,
                typeName,
                new PSCodeMethod(
                    @"ToString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.ToStringCodeMethods), @"PropertyValueCollection")),
                typeMembers,
                isOverride: false);
#endif
            #endregion System.DirectoryServices.PropertyValueCollection

            #region System.Drawing.Printing.PrintDocument

            typeName = @"System.Drawing.Printing.PrintDocument";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"Name");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Name",
                    GetScriptBlock(@"$this.PrinterSettings.PrinterName"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Color");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Color",
                    GetScriptBlock(@"$this.PrinterSettings.SupportsColor"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Duplex");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Duplex",
                    GetScriptBlock(@"$this.PrinterSettings.Duplex"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Drawing.Printing.PrintDocument

            #region System.Management.Automation.ApplicationInfo

            typeName = @"System.Management.Automation.ApplicationInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"FileVersionInfo");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"FileVersionInfo",
                    GetScriptBlock(@"[System.Diagnostics.FileVersionInfo]::getversioninfo( $this.Path )"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.ApplicationInfo

            #region System.DateTime

            typeName = @"System.DateTime";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"DateTime");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"DateTime",
                    GetScriptBlock(@"if ((& { Set-StrictMode -Version 1; $this.DisplayHint }) -ieq  ""Date"")
          {
          ""{0}"" -f $this.ToLongDateString()
          }
          elseif ((& { Set-StrictMode -Version 1; $this.DisplayHint }) -ieq ""Time"")
          {
          ""{0}"" -f  $this.ToLongTimeString()
          }
          else
          {
          ""{0} {1}"" -f $this.ToLongDateString(), $this.ToLongTimeString()
          }"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.DateTime

            #region System.Net.IPAddress

            typeName = @"System.Net.IPAddress";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"IPAddressToString");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"IPAddressToString",
                    GetScriptBlock(@"$this.Tostring()"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 2);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"DefaultDisplayProperty", @"IPAddressToString"),
                memberSetMembers,
                isOverride: false);

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

            #endregion System.Net.IPAddress

            #region Deserialized.System.Net.IPAddress

            typeName = @"Deserialized.System.Net.IPAddress";
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

            #endregion Deserialized.System.Net.IPAddress

            #region System.Diagnostics.ProcessModule

            typeName = @"System.Diagnostics.ProcessModule";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 6));

            // Process regular members.
            newMembers.Add(@"Size");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Size",
                    GetScriptBlock(@"$this.ModuleMemorySize / 1024"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Company");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Company",
                    GetScriptBlock(@"$this.FileVersionInfo.CompanyName"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"FileVersion");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"FileVersion",
                    GetScriptBlock(@"$this.FileVersionInfo.FileVersion"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ProductVersion");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"ProductVersion",
                    GetScriptBlock(@"$this.FileVersionInfo.ProductVersion"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Description");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Description",
                    GetScriptBlock(@"$this.FileVersionInfo.FileDescription"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Product");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Product",
                    GetScriptBlock(@"$this.FileVersionInfo.ProductName"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Diagnostics.ProcessModule

            #region System.Collections.DictionaryEntry

            typeName = @"System.Collections.DictionaryEntry";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"Name");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Name", @"Key", conversionType: null),
                typeMembers,
                isOverride: false);

            #endregion System.Collections.DictionaryEntry

            #region System.Management.Automation.PSModuleInfo

            typeName = @"System.Management.Automation.PSModuleInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Name", "Path", "Description", "Guid", "Version", "ModuleBase", "ModuleType", "PrivateData", "AccessMode", "ExportedAliases", "ExportedCmdlets", "ExportedFunctions", "ExportedVariables", "NestedModules" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.PSModuleInfo

            #region System.ServiceProcess.ServiceController

            typeName = @"System.ServiceProcess.ServiceController";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 4));

            // Process regular members.
            newMembers.Add(@"Name");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Name", @"ServiceName", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"RequiredServices");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"RequiredServices", @"ServicesDependedOn", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ToString");
            AddMember(
                errors,
                typeName,
                new PSScriptMethod(
                    @"ToString",
                    GetScriptBlock(@"$this.ServiceName"),
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
                    new List<string> { "Status", "Name", "DisplayName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.ServiceProcess.ServiceController

            #region Deserialized.System.ServiceProcess.ServiceController

            typeName = @"Deserialized.System.ServiceProcess.ServiceController";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Status", "Name", "DisplayName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Deserialized.System.ServiceProcess.ServiceController

            #region System.Management.Automation.CmdletInfo

            typeName = @"System.Management.Automation.CmdletInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"DLL");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"DLL",
                    GetScriptBlock(@"$this.ImplementingType.Assembly.Location"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.CmdletInfo

            #region System.Management.Automation.AliasInfo

            typeName = @"System.Management.Automation.AliasInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"ResolvedCommandName");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"ResolvedCommandName",
                    GetScriptBlock(@"$this.ResolvedCommand.Name"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"DisplayName");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"DisplayName",
                    GetScriptBlock(@"if ($null -ne $this.ResolvedCommand)
          {
          $this.Name + "" -> "" + $this.ResolvedCommand.Name
          }
          else
          {
          $this.Name + "" -> "" + $this.Definition
          }
          "),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.AliasInfo

            #region System.DirectoryServices.DirectoryEntry
#if !UNIX
            typeName = @"System.DirectoryServices.DirectoryEntry";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"ConvertLargeIntegerToInt64");
            AddMember(
                errors,
                typeName,
                new PSCodeMethod(
                    @"ConvertLargeIntegerToInt64",
                    GetMethodInfo(typeof(Microsoft.PowerShell.AdapterCodeMethods), @"ConvertLargeIntegerToInt64")),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ConvertDNWithBinaryToString");
            AddMember(
                errors,
                typeName,
                new PSCodeMethod(
                    @"ConvertDNWithBinaryToString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.AdapterCodeMethods), @"ConvertDNWithBinaryToString")),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "distinguishedName", "Path" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);
#endif
            #endregion System.DirectoryServices.DirectoryEntry

            #region System.IO.DirectoryInfo

            typeName = @"System.IO.DirectoryInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, static key => new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 9));

            // Process regular members.
            newMembers.Add(@"Mode");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"Mode",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"Mode"),
                    setterCodeReference: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ModeWithoutHardLink");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"ModeWithoutHardLink",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"ModeWithoutHardLink"),
                    setterCodeReference: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"BaseName");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"BaseName",
                    GetScriptBlock(@"$this.Name"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ResolvedTarget");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"ResolvedTarget",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.InternalSymbolicLinkLinkCodeMethods), @"ResolvedTarget"),
                    setterCodeReference: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Target");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Target", @"LinkTarget", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"LinkType");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"LinkType",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.InternalSymbolicLinkLinkCodeMethods), @"GetLinkType"),
                    setterCodeReference: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"NameString");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"NameString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"NameString"),
                    setterCodeReference: null)
                { IsHidden = true },
                typeMembers,
                isOverride: false);

            newMembers.Add(@"LengthString");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"LengthString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"LengthString"),
                    setterCodeReference: null)
                { IsHidden = true },
                typeMembers,
                isOverride: false);

            newMembers.Add(@"LastWriteTimeString");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"LastWriteTimeString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"LastWriteTimeString"),
                    setterCodeReference: null)
                { IsHidden = true },
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"DefaultDisplayProperty", @"Name"),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.IO.DirectoryInfo

            #region System.IO.FileInfo

            typeName = @"System.IO.FileInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, static key => new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 10));

            // Process regular members.
            newMembers.Add(@"Mode");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"Mode",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"Mode"),
                    setterCodeReference: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ModeWithoutHardLink");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"ModeWithoutHardLink",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"ModeWithoutHardLink"),
                    setterCodeReference: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"VersionInfo");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"VersionInfo",
                    GetScriptBlock(@"[System.Diagnostics.FileVersionInfo]::GetVersionInfo($this.FullName)"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"BaseName");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"BaseName",
                    GetScriptBlock(@"if ($this.Extension.Length -gt 0){$this.Name.Remove($this.Name.Length - $this.Extension.Length)}else{$this.Name}"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ResolvedTarget");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"ResolvedTarget",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.InternalSymbolicLinkLinkCodeMethods), @"ResolvedTarget"),
                    setterCodeReference: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Target");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Target", @"LinkTarget", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"LinkType");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"LinkType",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.InternalSymbolicLinkLinkCodeMethods), @"GetLinkType"),
                    setterCodeReference: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"NameString");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"NameString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"NameString"),
                    setterCodeReference: null)
                { IsHidden = true },
                typeMembers,
                isOverride: false);

            newMembers.Add(@"LengthString");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"LengthString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"LengthString"),
                    setterCodeReference: null)
                { IsHidden = true },
                typeMembers,
                isOverride: false);

            newMembers.Add(@"LastWriteTimeString");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"LastWriteTimeString",
                    GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), @"LastWriteTimeString"),
                    setterCodeReference: null)
                { IsHidden = true },
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "LastWriteTime", "Length", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.IO.FileInfo

            #region System.Diagnostics.FileVersionInfo

            typeName = @"System.Diagnostics.FileVersionInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"FileVersionRaw");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"FileVersionRaw",
                    GetScriptBlock(@"New-Object System.Version -ArgumentList @(
            $this.FileMajorPart
            $this.FileMinorPart
            $this.FileBuildPart
            $this.FilePrivatePart)"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ProductVersionRaw");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"ProductVersionRaw",
                    GetScriptBlock(@"New-Object System.Version -ArgumentList @(
            $this.ProductMajorPart
            $this.ProductMinorPart
            $this.ProductBuildPart
            $this.ProductPrivatePart)"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Diagnostics.FileVersionInfo

            #region System.Diagnostics.EventLogEntry

            typeName = @"System.Diagnostics.EventLogEntry";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"EventID");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"EventID",
                    GetScriptBlock(@"$this.get_EventID() -band 0xFFFF"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Diagnostics.EventLogEntry

            #region System.Management.ManagementBaseObject

            typeName = @"System.Management.ManagementBaseObject";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"PSComputerName");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"PSComputerName", @"__SERVER", conversionType: null),
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementBaseObject

            #region System.Management.ManagementObject#root\cimv2\Win32_PingStatus

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_PingStatus";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"IPV4Address");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"IPV4Address",
                    GetScriptBlock(@"$iphost = [System.Net.Dns]::GetHostEntry($this.address)
          $iphost.AddressList | Where-Object { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork } | Select-Object -first 1"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"IPV6Address");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"IPV6Address",
                    GetScriptBlock(@"$iphost = [System.Net.Dns]::GetHostEntry($this.address)
          $iphost.AddressList | Where-Object { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetworkV6 } | Select-Object -first 1"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_PingStatus

            #region System.Management.ManagementObject#root\cimv2\Win32_Process

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_Process";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 5));

            // Process regular members.
            newMembers.Add(@"ProcessName");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"ProcessName", @"Name", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Handles");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Handles", @"Handlecount", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"VM");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"VM", @"VirtualSize", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"WS");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"WS", @"WorkingSetSize", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Path");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Path",
                    GetScriptBlock(@"$this.ExecutablePath"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_Process

            #region System.Diagnostics.Process

            typeName = @"System.Diagnostics.Process";
            typeMembers = _extendedMembers.GetOrAdd(typeName, static key => new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 19));

            // Process regular members.
            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "Name", "Id", "PriorityClass", "FileVersion" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSResources");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSResources",
                    new List<string> { "Name", "Id", "Handlecount", "WorkingSet", "NonPagedMemorySize", "PagedMemorySize", "PrivateMemorySize", "VirtualMemorySize", "Threads.Count", "TotalProcessorTime" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Name");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Name", @"ProcessName", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"SI");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"SI", @"SessionId", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Handles");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Handles", @"Handlecount", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"VM");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"VM", @"VirtualMemorySize64", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"WS");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"WS", @"WorkingSet64", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PM");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"PM", @"PagedMemorySize64", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"NPM");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"NPM", @"NonpagedSystemMemorySize64", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Path");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Path",
                    GetScriptBlock(@"$this.Mainmodule.FileName"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"CommandLine");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"CommandLine",
                    GetScriptBlock(@"
                        if ($IsWindows) {
                            (Get-CimInstance Win32_Process -Filter ""ProcessId = $($this.Id)"").CommandLine
                        } elseif ($IsLinux) {
                            $rawCmd = Get-Content -LiteralPath ""/proc/$($this.Id)/cmdline""
                            $rawCmd.Substring(0, $rawCmd.Length - 1) -replace ""`0"", "" ""
                        }
                    "),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Parent");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"Parent",
                    GetMethodInfo(typeof(Microsoft.PowerShell.ProcessCodeMethods), @"GetParentProcess"),
                    setterCodeReference: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Company");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Company",
                    GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.CompanyName"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"CPU");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"CPU",
                    GetScriptBlock(@"$this.TotalProcessorTime.TotalSeconds"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"FileVersion");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"FileVersion",
                    GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.FileVersion"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ProductVersion");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"ProductVersion",
                    GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.ProductVersion"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Description");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Description",
                    GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.FileDescription"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Product");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Product",
                    GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.ProductName"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"__NounName");
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"__NounName", @"Process"),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Id", "Handles", "CPU", "SI", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Diagnostics.Process

            #region Deserialized.System.Diagnostics.Process

            typeName = @"Deserialized.System.Diagnostics.Process";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "Name", "Id", "PriorityClass", "FileVersion" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSResources");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSResources",
                    new List<string> { "Name", "Id", "Handlecount", "WorkingSet", "NonPagedMemorySize", "PagedMemorySize", "PrivateMemorySize", "VirtualMemorySize", "Threads.Count", "TotalProcessorTime" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Id", "Handles", "CPU", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Deserialized.System.Diagnostics.Process

            #region System.Management.ManagementObject#root\cli\Msft_CliAlias

            typeName = @"System.Management.ManagementObject#root\cli\Msft_CliAlias";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "FriendlyName", "PWhere", "Target" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cli\Msft_CliAlias

            #region System.Management.ManagementObject#root\cimv2\Win32_BaseBoard

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_BaseBoard";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "PoweredOn" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Manufacturer", "Model", "Name", "SerialNumber", "SKU", "Product" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_BaseBoard

            #region System.Management.ManagementObject#root\cimv2\Win32_BIOS

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_BIOS";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "Caption", "SMBIOSPresent" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "SMBIOSBIOSVersion", "Manufacturer", "Name", "SerialNumber", "Version" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_BIOS

            #region System.Management.ManagementObject#root\cimv2\Win32_BootConfiguration

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_BootConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "SettingID", "ConfigurationPath" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "BootDirectory", "Name", "SettingID", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_BootConfiguration

            #region System.Management.ManagementObject#root\cimv2\Win32_CDROMDrive

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_CDROMDrive";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Availability", "Drive", "ErrorCleared", "MediaLoaded", "NeedsCleaning", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Drive", "Manufacturer", "VolumeName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_CDROMDrive

            #region System.Management.ManagementObject#root\cimv2\Win32_ComputerSystem

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_ComputerSystem";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "AdminPasswordStatus", "BootupState", "ChassisBootupState", "KeyboardPasswordStatus", "PowerOnPasswordStatus", "PowerSupplyState", "PowerState", "FrontPanelResetStatus", "ThermalState", "Status", "Name" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"POWER");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"POWER",
                    new List<string> { "Name", "PowerManagementCapabilities", "PowerManagementSupported", "PowerOnPasswordStatus", "PowerState", "PowerSupplyState" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Domain", "Manufacturer", "Model", "Name", "PrimaryOwnerName", "TotalPhysicalMemory" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_ComputerSystem

            #region System.Management.ManagementObject#root\cimv2\WIN32_PROCESSOR

            typeName = @"System.Management.ManagementObject#root\cimv2\WIN32_PROCESSOR";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Availability", "CpuStatus", "CurrentVoltage", "DeviceID", "ErrorCleared", "ErrorDescription", "LastErrorCode", "LoadPercentage", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "AddressWidth", "DataWidth", "DeviceID", "ExtClock", "L2CacheSize", "L2CacheSpeed", "MaxClockSpeed", "PowerManagementSupported", "ProcessorType", "Revision", "SocketDesignation", "Version", "VoltageCaps" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "DeviceID", "Manufacturer", "MaxClockSpeed", "Name", "SocketDesignation" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\WIN32_PROCESSOR

            #region System.Management.ManagementObject#root\cimv2\Win32_ComputerSystemProduct

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_ComputerSystemProduct";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Version" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "IdentifyingNumber", "Name", "Vendor", "Version", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_ComputerSystemProduct

            #region System.Management.ManagementObject#root\cimv2\CIM_DataFile

            typeName = @"System.Management.ManagementObject#root\cimv2\CIM_DataFile";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Compressed", "Encrypted", "Size", "Hidden", "Name", "Readable", "System", "Version", "Writeable" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\CIM_DataFile

            #region System.Management.ManagementObject#root\cimv2\WIN32_DCOMApplication

            typeName = @"System.Management.ManagementObject#root\cimv2\WIN32_DCOMApplication";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Status" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "AppID", "InstallDate", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\WIN32_DCOMApplication

            #region System.Management.ManagementObject#root\cimv2\WIN32_DESKTOP

            typeName = @"System.Management.ManagementObject#root\cimv2\WIN32_DESKTOP";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "ScreenSaverActive" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Name", "ScreenSaverActive", "ScreenSaverSecure", "ScreenSaverTimeout", "SettingID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\WIN32_DESKTOP

            #region System.Management.ManagementObject#root\cimv2\WIN32_DESKTOPMONITOR

            typeName = @"System.Management.ManagementObject#root\cimv2\WIN32_DESKTOPMONITOR";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "DeviceID", "Name", "PixelsPerXLogicalInch", "PixelsPerYLogicalInch", "ScreenHeight", "ScreenWidth" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "DeviceID", "IsLocked", "LastErrorCode", "Name", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceID", "DisplayType", "MonitorManufacturer", "Name", "ScreenHeight", "ScreenWidth" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\WIN32_DESKTOPMONITOR

            #region System.Management.ManagementObject#root\cimv2\Win32_DeviceMemoryAddress

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_DeviceMemoryAddress";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "MemoryType" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "MemoryType", "Name", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_DeviceMemoryAddress

            #region System.Management.ManagementObject#root\cimv2\Win32_DiskDrive

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_DiskDrive";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "ConfigManagerErrorCode", "LastErrorCode", "NeedsCleaning", "Status", "DeviceID", "StatusInfo", "Partitions" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "BytesPerSector", "ConfigManagerUserConfig", "DefaultBlockSize", "DeviceID", "Index", "InstallDate", "InterfaceType", "MaxBlockSize", "MaxMediaSize", "MinBlockSize", "NumberOfMediaSupported", "Partitions", "SectorsPerTrack", "Size", "TotalCylinders", "TotalHeads", "TotalSectors", "TotalTracks", "TracksPerCylinder" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Partitions", "DeviceID", "Model", "Size", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_DiskDrive

            #region System.Management.ManagementObject#root\cimv2\Win32_DiskQuota

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_DiskQuota";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "__PATH", "Status" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DiskSpaceUsed", "Limit", "QuotaVolume", "User" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_DiskQuota

            #region System.Management.ManagementObject#root\cimv2\Win32_DMAChannel

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_DMAChannel";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "AddressSize", "DMAChannel", "MaxTransferSize", "Name", "Port" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_DMAChannel

            #region System.Management.ManagementObject#root\cimv2\Win32_Environment

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_Environment";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "SystemVariable" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "VariableValue", "Name", "UserName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_Environment

            #region System.Management.ManagementObject#root\cimv2\Win32_Directory

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_Directory";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Compressed", "Encrypted", "Name", "Readable", "Writeable" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Hidden", "Archive", "EightDotThreeFileName", "FileSize", "Name", "Compressed", "Encrypted", "Readable" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_Directory

            #region System.Management.ManagementObject#root\cimv2\Win32_Group

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_Group";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Domain", "Name", "SID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_Group

            #region System.Management.ManagementObject#root\cimv2\Win32_IDEController

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_IDEController";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Manufacturer", "Name", "ProtocolSupported", "Status", "StatusInfo" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_IDEController

            #region System.Management.ManagementObject#root\cimv2\Win32_IRQResource

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_IRQResource";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Caption", "Availability" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Hardware", "IRQNumber", "Name", "Shareable", "TriggerLevel", "TriggerType" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_IRQResource

            #region System.Management.ManagementObject#root\cimv2\Win32_ScheduledJob

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_ScheduledJob";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "JobId", "JobStatus", "ElapsedTime", "StartTime", "Owner" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "JobId", "Name", "Owner", "Priority", "Command" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_ScheduledJob

            #region System.Management.ManagementObject#root\cimv2\Win32_LoadOrderGroup

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_LoadOrderGroup";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "GroupOrder", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_LoadOrderGroup

            #region System.Management.ManagementObject#root\cimv2\Win32_LogicalDisk

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_LogicalDisk";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Availability", "DeviceID", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceID", "DriveType", "ProviderName", "FreeSpace", "Size", "VolumeName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_LogicalDisk

            #region System.Management.ManagementObject#root\cimv2\Win32_LogonSession

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_LogonSession";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "__PATH", "Status" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "AuthenticationPackage", "LogonId", "LogonType", "Name", "StartTime", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_LogonSession

            #region System.Management.ManagementObject#root\cimv2\WIN32_CACHEMEMORY

            typeName = @"System.Management.ManagementObject#root\cimv2\WIN32_CACHEMEMORY";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 4));

            // Process regular members.
            newMembers.Add(@"ERROR");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"ERROR",
                    new List<string> { "DeviceID", "ErrorCorrectType" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Availability", "DeviceID", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "BlockSize", "CacheSpeed", "CacheType", "DeviceID", "InstalledSize", "Level", "MaxCacheSize", "NumberOfBlocks", "Status", "WritePolicy" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "BlockSize", "CacheSpeed", "CacheType", "DeviceID", "InstalledSize", "Level", "MaxCacheSize", "NumberOfBlocks", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\WIN32_CACHEMEMORY

            #region System.Management.ManagementObject#root\cimv2\Win32_LogicalMemoryConfiguration

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_LogicalMemoryConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "AvailableVirtualMemory", "Name", "TotalVirtualMemory" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Name", "TotalVirtualMemory", "TotalPhysicalMemory", "TotalPageFileSpace" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_LogicalMemoryConfiguration

            #region System.Management.ManagementObject#root\cimv2\Win32_PhysicalMemoryArray

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_PhysicalMemoryArray";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "Replaceable", "Location" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Model", "Name", "MaxCapacity", "MemoryDevices" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_PhysicalMemoryArray

            #region System.Management.ManagementObject#root\cimv2\WIN32_NetworkClient

            typeName = @"System.Management.ManagementObject#root\cimv2\WIN32_NetworkClient";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Status" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "InstallDate", "Manufacturer", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\WIN32_NetworkClient

            #region System.Management.ManagementObject#root\cimv2\Win32_NetworkLoginProfile

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_NetworkLoginProfile";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Privileges", "Profile", "UserId", "UserType", "Workstations" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_NetworkLoginProfile

            #region System.Management.ManagementObject#root\cimv2\Win32_NetworkProtocol

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_NetworkProtocol";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"FULLXXX");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"FULLXXX",
                    new List<string> { "ConnectionlessService", "Description", "GuaranteesDelivery", "GuaranteesSequencing", "InstallDate", "MaximumAddressSize", "MaximumMessageSize", "MessageOriented", "MinimumAddressSize", "Name", "PseudoStreamOriented", "Status", "SupportsBroadcasting", "SupportsConnectData", "SupportsDisconnectData", "SupportsEncryption", "SupportsExpeditedData", "SupportsFragmentation", "SupportsGracefulClosing", "SupportsGuaranteedBandwidth", "SupportsMulticasting", "SupportsQualityofService" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Status", "SupportsBroadcasting", "SupportsConnectData", "SupportsDisconnectData", "SupportsEncryption", "SupportsExpeditedData", "SupportsFragmentation", "SupportsGracefulClosing", "SupportsGuaranteedBandwidth", "SupportsMulticasting", "SupportsQualityofService" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "GuaranteesDelivery", "GuaranteesSequencing", "ConnectionlessService", "Status", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_NetworkProtocol

            #region System.Management.ManagementObject#root\cimv2\Win32_NetworkConnection

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_NetworkConnection";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "ConnectionState", "Persistent", "LocalName", "RemoteName" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "LocalName", "RemoteName", "ConnectionState", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_NetworkConnection

            #region System.Management.ManagementObject#root\cimv2\Win32_NetworkAdapter

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_NetworkAdapter";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Availability", "Name", "Status", "StatusInfo", "DeviceID" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "ServiceName", "MACAddress", "AdapterType", "DeviceID", "Name", "NetworkAddresses", "Speed" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_NetworkAdapter

            #region System.Management.ManagementObject#root\cimv2\Win32_NetworkAdapterConfiguration

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_NetworkAdapterConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 6));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "DHCPLeaseExpires", "Index", "Description" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"DHCP");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DHCP",
                    new List<string> { "Description", "DHCPEnabled", "DHCPLeaseExpires", "DHCPLeaseObtained", "DHCPServer", "Index" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"DNS");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DNS",
                    new List<string> { "Description", "DNSDomain", "DNSDomainSuffixSearchOrder", "DNSEnabledForWINSResolution", "DNSHostName", "DNSServerSearchOrder", "DomainDNSRegistrationEnabled", "FullDNSRegistrationEnabled", "Index" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"IP");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"IP",
                    new List<string> { "Description", "Index", "IPAddress", "IPConnectionMetric", "IPEnabled", "IPFilterSecurityEnabled" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"WINS");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"WINS",
                    new List<string> { "Description", "Index", "WINSEnableLMHostsLookup", "WINSHostLookupFile", "WINSPrimaryServer", "WINSScopeID", "WINSSecondaryServer" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DHCPEnabled", "IPAddress", "DefaultIPGateway", "DNSDomain", "ServiceName", "Description", "Index" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_NetworkAdapterConfiguration

            #region System.Management.ManagementObject#root\cimv2\Win32_NTDomain

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_NTDomain";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "DomainName" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"GUID");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"GUID",
                    new List<string> { "DomainName", "DomainGuid" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "ClientSiteName", "DcSiteName", "Description", "DnsForestName", "DomainControllerAddress", "DomainControllerName", "DomainName", "Roles", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_NTDomain

            #region System.Management.ManagementObject#root\cimv2\Win32_NTLogEvent

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_NTLogEvent";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Category", "CategoryString", "EventCode", "EventIdentifier", "TypeEvent", "InsertionStrings", "LogFile", "Message", "RecordNumber", "SourceName", "TimeGenerated", "TimeWritten", "Type", "UserName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_NTLogEvent

            #region System.Management.ManagementObject#root\cimv2\Win32_NTEventlogFile

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_NTEventlogFile";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "LogfileName", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "FileSize", "LogfileName", "Name", "NumberOfRecords" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_NTEventlogFile

            #region System.Management.ManagementObject#root\cimv2\Win32_OnBoardDevice

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_OnBoardDevice";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Description" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceType", "SerialNumber", "Enabled", "Description" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_OnBoardDevice

            #region System.Management.ManagementObject#root\cimv2\Win32_OperatingSystem

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_OperatingSystem";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"FREE");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"FREE",
                    new List<string> { "FreePhysicalMemory", "FreeSpaceInPagingFiles", "FreeVirtualMemory", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "SystemDirectory", "Organization", "BuildNumber", "RegisteredUser", "SerialNumber", "Version" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_OperatingSystem

            #region System.Management.ManagementObject#root\cimv2\Win32_PageFileUsage

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_PageFileUsage";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "CurrentUsage" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Name", "PeakUsage" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_PageFileUsage

            #region System.Management.ManagementObject#root\cimv2\Win32_PageFileSetting

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_PageFileSetting";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "MaximumSize", "Name", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_PageFileSetting

            #region System.Management.ManagementObject#root\cimv2\Win32_DiskPartition

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_DiskPartition";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Index", "Status", "StatusInfo", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "NumberOfBlocks", "BootPartition", "Name", "PrimaryPartition", "Size", "Index" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_DiskPartition

            #region System.Management.ManagementObject#root\cimv2\Win32_PortResource

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_PortResource";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "NetConnectionStatus", "Status", "Name", "StartingAddress", "EndingAddress" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Name", "Alias" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_PortResource

            #region System.Management.ManagementObject#root\cimv2\Win32_PortConnector

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_PortConnector";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "ExternalReferenceDesignator" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Tag", "ConnectorType", "SerialNumber", "ExternalReferenceDesignator", "PortType" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_PortConnector

            #region System.Management.ManagementObject#root\cimv2\Win32_Printer

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_Printer";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Location", "Name", "PrinterState", "PrinterStatus", "ShareName", "SystemName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_Printer

            #region System.Management.ManagementObject#root\cimv2\Win32_PrinterConfiguration

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_PrinterConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "DriverVersion", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "PrintQuality", "DriverVersion", "Name", "PaperSize", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_PrinterConfiguration

            #region System.Management.ManagementObject#root\cimv2\Win32_PrintJob

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_PrintJob";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Document", "JobId", "JobStatus", "Name", "PagesPrinted", "Status", "JobIdCopy", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Document", "JobId", "JobStatus", "Owner", "Priority", "Size", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_PrintJob

            #region System.Management.ManagementObject#root\cimv2\Win32_ProcessXXX

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_ProcessXXX";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 5));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "ProcessId" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"MEMORY");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"MEMORY",
                    new List<string> { "Handle", "MaximumWorkingSetSize", "MinimumWorkingSetSize", "Name", "PageFaults", "PageFileUsage", "PeakPageFileUsage", "PeakVirtualSize", "PeakWorkingSetSize", "PrivatePageCount", "QuotaNonPagedPoolUsage", "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "QuotaPeakPagedPoolUsage", "VirtualSize", "WorkingSetSize" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"IO");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"IO",
                    new List<string> { "Name", "ProcessId", "ReadOperationCount", "ReadTransferCount", "WriteOperationCount", "WriteTransferCount" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"STATISTICS");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"STATISTICS",
                    new List<string> { "HandleCount", "Name", "KernelModeTime", "MaximumWorkingSetSize", "MinimumWorkingSetSize", "OtherOperationCount", "OtherTransferCount", "PageFaults", "PageFileUsage", "PeakPageFileUsage", "PeakVirtualSize", "PeakWorkingSetSize", "PrivatePageCount", "ProcessId", "QuotaNonPagedPoolUsage", "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "QuotaPeakPagedPoolUsage", "ReadOperationCount", "ReadTransferCount", "ThreadCount", "UserModeTime", "VirtualSize", "WorkingSetSize", "WriteOperationCount", "WriteTransferCount" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "ThreadCount", "HandleCount", "Name", "Priority", "ProcessId", "WorkingSetSize" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_ProcessXXX

            #region System.Management.ManagementObject#root\cimv2\Win32_Product

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_Product";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Version", "InstallState" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "IdentifyingNumber", "Name", "Vendor", "Version", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_Product

            #region System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"InstalledOn");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"InstalledOn",
                    GetScriptBlock(@"if ([environment]::osversion.version.build -ge 7000)
          {
          # WMI team fixed the formatting issue related to InstalledOn
          # property in Windows7 (to return string)..so returning the WMI's
          # version directly
           [DateTime]::Parse($this.psBase.properties[""InstalledOn""].Value, [System.Globalization.DateTimeFormatInfo]::new())
          }
          else
          {
          $orig = $this.psBase.properties[""InstalledOn""].Value
          $date = [datetime]::FromFileTimeUTC($(""0x"" + $orig))
          if ($date -lt ""1/1/1980"")
          {
          if ($orig -match ""([0-9]{4})([01][0-9])([012][0-9])"")
          {
          new-object datetime @([int]$matches[1], [int]$matches[2], [int]$matches[3])
          }
          }
          else
          {
          $date
          }
          }"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "__PATH", "Status" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Description", "FixComments", "HotFixID", "InstallDate", "InstalledBy", "InstalledOn", "Name", "ServicePackInEffect", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering

            #region System.Management.ManagementObject#root\cimv2\Win32_QuotaSetting

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_QuotaSetting";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "State", "VolumePath", "Caption" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "DefaultLimit", "SettingID", "State", "VolumePath", "DefaultWarningLimit" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_QuotaSetting

            #region System.Management.ManagementObject#root\cimv2\Win32_OSRecoveryConfiguration

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_OSRecoveryConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DebugFilePath", "Name", "SettingID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_OSRecoveryConfiguration

            #region System.Management.ManagementObject#root\cimv2\Win32_Registry

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_Registry";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "CurrentSize", "MaximumSize", "ProposedSize" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "CurrentSize", "MaximumSize", "Name", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_Registry

            #region System.Management.ManagementObject#root\cimv2\Win32_SCSIController

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_SCSIController";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DriverName", "Manufacturer", "Name", "ProtocolSupported", "Status", "StatusInfo" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_SCSIController

            #region System.Management.ManagementObject#root\cimv2\Win32_PerfRawData_PerfNet_Server

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_PerfRawData_PerfNet_Server";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "LogonPerSec", "LogonTotal", "Name", "ServerSessions", "WorkItemShortages" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_PerfRawData_PerfNet_Server

            #region System.Management.ManagementObject#root\cimv2\Win32_Service

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_Service";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Status", "ExitCode" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "DesktopInteract", "ErrorControl", "Name", "PathName", "ServiceType", "StartMode" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "ExitCode", "Name", "ProcessId", "StartMode", "State", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_Service

            #region System.Management.ManagementObject#root\cimv2\Win32_Share

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_Share";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Type", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Name", "Path", "Description" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_Share

            #region System.Management.ManagementObject#root\cimv2\Win32_SoftwareElement

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_SoftwareElement";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "SoftwareElementState", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Name", "Path", "SerialNumber", "SoftwareElementID", "Version" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_SoftwareElement

            #region System.Management.ManagementObject#root\cimv2\Win32_SoftwareFeature

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_SoftwareFeature";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "InstallState", "LastUse" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "IdentifyingNumber", "ProductName", "Vendor", "Version" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_SoftwareFeature

            #region System.Management.ManagementObject#root\cimv2\WIN32_SoundDevice

            typeName = @"System.Management.ManagementObject#root\cimv2\WIN32_SoundDevice";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "ConfigManagerUserConfig", "Name", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Manufacturer", "Name", "Status", "StatusInfo" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\WIN32_SoundDevice

            #region System.Management.ManagementObject#root\cimv2\Win32_StartupCommand

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_StartupCommand";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Command", "User", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_StartupCommand

            #region System.Management.ManagementObject#root\cimv2\Win32_SystemAccount

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_SystemAccount";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "SIDType", "Name", "Domain" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Domain", "Name", "SID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_SystemAccount

            #region System.Management.ManagementObject#root\cimv2\Win32_SystemDriver

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_SystemDriver";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "State", "ExitCode", "Started", "ServiceSpecificExitCode" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DisplayName", "Name", "State", "Status", "Started" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_SystemDriver

            #region System.Management.ManagementObject#root\cimv2\Win32_SystemEnclosure

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_SystemEnclosure";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Tag", "Status", "Name", "SecurityStatus" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Manufacturer", "Model", "LockPresent", "SerialNumber", "SMBIOSAssetTag", "SecurityStatus" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_SystemEnclosure

            #region System.Management.ManagementObject#root\cimv2\Win32_SystemSlot

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_SystemSlot";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "SlotDesignation" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "SlotDesignation", "Tag", "SupportsHotPlug", "Status", "Shared", "PMESignal", "MaxDataWidth" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_SystemSlot

            #region System.Management.ManagementObject#root\cimv2\Win32_TapeDrive

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_TapeDrive";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Availability", "DeviceID", "NeedsCleaning", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceID", "Id", "Manufacturer", "Name", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_TapeDrive

            #region System.Management.ManagementObject#root\cimv2\Win32_TemperatureProbe

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_TemperatureProbe";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "CurrentReading", "DeviceID", "Name", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "CurrentReading", "Name", "Description", "MinReadable", "MaxReadable", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_TemperatureProbe

            #region System.Management.ManagementObject#root\cimv2\Win32_TimeZone

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_TimeZone";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Bias", "SettingID", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_TimeZone

            #region System.Management.ManagementObject#root\cimv2\Win32_UninterruptiblePowerSupply

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_UninterruptiblePowerSupply";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "DeviceID", "EstimatedChargeRemaining", "EstimatedRunTime", "Name", "StatusInfo", "TimeOnBackup" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceID", "EstimatedRunTime", "Name", "TimeOnBackup", "UPSPort", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_UninterruptiblePowerSupply

            #region System.Management.ManagementObject#root\cimv2\Win32_UserAccount

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_UserAccount";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Caption", "PasswordExpires" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "AccountType", "Caption", "Domain", "SID", "FullName", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_UserAccount

            #region System.Management.ManagementObject#root\cimv2\Win32_VoltageProbe

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_VoltageProbe";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "DeviceID", "Name", "NominalReading", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Status", "Description", "CurrentReading", "MaxReadable", "MinReadable" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_VoltageProbe

            #region System.Management.ManagementObject#root\cimv2\Win32_VolumeQuotaSetting

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_VolumeQuotaSetting";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Element", "Setting" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_VolumeQuotaSetting

            #region System.Management.ManagementObject#root\cimv2\Win32_WMISetting

            typeName = @"System.Management.ManagementObject#root\cimv2\Win32_WMISetting";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "BuildVersion", "Caption", "DatabaseDirectory", "EnableEvents", "LoggingLevel", "SettingID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject#root\cimv2\Win32_WMISetting

            #region System.Management.ManagementObject

            typeName = @"System.Management.ManagementObject";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"ConvertToDateTime");
            AddMember(
                errors,
                typeName,
                new PSScriptMethod(
                    @"ConvertToDateTime",
                    GetScriptBlock(@"[System.Management.ManagementDateTimeConverter]::ToDateTime($args[0])"),
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ConvertFromDateTime");
            AddMember(
                errors,
                typeName,
                new PSScriptMethod(
                    @"ConvertFromDateTime",
                    GetScriptBlock(@"[System.Management.ManagementDateTimeConverter]::ToDmtfDateTime($args[0])"),
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementObject

            #region Microsoft.PowerShell.Commands.HistoryInfo

            typeName = @"Microsoft.PowerShell.Commands.HistoryInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultKeyPropertySet",
                    new List<string> { "Id" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.PowerShell.Commands.HistoryInfo

            #region System.Management.ManagementClass

            typeName = @"System.Management.ManagementClass";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"Name");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Name", @"__Class", conversionType: null),
                typeMembers,
                isOverride: false);

            #endregion System.Management.ManagementClass

            #region System.Management.Automation.Runspaces.PSSession

            typeName = @"System.Management.Automation.Runspaces.PSSession";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 5));

            // Process regular members.
            newMembers.Add(@"State");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"State",
                    GetScriptBlock(@"$this.Runspace.RunspaceStateInfo.State"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"IdleTimeout");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"IdleTimeout",
                    GetScriptBlock(@"$this.Runspace.ConnectionInfo.IdleTimeout"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"OutputBufferingMode");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"OutputBufferingMode",
                    GetScriptBlock(@"$this.Runspace.ConnectionInfo.OutputBufferingMode"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"DisconnectedOn");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"DisconnectedOn",
                    GetScriptBlock(@"$this.Runspace.DisconnectedOn"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"ExpiresOn");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"ExpiresOn",
                    GetScriptBlock(@"$this.Runspace.ExpiresOn"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.Runspaces.PSSession

            #region System.Guid

            typeName = @"System.Guid";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"Guid");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Guid",
                    GetScriptBlock(@"$this.ToString()"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Guid

            #region System.Management.Automation.Signature

            typeName = @"System.Management.Automation.Signature";
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

            #endregion System.Management.Automation.Signature

            #region System.Management.Automation.Job

            typeName = @"System.Management.Automation.Job";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"State");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"State",
                    GetScriptBlock(@"$this.JobStateInfo.State.ToString()"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
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
                    new List<string> { "HasMoreData", "StatusMessage", "Location", "Command", "JobStateInfo", "InstanceId", "Id", "Name", "State", "ChildJobs", "PSJobTypeName", "PSBeginTime", "PSEndTime" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.Job

            #region System.Management.Automation.JobStateInfo

            typeName = @"System.Management.Automation.JobStateInfo";
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

            #endregion System.Management.Automation.JobStateInfo

            #region Deserialized.System.Management.Automation.JobStateInfo

            typeName = @"Deserialized.System.Management.Automation.JobStateInfo";
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

            #endregion Deserialized.System.Management.Automation.JobStateInfo

            #region Microsoft.PowerShell.DeserializingTypeConverter

            typeName = @"Microsoft.PowerShell.DeserializingTypeConverter";

            // Process type converter.
            ProcessTypeConverter(
                errors,
                typeName,
                typeof(Microsoft.PowerShell.DeserializingTypeConverter),
                _typeConverters,
                isOverride: false);

            #endregion Microsoft.PowerShell.DeserializingTypeConverter

            #region System.Net.Mail.MailAddress

            typeName = @"System.Net.Mail.MailAddress";
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

            #endregion System.Net.Mail.MailAddress

            #region Deserialized.System.Net.Mail.MailAddress

            typeName = @"Deserialized.System.Net.Mail.MailAddress";
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

            #endregion Deserialized.System.Net.Mail.MailAddress

            #region System.Globalization.CultureInfo

            typeName = @"System.Globalization.CultureInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

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
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PropertySerializationSet",
                    new List<string> { "LCID", "Name", "DisplayName", "IetfLanguageTag", "ThreeLetterISOLanguageName", "ThreeLetterWindowsLanguageName", "TwoLetterISOLanguageName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Globalization.CultureInfo

            #region Deserialized.System.Globalization.CultureInfo

            typeName = @"Deserialized.System.Globalization.CultureInfo";
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

            #endregion Deserialized.System.Globalization.CultureInfo

            #region System.Management.Automation.PSCredential

            typeName = @"System.Management.Automation.PSCredential";
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

            #endregion System.Management.Automation.PSCredential

            #region Deserialized.System.Management.Automation.PSCredential

            typeName = @"Deserialized.System.Management.Automation.PSCredential";
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

            #endregion Deserialized.System.Management.Automation.PSCredential

            #region System.Management.Automation.PSPrimitiveDictionary

            typeName = @"System.Management.Automation.PSPrimitiveDictionary";
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

            #endregion System.Management.Automation.PSPrimitiveDictionary

            #region Deserialized.System.Management.Automation.PSPrimitiveDictionary

            typeName = @"Deserialized.System.Management.Automation.PSPrimitiveDictionary";
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

            #endregion Deserialized.System.Management.Automation.PSPrimitiveDictionary

            #region System.Management.Automation.SwitchParameter

            typeName = @"System.Management.Automation.SwitchParameter";
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

            #endregion System.Management.Automation.SwitchParameter

            #region Deserialized.System.Management.Automation.SwitchParameter

            typeName = @"Deserialized.System.Management.Automation.SwitchParameter";
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

            #endregion Deserialized.System.Management.Automation.SwitchParameter

            #region System.Management.Automation.PSListModifier

            typeName = @"System.Management.Automation.PSListModifier";
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

            #endregion System.Management.Automation.PSListModifier

            #region Deserialized.System.Management.Automation.PSListModifier

            typeName = @"Deserialized.System.Management.Automation.PSListModifier";
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

            #endregion Deserialized.System.Management.Automation.PSListModifier

            #region System.Security.Cryptography.X509Certificates.X509Certificate2

            typeName = @"System.Security.Cryptography.X509Certificates.X509Certificate2";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

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
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PropertySerializationSet",
                    new List<string> { "RawData" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Security.Cryptography.X509Certificates.X509Certificate2

            #region Deserialized.System.Security.Cryptography.X509Certificates.X509Certificate2

            typeName = @"Deserialized.System.Security.Cryptography.X509Certificates.X509Certificate2";
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

            #endregion Deserialized.System.Security.Cryptography.X509Certificates.X509Certificate2

            #region System.Security.Cryptography.X509Certificates.X500DistinguishedName

            typeName = @"System.Security.Cryptography.X509Certificates.X500DistinguishedName";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

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
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PropertySerializationSet",
                    new List<string> { "RawData" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Security.Cryptography.X509Certificates.X500DistinguishedName

            #region Deserialized.System.Security.Cryptography.X509Certificates.X500DistinguishedName

            typeName = @"Deserialized.System.Security.Cryptography.X509Certificates.X500DistinguishedName";
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

            #endregion Deserialized.System.Security.Cryptography.X509Certificates.X500DistinguishedName

            #region System.Security.AccessControl.RegistrySecurity

            typeName = @"System.Security.AccessControl.RegistrySecurity";
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

            #endregion System.Security.AccessControl.RegistrySecurity

            #region Deserialized.System.Security.AccessControl.RegistrySecurity

            typeName = @"Deserialized.System.Security.AccessControl.RegistrySecurity";
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

            #endregion Deserialized.System.Security.AccessControl.RegistrySecurity

            #region System.Security.AccessControl.FileSystemSecurity

            typeName = @"System.Security.AccessControl.FileSystemSecurity";
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

            #endregion System.Security.AccessControl.FileSystemSecurity

            #region Deserialized.System.Security.AccessControl.FileSystemSecurity

            typeName = @"Deserialized.System.Security.AccessControl.FileSystemSecurity";
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

            #endregion Deserialized.System.Security.AccessControl.FileSystemSecurity

            #region HelpInfo

            typeName = @"HelpInfo";
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

            #endregion HelpInfo

            #region System.Management.Automation.PSTypeName

            typeName = @"System.Management.Automation.PSTypeName";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 2);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationMethod", @"String"),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"StringSerializationSource", @"Name", conversionType: null),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.PSTypeName

            #region System.Management.Automation.ParameterMetadata

            typeName = @"System.Management.Automation.ParameterMetadata";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 2);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationMethod", @"SpecificProperties"),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PropertySerializationSet",
                    new List<string> { "Name", "ParameterType", "Aliases", "IsDynamic", "SwitchParameter" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.ParameterMetadata

            #region System.Management.Automation.CommandInfo

            typeName = @"System.Management.Automation.CommandInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"Namespace");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Namespace", @"ModuleName", conversionType: null) { IsHidden = true },
                typeMembers,
                isOverride: false);

            newMembers.Add(@"HelpUri");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"HelpUri",
                    GetScriptBlock(@"$oldProgressPreference = $ProgressPreference
          $ProgressPreference = 'SilentlyContinue'
          try
          {
          [Microsoft.PowerShell.Commands.GetHelpCodeMethods]::GetHelpUri($this)
          }
          catch {}
          finally
          {
          $ProgressPreference = $oldProgressPreference
          }"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.CommandInfo

            #region System.Management.Automation.ParameterSetMetadata

            typeName = @"System.Management.Automation.ParameterSetMetadata";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"Flags");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"Flags",
                    GetMethodInfo(typeof(Microsoft.PowerShell.DeserializingTypeConverter), @"GetParameterSetMetadataFlags"),
                    setterCodeReference: null)
                { IsHidden = true },
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 2);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationMethod", @"SpecificProperties"),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PropertySerializationSet",
                    new List<string> { "Position", "Flags", "HelpMessage" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.ParameterSetMetadata

            #region Deserialized.System.Management.Automation.ParameterSetMetadata

            typeName = @"Deserialized.System.Management.Automation.ParameterSetMetadata";
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

            #endregion Deserialized.System.Management.Automation.ParameterSetMetadata

            #region Deserialized.System.Management.Automation.ExtendedTypeDefinition

            typeName = @"Deserialized.System.Management.Automation.ExtendedTypeDefinition";
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

            #endregion Deserialized.System.Management.Automation.ExtendedTypeDefinition

            #region System.Management.Automation.ExtendedTypeDefinition

            typeName = @"System.Management.Automation.ExtendedTypeDefinition";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 4);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationMethod", @"SpecificProperties"),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "TypeNames", "FormatViewDefinition" }),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PropertySerializationSet",
                    new List<string> { "TypeName", "TypeNames", "FormatViewDefinition" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.ExtendedTypeDefinition

            #region Deserialized.System.Management.Automation.FormatViewDefinition

            typeName = @"Deserialized.System.Management.Automation.FormatViewDefinition";
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

            #endregion Deserialized.System.Management.Automation.FormatViewDefinition

            #region System.Management.Automation.FormatViewDefinition

            typeName = @"System.Management.Automation.FormatViewDefinition";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"InstanceId");
            AddMember(
                errors,
                typeName,
                new PSCodeProperty(
                    @"InstanceId",
                    GetMethodInfo(typeof(Microsoft.PowerShell.DeserializingTypeConverter), @"GetFormatViewDefinitionInstanceId"),
                    setterCodeReference: null)
                { IsHidden = true },
                typeMembers,
                isOverride: false);

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

            #endregion System.Management.Automation.FormatViewDefinition

            #region Deserialized.System.Management.Automation.PSControl

            typeName = @"Deserialized.System.Management.Automation.PSControl";
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

            #endregion Deserialized.System.Management.Automation.PSControl

            #region System.Management.Automation.PSControl

            typeName = @"System.Management.Automation.PSControl";
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

            #endregion System.Management.Automation.PSControl

            #region Deserialized.System.Management.Automation.PSControlGroupBy

            typeName = @"Deserialized.System.Management.Automation.PSControlGroupBy";
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

            #endregion Deserialized.System.Management.Automation.PSControlGroupBy

            #region System.Management.Automation.PSControlGroupBy

            typeName = @"System.Management.Automation.PSControlGroupBy";
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

            #endregion System.Management.Automation.PSControlGroupBy

            #region Deserialized.System.Management.Automation.EntrySelectedBy

            typeName = @"Deserialized.System.Management.Automation.EntrySelectedBy";
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

            #endregion Deserialized.System.Management.Automation.EntrySelectedBy

            #region System.Management.Automation.EntrySelectedBy

            typeName = @"System.Management.Automation.EntrySelectedBy";
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

            #endregion System.Management.Automation.EntrySelectedBy

            #region Deserialized.System.Management.Automation.DisplayEntry

            typeName = @"Deserialized.System.Management.Automation.DisplayEntry";
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

            #endregion Deserialized.System.Management.Automation.DisplayEntry

            #region System.Management.Automation.DisplayEntry

            typeName = @"System.Management.Automation.DisplayEntry";
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

            #endregion System.Management.Automation.DisplayEntry

            #region Deserialized.System.Management.Automation.TableControlColumnHeader

            typeName = @"Deserialized.System.Management.Automation.TableControlColumnHeader";
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

            #endregion Deserialized.System.Management.Automation.TableControlColumnHeader

            #region System.Management.Automation.TableControlColumnHeader

            typeName = @"System.Management.Automation.TableControlColumnHeader";
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

            #endregion System.Management.Automation.TableControlColumnHeader

            #region Deserialized.System.Management.Automation.TableControlRow

            typeName = @"Deserialized.System.Management.Automation.TableControlRow";
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

            #endregion Deserialized.System.Management.Automation.TableControlRow

            #region System.Management.Automation.TableControlRow

            typeName = @"System.Management.Automation.TableControlRow";
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

            #endregion System.Management.Automation.TableControlRow

            #region Deserialized.System.Management.Automation.TableControlColumn

            typeName = @"Deserialized.System.Management.Automation.TableControlColumn";
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

            #endregion Deserialized.System.Management.Automation.TableControlColumn

            #region System.Management.Automation.TableControlColumn

            typeName = @"System.Management.Automation.TableControlColumn";
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

            #endregion System.Management.Automation.TableControlColumn

            #region Deserialized.System.Management.Automation.ListControlEntry

            typeName = @"Deserialized.System.Management.Automation.ListControlEntry";
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

            #endregion Deserialized.System.Management.Automation.ListControlEntry

            #region System.Management.Automation.ListControlEntry

            typeName = @"System.Management.Automation.ListControlEntry";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 4);
            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationMethod", @"SpecificProperties"),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSNoteProperty(@"SerializationDepth", 1),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Items", "EntrySelectedBy" }),
                memberSetMembers,
                isOverride: false);

            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PropertySerializationSet",
                    new List<string> { "Items", "SelectedBy", "EntrySelectedBy" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.ListControlEntry

            #region Deserialized.System.Management.Automation.ListControlEntryItem

            typeName = @"Deserialized.System.Management.Automation.ListControlEntryItem";
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

            #endregion Deserialized.System.Management.Automation.ListControlEntryItem

            #region System.Management.Automation.ListControlEntryItem

            typeName = @"System.Management.Automation.ListControlEntryItem";
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

            #endregion System.Management.Automation.ListControlEntryItem

            #region Deserialized.System.Management.Automation.WideControlEntryItem

            typeName = @"Deserialized.System.Management.Automation.WideControlEntryItem";
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

            #endregion Deserialized.System.Management.Automation.WideControlEntryItem

            #region System.Management.Automation.WideControlEntryItem

            typeName = @"System.Management.Automation.WideControlEntryItem";
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

            #endregion System.Management.Automation.WideControlEntryItem

            #region Deserialized.System.Management.Automation.CustomControlEntry

            typeName = @"Deserialized.System.Management.Automation.CustomControlEntry";
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

            #endregion Deserialized.System.Management.Automation.CustomControlEntry

            #region System.Management.Automation.CustomControlEntry

            typeName = @"System.Management.Automation.CustomControlEntry";
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

            #endregion System.Management.Automation.CustomControlEntry

            #region Deserialized.System.Management.Automation.CustomItemBase

            typeName = @"Deserialized.System.Management.Automation.CustomItemBase";
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

            #endregion Deserialized.System.Management.Automation.CustomItemBase

            #region System.Management.Automation.CustomItemBase

            typeName = @"System.Management.Automation.CustomItemBase";
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

            #endregion System.Management.Automation.CustomItemBase

            #region System.Web.Services.Protocols.SoapException

            typeName = @"System.Web.Services.Protocols.SoapException";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"PSMessageDetails");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"PSMessageDetails",
                    GetScriptBlock(@"$this.Detail.""#text"""),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Web.Services.Protocols.SoapException

            #region System.Management.Automation.ErrorRecord

            typeName = @"System.Management.Automation.ErrorRecord";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"PSMessageDetails");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"PSMessageDetails",
                    GetScriptBlock(@"& { Set-StrictMode -Version 1; $this.Exception.InnerException.PSMessageDetails }"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.ErrorRecord

            #region Deserialized.System.Enum

            typeName = @"Deserialized.System.Enum";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"Value");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Value",
                    GetScriptBlock(@"$this.ToString()"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion Deserialized.System.Enum

            #region Microsoft.PowerShell.Commands.Internal.Format.FormatInfoData

            typeName = @"Microsoft.PowerShell.Commands.Internal.Format.FormatInfoData";
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

            #endregion Microsoft.PowerShell.Commands.Internal.Format.FormatInfoData

            #region Deserialized.Microsoft.PowerShell.Commands.Internal.Format.FormatInfoData

            typeName = @"Deserialized.Microsoft.PowerShell.Commands.Internal.Format.FormatInfoData";
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

            #endregion Deserialized.Microsoft.PowerShell.Commands.Internal.Format.FormatInfoData

            #region System.Management.ManagementEventArgs

            typeName = @"System.Management.ManagementEventArgs";
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

            #endregion System.Management.ManagementEventArgs

            #region Deserialized.System.Management.ManagementEventArgs

            typeName = @"Deserialized.System.Management.ManagementEventArgs";
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

            #endregion Deserialized.System.Management.ManagementEventArgs

            #region System.Management.Automation.CallStackFrame

            typeName = @"System.Management.Automation.CallStackFrame";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"Command");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Command",
                    GetScriptBlock(@"if ($null -eq $this.InvocationInfo) { return $this.FunctionName }

          $commandInfo = $this.InvocationInfo.MyCommand
          if ($null -eq $commandInfo) { return $this.InvocationInfo.InvocationName }

          if ($commandInfo.Name -ne """") { return $commandInfo.Name }

          return $this.FunctionName"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Location");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Location",
                    GetScriptBlock(@"$this.GetScriptLocation()"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Arguments");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Arguments",
                    GetScriptBlock(@"$argumentsBuilder = new-object System.Text.StringBuilder

          $null = $(
          $argumentsBuilder.Append(""{"")
          foreach ($entry in $this.InvocationInfo.BoundParameters.GetEnumerator())
          {
          if ($argumentsBuilder.Length -gt 1)
          {
          $argumentsBuilder.Append("", "");
          }

          $argumentsBuilder.Append($entry.Key).Append(""="")

          if ($entry.Value)
          {
          $argumentsBuilder.Append([string]$entry.Value)
          }
          }

          foreach ($arg in $this.InvocationInfo.UnboundArguments.GetEnumerator())
          {
          if ($argumentsBuilder.Length -gt 1)
          {
          $argumentsBuilder.Append("", "")
          }

          if ($arg)
          {
          $argumentsBuilder.Append([string]$arg)
          }
          else
          {
          $argumentsBuilder.Append('$null')
          }
          }

          $argumentsBuilder.Append('}');
          )

          return $argumentsBuilder.ToString();"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Management.Automation.CallStackFrame

            #region Microsoft.PowerShell.Commands.PSSessionConfigurationCommands#PSSessionConfiguration

            typeName = @"Microsoft.PowerShell.Commands.PSSessionConfigurationCommands#PSSessionConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"Permission");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Permission",
                    GetScriptBlock(@"trap { continue; }

          $private:sd = $null
          $private:sd = new-object System.Security.AccessControl.CommonSecurityDescriptor $false,$false,$this.SecurityDescriptorSddl
          if ($private:sd)
          {
          # reset trap
          trap { }

          $private:dacls = """";
          $private:first = $true
          $private:sd.DiscretionaryAcl | ForEach-Object {
          trap { }

          if ($private:first)
          {
          $private:first = $false;
          }
          else
          {
          $private:dacls += "", ""
          }

          $private:dacls += $_.SecurityIdentifier.Translate([System.Security.Principal.NTAccount]).ToString() + "" "" + $_.AceType
          } # end of foreach

          return $private:dacls
          }"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion Microsoft.PowerShell.Commands.PSSessionConfigurationCommands#PSSessionConfiguration

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PingStatus

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PingStatus";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"IPV4Address");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"IPV4Address",
                    GetScriptBlock(@"$iphost = [System.Net.Dns]::GetHostEntry($this.address)
          $iphost.AddressList | Where-Object { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork } | Select-Object -first 1"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"IPV6Address");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"IPV6Address",
                    GetScriptBlock(@"$iphost = [System.Net.Dns]::GetHostEntry($this.address)
          $iphost.AddressList | Where-Object { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetworkV6 } | Select-Object -first 1"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PingStatus

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Process

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Process";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 6));

            // Process regular members.
            newMembers.Add(@"ProcessName");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"ProcessName", @"Name", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Handles");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"Handles", @"Handlecount", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"VM");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"VM", @"VirtualSize", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"WS");
            AddMember(
                errors,
                typeName,
                new PSAliasProperty(@"WS", @"WorkingSetSize", conversionType: null),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Path");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"Path",
                    GetScriptBlock(@"$this.ExecutablePath"),
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
                    new List<string> { "ProcessId", "Name", "HandleCount", "WorkingSetSize", "VirtualSize" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Process

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Msft_CliAlias

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Msft_CliAlias";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "FriendlyName", "PWhere", "Target" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Msft_CliAlias

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BaseBoard

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BaseBoard";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "PoweredOn" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Manufacturer", "Model", "Name", "SerialNumber", "SKU", "Product" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BaseBoard

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BIOS

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BIOS";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "Caption", "SMBIOSPresent" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "SMBIOSBIOSVersion", "Manufacturer", "Name", "SerialNumber", "Version" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BIOS

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BootConfiguration

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BootConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "SettingID", "ConfigurationPath" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "BootDirectory", "Name", "SettingID", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BootConfiguration

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_CDROMDrive

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_CDROMDrive";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Availability", "Drive", "ErrorCleared", "MediaLoaded", "NeedsCleaning", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Drive", "Manufacturer", "VolumeName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_CDROMDrive

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystem

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystem";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "AdminPasswordStatus", "BootupState", "ChassisBootupState", "KeyboardPasswordStatus", "PowerOnPasswordStatus", "PowerSupplyState", "PowerState", "FrontPanelResetStatus", "ThermalState", "Status", "Name" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"POWER");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"POWER",
                    new List<string> { "Name", "PowerManagementCapabilities", "PowerManagementSupported", "PowerOnPasswordStatus", "PowerState", "PowerSupplyState" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Domain", "Manufacturer", "Model", "Name", "PrimaryOwnerName", "TotalPhysicalMemory" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystem

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_PROCESSOR

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_PROCESSOR";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Availability", "CpuStatus", "CurrentVoltage", "DeviceID", "ErrorCleared", "ErrorDescription", "LastErrorCode", "LoadPercentage", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "AddressWidth", "DataWidth", "DeviceID", "ExtClock", "L2CacheSize", "L2CacheSpeed", "MaxClockSpeed", "PowerManagementSupported", "ProcessorType", "Revision", "SocketDesignation", "Version", "VoltageCaps" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "DeviceID", "Manufacturer", "MaxClockSpeed", "Name", "SocketDesignation" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_PROCESSOR

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystemProduct

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystemProduct";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Version" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "IdentifyingNumber", "Name", "Vendor", "Version", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystemProduct

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/CIM_DataFile

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/CIM_DataFile";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Compressed", "Encrypted", "Size", "Hidden", "Name", "Readable", "System", "Version", "Writeable" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/CIM_DataFile

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DCOMApplication

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DCOMApplication";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Status" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "AppID", "InstallDate", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DCOMApplication

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOP

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOP";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "ScreenSaverActive" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Name", "ScreenSaverActive", "ScreenSaverSecure", "ScreenSaverTimeout", "SettingID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOP

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOPMONITOR

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOPMONITOR";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "DeviceID", "Name", "PixelsPerXLogicalInch", "PixelsPerYLogicalInch", "ScreenHeight", "ScreenWidth" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "DeviceID", "IsLocked", "LastErrorCode", "Name", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceID", "DisplayType", "MonitorManufacturer", "Name", "ScreenHeight", "ScreenWidth" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOPMONITOR

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DeviceMemoryAddress

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DeviceMemoryAddress";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "MemoryType" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "MemoryType", "Name", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DeviceMemoryAddress

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskDrive

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskDrive";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "ConfigManagerErrorCode", "LastErrorCode", "NeedsCleaning", "Status", "DeviceID", "StatusInfo", "Partitions" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "BytesPerSector", "ConfigManagerUserConfig", "DefaultBlockSize", "DeviceID", "Index", "InstallDate", "InterfaceType", "MaxBlockSize", "MaxMediaSize", "MinBlockSize", "NumberOfMediaSupported", "Partitions", "SectorsPerTrack", "Size", "TotalCylinders", "TotalHeads", "TotalSectors", "TotalTracks", "TracksPerCylinder" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Partitions", "DeviceID", "Model", "Size", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskDrive

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskQuota

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskQuota";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DiskSpaceUsed", "Limit", "QuotaVolume", "User" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskQuota

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DMAChannel

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DMAChannel";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "AddressSize", "DMAChannel", "MaxTransferSize", "Name", "Port" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DMAChannel

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Environment

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Environment";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "SystemVariable" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "VariableValue", "Name", "UserName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Environment

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Directory

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Directory";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Compressed", "Encrypted", "Name", "Readable", "Writeable" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Hidden", "Archive", "EightDotThreeFileName", "FileSize", "Name", "Compressed", "Encrypted", "Readable" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Directory

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Group

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Group";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Domain", "Name", "SID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Group

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IDEController

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IDEController";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Manufacturer", "Name", "ProtocolSupported", "Status", "StatusInfo" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IDEController

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IRQResource

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IRQResource";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Caption", "Availability" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Hardware", "IRQNumber", "Name", "Shareable", "TriggerLevel", "TriggerType" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IRQResource

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ScheduledJob

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ScheduledJob";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "JobId", "JobStatus", "ElapsedTime", "StartTime", "Owner" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "JobId", "Name", "Owner", "Priority", "Command" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ScheduledJob

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LoadOrderGroup

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LoadOrderGroup";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "GroupOrder", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LoadOrderGroup

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalDisk

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalDisk";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Availability", "DeviceID", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceID", "DriveType", "ProviderName", "FreeSpace", "Size", "VolumeName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalDisk

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogonSession

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogonSession";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "AuthenticationPackage", "LogonId", "LogonType", "Name", "StartTime", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogonSession

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_CACHEMEMORY

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_CACHEMEMORY";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 4));

            // Process regular members.
            newMembers.Add(@"ERROR");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"ERROR",
                    new List<string> { "DeviceID", "ErrorCorrectType" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Availability", "DeviceID", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "BlockSize", "CacheSpeed", "CacheType", "DeviceID", "InstalledSize", "Level", "MaxCacheSize", "NumberOfBlocks", "Status", "WritePolicy" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "BlockSize", "CacheSpeed", "CacheType", "DeviceID", "InstalledSize", "Level", "MaxCacheSize", "NumberOfBlocks", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_CACHEMEMORY

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalMemoryConfiguration

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalMemoryConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "AvailableVirtualMemory", "Name", "TotalVirtualMemory" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Name", "TotalVirtualMemory", "TotalPhysicalMemory", "TotalPageFileSpace" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalMemoryConfiguration

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PhysicalMemoryArray

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PhysicalMemoryArray";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "Replaceable", "Location" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Model", "Name", "MaxCapacity", "MemoryDevices" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PhysicalMemoryArray

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_NetworkClient

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_NetworkClient";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Status" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "InstallDate", "Manufacturer", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_NetworkClient

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkLoginProfile

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkLoginProfile";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Privileges", "Profile", "UserId", "UserType", "Workstations" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkLoginProfile

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkProtocol

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkProtocol";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"FULLXXX");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"FULLXXX",
                    new List<string> { "ConnectionlessService", "Description", "GuaranteesDelivery", "GuaranteesSequencing", "InstallDate", "MaximumAddressSize", "MaximumMessageSize", "MessageOriented", "MinimumAddressSize", "Name", "PseudoStreamOriented", "Status", "SupportsBroadcasting", "SupportsConnectData", "SupportsDisconnectData", "SupportsEncryption", "SupportsExpeditedData", "SupportsFragmentation", "SupportsGracefulClosing", "SupportsGuaranteedBandwidth", "SupportsMulticasting", "SupportsQualityofService" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Status", "SupportsBroadcasting", "SupportsConnectData", "SupportsDisconnectData", "SupportsEncryption", "SupportsExpeditedData", "SupportsFragmentation", "SupportsGracefulClosing", "SupportsGuaranteedBandwidth", "SupportsMulticasting", "SupportsQualityofService" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "GuaranteesDelivery", "GuaranteesSequencing", "ConnectionlessService", "Status", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkProtocol

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkConnection

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkConnection";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "ConnectionState", "Persistent", "LocalName", "RemoteName" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "LocalName", "RemoteName", "ConnectionState", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkConnection

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapter

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapter";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Availability", "Name", "Status", "StatusInfo", "DeviceID" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "ServiceName", "MACAddress", "AdapterType", "DeviceID", "Name", "NetworkAddresses", "Speed" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapter

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapterConfiguration

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapterConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 6));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "DHCPLeaseExpires", "Index", "Description" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"DHCP");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DHCP",
                    new List<string> { "Description", "DHCPEnabled", "DHCPLeaseExpires", "DHCPLeaseObtained", "DHCPServer", "Index" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"DNS");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DNS",
                    new List<string> { "Description", "DNSDomain", "DNSDomainSuffixSearchOrder", "DNSEnabledForWINSResolution", "DNSHostName", "DNSServerSearchOrder", "DomainDNSRegistrationEnabled", "FullDNSRegistrationEnabled", "Index" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"IP");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"IP",
                    new List<string> { "Description", "Index", "IPAddress", "IPConnectionMetric", "IPEnabled", "IPFilterSecurityEnabled" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"WINS");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"WINS",
                    new List<string> { "Description", "Index", "WINSEnableLMHostsLookup", "WINSHostLookupFile", "WINSPrimaryServer", "WINSScopeID", "WINSSecondaryServer" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DHCPEnabled", "IPAddress", "DefaultIPGateway", "DNSDomain", "ServiceName", "Description", "Index" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapterConfiguration

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTDomain

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTDomain";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "DomainName" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"GUID");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"GUID",
                    new List<string> { "DomainName", "DomainGuid" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "ClientSiteName", "DcSiteName", "Description", "DnsForestName", "DomainControllerAddress", "DomainControllerName", "DomainName", "Roles", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTDomain

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTLogEvent

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTLogEvent";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Category", "CategoryString", "EventCode", "EventIdentifier", "TypeEvent", "InsertionStrings", "LogFile", "Message", "RecordNumber", "SourceName", "TimeGenerated", "TimeWritten", "Type", "UserName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTLogEvent

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTEventlogFile

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTEventlogFile";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "LogfileName", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "FileSize", "LogfileName", "Name", "NumberOfRecords" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTEventlogFile

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OnBoardDevice

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OnBoardDevice";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Description" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceType", "SerialNumber", "Enabled", "Description" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OnBoardDevice

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OperatingSystem

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OperatingSystem";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"FREE");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"FREE",
                    new List<string> { "FreePhysicalMemory", "FreeSpaceInPagingFiles", "FreeVirtualMemory", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "SystemDirectory", "Organization", "BuildNumber", "RegisteredUser", "SerialNumber", "Version" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OperatingSystem

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PageFileUsage

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PageFileUsage";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "CurrentUsage" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Name", "PeakUsage" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PageFileUsage

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PageFileSetting

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PageFileSetting";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "MaximumSize", "Name", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PageFileSetting

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskPartition

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskPartition";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Index", "Status", "StatusInfo", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "NumberOfBlocks", "BootPartition", "Name", "PrimaryPartition", "Size", "Index" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskPartition

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortResource

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortResource";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "NetConnectionStatus", "Status", "Name", "StartingAddress", "EndingAddress" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Name", "Alias" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortResource

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortConnector

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortConnector";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "ExternalReferenceDesignator" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Tag", "ConnectorType", "SerialNumber", "ExternalReferenceDesignator", "PortType" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortConnector

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Printer

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Printer";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Location", "Name", "PrinterState", "PrinterStatus", "ShareName", "SystemName" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Printer

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrinterConfiguration

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrinterConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "DriverVersion", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "PrintQuality", "DriverVersion", "Name", "PaperSize", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrinterConfiguration

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrintJob

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrintJob";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Document", "JobId", "JobStatus", "Name", "PagesPrinted", "Status", "JobIdCopy", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Document", "JobId", "JobStatus", "Owner", "Priority", "Size", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrintJob

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ProcessXXX

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ProcessXXX";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 5));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "ProcessId" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"MEMORY");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"MEMORY",
                    new List<string> { "Handle", "MaximumWorkingSetSize", "MinimumWorkingSetSize", "Name", "PageFaults", "PageFileUsage", "PeakPageFileUsage", "PeakVirtualSize", "PeakWorkingSetSize", "PrivatePageCount", "QuotaNonPagedPoolUsage", "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "QuotaPeakPagedPoolUsage", "VirtualSize", "WorkingSetSize" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"IO");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"IO",
                    new List<string> { "Name", "ProcessId", "ReadOperationCount", "ReadTransferCount", "WriteOperationCount", "WriteTransferCount" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"STATISTICS");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"STATISTICS",
                    new List<string> { "HandleCount", "Name", "KernelModeTime", "MaximumWorkingSetSize", "MinimumWorkingSetSize", "OtherOperationCount", "OtherTransferCount", "PageFaults", "PageFileUsage", "PeakPageFileUsage", "PeakVirtualSize", "PeakWorkingSetSize", "PrivatePageCount", "ProcessId", "QuotaNonPagedPoolUsage", "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "QuotaPeakPagedPoolUsage", "ReadOperationCount", "ReadTransferCount", "ThreadCount", "UserModeTime", "VirtualSize", "WorkingSetSize", "WriteOperationCount", "WriteTransferCount" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "ThreadCount", "HandleCount", "Name", "Priority", "ProcessId", "WorkingSetSize" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ProcessXXX

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Product

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Product";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Version", "InstallState" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "IdentifyingNumber", "Name", "Vendor", "Version", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Product

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuickFixEngineering

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuickFixEngineering";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"InstalledOn");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"InstalledOn",
                    GetScriptBlock(@"if ([environment]::osversion.version.build -ge 7000)
          {
          # WMI team fixed the formatting issue related to InstalledOn
          # property in Windows7 (to return string)..so returning the WMI's
          # version directly
          [DateTime]::Parse($this.psBase.CimInstanceProperties[""InstalledOn""].Value, [System.Globalization.DateTimeFormatInfo]::new())
          }
          else
          {
          $orig = $this.psBase.CimInstanceProperties[""InstalledOn""].Value
          $date = [datetime]::FromFileTimeUTC($(""0x"" + $orig))
          if ($date -lt ""1/1/1980"")
          {
          if ($orig -match ""([0-9]{4})([01][0-9])([012][0-9])"")
          {
          new-object datetime @([int]$matches[1], [int]$matches[2], [int]$matches[3])
          }
          }
          else
          {
          $date
          }
          }"),
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
                    new List<string> { "Description", "FixComments", "HotFixID", "InstallDate", "InstalledBy", "InstalledOn", "Name", "ServicePackInEffect", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuickFixEngineering

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuotaSetting

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuotaSetting";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "State", "VolumePath", "Caption" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "DefaultLimit", "SettingID", "State", "VolumePath", "DefaultWarningLimit" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuotaSetting

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OSRecoveryConfiguration

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OSRecoveryConfiguration";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DebugFilePath", "Name", "SettingID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OSRecoveryConfiguration

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Registry

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Registry";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "CurrentSize", "MaximumSize", "ProposedSize" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "CurrentSize", "MaximumSize", "Name", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Registry

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SCSIController

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SCSIController";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DriverName", "Manufacturer", "Name", "ProtocolSupported", "Status", "StatusInfo" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SCSIController

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PerfRawData_PerfNet_Server

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PerfRawData_PerfNet_Server";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "LogonPerSec", "LogonTotal", "Name", "ServerSessions", "WorkItemShortages" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PerfRawData_PerfNet_Server

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Service

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Service";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 3));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Name", "Status", "ExitCode" }),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"PSConfiguration");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSConfiguration",
                    new List<string> { "DesktopInteract", "ErrorControl", "Name", "PathName", "ServiceType", "StartMode" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "ExitCode", "Name", "ProcessId", "StartMode", "State", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Service

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Share

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Share";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Type", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Name", "Path", "Description" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Share

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SoftwareElement

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SoftwareElement";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "SoftwareElementState", "Name" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Name", "Path", "SerialNumber", "SoftwareElementID", "Version" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SoftwareElement

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SoftwareFeature

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SoftwareFeature";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "InstallState", "LastUse" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "IdentifyingNumber", "ProductName", "Vendor", "Version" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SoftwareFeature

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_SoundDevice

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_SoundDevice";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "ConfigManagerUserConfig", "Name", "Status", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Manufacturer", "Name", "Status", "StatusInfo" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_SoundDevice

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_StartupCommand

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_StartupCommand";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Command", "User", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_StartupCommand

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemAccount

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemAccount";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "SIDType", "Name", "Domain" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Caption", "Domain", "Name", "SID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemAccount

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemDriver

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemDriver";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Name", "State", "ExitCode", "Started", "ServiceSpecificExitCode" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DisplayName", "Name", "State", "Status", "Started" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemDriver

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemEnclosure

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemEnclosure";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Tag", "Status", "Name", "SecurityStatus" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Manufacturer", "Model", "LockPresent", "SerialNumber", "SMBIOSAssetTag", "SecurityStatus" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemEnclosure

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemSlot

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemSlot";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "SlotDesignation" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "SlotDesignation", "Tag", "SupportsHotPlug", "Status", "Shared", "PMESignal", "MaxDataWidth" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemSlot

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TapeDrive

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TapeDrive";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Availability", "DeviceID", "NeedsCleaning", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceID", "Id", "Manufacturer", "Name", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TapeDrive

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TemperatureProbe

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TemperatureProbe";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "CurrentReading", "DeviceID", "Name", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "CurrentReading", "Name", "Description", "MinReadable", "MaxReadable", "Status" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TemperatureProbe

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TimeZone

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TimeZone";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Bias", "SettingID", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TimeZone

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UninterruptiblePowerSupply

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UninterruptiblePowerSupply";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "DeviceID", "EstimatedChargeRemaining", "EstimatedRunTime", "Name", "StatusInfo", "TimeOnBackup" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "DeviceID", "EstimatedRunTime", "Name", "TimeOnBackup", "UPSPort", "Caption" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UninterruptiblePowerSupply

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UserAccount

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UserAccount";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "Caption", "PasswordExpires" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "AccountType", "Caption", "Domain", "SID", "FullName", "Name" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UserAccount

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_VoltageProbe

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_VoltageProbe";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 2));

            // Process regular members.
            newMembers.Add(@"PSStatus");
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"PSStatus",
                    new List<string> { "Status", "DeviceID", "Name", "NominalReading", "StatusInfo" }),
                typeMembers,
                isOverride: false);

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Status", "Description", "CurrentReading", "MaxReadable", "MinReadable" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_VoltageProbe

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_VolumeQuotaSetting

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_VolumeQuotaSetting";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "Element", "Setting" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_VolumeQuotaSetting

            #region Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_WMISetting

            typeName = @"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_WMISetting";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process standard members.
            memberSetMembers = new PSMemberInfoInternalCollection<PSMemberInfo>(capacity: 1);
            AddMember(
                errors,
                typeName,
                new PSPropertySet(
                    @"DefaultDisplayPropertySet",
                    new List<string> { "BuildVersion", "Caption", "DatabaseDirectory", "EnableEvents", "LoggingLevel", "SettingID" }),
                memberSetMembers,
                isOverride: false);

            ProcessStandardMembers(
                errors,
                typeName,
                memberSetMembers,
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_WMISetting

            #region Microsoft.Management.Infrastructure.CimClass

            typeName = @"Microsoft.Management.Infrastructure.CimClass";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"CimClassName");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(
                    @"CimClassName",
                    GetScriptBlock(@"[OutputType([string])]
          param()
          $this.PSBase.CimSystemProperties.ClassName"),
                    setterScript: null,
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion Microsoft.Management.Infrastructure.CimClass

            #region Microsoft.Management.Infrastructure.CimCmdlets.CimIndicationEventInstanceEventArgs

            typeName = @"Microsoft.Management.Infrastructure.CimCmdlets.CimIndicationEventInstanceEventArgs";
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

            #endregion Microsoft.Management.Infrastructure.CimCmdlets.CimIndicationEventInstanceEventArgs

            #region System.Management.Automation.Breakpoint

            typeName = @"System.Management.Automation.Breakpoint";
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

            #endregion System.Management.Automation.Breakpoint

            #region Deserialized.System.Management.Automation.Breakpoint

            typeName = @"Deserialized.System.Management.Automation.Breakpoint";
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

            #endregion Deserialized.System.Management.Automation.Breakpoint

            #region System.Management.Automation.BreakpointUpdatedEventArgs

            typeName = @"System.Management.Automation.BreakpointUpdatedEventArgs";
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

            #endregion System.Management.Automation.BreakpointUpdatedEventArgs

            #region Deserialized.System.Management.Automation.BreakpointUpdatedEventArgs

            typeName = @"Deserialized.System.Management.Automation.BreakpointUpdatedEventArgs";
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

            #endregion Deserialized.System.Management.Automation.BreakpointUpdatedEventArgs

            #region System.Management.Automation.DebuggerCommand

            typeName = @"System.Management.Automation.DebuggerCommand";
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

            #endregion System.Management.Automation.DebuggerCommand

            #region Deserialized.System.Management.Automation.DebuggerCommand

            typeName = @"Deserialized.System.Management.Automation.DebuggerCommand";
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

            #endregion Deserialized.System.Management.Automation.DebuggerCommand

            #region System.Management.Automation.DebuggerCommandResults

            typeName = @"System.Management.Automation.DebuggerCommandResults";
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

            #endregion System.Management.Automation.DebuggerCommandResults

            #region Deserialized.System.Management.Automation.DebuggerCommandResults

            typeName = @"Deserialized.System.Management.Automation.DebuggerCommandResults";
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

            #endregion Deserialized.System.Management.Automation.DebuggerCommandResults

            #region System.Version#IncludeLabel

            typeName = @"System.Version#IncludeLabel";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Process regular members.
            newMembers.Add(@"ToString");
            AddMember(
                errors,
                typeName,
                new PSScriptMethod(
                    @"ToString",
                    GetScriptBlock(@"
          $suffix = """"
          if (![String]::IsNullOrEmpty($this.PSSemVerPreReleaseLabel))
          {
              $suffix = ""-""+$this.PSSemVerPreReleaseLabel
          }

          if (![String]::IsNullOrEmpty($this.PSSemVerBuildLabel))
          {
              $suffix += ""+""+$this.PSSemVerBuildLabel
          }
          ""$($this.Major).$($this.Minor).$($this.Build)""+$suffix
            "),
                    shouldCloneOnAccess: true),
                typeMembers,
                isOverride: false);

            #endregion System.Version#IncludeLabel

#if UNIX
            #region UnixStat

            typeName = @"System.IO.FileSystemInfo";
            typeMembers = _extendedMembers.GetOrAdd(typeName, GetValueFactoryBasedOnInitCapacity(capacity: 1));

            // Where we have a method to invoke below, first check to be sure that the object is present
            // to avoid null reference issues
            newMembers.Add(@"UnixMode");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(@"UnixMode", GetScriptBlock(@"if ($this.UnixStat) { $this.UnixStat.GetModeString() }")),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"User");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(@"User", GetScriptBlock(@" if ($this.UnixStat) { $this.UnixStat.GetUserName() } ")),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Group");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(@"Group", GetScriptBlock(@" if ($this.UnixStat) { $this.UnixStat.GetGroupName() } ")),
                typeMembers,
                isOverride: false);

            newMembers.Add(@"Size");
            AddMember(
                errors,
                typeName,
                new PSScriptProperty(@"Size", GetScriptBlock(@"$this.UnixStat.Size")),
                typeMembers,
                isOverride: false);

            #endregion
#endif

            // Update binder version for newly added members.
            foreach (string memberName in newMembers)
            {
                PSGetMemberBinder.TypeTableMemberAdded(memberName);
            }
        }
    }
}
