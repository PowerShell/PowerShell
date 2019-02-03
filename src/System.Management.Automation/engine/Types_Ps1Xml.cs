// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;

namespace System.Management.Automation.Runspaces
{
    internal sealed class Types_Ps1Xml
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        static MethodInfo GetMethodInfo(Type type, string method)
        {
            return type.GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.IgnoreCase);
        }

        static ScriptBlock GetScriptBlock(string s)
        {
            var sb = ScriptBlock.CreateDelayParsedScriptBlock(s, isProductCode: true);
            sb.LanguageMode = PSLanguageMode.FullLanguage;
            return sb;
        }

        public static IEnumerable<TypeData> Get()
        {
            TypeData td;

            var td2 = new TypeData(@"System.Xml.XmlNode", true);
            td2.Members.Add("ToString",
                new CodeMethodData("ToString", GetMethodInfo(typeof(Microsoft.PowerShell.ToStringCodeMethods), @"XmlNode")));
            yield return td2;

            var td3 = new TypeData(@"System.Xml.XmlNodeList", true);
            td3.Members.Add("ToString",
                new CodeMethodData("ToString", GetMethodInfo(typeof(Microsoft.PowerShell.ToStringCodeMethods), @"XmlNodeList")));
            yield return td3;

            var td4 = new TypeData(@"System.Management.Automation.PSDriveInfo", true);
            td4.Members.Add("Used",
                new ScriptPropertyData(@"Used", GetScriptBlock(@"## Ensure that this is a FileSystem drive
          if($this.Provider.ImplementingType -eq
          [Microsoft.PowerShell.Commands.FileSystemProvider])
          {
          $driveInfo = [System.IO.DriveInfo]::New($this.Root)
          if ( $driveInfo.IsReady ) { $driveInfo.TotalSize - $driveInfo.AvailableFreeSpace }
          }"), null));
            td4.Members.Add("Free",
                new ScriptPropertyData(@"Free", GetScriptBlock(@"## Ensure that this is a FileSystem drive
          if($this.Provider.ImplementingType -eq
          [Microsoft.PowerShell.Commands.FileSystemProvider])
          {
          [System.IO.DriveInfo]::New($this.Root).AvailableFreeSpace
          }"), null));
            yield return td4;

#if !UNIX
            var td5 = new TypeData(@"System.DirectoryServices.PropertyValueCollection", true);
            td5.Members.Add("ToString",
                new CodeMethodData("ToString", GetMethodInfo(typeof(Microsoft.PowerShell.ToStringCodeMethods), @"PropertyValueCollection")));
            yield return td5;
#endif

            var td6 = new TypeData(@"System.Drawing.Printing.PrintDocument", true);
            td6.Members.Add("Name",
                new ScriptPropertyData(@"Name", GetScriptBlock(@"$this.PrinterSettings.PrinterName"), null));
            td6.Members.Add("Color",
                new ScriptPropertyData(@"Color", GetScriptBlock(@"$this.PrinterSettings.SupportsColor"), null));
            td6.Members.Add("Duplex",
                new ScriptPropertyData(@"Duplex", GetScriptBlock(@"$this.PrinterSettings.Duplex"), null));
            yield return td6;

            var td7 = new TypeData(@"System.Management.Automation.ApplicationInfo", true);
            td7.Members.Add("FileVersionInfo",
                new ScriptPropertyData(@"FileVersionInfo", GetScriptBlock(@"[System.Diagnostics.FileVersionInfo]::getversioninfo( $this.Path )"), null));
            yield return td7;

            var td8 = new TypeData(@"System.DateTime", true);
            td8.Members.Add("DateTime",
                new ScriptPropertyData(@"DateTime", GetScriptBlock(@"if ((& { Set-StrictMode -Version 1; $this.DisplayHint }) -ieq  ""Date"")
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
          }"), null));
            yield return td8;

            var td9 = new TypeData(@"System.Net.IPAddress", true);
            td9.Members.Add("IPAddressToString",
                new ScriptPropertyData(@"IPAddressToString", GetScriptBlock(@"$this.Tostring()"), null));
            td9.DefaultDisplayProperty = @"IPAddressToString";
            td9.SerializationDepth = 1;
            yield return td9;

            var td10 = new TypeData(@"Deserialized.System.Net.IPAddress", true);
            td10.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td10;

            var td11 = new TypeData(@"System.Diagnostics.ProcessModule", true);
            td11.Members.Add("Size",
                new ScriptPropertyData(@"Size", GetScriptBlock(@"$this.ModuleMemorySize / 1024"), null));
            td11.Members.Add("Company",
                new ScriptPropertyData(@"Company", GetScriptBlock(@"$this.FileVersionInfo.CompanyName"), null));
            td11.Members.Add("FileVersion",
                new ScriptPropertyData(@"FileVersion", GetScriptBlock(@"$this.FileVersionInfo.FileVersion"), null));
            td11.Members.Add("ProductVersion",
                new ScriptPropertyData(@"ProductVersion", GetScriptBlock(@"$this.FileVersionInfo.ProductVersion"), null));
            td11.Members.Add("Description",
                new ScriptPropertyData(@"Description", GetScriptBlock(@"$this.FileVersionInfo.FileDescription"), null));
            td11.Members.Add("Product",
                new ScriptPropertyData(@"Product", GetScriptBlock(@"$this.FileVersionInfo.ProductName"), null));
            yield return td11;

            var td12 = new TypeData(@"System.Collections.DictionaryEntry", true);
            td12.Members.Add("Name",
                new AliasPropertyData("Name", "Key"));
            yield return td12;

            var td13 = new TypeData(@"System.Management.Automation.PSModuleInfo", true);
            td13.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Name", "Path", "Description", "Guid", "Version", "ModuleBase", "ModuleType", "PrivateData", "AccessMode", "ExportedAliases", "ExportedCmdlets", "ExportedFunctions", "ExportedVariables", "NestedModules" }) { Name = "DefaultDisplayPropertySet" };
            yield return td13;

            var td14 = new TypeData(@"System.ServiceProcess.ServiceController", true);
            td14.Members.Add("Name",
                new AliasPropertyData("Name", "ServiceName"));
            td14.Members.Add("RequiredServices",
                new AliasPropertyData("RequiredServices", "ServicesDependedOn"));
            td14.Members.Add("ToString",
                new ScriptMethodData(@"ToString", GetScriptBlock(@"$this.ServiceName")));
            td14.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Status", "Name", "DisplayName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td14;

            var td15 = new TypeData(@"Deserialized.System.ServiceProcess.ServiceController", true);
            td15.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Status", "Name", "DisplayName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td15;

            var td16 = new TypeData(@"System.Management.Automation.CmdletInfo", true);
            td16.Members.Add("DLL",
                new ScriptPropertyData(@"DLL", GetScriptBlock(@"$this.ImplementingType.Assembly.Location"), null));
            yield return td16;

            var td17 = new TypeData(@"System.Management.Automation.AliasInfo", true);
            td17.Members.Add("ResolvedCommandName",
                new ScriptPropertyData(@"ResolvedCommandName", GetScriptBlock(@"$this.ResolvedCommand.Name"), null));
            td17.Members.Add("DisplayName",
                new ScriptPropertyData(@"DisplayName", GetScriptBlock(@"if ($this.Name.IndexOf('-') -lt 0)
          {
          if ($null -ne $this.ResolvedCommand)
          {
          $this.Name + "" -> "" + $this.ResolvedCommand.Name
          }
          else
          {
          $this.Name + "" -> "" + $this.Definition
          }
          }
          else
          {
          $this.Name
          }"), null));
            yield return td17;

#if !UNIX
            var td18 = new TypeData(@"System.DirectoryServices.DirectoryEntry", true);
            td18.Members.Add("ConvertLargeIntegerToInt64",
                new CodeMethodData("ConvertLargeIntegerToInt64", GetMethodInfo(typeof(Microsoft.PowerShell.AdapterCodeMethods), @"ConvertLargeIntegerToInt64")));
            td18.Members.Add("ConvertDNWithBinaryToString",
                new CodeMethodData("ConvertDNWithBinaryToString", GetMethodInfo(typeof(Microsoft.PowerShell.AdapterCodeMethods), @"ConvertDNWithBinaryToString")));
            td18.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "distinguishedName", "Path" }) { Name = "DefaultDisplayPropertySet" };
            yield return td18;
#endif

            var td19 = new TypeData(@"System.IO.DirectoryInfo", true);
            td19.Members.Add("Mode",
                new CodePropertyData("Mode", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "Mode"), null));
            td19.Members.Add("ModeWithoutHardLink",
                new CodePropertyData("ModeWithoutHardLink", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "ModeWithoutHardLink"), null));
            td19.Members.Add("BaseName",
                new ScriptPropertyData(@"BaseName", GetScriptBlock(@"$this.Name"), null));
            td19.Members.Add("Target",
                new CodePropertyData("Target", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.InternalSymbolicLinkLinkCodeMethods), "GetTarget"), null));
            td19.Members.Add("LinkType",
                new CodePropertyData("LinkType", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.InternalSymbolicLinkLinkCodeMethods), "GetLinkType"), null));
            td19.Members.Add("NameString",
                new CodePropertyData("NameString", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "NameString"), null) { IsHidden = true });
            td19.Members.Add("LengthString",
                new CodePropertyData("LengthString", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "LengthString"), null) { IsHidden = true });
            td19.Members.Add("LastWriteTimeString",
                new CodePropertyData("LastWriteTimeString", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "LastWriteTimeString"), null) { IsHidden = true });
            td19.DefaultDisplayProperty = @"Name";
            yield return td19;

            var td20 = new TypeData(@"System.IO.FileInfo", true);
            td20.Members.Add("Mode",
                new CodePropertyData("Mode", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "Mode"), null));
            td20.Members.Add("ModeWithoutHardLink",
                new CodePropertyData("ModeWithoutHardLink", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "ModeWithoutHardLink"), null));
            td20.Members.Add("VersionInfo",
                new ScriptPropertyData(@"VersionInfo", GetScriptBlock(@"[System.Diagnostics.FileVersionInfo]::GetVersionInfo($this.FullName)"), null));
            td20.Members.Add("BaseName",
                new ScriptPropertyData(@"BaseName", GetScriptBlock(@"if ($this.Extension.Length -gt 0){$this.Name.Remove($this.Name.Length - $this.Extension.Length)}else{$this.Name}"), null));
            td20.Members.Add("Target",
                new CodePropertyData("Target", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.InternalSymbolicLinkLinkCodeMethods), "GetTarget"), null));
            td20.Members.Add("LinkType",
                new CodePropertyData("LinkType", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.InternalSymbolicLinkLinkCodeMethods), "GetLinkType"), null));
            td20.Members.Add("NameString",
                new CodePropertyData("NameString", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "NameString"), null) { IsHidden = true });
            td20.Members.Add("LengthString",
                new CodePropertyData("LengthString", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "LengthString"), null) { IsHidden = true });
            td20.Members.Add("LastWriteTimeString",
                new CodePropertyData("LastWriteTimeString", GetMethodInfo(typeof(Microsoft.PowerShell.Commands.FileSystemProvider), "LastWriteTimeString"), null) { IsHidden = true });
            td20.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "LastWriteTime", "Length", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td20;

            var td21 = new TypeData(@"System.Diagnostics.FileVersionInfo", true);
            td21.Members.Add("FileVersionRaw",
                new ScriptPropertyData(@"FileVersionRaw", GetScriptBlock(@"New-Object System.Version -ArgumentList @(
            $this.FileMajorPart
            $this.FileMinorPart
            $this.FileBuildPart
            $this.FilePrivatePart)"), null));
            td21.Members.Add("ProductVersionRaw",
                new ScriptPropertyData(@"ProductVersionRaw", GetScriptBlock(@"New-Object System.Version -ArgumentList @(
            $this.ProductMajorPart
            $this.ProductMinorPart
            $this.ProductBuildPart
            $this.ProductPrivatePart)"), null));
            yield return td21;

            var td22 = new TypeData(@"System.Diagnostics.EventLogEntry", true);
            td22.Members.Add("EventID",
                new ScriptPropertyData(@"EventID", GetScriptBlock(@"$this.get_EventID() -band 0xFFFF"), null));
            yield return td22;

            var td23 = new TypeData(@"System.Management.ManagementBaseObject", true);
            td23.Members.Add("PSComputerName",
                new AliasPropertyData("PSComputerName", "__SERVER"));
            yield return td23;

            var td24 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_PingStatus", true);
            td24.Members.Add("IPV4Address",
                new ScriptPropertyData(@"IPV4Address", GetScriptBlock(@"$iphost = [System.Net.Dns]::GetHostEntry($this.address)
          $iphost.AddressList | Where-Object { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork } | Select-Object -first 1"), null));
            td24.Members.Add("IPV6Address",
                new ScriptPropertyData(@"IPV6Address", GetScriptBlock(@"$iphost = [System.Net.Dns]::GetHostEntry($this.address)
          $iphost.AddressList | Where-Object { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetworkV6 } | Select-Object -first 1"), null));
            yield return td24;

            var td25 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_Process", true);
            td25.Members.Add("ProcessName",
                new AliasPropertyData("ProcessName", "Name"));
            td25.Members.Add("Handles",
                new AliasPropertyData("Handles", "Handlecount"));
            td25.Members.Add("VM",
                new AliasPropertyData("VM", "VirtualSize"));
            td25.Members.Add("WS",
                new AliasPropertyData("WS", "WorkingSetSize"));
            td25.Members.Add("Path",
                new ScriptPropertyData(@"Path", GetScriptBlock(@"$this.ExecutablePath"), null));
            yield return td25;

            var td26 = new TypeData(@"System.Diagnostics.Process", true);
            td26.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "Name", "Id", "PriorityClass", "FileVersion" }) { Name = "PSConfiguration" });
            td26.Members.Add("PSResources",
                new PropertySetData(new[] { "Name", "Id", "Handlecount", "WorkingSet", "NonPagedMemorySize", "PagedMemorySize", "PrivateMemorySize", "VirtualMemorySize", "Threads.Count", "TotalProcessorTime" }) { Name = "PSResources" });
            td26.Members.Add("Name",
                new AliasPropertyData("Name", "ProcessName"));
            td26.Members.Add("SI",
                new AliasPropertyData("SI", "SessionId"));
            td26.Members.Add("Handles",
                new AliasPropertyData("Handles", "Handlecount"));
            td26.Members.Add("VM",
                new AliasPropertyData("VM", "VirtualMemorySize64"));
            td26.Members.Add("WS",
                new AliasPropertyData("WS", "WorkingSet64"));
            td26.Members.Add("PM",
                new AliasPropertyData("PM", "PagedMemorySize64"));
            td26.Members.Add("NPM",
                new AliasPropertyData("NPM", "NonpagedSystemMemorySize64"));
            td26.Members.Add("Path",
                new ScriptPropertyData(@"Path", GetScriptBlock(@"$this.Mainmodule.FileName"), null));
            td26.Members.Add("Parent",
                new CodePropertyData("Parent", GetMethodInfo(typeof(Microsoft.PowerShell.ProcessCodeMethods), @"GetParentProcess")));
            td26.Members.Add("Company",
                new ScriptPropertyData(@"Company", GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.CompanyName"), null));
            td26.Members.Add("CPU",
                new ScriptPropertyData(@"CPU", GetScriptBlock(@"$this.TotalProcessorTime.TotalSeconds"), null));
            td26.Members.Add("FileVersion",
                new ScriptPropertyData(@"FileVersion", GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.FileVersion"), null));
            td26.Members.Add("ProductVersion",
                new ScriptPropertyData(@"ProductVersion", GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.ProductVersion"), null));
            td26.Members.Add("Description",
                new ScriptPropertyData(@"Description", GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.FileDescription"), null));
            td26.Members.Add("Product",
                new ScriptPropertyData(@"Product", GetScriptBlock(@"$this.Mainmodule.FileVersionInfo.ProductName"), null));
            td26.Members.Add("__NounName",
                new NotePropertyData(@"__NounName", @"Process"));
            td26.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Id", "Handles", "CPU", "SI", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td26;

            var td27 = new TypeData(@"Deserialized.System.Diagnostics.Process", true);
            td27.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "Name", "Id", "PriorityClass", "FileVersion" }) { Name = "PSConfiguration" });
            td27.Members.Add("PSResources",
                new PropertySetData(new[] { "Name", "Id", "Handlecount", "WorkingSet", "NonPagedMemorySize", "PagedMemorySize", "PrivateMemorySize", "VirtualMemorySize", "Threads.Count", "TotalProcessorTime" }) { Name = "PSResources" });
            td27.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Id", "Handles", "CPU", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td27;

            var td28 = new TypeData(@"System.Management.ManagementObject#root\cli\Msft_CliAlias", true);
            td28.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "FriendlyName", "PWhere", "Target" }) { Name = "DefaultDisplayPropertySet" };
            yield return td28;

            var td29 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_BaseBoard", true);
            td29.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "PoweredOn" }) { Name = "PSStatus" });
            td29.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Manufacturer", "Model", "Name", "SerialNumber", "SKU", "Product" }) { Name = "DefaultDisplayPropertySet" };
            yield return td29;

            var td30 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_BIOS", true);
            td30.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "Caption", "SMBIOSPresent" }) { Name = "PSStatus" });
            td30.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "SMBIOSBIOSVersion", "Manufacturer", "Name", "SerialNumber", "Version" }) { Name = "DefaultDisplayPropertySet" };
            yield return td30;

            var td31 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_BootConfiguration", true);
            td31.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "SettingID", "ConfigurationPath" }) { Name = "PSStatus" });
            td31.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "BootDirectory", "Name", "SettingID", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td31;

            var td32 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_CDROMDrive", true);
            td32.Members.Add("PSStatus",
                new PropertySetData(new[] { "Availability", "Drive", "ErrorCleared", "MediaLoaded", "NeedsCleaning", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td32.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Drive", "Manufacturer", "VolumeName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td32;

            var td33 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_ComputerSystem", true);
            td33.Members.Add("PSStatus",
                new PropertySetData(new[] { "AdminPasswordStatus", "BootupState", "ChassisBootupState", "KeyboardPasswordStatus", "PowerOnPasswordStatus", "PowerSupplyState", "PowerState", "FrontPanelResetStatus", "ThermalState", "Status", "Name" }) { Name = "PSStatus" });
            td33.Members.Add("POWER",
                new PropertySetData(new[] { "Name", "PowerManagementCapabilities", "PowerManagementSupported", "PowerOnPasswordStatus", "PowerState", "PowerSupplyState" }) { Name = "POWER" });
            td33.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Domain", "Manufacturer", "Model", "Name", "PrimaryOwnerName", "TotalPhysicalMemory" }) { Name = "DefaultDisplayPropertySet" };
            yield return td33;

            var td34 = new TypeData(@"System.Management.ManagementObject#root\cimv2\WIN32_PROCESSOR", true);
            td34.Members.Add("PSStatus",
                new PropertySetData(new[] { "Availability", "CpuStatus", "CurrentVoltage", "DeviceID", "ErrorCleared", "ErrorDescription", "LastErrorCode", "LoadPercentage", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td34.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "AddressWidth", "DataWidth", "DeviceID", "ExtClock", "L2CacheSize", "L2CacheSpeed", "MaxClockSpeed", "PowerManagementSupported", "ProcessorType", "Revision", "SocketDesignation", "Version", "VoltageCaps" }) { Name = "PSConfiguration" });
            td34.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "DeviceID", "Manufacturer", "MaxClockSpeed", "Name", "SocketDesignation" }) { Name = "DefaultDisplayPropertySet" };
            yield return td34;

            var td35 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_ComputerSystemProduct", true);
            td35.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Version" }) { Name = "PSStatus" });
            td35.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "IdentifyingNumber", "Name", "Vendor", "Version", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td35;

            var td36 = new TypeData(@"System.Management.ManagementObject#root\cimv2\CIM_DataFile", true);
            td36.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td36.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Compressed", "Encrypted", "Size", "Hidden", "Name", "Readable", "System", "Version", "Writeable" }) { Name = "DefaultDisplayPropertySet" };
            yield return td36;

            var td37 = new TypeData(@"System.Management.ManagementObject#root\cimv2\WIN32_DCOMApplication", true);
            td37.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Status" }) { Name = "PSStatus" });
            td37.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "AppID", "InstallDate", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td37;

            var td38 = new TypeData(@"System.Management.ManagementObject#root\cimv2\WIN32_DESKTOP", true);
            td38.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "ScreenSaverActive" }) { Name = "PSStatus" });
            td38.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Name", "ScreenSaverActive", "ScreenSaverSecure", "ScreenSaverTimeout", "SettingID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td38;

            var td39 = new TypeData(@"System.Management.ManagementObject#root\cimv2\WIN32_DESKTOPMONITOR", true);
            td39.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "DeviceID", "Name", "PixelsPerXLogicalInch", "PixelsPerYLogicalInch", "ScreenHeight", "ScreenWidth" }) { Name = "PSConfiguration" });
            td39.Members.Add("PSStatus",
                new PropertySetData(new[] { "DeviceID", "IsLocked", "LastErrorCode", "Name", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td39.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceID", "DisplayType", "MonitorManufacturer", "Name", "ScreenHeight", "ScreenWidth" }) { Name = "DefaultDisplayPropertySet" };
            yield return td39;

            var td40 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_DeviceMemoryAddress", true);
            td40.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "MemoryType" }) { Name = "PSStatus" });
            td40.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "MemoryType", "Name", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td40;

            var td41 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_DiskDrive", true);
            td41.Members.Add("PSStatus",
                new PropertySetData(new[] { "ConfigManagerErrorCode", "LastErrorCode", "NeedsCleaning", "Status", "DeviceID", "StatusInfo", "Partitions" }) { Name = "PSStatus" });
            td41.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "BytesPerSector", "ConfigManagerUserConfig", "DefaultBlockSize", "DeviceID", "Index", "InstallDate", "InterfaceType", "MaxBlockSize", "MaxMediaSize", "MinBlockSize", "NumberOfMediaSupported", "Partitions", "SectorsPerTrack", "Size", "TotalCylinders", "TotalHeads", "TotalSectors", "TotalTracks", "TracksPerCylinder" }) { Name = "PSConfiguration" });
            td41.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Partitions", "DeviceID", "Model", "Size", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td41;

            var td42 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_DiskQuota", true);
            td42.Members.Add("PSStatus",
                new PropertySetData(new[] { "__PATH", "Status" }) { Name = "PSStatus" });
            td42.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DiskSpaceUsed", "Limit", "QuotaVolume", "User" }) { Name = "DefaultDisplayPropertySet" };
            yield return td42;

            var td43 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_DMAChannel", true);
            td43.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td43.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "AddressSize", "DMAChannel", "MaxTransferSize", "Name", "Port" }) { Name = "DefaultDisplayPropertySet" };
            yield return td43;

            var td44 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_Environment", true);
            td44.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "SystemVariable" }) { Name = "PSStatus" });
            td44.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "VariableValue", "Name", "UserName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td44;

            var td45 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_Directory", true);
            td45.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Compressed", "Encrypted", "Name", "Readable", "Writeable" }) { Name = "PSStatus" });
            td45.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Hidden", "Archive", "EightDotThreeFileName", "FileSize", "Name", "Compressed", "Encrypted", "Readable" }) { Name = "DefaultDisplayPropertySet" };
            yield return td45;

            var td46 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_Group", true);
            td46.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td46.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Domain", "Name", "SID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td46;

            var td47 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_IDEController", true);
            td47.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td47.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Manufacturer", "Name", "ProtocolSupported", "Status", "StatusInfo" }) { Name = "DefaultDisplayPropertySet" };
            yield return td47;

            var td48 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_IRQResource", true);
            td48.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Caption", "Availability" }) { Name = "PSStatus" });
            td48.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Hardware", "IRQNumber", "Name", "Shareable", "TriggerLevel", "TriggerType" }) { Name = "DefaultDisplayPropertySet" };
            yield return td48;

            var td49 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_ScheduledJob", true);
            td49.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "JobId", "JobStatus", "ElapsedTime", "StartTime", "Owner" }) { Name = "PSStatus" });
            td49.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "JobId", "Name", "Owner", "Priority", "Command" }) { Name = "DefaultDisplayPropertySet" };
            yield return td49;

            var td50 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_LoadOrderGroup", true);
            td50.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td50.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "GroupOrder", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td50;

            var td51 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_LogicalDisk", true);
            td51.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Availability", "DeviceID", "StatusInfo" }) { Name = "PSStatus" });
            td51.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceID", "DriveType", "ProviderName", "FreeSpace", "Size", "VolumeName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td51;

            var td52 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_LogonSession", true);
            td52.Members.Add("PSStatus",
                new PropertySetData(new[] { "__PATH", "Status" }) { Name = "PSStatus" });
            td52.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "AuthenticationPackage", "LogonId", "LogonType", "Name", "StartTime", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td52;

            var td53 = new TypeData(@"System.Management.ManagementObject#root\cimv2\WIN32_CACHEMEMORY", true);
            td53.Members.Add("ERROR",
                new PropertySetData(new[] { "DeviceID", "ErrorCorrectType" }) { Name = "ERROR" });
            td53.Members.Add("PSStatus",
                new PropertySetData(new[] { "Availability", "DeviceID", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td53.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "BlockSize", "CacheSpeed", "CacheType", "DeviceID", "InstalledSize", "Level", "MaxCacheSize", "NumberOfBlocks", "Status", "WritePolicy" }) { Name = "PSConfiguration" });
            td53.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "BlockSize", "CacheSpeed", "CacheType", "DeviceID", "InstalledSize", "Level", "MaxCacheSize", "NumberOfBlocks", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td53;

            var td54 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_LogicalMemoryConfiguration", true);
            td54.Members.Add("PSStatus",
                new PropertySetData(new[] { "AvailableVirtualMemory", "Name", "TotalVirtualMemory" }) { Name = "PSStatus" });
            td54.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Name", "TotalVirtualMemory", "TotalPhysicalMemory", "TotalPageFileSpace" }) { Name = "DefaultDisplayPropertySet" };
            yield return td54;

            var td55 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_PhysicalMemoryArray", true);
            td55.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "Replaceable", "Location" }) { Name = "PSStatus" });
            td55.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Model", "Name", "MaxCapacity", "MemoryDevices" }) { Name = "DefaultDisplayPropertySet" };
            yield return td55;

            var td56 = new TypeData(@"System.Management.ManagementObject#root\cimv2\WIN32_NetworkClient", true);
            td56.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Status" }) { Name = "PSStatus" });
            td56.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "InstallDate", "Manufacturer", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td56;

            var td57 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_NetworkLoginProfile", true);
            td57.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Privileges", "Profile", "UserId", "UserType", "Workstations" }) { Name = "DefaultDisplayPropertySet" };
            yield return td57;

            var td58 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_NetworkProtocol", true);
            td58.Members.Add("FULLXXX",
                new PropertySetData(new[] { "ConnectionlessService", "Description", "GuaranteesDelivery", "GuaranteesSequencing", "InstallDate", "MaximumAddressSize", "MaximumMessageSize", "MessageOriented", "MinimumAddressSize", "Name", "PseudoStreamOriented", "Status", "SupportsBroadcasting", "SupportsConnectData", "SupportsDisconnectData", "SupportsEncryption", "SupportsExpeditedData", "SupportsFragmentation", "SupportsGracefulClosing", "SupportsGuaranteedBandwidth", "SupportsMulticasting", "SupportsQualityofService" }) { Name = "FULLXXX" });
            td58.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Status", "SupportsBroadcasting", "SupportsConnectData", "SupportsDisconnectData", "SupportsEncryption", "SupportsExpeditedData", "SupportsFragmentation", "SupportsGracefulClosing", "SupportsGuaranteedBandwidth", "SupportsMulticasting", "SupportsQualityofService" }) { Name = "PSStatus" });
            td58.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "GuaranteesDelivery", "GuaranteesSequencing", "ConnectionlessService", "Status", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td58;

            var td59 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_NetworkConnection", true);
            td59.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "ConnectionState", "Persistent", "LocalName", "RemoteName" }) { Name = "PSStatus" });
            td59.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "LocalName", "RemoteName", "ConnectionState", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td59;

            var td60 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_NetworkAdapter", true);
            td60.Members.Add("PSStatus",
                new PropertySetData(new[] { "Availability", "Name", "Status", "StatusInfo", "DeviceID" }) { Name = "PSStatus" });
            td60.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "ServiceName", "MACAddress", "AdapterType", "DeviceID", "Name", "NetworkAddresses", "Speed" }) { Name = "DefaultDisplayPropertySet" };
            yield return td60;

            var td61 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_NetworkAdapterConfiguration", true);
            td61.Members.Add("PSStatus",
                new PropertySetData(new[] { "DHCPLeaseExpires", "Index", "Description" }) { Name = "PSStatus" });
            td61.Members.Add("DHCP",
                new PropertySetData(new[] { "Description", "DHCPEnabled", "DHCPLeaseExpires", "DHCPLeaseObtained", "DHCPServer", "Index" }) { Name = "DHCP" });
            td61.Members.Add("DNS",
                new PropertySetData(new[] { "Description", "DNSDomain", "DNSDomainSuffixSearchOrder", "DNSEnabledForWINSResolution", "DNSHostName", "DNSServerSearchOrder", "DomainDNSRegistrationEnabled", "FullDNSRegistrationEnabled", "Index" }) { Name = "DNS" });
            td61.Members.Add("IP",
                new PropertySetData(new[] { "Description", "Index", "IPAddress", "IPConnectionMetric", "IPEnabled", "IPFilterSecurityEnabled" }) { Name = "IP" });
            td61.Members.Add("WINS",
                new PropertySetData(new[] { "Description", "Index", "WINSEnableLMHostsLookup", "WINSHostLookupFile", "WINSPrimaryServer", "WINSScopeID", "WINSSecondaryServer" }) { Name = "WINS" });
            td61.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DHCPEnabled", "IPAddress", "DefaultIPGateway", "DNSDomain", "ServiceName", "Description", "Index" }) { Name = "DefaultDisplayPropertySet" };
            yield return td61;

            var td62 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_NTDomain", true);
            td62.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "DomainName" }) { Name = "PSStatus" });
            td62.Members.Add("GUID",
                new PropertySetData(new[] { "DomainName", "DomainGuid" }) { Name = "GUID" });
            td62.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "ClientSiteName", "DcSiteName", "Description", "DnsForestName", "DomainControllerAddress", "DomainControllerName", "DomainName", "Roles", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td62;

            var td63 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_NTLogEvent", true);
            td63.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Category", "CategoryString", "EventCode", "EventIdentifier", "TypeEvent", "InsertionStrings", "LogFile", "Message", "RecordNumber", "SourceName", "TimeGenerated", "TimeWritten", "Type", "UserName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td63;

            var td64 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_NTEventlogFile", true);
            td64.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "LogfileName", "Name" }) { Name = "PSStatus" });
            td64.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "FileSize", "LogfileName", "Name", "NumberOfRecords" }) { Name = "DefaultDisplayPropertySet" };
            yield return td64;

            var td65 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_OnBoardDevice", true);
            td65.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Description" }) { Name = "PSStatus" });
            td65.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceType", "SerialNumber", "Enabled", "Description" }) { Name = "DefaultDisplayPropertySet" };
            yield return td65;

            var td66 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_OperatingSystem", true);
            td66.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td66.Members.Add("FREE",
                new PropertySetData(new[] { "FreePhysicalMemory", "FreeSpaceInPagingFiles", "FreeVirtualMemory", "Name" }) { Name = "FREE" });
            td66.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "SystemDirectory", "Organization", "BuildNumber", "RegisteredUser", "SerialNumber", "Version" }) { Name = "DefaultDisplayPropertySet" };
            yield return td66;

            var td67 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_PageFileUsage", true);
            td67.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "CurrentUsage" }) { Name = "PSStatus" });
            td67.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Name", "PeakUsage" }) { Name = "DefaultDisplayPropertySet" };
            yield return td67;

            var td68 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_PageFileSetting", true);
            td68.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "MaximumSize", "Name", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td68;

            var td69 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_DiskPartition", true);
            td69.Members.Add("PSStatus",
                new PropertySetData(new[] { "Index", "Status", "StatusInfo", "Name" }) { Name = "PSStatus" });
            td69.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "NumberOfBlocks", "BootPartition", "Name", "PrimaryPartition", "Size", "Index" }) { Name = "DefaultDisplayPropertySet" };
            yield return td69;

            var td70 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_PortResource", true);
            td70.Members.Add("PSStatus",
                new PropertySetData(new[] { "NetConnectionStatus", "Status", "Name", "StartingAddress", "EndingAddress" }) { Name = "PSStatus" });
            td70.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Name", "Alias" }) { Name = "DefaultDisplayPropertySet" };
            yield return td70;

            var td71 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_PortConnector", true);
            td71.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "ExternalReferenceDesignator" }) { Name = "PSStatus" });
            td71.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Tag", "ConnectorType", "SerialNumber", "ExternalReferenceDesignator", "PortType" }) { Name = "DefaultDisplayPropertySet" };
            yield return td71;

            var td72 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_Printer", true);
            td72.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td72.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Location", "Name", "PrinterState", "PrinterStatus", "ShareName", "SystemName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td72;

            var td73 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_PrinterConfiguration", true);
            td73.Members.Add("PSStatus",
                new PropertySetData(new[] { "DriverVersion", "Name" }) { Name = "PSStatus" });
            td73.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "PrintQuality", "DriverVersion", "Name", "PaperSize", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td73;

            var td74 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_PrintJob", true);
            td74.Members.Add("PSStatus",
                new PropertySetData(new[] { "Document", "JobId", "JobStatus", "Name", "PagesPrinted", "Status", "JobIdCopy", "Name" }) { Name = "PSStatus" });
            td74.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Document", "JobId", "JobStatus", "Owner", "Priority", "Size", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td74;

            var td75 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_ProcessXXX", true);
            td75.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "ProcessId" }) { Name = "PSStatus" });
            td75.Members.Add("MEMORY",
                new PropertySetData(new[] { "Handle", "MaximumWorkingSetSize", "MinimumWorkingSetSize", "Name", "PageFaults", "PageFileUsage", "PeakPageFileUsage", "PeakVirtualSize", "PeakWorkingSetSize", "PrivatePageCount", "QuotaNonPagedPoolUsage", "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "QuotaPeakPagedPoolUsage", "VirtualSize", "WorkingSetSize" }) { Name = "MEMORY" });
            td75.Members.Add("IO",
                new PropertySetData(new[] { "Name", "ProcessId", "ReadOperationCount", "ReadTransferCount", "WriteOperationCount", "WriteTransferCount" }) { Name = "IO" });
            td75.Members.Add("STATISTICS",
                new PropertySetData(new[] { "HandleCount", "Name", "KernelModeTime", "MaximumWorkingSetSize", "MinimumWorkingSetSize", "OtherOperationCount", "OtherTransferCount", "PageFaults", "PageFileUsage", "PeakPageFileUsage", "PeakVirtualSize", "PeakWorkingSetSize", "PrivatePageCount", "ProcessId", "QuotaNonPagedPoolUsage", "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "QuotaPeakPagedPoolUsage", "ReadOperationCount", "ReadTransferCount", "ThreadCount", "UserModeTime", "VirtualSize", "WorkingSetSize", "WriteOperationCount", "WriteTransferCount" }) { Name = "STATISTICS" });
            td75.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "ThreadCount", "HandleCount", "Name", "Priority", "ProcessId", "WorkingSetSize" }) { Name = "DefaultDisplayPropertySet" };
            yield return td75;

            var td76 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_Product", true);
            td76.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Version", "InstallState" }) { Name = "PSStatus" });
            td76.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "IdentifyingNumber", "Name", "Vendor", "Version", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td76;

            var td77 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering", true);
            td77.Members.Add("InstalledOn",
                new ScriptPropertyData(@"InstalledOn", GetScriptBlock(@"if ([environment]::osversion.version.build -ge 7000)
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
          }"), null));
            td77.Members.Add("PSStatus",
                new PropertySetData(new[] { "__PATH", "Status" }) { Name = "PSStatus" });
            td77.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Description", "FixComments", "HotFixID", "InstallDate", "InstalledBy", "InstalledOn", "Name", "ServicePackInEffect", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td77;

            var td78 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_QuotaSetting", true);
            td78.Members.Add("PSStatus",
                new PropertySetData(new[] { "State", "VolumePath", "Caption" }) { Name = "PSStatus" });
            td78.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "DefaultLimit", "SettingID", "State", "VolumePath", "DefaultWarningLimit" }) { Name = "DefaultDisplayPropertySet" };
            yield return td78;

            var td79 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_OSRecoveryConfiguration", true);
            td79.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DebugFilePath", "Name", "SettingID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td79;

            var td80 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_Registry", true);
            td80.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "CurrentSize", "MaximumSize", "ProposedSize" }) { Name = "PSStatus" });
            td80.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "CurrentSize", "MaximumSize", "Name", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td80;

            var td81 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_SCSIController", true);
            td81.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "StatusInfo" }) { Name = "PSStatus" });
            td81.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DriverName", "Manufacturer", "Name", "ProtocolSupported", "Status", "StatusInfo" }) { Name = "DefaultDisplayPropertySet" };
            yield return td81;

            var td82 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_PerfRawData_PerfNet_Server", true);
            td82.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "LogonPerSec", "LogonTotal", "Name", "ServerSessions", "WorkItemShortages" }) { Name = "DefaultDisplayPropertySet" };
            yield return td82;

            var td83 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_Service", true);
            td83.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Status", "ExitCode" }) { Name = "PSStatus" });
            td83.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "DesktopInteract", "ErrorControl", "Name", "PathName", "ServiceType", "StartMode" }) { Name = "PSConfiguration" });
            td83.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "ExitCode", "Name", "ProcessId", "StartMode", "State", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td83;

            var td84 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_Share", true);
            td84.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Type", "Name" }) { Name = "PSStatus" });
            td84.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Name", "Path", "Description" }) { Name = "DefaultDisplayPropertySet" };
            yield return td84;

            var td85 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_SoftwareElement", true);
            td85.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "SoftwareElementState", "Name" }) { Name = "PSStatus" });
            td85.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Name", "Path", "SerialNumber", "SoftwareElementID", "Version" }) { Name = "DefaultDisplayPropertySet" };
            yield return td85;

            var td86 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_SoftwareFeature", true);
            td86.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "InstallState", "LastUse" }) { Name = "PSStatus" });
            td86.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "IdentifyingNumber", "ProductName", "Vendor", "Version" }) { Name = "DefaultDisplayPropertySet" };
            yield return td86;

            var td87 = new TypeData(@"System.Management.ManagementObject#root\cimv2\WIN32_SoundDevice", true);
            td87.Members.Add("PSStatus",
                new PropertySetData(new[] { "ConfigManagerUserConfig", "Name", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td87.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Manufacturer", "Name", "Status", "StatusInfo" }) { Name = "DefaultDisplayPropertySet" };
            yield return td87;

            var td88 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_StartupCommand", true);
            td88.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Command", "User", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td88;

            var td89 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_SystemAccount", true);
            td89.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "SIDType", "Name", "Domain" }) { Name = "PSStatus" });
            td89.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Domain", "Name", "SID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td89;

            var td90 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_SystemDriver", true);
            td90.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "State", "ExitCode", "Started", "ServiceSpecificExitCode" }) { Name = "PSStatus" });
            td90.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DisplayName", "Name", "State", "Status", "Started" }) { Name = "DefaultDisplayPropertySet" };
            yield return td90;

            var td91 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_SystemEnclosure", true);
            td91.Members.Add("PSStatus",
                new PropertySetData(new[] { "Tag", "Status", "Name", "SecurityStatus" }) { Name = "PSStatus" });
            td91.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Manufacturer", "Model", "LockPresent", "SerialNumber", "SMBIOSAssetTag", "SecurityStatus" }) { Name = "DefaultDisplayPropertySet" };
            yield return td91;

            var td92 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_SystemSlot", true);
            td92.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "SlotDesignation" }) { Name = "PSStatus" });
            td92.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "SlotDesignation", "Tag", "SupportsHotPlug", "Status", "Shared", "PMESignal", "MaxDataWidth" }) { Name = "DefaultDisplayPropertySet" };
            yield return td92;

            var td93 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_TapeDrive", true);
            td93.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Availability", "DeviceID", "NeedsCleaning", "StatusInfo" }) { Name = "PSStatus" });
            td93.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceID", "Id", "Manufacturer", "Name", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td93;

            var td94 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_TemperatureProbe", true);
            td94.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "CurrentReading", "DeviceID", "Name", "StatusInfo" }) { Name = "PSStatus" });
            td94.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "CurrentReading", "Name", "Description", "MinReadable", "MaxReadable", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td94;

            var td95 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_TimeZone", true);
            td95.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Bias", "SettingID", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td95;

            var td96 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_UninterruptiblePowerSupply", true);
            td96.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "DeviceID", "EstimatedChargeRemaining", "EstimatedRunTime", "Name", "StatusInfo", "TimeOnBackup" }) { Name = "PSStatus" });
            td96.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceID", "EstimatedRunTime", "Name", "TimeOnBackup", "UPSPort", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td96;

            var td97 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_UserAccount", true);
            td97.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Caption", "PasswordExpires" }) { Name = "PSStatus" });
            td97.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "AccountType", "Caption", "Domain", "SID", "FullName", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td97;

            var td98 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_VoltageProbe", true);
            td98.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "DeviceID", "Name", "NominalReading", "StatusInfo" }) { Name = "PSStatus" });
            td98.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Status", "Description", "CurrentReading", "MaxReadable", "MinReadable" }) { Name = "DefaultDisplayPropertySet" };
            yield return td98;

            var td99 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_VolumeQuotaSetting", true);
            td99.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Element", "Setting" }) { Name = "DefaultDisplayPropertySet" };
            yield return td99;

            var td100 = new TypeData(@"System.Management.ManagementObject#root\cimv2\Win32_WMISetting", true);
            td100.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "BuildVersion", "Caption", "DatabaseDirectory", "EnableEvents", "LoggingLevel", "SettingID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td100;

            var td101 = new TypeData(@"System.Management.ManagementObject", true);
            td101.Members.Add("ConvertToDateTime",
                new ScriptMethodData(@"ConvertToDateTime", GetScriptBlock(@"[System.Management.ManagementDateTimeConverter]::ToDateTime($args[0])")));
            td101.Members.Add("ConvertFromDateTime",
                new ScriptMethodData(@"ConvertFromDateTime", GetScriptBlock(@"[System.Management.ManagementDateTimeConverter]::ToDmtfDateTime($args[0])")));
            yield return td101;

            Exception exception;
            var securityDescriptorCommandsBaseType = Language.TypeResolver.ResolveType("Microsoft.PowerShell.Commands.SecurityDescriptorCommandsBase", out exception);

            var td102 = new TypeData(@"System.Security.AccessControl.ObjectSecurity", true);
            td102.Members.Add("Path", new CodePropertyData("Path", GetMethodInfo(securityDescriptorCommandsBaseType, "GetPath"), null));
            td102.Members.Add("Owner", new CodePropertyData("Owner", GetMethodInfo(securityDescriptorCommandsBaseType, "GetOwner"), null));
            td102.Members.Add("Group", new CodePropertyData("Group", GetMethodInfo(securityDescriptorCommandsBaseType, "GetGroup"), null));
            td102.Members.Add("Access", new CodePropertyData("Access", GetMethodInfo(securityDescriptorCommandsBaseType, "GetAccess"), null));
            td102.Members.Add("Sddl", new CodePropertyData("Sddl", GetMethodInfo(securityDescriptorCommandsBaseType, "GetSddl"), null));
            td102.Members.Add("AccessToString",
                new ScriptPropertyData(@"AccessToString", GetScriptBlock(@"$toString = """";
          $first = $true;
          if ( ! $this.Access ) { return """" }

          foreach($ace in $this.Access)
          {
          if($first)
          {
          $first = $false;
          }
          else
          {
          $tostring += ""`n"";
          }

          $toString += $ace.IdentityReference.ToString();
          $toString += "" "";
          $toString += $ace.AccessControlType.ToString();
          $toString += ""  "";
          if($ace -is [System.Security.AccessControl.FileSystemAccessRule])
          {
          $toString += $ace.FileSystemRights.ToString();
          }
          elseif($ace -is  [System.Security.AccessControl.RegistryAccessRule])
          {
          $toString += $ace.RegistryRights.ToString();
          }
          }

          return $toString;"), null));
            td102.Members.Add("AuditToString",
                new ScriptPropertyData(@"AuditToString", GetScriptBlock(@"$toString = """";
          $first = $true;
          if ( ! (& { Set-StrictMode -Version 1; $this.audit }) ) { return """" }

          foreach($ace in (& { Set-StrictMode -Version 1; $this.audit }))
          {
          if($first)
          {
          $first = $false;
          }
          else
          {
          $tostring += ""`n"";
          }

          $toString += $ace.IdentityReference.ToString();
          $toString += "" "";
          $toString += $ace.AuditFlags.ToString();
          $toString += ""  "";
          if($ace -is [System.Security.AccessControl.FileSystemAuditRule])
          {
          $toString += $ace.FileSystemRights.ToString();
          }
          elseif($ace -is [System.Security.AccessControl.RegistryAuditRule])
          {
          $toString += $ace.RegistryRights.ToString();
          }
          }

          return $toString;"), null));
            yield return td102;

            var td103 = new TypeData(@"Microsoft.PowerShell.Commands.HistoryInfo", true);
            td103.DefaultKeyPropertySet =
                new PropertySetData(new[] { "Id" }) { Name = "DefaultKeyPropertySet" };
            yield return td103;

            var td104 = new TypeData(@"System.Management.ManagementClass", true);
            td104.Members.Add("Name",
                new AliasPropertyData("Name", "__Class"));
            yield return td104;

            var td105 = new TypeData(@"System.Management.Automation.Runspaces.PSSession", true);
            td105.Members.Add("State",
                new ScriptPropertyData(@"State", GetScriptBlock(@"$this.Runspace.RunspaceStateInfo.State"), null));
            td105.Members.Add("IdleTimeout",
                new ScriptPropertyData(@"IdleTimeout", GetScriptBlock(@"$this.Runspace.ConnectionInfo.IdleTimeout"), null));
            td105.Members.Add("OutputBufferingMode",
                new ScriptPropertyData(@"OutputBufferingMode", GetScriptBlock(@"$this.Runspace.ConnectionInfo.OutputBufferingMode"), null));
            td105.Members.Add("DisconnectedOn",
                new ScriptPropertyData(@"DisconnectedOn", GetScriptBlock(@"$this.Runspace.DisconnectedOn"), null));
            td105.Members.Add("ExpiresOn",
                new ScriptPropertyData(@"ExpiresOn", GetScriptBlock(@"$this.Runspace.ExpiresOn"), null));
            yield return td105;

            var td106 = new TypeData(@"System.Guid", true);
            td106.Members.Add("Guid",
                new ScriptPropertyData(@"Guid", GetScriptBlock(@"$this.ToString()"), null));
            yield return td106;

            var td107 = new TypeData(@"System.Management.Automation.Signature", true);
            td107.SerializationDepth = 2;
            yield return td107;

            var td108 = new TypeData(@"System.Management.Automation.Job", true);
            td108.Members.Add("State",
                new ScriptPropertyData(@"State", GetScriptBlock(@"$this.JobStateInfo.State.ToString()"), null));
            td108.SerializationMethod = "SpecificProperties";
            td108.SerializationDepth = 2;
            td108.PropertySerializationSet  =
                new PropertySetData(new[] { "HasMoreData", "StatusMessage", "Location", "Command", "JobStateInfo", "InstanceId", "Id", "Name", "State", "ChildJobs", "PSJobTypeName", "PSBeginTime", "PSEndTime" }) { Name = "PropertySerializationSet" };
            yield return td108;

            var td109 = new TypeData(@"System.Management.Automation.JobStateInfo", true);
            td109.SerializationDepth = 1;
            yield return td109;

            var td110 = new TypeData(@"Deserialized.System.Management.Automation.JobStateInfo", true);
            td110.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td110;

            var td111 = new TypeData(@"Microsoft.PowerShell.DeserializingTypeConverter", true);
            td111.TypeConverter = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td111;

            var td112 = new TypeData(@"System.Net.Mail.MailAddress", true);
            td112.SerializationDepth = 1;
            yield return td112;

            var td113 = new TypeData(@"Deserialized.System.Net.Mail.MailAddress", true);
            td113.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td113;

            var td114 = new TypeData(@"System.Globalization.CultureInfo", true);
            td114.SerializationMethod = "SpecificProperties";
            td114.SerializationDepth = 1;
            td114.PropertySerializationSet  =
                new PropertySetData(new[] { "LCID", "Name", "DisplayName", "IetfLanguageTag", "ThreeLetterISOLanguageName", "ThreeLetterWindowsLanguageName", "TwoLetterISOLanguageName" }) { Name = "PropertySerializationSet" };
            yield return td114;

            var td115 = new TypeData(@"Deserialized.System.Globalization.CultureInfo", true);
            td115.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td115;

            var td116 = new TypeData(@"System.Management.Automation.PSCredential", true);
            td116.SerializationDepth = 1;
            yield return td116;

            var td117 = new TypeData(@"Deserialized.System.Management.Automation.PSCredential", true);
            td117.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td117;

            var td118 = new TypeData(@"System.Management.Automation.PSPrimitiveDictionary", true);
            td118.SerializationDepth = 1;
            yield return td118;

            var td119 = new TypeData(@"Deserialized.System.Management.Automation.PSPrimitiveDictionary", true);
            td119.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td119;

            var td120 = new TypeData(@"System.Management.Automation.SwitchParameter", true);
            td120.SerializationDepth = 1;
            yield return td120;

            var td121 = new TypeData(@"Deserialized.System.Management.Automation.SwitchParameter", true);
            td121.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td121;

            var td122 = new TypeData(@"System.Management.Automation.PSListModifier", true);
            td122.SerializationDepth = 2;
            yield return td122;

            var td123 = new TypeData(@"Deserialized.System.Management.Automation.PSListModifier", true);
            td123.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td123;

            var td124 = new TypeData(@"System.Security.Cryptography.X509Certificates.X509Certificate2", true);
            td124.SerializationMethod = "SpecificProperties";
            td124.SerializationDepth = 1;
            td124.PropertySerializationSet  =
                new PropertySetData(new[] { "RawData" }) { Name = "PropertySerializationSet" };
            yield return td124;

            var td125 = new TypeData(@"Deserialized.System.Security.Cryptography.X509Certificates.X509Certificate2", true);
            td125.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td125;

            var td126 = new TypeData(@"System.Security.Cryptography.X509Certificates.X500DistinguishedName", true);
            td126.SerializationMethod = "SpecificProperties";
            td126.SerializationDepth = 1;
            td126.PropertySerializationSet  =
                new PropertySetData(new[] { "RawData" }) { Name = "PropertySerializationSet" };
            yield return td126;

            var td127 = new TypeData(@"Deserialized.System.Security.Cryptography.X509Certificates.X500DistinguishedName", true);
            td127.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td127;

            var td128 = new TypeData(@"System.Security.AccessControl.RegistrySecurity", true);
            td128.SerializationDepth = 1;
            yield return td128;

            var td129 = new TypeData(@"Deserialized.System.Security.AccessControl.RegistrySecurity", true);
            td129.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td129;

            var td130 = new TypeData(@"System.Security.AccessControl.FileSystemSecurity", true);
            td130.SerializationDepth = 1;
            yield return td130;

            var td131 = new TypeData(@"Deserialized.System.Security.AccessControl.FileSystemSecurity", true);
            td131.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td131;

            var td132 = new TypeData(@"HelpInfo", true);
            td132.SerializationDepth = 1;
            yield return td132;

            var td133 = new TypeData(@"System.Management.Automation.PSTypeName", true);
            td133.SerializationMethod = "String";
            td133.StringSerializationSource = "Name";
            yield return td133;

            var td134 = new TypeData(@"System.Management.Automation.ParameterMetadata", true);
            td134.SerializationMethod = "SpecificProperties";
            td134.PropertySerializationSet  =
                new PropertySetData(new[] { "Name", "ParameterType", "Aliases", "IsDynamic", "SwitchParameter" }) { Name = "PropertySerializationSet" };
            yield return td134;

            var td135 = new TypeData(@"System.Management.Automation.CommandInfo", true);
            td135.Members.Add("Namespace",
                new AliasPropertyData("Namespace", "ModuleName") { IsHidden = true });
            td135.Members.Add("HelpUri",
                new ScriptPropertyData(@"HelpUri", GetScriptBlock(@"$oldProgressPreference = $ProgressPreference
          $ProgressPreference = 'SilentlyContinue'
          try
          {
          [Microsoft.PowerShell.Commands.GetHelpCodeMethods]::GetHelpUri($this)
          }
          catch {}
          finally
          {
          $ProgressPreference = $oldProgressPreference
          }"), null));
            yield return td135;

            var td136 = new TypeData(@"System.Management.Automation.ParameterSetMetadata", true);
            td136.Members.Add("Flags",
                new CodePropertyData("Flags", GetMethodInfo(typeof(Microsoft.PowerShell.DeserializingTypeConverter), "GetParameterSetMetadataFlags"), null) { IsHidden = true });
            td136.SerializationMethod = "SpecificProperties";
            td136.PropertySerializationSet  =
                new PropertySetData(new[] { "Position", "Flags", "HelpMessage" }) { Name = "PropertySerializationSet" };
            yield return td136;

            var td137 = new TypeData(@"Deserialized.System.Management.Automation.ParameterSetMetadata", true);
            td137.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td137;

            var td138 = new TypeData(@"Deserialized.System.Management.Automation.ExtendedTypeDefinition", true);
            td138.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td138;

            var td139 = new TypeData(@"System.Management.Automation.ExtendedTypeDefinition", true);
            td139.SerializationMethod = "SpecificProperties";
            td139.SerializationDepth = 1;
            td139.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "TypeNames", "FormatViewDefinition" }) { Name = "DefaultDisplayPropertySet" };
            // Serialize TypeName for remote machines running earlier versions of PowerShell that do not
            // expect TypeNames, it won't do the right thing when there are multiple type names, but
            // it's better than having no type names.
            td139.PropertySerializationSet =
                new PropertySetData(new[] { "TypeName", "TypeNames", "FormatViewDefinition" }) { Name = "PropertySerializationSet" };
            yield return td139;

            var td140 = new TypeData(@"Deserialized.System.Management.Automation.FormatViewDefinition", true);
            td140.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td140;

            var td141 = new TypeData(@"System.Management.Automation.FormatViewDefinition", true);
            td141.Members.Add("InstanceId",
                new CodePropertyData("InstanceId", GetMethodInfo(typeof(Microsoft.PowerShell.DeserializingTypeConverter), "GetFormatViewDefinitionInstanceId"), null) { IsHidden = true });
            td141.SerializationDepth = 1;
            yield return td141;

            var td142 = new TypeData(@"Deserialized.System.Management.Automation.PSControl", true);
            td142.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td142;

            var td143 = new TypeData(@"System.Management.Automation.PSControl", true);
            td143.SerializationDepth = 1;
            yield return td143;

            td = new TypeData(@"Deserialized.System.Management.Automation.PSControlGroupBy", true);
            td.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td;

            td = new TypeData(@"System.Management.Automation.PSControlGroupBy", true);
            td.SerializationDepth = 2;
            yield return td;

            td = new TypeData(@"Deserialized.System.Management.Automation.EntrySelectedBy", true);
            td.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td;

            td = new TypeData(@"System.Management.Automation.EntrySelectedBy", true);
            td.SerializationDepth = 1;
            yield return td;

            var td144 = new TypeData(@"Deserialized.System.Management.Automation.DisplayEntry", true);
            td144.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td144;

            var td145 = new TypeData(@"System.Management.Automation.DisplayEntry", true);
            td145.SerializationDepth = 1;
            yield return td145;

            var td146 = new TypeData(@"Deserialized.System.Management.Automation.TableControlColumnHeader", true);
            td146.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td146;

            var td147 = new TypeData(@"System.Management.Automation.TableControlColumnHeader", true);
            td147.SerializationDepth = 1;
            yield return td147;

            var td148 = new TypeData(@"Deserialized.System.Management.Automation.TableControlRow", true);
            td148.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td148;

            var td149 = new TypeData(@"System.Management.Automation.TableControlRow", true);
            td149.SerializationDepth = 1;
            yield return td149;

            var td150 = new TypeData(@"Deserialized.System.Management.Automation.TableControlColumn", true);
            td150.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td150;

            var td151 = new TypeData(@"System.Management.Automation.TableControlColumn", true);
            td151.SerializationDepth = 1;
            yield return td151;

            var td152 = new TypeData(@"Deserialized.System.Management.Automation.ListControlEntry", true);
            td152.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td152;

            var td153 = new TypeData(@"System.Management.Automation.ListControlEntry", true);
            td153.SerializationMethod = "SpecificProperties";
            td153.SerializationDepth = 1;
            td153.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Items", "EntrySelectedBy" }) { Name = "DefaultDisplayPropertySet" };
            // Serialize SelectedBy for remote machines running earlier versions of PowerShell that do not
            // expect EntrySelectedBy, it won't do the right thing when there are conditions in the EntrySelectedBy,
            // but it's better than nothing.
            td153.PropertySerializationSet =
                new PropertySetData(new[] { "Items", "SelectedBy", "EntrySelectedBy" }) { Name = "PropertySerializationSet" };
            yield return td153;

            var td154 = new TypeData(@"Deserialized.System.Management.Automation.ListControlEntryItem", true);
            td154.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td154;

            var td155 = new TypeData(@"System.Management.Automation.ListControlEntryItem", true);
            td155.SerializationDepth = 1;
            yield return td155;

            var td156 = new TypeData(@"Deserialized.System.Management.Automation.WideControlEntryItem", true);
            td156.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td156;

            var td157 = new TypeData(@"System.Management.Automation.WideControlEntryItem", true);
            td157.SerializationDepth = 1;
            yield return td157;

            td = new TypeData(@"Deserialized.System.Management.Automation.CustomControlEntry", true);
            td.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td;

            td = new TypeData(@"System.Management.Automation.CustomControlEntry", true);
            td.SerializationDepth = 1;
            yield return td;

            td = new TypeData(@"Deserialized.System.Management.Automation.CustomItemBase", true);
            td.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td;

            td = new TypeData(@"System.Management.Automation.CustomItemBase", true);
            td.SerializationDepth = 1;
            yield return td;

            var td158 = new TypeData(@"System.Web.Services.Protocols.SoapException", true);
            td158.Members.Add("PSMessageDetails",
                new ScriptPropertyData(@"PSMessageDetails", GetScriptBlock(@"$this.Detail.""#text"""), null));
            yield return td158;

            var td159 = new TypeData(@"System.Management.Automation.ErrorRecord", true);
            td159.Members.Add("PSMessageDetails",
                new ScriptPropertyData(@"PSMessageDetails", GetScriptBlock(@"& { Set-StrictMode -Version 1; $this.Exception.InnerException.PSMessageDetails }"), null));
            yield return td159;

            var td160 = new TypeData(@"Deserialized.System.Enum", true);
            td160.Members.Add("Value",
                new ScriptPropertyData(@"Value", GetScriptBlock(@"$this.ToString()"), null));
            yield return td160;

            var td161 = new TypeData(@"Microsoft.PowerShell.Commands.Internal.Format.FormatInfoData", true);
            td161.SerializationDepth = 1;
            yield return td161;

            var td162 = new TypeData(@"Deserialized.Microsoft.PowerShell.Commands.Internal.Format.FormatInfoData", true);
            td162.SerializationDepth = 1;
            yield return td162;

            var td163 = new TypeData(@"System.Management.ManagementEventArgs", true);
            td163.SerializationDepth = 2;
            yield return td163;

            var td164 = new TypeData(@"Deserialized.System.Management.ManagementEventArgs", true);
            td164.SerializationDepth = 2;
            yield return td164;

            var td165 = new TypeData(@"System.Management.Automation.CallStackFrame", true);
            td165.Members.Add("Command",
                new ScriptPropertyData(@"Command", GetScriptBlock(@"if ($null -eq $this.InvocationInfo) { return $this.FunctionName }

          $commandInfo = $this.InvocationInfo.MyCommand
          if ($null -eq $commandInfo) { return $this.InvocationInfo.InvocationName }

          if ($commandInfo.Name -ne """") { return $commandInfo.Name }

          return $this.FunctionName"), null));
            td165.Members.Add("Location",
                new ScriptPropertyData(@"Location", GetScriptBlock(@"$this.GetScriptLocation()"), null));
            td165.Members.Add("Arguments",
                new ScriptPropertyData(@"Arguments", GetScriptBlock(@"$argumentsBuilder = new-object System.Text.StringBuilder

          $null = $(
          $argumentsBuilder.Append(""{"")
          foreach ($entry in $this.InvocationInfo.BoundParameters.GetEnumerator())
          {
          if ($argumentsBuilder.Length -gt 1)
          {
          $argumentsBuilder.Append(string.Empty, string.Empty);
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
          $argumentsBuilder.Append(string.Empty, string.Empty)
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

          return $argumentsBuilder.ToString();"), null));
            yield return td165;

            var td166 = new TypeData(@"Microsoft.PowerShell.Commands.PSSessionConfigurationCommands#PSSessionConfiguration", true);
            td166.Members.Add("Permission",
                new ScriptPropertyData(@"Permission", GetScriptBlock(@"trap { continue; }

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
          }"), null));
            yield return td166;

            var td167 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PingStatus", true);
            td167.Members.Add("IPV4Address",
                new ScriptPropertyData(@"IPV4Address", GetScriptBlock(@"$iphost = [System.Net.Dns]::GetHostEntry($this.address)
          $iphost.AddressList | Where-Object { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork } | Select-Object -first 1"), null));
            td167.Members.Add("IPV6Address",
                new ScriptPropertyData(@"IPV6Address", GetScriptBlock(@"$iphost = [System.Net.Dns]::GetHostEntry($this.address)
          $iphost.AddressList | Where-Object { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetworkV6 } | Select-Object -first 1"), null));
            yield return td167;

            var td168 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Process", true);
            td168.Members.Add("ProcessName",
                new AliasPropertyData("ProcessName", "Name"));
            td168.Members.Add("Handles",
                new AliasPropertyData("Handles", "Handlecount"));
            td168.Members.Add("VM",
                new AliasPropertyData("VM", "VirtualSize"));
            td168.Members.Add("WS",
                new AliasPropertyData("WS", "WorkingSetSize"));
            td168.Members.Add("Path",
                new ScriptPropertyData(@"Path", GetScriptBlock(@"$this.ExecutablePath"), null));
            td168.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "ProcessId", "Name", "HandleCount", "WorkingSetSize", "VirtualSize" }) { Name = "DefaultDisplayPropertySet" };
            yield return td168;

            var td169 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Msft_CliAlias", true);
            td169.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "FriendlyName", "PWhere", "Target" }) { Name = "DefaultDisplayPropertySet" };
            yield return td169;

            var td170 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BaseBoard", true);
            td170.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "PoweredOn" }) { Name = "PSStatus" });
            td170.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Manufacturer", "Model", "Name", "SerialNumber", "SKU", "Product" }) { Name = "DefaultDisplayPropertySet" };
            yield return td170;

            var td171 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BIOS", true);
            td171.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "Caption", "SMBIOSPresent" }) { Name = "PSStatus" });
            td171.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "SMBIOSBIOSVersion", "Manufacturer", "Name", "SerialNumber", "Version" }) { Name = "DefaultDisplayPropertySet" };
            yield return td171;

            var td172 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_BootConfiguration", true);
            td172.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "SettingID", "ConfigurationPath" }) { Name = "PSStatus" });
            td172.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "BootDirectory", "Name", "SettingID", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td172;

            var td173 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_CDROMDrive", true);
            td173.Members.Add("PSStatus",
                new PropertySetData(new[] { "Availability", "Drive", "ErrorCleared", "MediaLoaded", "NeedsCleaning", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td173.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Drive", "Manufacturer", "VolumeName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td173;

            var td174 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystem", true);
            td174.Members.Add("PSStatus",
                new PropertySetData(new[] { "AdminPasswordStatus", "BootupState", "ChassisBootupState", "KeyboardPasswordStatus", "PowerOnPasswordStatus", "PowerSupplyState", "PowerState", "FrontPanelResetStatus", "ThermalState", "Status", "Name" }) { Name = "PSStatus" });
            td174.Members.Add("POWER",
                new PropertySetData(new[] { "Name", "PowerManagementCapabilities", "PowerManagementSupported", "PowerOnPasswordStatus", "PowerState", "PowerSupplyState" }) { Name = "POWER" });
            td174.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Domain", "Manufacturer", "Model", "Name", "PrimaryOwnerName", "TotalPhysicalMemory" }) { Name = "DefaultDisplayPropertySet" };
            yield return td174;

            var td175 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_PROCESSOR", true);
            td175.Members.Add("PSStatus",
                new PropertySetData(new[] { "Availability", "CpuStatus", "CurrentVoltage", "DeviceID", "ErrorCleared", "ErrorDescription", "LastErrorCode", "LoadPercentage", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td175.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "AddressWidth", "DataWidth", "DeviceID", "ExtClock", "L2CacheSize", "L2CacheSpeed", "MaxClockSpeed", "PowerManagementSupported", "ProcessorType", "Revision", "SocketDesignation", "Version", "VoltageCaps" }) { Name = "PSConfiguration" });
            td175.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "DeviceID", "Manufacturer", "MaxClockSpeed", "Name", "SocketDesignation" }) { Name = "DefaultDisplayPropertySet" };
            yield return td175;

            var td176 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ComputerSystemProduct", true);
            td176.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Version" }) { Name = "PSStatus" });
            td176.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "IdentifyingNumber", "Name", "Vendor", "Version", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td176;

            var td177 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/CIM_DataFile", true);
            td177.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td177.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Compressed", "Encrypted", "Size", "Hidden", "Name", "Readable", "System", "Version", "Writeable" }) { Name = "DefaultDisplayPropertySet" };
            yield return td177;

            var td178 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DCOMApplication", true);
            td178.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Status" }) { Name = "PSStatus" });
            td178.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "AppID", "InstallDate", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td178;

            var td179 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOP", true);
            td179.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "ScreenSaverActive" }) { Name = "PSStatus" });
            td179.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Name", "ScreenSaverActive", "ScreenSaverSecure", "ScreenSaverTimeout", "SettingID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td179;

            var td180 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_DESKTOPMONITOR", true);
            td180.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "DeviceID", "Name", "PixelsPerXLogicalInch", "PixelsPerYLogicalInch", "ScreenHeight", "ScreenWidth" }) { Name = "PSConfiguration" });
            td180.Members.Add("PSStatus",
                new PropertySetData(new[] { "DeviceID", "IsLocked", "LastErrorCode", "Name", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td180.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceID", "DisplayType", "MonitorManufacturer", "Name", "ScreenHeight", "ScreenWidth" }) { Name = "DefaultDisplayPropertySet" };
            yield return td180;

            var td181 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DeviceMemoryAddress", true);
            td181.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "MemoryType" }) { Name = "PSStatus" });
            td181.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "MemoryType", "Name", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td181;

            var td182 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskDrive", true);
            td182.Members.Add("PSStatus",
                new PropertySetData(new[] { "ConfigManagerErrorCode", "LastErrorCode", "NeedsCleaning", "Status", "DeviceID", "StatusInfo", "Partitions" }) { Name = "PSStatus" });
            td182.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "BytesPerSector", "ConfigManagerUserConfig", "DefaultBlockSize", "DeviceID", "Index", "InstallDate", "InterfaceType", "MaxBlockSize", "MaxMediaSize", "MinBlockSize", "NumberOfMediaSupported", "Partitions", "SectorsPerTrack", "Size", "TotalCylinders", "TotalHeads", "TotalSectors", "TotalTracks", "TracksPerCylinder" }) { Name = "PSConfiguration" });
            td182.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Partitions", "DeviceID", "Model", "Size", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td182;

            var td183 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskQuota", true);
            td183.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DiskSpaceUsed", "Limit", "QuotaVolume", "User" }) { Name = "DefaultDisplayPropertySet" };
            yield return td183;

            var td184 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DMAChannel", true);
            td184.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td184.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "AddressSize", "DMAChannel", "MaxTransferSize", "Name", "Port" }) { Name = "DefaultDisplayPropertySet" };
            yield return td184;

            var td185 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Environment", true);
            td185.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "SystemVariable" }) { Name = "PSStatus" });
            td185.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "VariableValue", "Name", "UserName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td185;

            var td186 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Directory", true);
            td186.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Compressed", "Encrypted", "Name", "Readable", "Writeable" }) { Name = "PSStatus" });
            td186.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Hidden", "Archive", "EightDotThreeFileName", "FileSize", "Name", "Compressed", "Encrypted", "Readable" }) { Name = "DefaultDisplayPropertySet" };
            yield return td186;

            var td187 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Group", true);
            td187.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td187.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Domain", "Name", "SID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td187;

            var td188 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IDEController", true);
            td188.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td188.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Manufacturer", "Name", "ProtocolSupported", "Status", "StatusInfo" }) { Name = "DefaultDisplayPropertySet" };
            yield return td188;

            var td189 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_IRQResource", true);
            td189.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Caption", "Availability" }) { Name = "PSStatus" });
            td189.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Hardware", "IRQNumber", "Name", "Shareable", "TriggerLevel", "TriggerType" }) { Name = "DefaultDisplayPropertySet" };
            yield return td189;

            var td190 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ScheduledJob", true);
            td190.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "JobId", "JobStatus", "ElapsedTime", "StartTime", "Owner" }) { Name = "PSStatus" });
            td190.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "JobId", "Name", "Owner", "Priority", "Command" }) { Name = "DefaultDisplayPropertySet" };
            yield return td190;

            var td191 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LoadOrderGroup", true);
            td191.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td191.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "GroupOrder", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td191;

            var td192 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalDisk", true);
            td192.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Availability", "DeviceID", "StatusInfo" }) { Name = "PSStatus" });
            td192.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceID", "DriveType", "ProviderName", "FreeSpace", "Size", "VolumeName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td192;

            var td193 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogonSession", true);
            td193.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "AuthenticationPackage", "LogonId", "LogonType", "Name", "StartTime", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td193;

            var td194 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_CACHEMEMORY", true);
            td194.Members.Add("ERROR",
                new PropertySetData(new[] { "DeviceID", "ErrorCorrectType" }) { Name = "ERROR" });
            td194.Members.Add("PSStatus",
                new PropertySetData(new[] { "Availability", "DeviceID", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td194.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "BlockSize", "CacheSpeed", "CacheType", "DeviceID", "InstalledSize", "Level", "MaxCacheSize", "NumberOfBlocks", "Status", "WritePolicy" }) { Name = "PSConfiguration" });
            td194.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "BlockSize", "CacheSpeed", "CacheType", "DeviceID", "InstalledSize", "Level", "MaxCacheSize", "NumberOfBlocks", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td194;

            var td195 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalMemoryConfiguration", true);
            td195.Members.Add("PSStatus",
                new PropertySetData(new[] { "AvailableVirtualMemory", "Name", "TotalVirtualMemory" }) { Name = "PSStatus" });
            td195.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Name", "TotalVirtualMemory", "TotalPhysicalMemory", "TotalPageFileSpace" }) { Name = "DefaultDisplayPropertySet" };
            yield return td195;

            var td196 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PhysicalMemoryArray", true);
            td196.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "Replaceable", "Location" }) { Name = "PSStatus" });
            td196.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Model", "Name", "MaxCapacity", "MemoryDevices" }) { Name = "DefaultDisplayPropertySet" };
            yield return td196;

            var td197 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_NetworkClient", true);
            td197.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Status" }) { Name = "PSStatus" });
            td197.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "InstallDate", "Manufacturer", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td197;

            var td198 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkLoginProfile", true);
            td198.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Privileges", "Profile", "UserId", "UserType", "Workstations" }) { Name = "DefaultDisplayPropertySet" };
            yield return td198;

            var td199 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkProtocol", true);
            td199.Members.Add("FULLXXX",
                new PropertySetData(new[] { "ConnectionlessService", "Description", "GuaranteesDelivery", "GuaranteesSequencing", "InstallDate", "MaximumAddressSize", "MaximumMessageSize", "MessageOriented", "MinimumAddressSize", "Name", "PseudoStreamOriented", "Status", "SupportsBroadcasting", "SupportsConnectData", "SupportsDisconnectData", "SupportsEncryption", "SupportsExpeditedData", "SupportsFragmentation", "SupportsGracefulClosing", "SupportsGuaranteedBandwidth", "SupportsMulticasting", "SupportsQualityofService" }) { Name = "FULLXXX" });
            td199.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Status", "SupportsBroadcasting", "SupportsConnectData", "SupportsDisconnectData", "SupportsEncryption", "SupportsExpeditedData", "SupportsFragmentation", "SupportsGracefulClosing", "SupportsGuaranteedBandwidth", "SupportsMulticasting", "SupportsQualityofService" }) { Name = "PSStatus" });
            td199.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "GuaranteesDelivery", "GuaranteesSequencing", "ConnectionlessService", "Status", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td199;

            var td200 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkConnection", true);
            td200.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "ConnectionState", "Persistent", "LocalName", "RemoteName" }) { Name = "PSStatus" });
            td200.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "LocalName", "RemoteName", "ConnectionState", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td200;

            var td201 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapter", true);
            td201.Members.Add("PSStatus",
                new PropertySetData(new[] { "Availability", "Name", "Status", "StatusInfo", "DeviceID" }) { Name = "PSStatus" });
            td201.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "ServiceName", "MACAddress", "AdapterType", "DeviceID", "Name", "NetworkAddresses", "Speed" }) { Name = "DefaultDisplayPropertySet" };
            yield return td201;

            var td202 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NetworkAdapterConfiguration", true);
            td202.Members.Add("PSStatus",
                new PropertySetData(new[] { "DHCPLeaseExpires", "Index", "Description" }) { Name = "PSStatus" });
            td202.Members.Add("DHCP",
                new PropertySetData(new[] { "Description", "DHCPEnabled", "DHCPLeaseExpires", "DHCPLeaseObtained", "DHCPServer", "Index" }) { Name = "DHCP" });
            td202.Members.Add("DNS",
                new PropertySetData(new[] { "Description", "DNSDomain", "DNSDomainSuffixSearchOrder", "DNSEnabledForWINSResolution", "DNSHostName", "DNSServerSearchOrder", "DomainDNSRegistrationEnabled", "FullDNSRegistrationEnabled", "Index" }) { Name = "DNS" });
            td202.Members.Add("IP",
                new PropertySetData(new[] { "Description", "Index", "IPAddress", "IPConnectionMetric", "IPEnabled", "IPFilterSecurityEnabled" }) { Name = "IP" });
            td202.Members.Add("WINS",
                new PropertySetData(new[] { "Description", "Index", "WINSEnableLMHostsLookup", "WINSHostLookupFile", "WINSPrimaryServer", "WINSScopeID", "WINSSecondaryServer" }) { Name = "WINS" });
            td202.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DHCPEnabled", "IPAddress", "DefaultIPGateway", "DNSDomain", "ServiceName", "Description", "Index" }) { Name = "DefaultDisplayPropertySet" };
            yield return td202;

            var td203 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTDomain", true);
            td203.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "DomainName" }) { Name = "PSStatus" });
            td203.Members.Add("GUID",
                new PropertySetData(new[] { "DomainName", "DomainGuid" }) { Name = "GUID" });
            td203.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "ClientSiteName", "DcSiteName", "Description", "DnsForestName", "DomainControllerAddress", "DomainControllerName", "DomainName", "Roles", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td203;

            var td204 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTLogEvent", true);
            td204.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Category", "CategoryString", "EventCode", "EventIdentifier", "TypeEvent", "InsertionStrings", "LogFile", "Message", "RecordNumber", "SourceName", "TimeGenerated", "TimeWritten", "Type", "UserName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td204;

            var td205 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_NTEventlogFile", true);
            td205.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "LogfileName", "Name" }) { Name = "PSStatus" });
            td205.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "FileSize", "LogfileName", "Name", "NumberOfRecords" }) { Name = "DefaultDisplayPropertySet" };
            yield return td205;

            var td206 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OnBoardDevice", true);
            td206.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Description" }) { Name = "PSStatus" });
            td206.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceType", "SerialNumber", "Enabled", "Description" }) { Name = "DefaultDisplayPropertySet" };
            yield return td206;

            var td207 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OperatingSystem", true);
            td207.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td207.Members.Add("FREE",
                new PropertySetData(new[] { "FreePhysicalMemory", "FreeSpaceInPagingFiles", "FreeVirtualMemory", "Name" }) { Name = "FREE" });
            td207.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "SystemDirectory", "Organization", "BuildNumber", "RegisteredUser", "SerialNumber", "Version" }) { Name = "DefaultDisplayPropertySet" };
            yield return td207;

            var td208 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PageFileUsage", true);
            td208.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "CurrentUsage" }) { Name = "PSStatus" });
            td208.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Name", "PeakUsage" }) { Name = "DefaultDisplayPropertySet" };
            yield return td208;

            var td209 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PageFileSetting", true);
            td209.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "MaximumSize", "Name", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td209;

            var td210 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_DiskPartition", true);
            td210.Members.Add("PSStatus",
                new PropertySetData(new[] { "Index", "Status", "StatusInfo", "Name" }) { Name = "PSStatus" });
            td210.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "NumberOfBlocks", "BootPartition", "Name", "PrimaryPartition", "Size", "Index" }) { Name = "DefaultDisplayPropertySet" };
            yield return td210;

            var td211 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortResource", true);
            td211.Members.Add("PSStatus",
                new PropertySetData(new[] { "NetConnectionStatus", "Status", "Name", "StartingAddress", "EndingAddress" }) { Name = "PSStatus" });
            td211.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Name", "Alias" }) { Name = "DefaultDisplayPropertySet" };
            yield return td211;

            var td212 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PortConnector", true);
            td212.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "ExternalReferenceDesignator" }) { Name = "PSStatus" });
            td212.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Tag", "ConnectorType", "SerialNumber", "ExternalReferenceDesignator", "PortType" }) { Name = "DefaultDisplayPropertySet" };
            yield return td212;

            var td213 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Printer", true);
            td213.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name" }) { Name = "PSStatus" });
            td213.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Location", "Name", "PrinterState", "PrinterStatus", "ShareName", "SystemName" }) { Name = "DefaultDisplayPropertySet" };
            yield return td213;

            var td214 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrinterConfiguration", true);
            td214.Members.Add("PSStatus",
                new PropertySetData(new[] { "DriverVersion", "Name" }) { Name = "PSStatus" });
            td214.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "PrintQuality", "DriverVersion", "Name", "PaperSize", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td214;

            var td215 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PrintJob", true);
            td215.Members.Add("PSStatus",
                new PropertySetData(new[] { "Document", "JobId", "JobStatus", "Name", "PagesPrinted", "Status", "JobIdCopy", "Name" }) { Name = "PSStatus" });
            td215.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Document", "JobId", "JobStatus", "Owner", "Priority", "Size", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td215;

            var td216 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_ProcessXXX", true);
            td216.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "ProcessId" }) { Name = "PSStatus" });
            td216.Members.Add("MEMORY",
                new PropertySetData(new[] { "Handle", "MaximumWorkingSetSize", "MinimumWorkingSetSize", "Name", "PageFaults", "PageFileUsage", "PeakPageFileUsage", "PeakVirtualSize", "PeakWorkingSetSize", "PrivatePageCount", "QuotaNonPagedPoolUsage", "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "QuotaPeakPagedPoolUsage", "VirtualSize", "WorkingSetSize" }) { Name = "MEMORY" });
            td216.Members.Add("IO",
                new PropertySetData(new[] { "Name", "ProcessId", "ReadOperationCount", "ReadTransferCount", "WriteOperationCount", "WriteTransferCount" }) { Name = "IO" });
            td216.Members.Add("STATISTICS",
                new PropertySetData(new[] { "HandleCount", "Name", "KernelModeTime", "MaximumWorkingSetSize", "MinimumWorkingSetSize", "OtherOperationCount", "OtherTransferCount", "PageFaults", "PageFileUsage", "PeakPageFileUsage", "PeakVirtualSize", "PeakWorkingSetSize", "PrivatePageCount", "ProcessId", "QuotaNonPagedPoolUsage", "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage", "QuotaPeakPagedPoolUsage", "ReadOperationCount", "ReadTransferCount", "ThreadCount", "UserModeTime", "VirtualSize", "WorkingSetSize", "WriteOperationCount", "WriteTransferCount" }) { Name = "STATISTICS" });
            td216.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "ThreadCount", "HandleCount", "Name", "Priority", "ProcessId", "WorkingSetSize" }) { Name = "DefaultDisplayPropertySet" };
            yield return td216;

            var td217 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Product", true);
            td217.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Version", "InstallState" }) { Name = "PSStatus" });
            td217.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "IdentifyingNumber", "Name", "Vendor", "Version", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td217;

            var td218 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuickFixEngineering", true);
            td218.Members.Add("InstalledOn",
                new ScriptPropertyData(@"InstalledOn", GetScriptBlock(@"if ([environment]::osversion.version.build -ge 7000)
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
          }"), null));
            td218.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Description", "FixComments", "HotFixID", "InstallDate", "InstalledBy", "InstalledOn", "Name", "ServicePackInEffect", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td218;

            var td219 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_QuotaSetting", true);
            td219.Members.Add("PSStatus",
                new PropertySetData(new[] { "State", "VolumePath", "Caption" }) { Name = "PSStatus" });
            td219.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "DefaultLimit", "SettingID", "State", "VolumePath", "DefaultWarningLimit" }) { Name = "DefaultDisplayPropertySet" };
            yield return td219;

            var td220 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OSRecoveryConfiguration", true);
            td220.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DebugFilePath", "Name", "SettingID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td220;

            var td221 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Registry", true);
            td221.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "CurrentSize", "MaximumSize", "ProposedSize" }) { Name = "PSStatus" });
            td221.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "CurrentSize", "MaximumSize", "Name", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td221;

            var td222 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SCSIController", true);
            td222.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "StatusInfo" }) { Name = "PSStatus" });
            td222.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DriverName", "Manufacturer", "Name", "ProtocolSupported", "Status", "StatusInfo" }) { Name = "DefaultDisplayPropertySet" };
            yield return td222;

            var td223 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_PerfRawData_PerfNet_Server", true);
            td223.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "LogonPerSec", "LogonTotal", "Name", "ServerSessions", "WorkItemShortages" }) { Name = "DefaultDisplayPropertySet" };
            yield return td223;

            var td224 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Service", true);
            td224.Members.Add("PSStatus",
                new PropertySetData(new[] { "Name", "Status", "ExitCode" }) { Name = "PSStatus" });
            td224.Members.Add("PSConfiguration",
                new PropertySetData(new[] { "DesktopInteract", "ErrorControl", "Name", "PathName", "ServiceType", "StartMode" }) { Name = "PSConfiguration" });
            td224.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "ExitCode", "Name", "ProcessId", "StartMode", "State", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td224;

            var td225 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_Share", true);
            td225.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Type", "Name" }) { Name = "PSStatus" });
            td225.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Name", "Path", "Description" }) { Name = "DefaultDisplayPropertySet" };
            yield return td225;

            var td226 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SoftwareElement", true);
            td226.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "SoftwareElementState", "Name" }) { Name = "PSStatus" });
            td226.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Name", "Path", "SerialNumber", "SoftwareElementID", "Version" }) { Name = "DefaultDisplayPropertySet" };
            yield return td226;

            var td227 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SoftwareFeature", true);
            td227.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "InstallState", "LastUse" }) { Name = "PSStatus" });
            td227.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "IdentifyingNumber", "ProductName", "Vendor", "Version" }) { Name = "DefaultDisplayPropertySet" };
            yield return td227;

            var td228 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/WIN32_SoundDevice", true);
            td228.Members.Add("PSStatus",
                new PropertySetData(new[] { "ConfigManagerUserConfig", "Name", "Status", "StatusInfo" }) { Name = "PSStatus" });
            td228.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Manufacturer", "Name", "Status", "StatusInfo" }) { Name = "DefaultDisplayPropertySet" };
            yield return td228;

            var td229 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_StartupCommand", true);
            td229.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Command", "User", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td229;

            var td230 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemAccount", true);
            td230.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "SIDType", "Name", "Domain" }) { Name = "PSStatus" });
            td230.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Caption", "Domain", "Name", "SID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td230;

            var td231 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemDriver", true);
            td231.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Name", "State", "ExitCode", "Started", "ServiceSpecificExitCode" }) { Name = "PSStatus" });
            td231.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DisplayName", "Name", "State", "Status", "Started" }) { Name = "DefaultDisplayPropertySet" };
            yield return td231;

            var td232 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemEnclosure", true);
            td232.Members.Add("PSStatus",
                new PropertySetData(new[] { "Tag", "Status", "Name", "SecurityStatus" }) { Name = "PSStatus" });
            td232.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Manufacturer", "Model", "LockPresent", "SerialNumber", "SMBIOSAssetTag", "SecurityStatus" }) { Name = "DefaultDisplayPropertySet" };
            yield return td232;

            var td233 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_SystemSlot", true);
            td233.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "SlotDesignation" }) { Name = "PSStatus" });
            td233.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "SlotDesignation", "Tag", "SupportsHotPlug", "Status", "Shared", "PMESignal", "MaxDataWidth" }) { Name = "DefaultDisplayPropertySet" };
            yield return td233;

            var td234 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TapeDrive", true);
            td234.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Availability", "DeviceID", "NeedsCleaning", "StatusInfo" }) { Name = "PSStatus" });
            td234.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceID", "Id", "Manufacturer", "Name", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td234;

            var td235 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TemperatureProbe", true);
            td235.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "CurrentReading", "DeviceID", "Name", "StatusInfo" }) { Name = "PSStatus" });
            td235.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "CurrentReading", "Name", "Description", "MinReadable", "MaxReadable", "Status" }) { Name = "DefaultDisplayPropertySet" };
            yield return td235;

            var td236 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_TimeZone", true);
            td236.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Bias", "SettingID", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td236;

            var td237 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UninterruptiblePowerSupply", true);
            td237.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "DeviceID", "EstimatedChargeRemaining", "EstimatedRunTime", "Name", "StatusInfo", "TimeOnBackup" }) { Name = "PSStatus" });
            td237.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "DeviceID", "EstimatedRunTime", "Name", "TimeOnBackup", "UPSPort", "Caption" }) { Name = "DefaultDisplayPropertySet" };
            yield return td237;

            var td238 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_UserAccount", true);
            td238.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "Caption", "PasswordExpires" }) { Name = "PSStatus" });
            td238.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "AccountType", "Caption", "Domain", "SID", "FullName", "Name" }) { Name = "DefaultDisplayPropertySet" };
            yield return td238;

            var td239 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_VoltageProbe", true);
            td239.Members.Add("PSStatus",
                new PropertySetData(new[] { "Status", "DeviceID", "Name", "NominalReading", "StatusInfo" }) { Name = "PSStatus" });
            td239.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Status", "Description", "CurrentReading", "MaxReadable", "MinReadable" }) { Name = "DefaultDisplayPropertySet" };
            yield return td239;

            var td240 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_VolumeQuotaSetting", true);
            td240.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "Element", "Setting" }) { Name = "DefaultDisplayPropertySet" };
            yield return td240;

            var td241 = new TypeData(@"Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_WMISetting", true);
            td241.DefaultDisplayPropertySet =
                new PropertySetData(new[] { "BuildVersion", "Caption", "DatabaseDirectory", "EnableEvents", "LoggingLevel", "SettingID" }) { Name = "DefaultDisplayPropertySet" };
            yield return td241;

            var td242 = new TypeData(@"Microsoft.Management.Infrastructure.CimClass", true);
            td242.Members.Add("CimClassName",
                new ScriptPropertyData(@"CimClassName", GetScriptBlock(@"[OutputType([string])]
          param()
          $this.PSBase.CimSystemProperties.ClassName"), null));
            yield return td242;

            var td243 = new TypeData(@"Microsoft.Management.Infrastructure.CimCmdlets.CimIndicationEventInstanceEventArgs", true);
            td243.SerializationDepth = 1;
            yield return td243;

            var td244 = new TypeData(@"System.Management.Automation.Breakpoint", true);
            td244.SerializationDepth = 1;
            yield return td244;

            var td245 = new TypeData(@"Deserialized.System.Management.Automation.Breakpoint", true);
            td245.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td245;

            var td246 = new TypeData(@"System.Management.Automation.BreakpointUpdatedEventArgs", true);
            td246.SerializationDepth = 2;
            yield return td246;

            var td247 = new TypeData(@"Deserialized.System.Management.Automation.BreakpointUpdatedEventArgs", true);
            td247.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td247;

            var td248 = new TypeData(@"System.Management.Automation.DebuggerCommand", true);
            td248.SerializationDepth = 1;
            yield return td248;

            var td249 = new TypeData(@"Deserialized.System.Management.Automation.DebuggerCommand", true);
            td249.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td249;

            var td250 = new TypeData(@"System.Management.Automation.DebuggerCommandResults", true);
            td250.SerializationDepth = 1;
            yield return td250;

            var td251 = new TypeData(@"Deserialized.System.Management.Automation.DebuggerCommandResults", true);
            td251.TargetTypeForDeserialization = typeof(Microsoft.PowerShell.DeserializingTypeConverter);
            yield return td251;

            var td252 = new TypeData(@"System.Version#IncludeLabel", true);
            td252.Members.Add("ToString",
                new ScriptMethodData(@"ToString", GetScriptBlock(@"
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
            ")));
            yield return td252;
        }
    }
}
