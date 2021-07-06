// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Management.Infrastructure;
using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    using Extensions;

    #region GetComputerInfoCommand cmdlet implementation
    /// <summary>
    /// The Get-ComputerInfo cmdlet gathers and reports information
    /// about a computer.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ComputerInfo",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkId=2096810")]
    [Alias("gin")]
    [OutputType(typeof(ComputerInfo), typeof(PSObject))]
    public class GetComputerInfoCommand : PSCmdlet
    {
        #region Inner Types
        private sealed class OSInfoGroup
        {
            public WmiOperatingSystem os;
            public HotFix[] hotFixes;
            public WmiPageFileUsage[] pageFileUsage;
            public string halVersion;
            public TimeSpan? upTime;
            public RegWinNtCurrentVersion regCurVer;
        }

        private sealed class SystemInfoGroup
        {
            public WmiBaseBoard baseboard;
            public WmiBios bios;
            public WmiComputerSystem computer;
            public Processor[] processors;
            public NetworkAdapter[] networkAdapters;
        }

        private sealed class HyperVInfo
        {
            public bool? Present;
            public bool? VMMonitorModeExtensions;
            public bool? SecondLevelAddressTranslation;
            public bool? VirtualizationFirmwareEnabled;
            public bool? DataExecutionPreventionAvailable;
        }

        private sealed class DeviceGuardInfo
        {
            public DeviceGuardSmartStatus status;
            public DeviceGuard deviceGuard;
        }

        private sealed class MiscInfoGroup
        {
            public ulong? physicallyInstalledMemory;
            public string timeZone;
            public string logonServer;
            public FirmwareType? firmwareType;
            public PowerPlatformRole? powerPlatformRole;
            public WmiKeyboard[] keyboards;
            public HyperVInfo hyperV;
            public ServerLevel? serverLevel;
            public DeviceGuardInfo deviceGuard;
        }
        #endregion Inner Types

        #region Static Data and Constants
        private const string activity = "Get-ComputerInfo";
        private const string localMachineName = null;
        #endregion Static Data and Constants

        #region Instance Data
        private readonly string _machineName = localMachineName;  // we might need to have cmdlet work on another machine

        /// <summary>
        /// Collection of property names from the Property parameter,
        /// including any names resulting from the expansion of wild-card
        /// patterns given. This list will itself contain no wildcard patterns.
        /// </summary>
        private List<string> _namedProperties = null;
        #endregion Instance Data

        #region Parameters
        /// <summary>
        /// The Property parameter contains the names of properties to be retrieved.
        /// If this parameter is given, the cmdlet returns a PSCustomObject
        /// containing only the requested properties.
        /// Wild-card patterns may be provided.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Any named properties that are not recognized are ignored. If no
        /// recognized properties are provided the cmdlet returns an empty
        /// PSCustomObject.
        /// </para>
        /// <para>
        /// If a provided wild-card pattern contains only an asterisk ("*"),
        /// the cmdlet will operate as if the parameter were not given at all
        /// and will return a fully-populated ComputerInfo object.
        /// </para>
        /// </remarks>
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Property { get; set; }
        #endregion Parameters

        #region Cmdlet Overrides
        /// <summary>
        /// Perform any first-stage processing.
        /// </summary>
        protected override void BeginProcessing()
        {
            // if the Property parameter was given, determine the requested
            // property names
            if (Property != null && Property.Length > 0)
            {
                try
                {
                    _namedProperties = CollectPropertyNames(Property);
                }
                catch (WildcardPatternException ex)
                {
                    WriteError(new ErrorRecord(ex, "WildcardPattern", ErrorCategory.InvalidArgument, this));
                }
            }
        }

        /// <summary>
        /// Performs the cmdlet's work.
        /// </summary>
        protected override void ProcessRecord()
        {
            // if the user provided property names but no matching properties
            // were found, return an empty custom object
            if (_namedProperties != null && _namedProperties.Count == 0)
            {
                WriteObject(new PSObject());
                return;
            }

            MiscInfoGroup miscInfo = null;
            var osInfo = new OSInfoGroup();
            var systemInfo = new SystemInfoGroup();
            var now = DateTime.Now;

            using (var session = CimSession.Create(_machineName))
            {
                UpdateProgress(ComputerInfoResources.LoadingOperationSystemInfo);

                osInfo.os = session.GetFirst<WmiOperatingSystem>(CIMHelper.ClassNames.OperatingSystem);
                osInfo.pageFileUsage = session.GetAll<WmiPageFileUsage>(CIMHelper.ClassNames.PageFileUsage);

                if (osInfo.os != null)
                {
                    osInfo.halVersion = GetHalVersion(session, osInfo.os.SystemDirectory);

                    if (osInfo.os.LastBootUpTime != null)
                        osInfo.upTime = now - osInfo.os.LastBootUpTime.Value;
                }

                UpdateProgress(ComputerInfoResources.LoadingHotPatchInfo);
                osInfo.hotFixes = session.GetAll<HotFix>(CIMHelper.ClassNames.HotFix);

                UpdateProgress(ComputerInfoResources.LoadingRegistryInfo);
                osInfo.regCurVer = RegistryInfo.GetWinNtCurrentVersion();

                UpdateProgress(ComputerInfoResources.LoadingBiosInfo);
                systemInfo.bios = session.GetFirst<WmiBios>(CIMHelper.ClassNames.Bios);

                UpdateProgress(ComputerInfoResources.LoadingMotherboardInfo);
                systemInfo.baseboard = session.GetFirst<WmiBaseBoard>(CIMHelper.ClassNames.BaseBoard);

                UpdateProgress(ComputerInfoResources.LoadingComputerInfo);
                systemInfo.computer = session.GetFirst<WmiComputerSystem>(CIMHelper.ClassNames.ComputerSystem);
                miscInfo = GetOtherInfo(session);

                UpdateProgress(ComputerInfoResources.LoadingProcessorInfo);
                systemInfo.processors = GetProcessors(session);

                UpdateProgress(ComputerInfoResources.LoadingNetworkAdapterInfo);
                systemInfo.networkAdapters = GetNetworkAdapters(session);

                UpdateProgress(null);   // close the progress bar
            }

            var infoOutput = CreateFullOutputObject(systemInfo, osInfo, miscInfo);

            if (_namedProperties != null)
            {
                // var output = CreateCustomOutputObject(namedProperties, systemInfo, osInfo, miscInfo);
                var output = CreateCustomOutputObject(infoOutput, _namedProperties);

                WriteObject(output);
            }
            else
            {
                WriteObject(infoOutput);
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        /// <summary>
        /// Display progress.
        /// </summary>
        /// <param name="status">
        /// Text to be displayed in status bar
        /// </param>
        private void UpdateProgress(string status)
        {
            ProgressRecord progress = new(0, activity, status ?? ComputerResources.ProgressStatusCompleted);
            progress.RecordType = status == null ? ProgressRecordType.Completed : ProgressRecordType.Processing;

            WriteProgress(progress);
        }

        /// <summary>
        /// Retrieves the version of the system's hal.dll.
        /// </summary>
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object
        /// representing the CIM session to query.
        /// </param>
        /// <param name="systemDirectory">
        /// Path to the system directory, which should contain the hal.dll file.
        /// </param>
        private static string GetHalVersion(CimSession session, string systemDirectory)
        {
            string halVersion = null;

            try
            {
                var halPath = CIMHelper.EscapePath(System.IO.Path.Combine(systemDirectory, "hal.dll"));
                var query = string.Format("SELECT * FROM CIM_DataFile Where Name='{0}'", halPath);
                var instance = session.QueryFirstInstance(query);

                if (instance != null)
                    halVersion = instance.CimInstanceProperties["Version"].Value.ToString();
            }
            catch (Exception)
            {
                // On any error, fall through to the return
            }

            return halVersion;
        }

        /// <summary>
        /// Create an array of <see cref="NetworkAdapter"/> object from values in
        /// Win32_NetworkAdapter and Win32_NetworkAdapterConfiguration instances.
        /// </summary>
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object representing
        /// a CIM session.
        /// </param>
        /// <returns>
        /// An array of NetworkAdapter objects.
        /// </returns>
        /// <remarks>
        /// This method matches network adapters associated network adapter configurations.
        /// The returned array contains entries only for matched adapter/configuration objects.
        /// </remarks>
        private static NetworkAdapter[] GetNetworkAdapters(CimSession session)
        {
            var adaptersMsft = session.GetAll<WmiMsftNetAdapter>(CIMHelper.MicrosoftNetworkAdapterNamespace, CIMHelper.ClassNames.MicrosoftNetworkAdapter);
            var adapters = session.GetAll<WmiNetworkAdapter>(CIMHelper.ClassNames.NetworkAdapter);
            var configs = session.GetAll<WmiNetworkAdapterConfiguration>(CIMHelper.ClassNames.NetworkAdapterConfiguration);

            var list = new List<NetworkAdapter>();

            if (adapters != null && configs != null)
            {
                var configDict = new Dictionary<uint, WmiNetworkAdapterConfiguration>();

                foreach (var config in configs)
                {
                    if (config.Index != null)
                        configDict[config.Index.Value] = config;
                }

                if (configDict.Count > 0)
                {
                    foreach (var adapter in adapters)
                    {
                        // Only include adapters that have a non-null connection status
                        // and a non-null index
                        if (adapter.NetConnectionStatus != null
                            && adapter.Index != null)
                        {
                            if (configDict.ContainsKey(adapter.Index.Value))
                            {
                                var config = configDict[adapter.Index.Value];
                                var nwAdapter = new NetworkAdapter
                                {
                                    Description = adapter.Description,
                                    ConnectionID = adapter.NetConnectionID
                                };

                                var status = EnumConverter<NetConnectionStatus>.Convert(adapter.NetConnectionStatus);
                                nwAdapter.ConnectionStatus = status == null ? NetConnectionStatus.Other
                                                                            : status.Value;

                                if (nwAdapter.ConnectionStatus == NetConnectionStatus.Connected)
                                {
                                    nwAdapter.DHCPEnabled = config.DHCPEnabled;
                                    nwAdapter.DHCPServer = config.DHCPServer;
                                    nwAdapter.IPAddresses = config.IPAddress;
                                }

                                list.Add(nwAdapter);
                            }
                        }
                    }
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// Create an array of <see cref="Processor"/> objects, using data acquired
        /// from WMI via the Win32_Processor class.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private static Processor[] GetProcessors(CimSession session)
        {
            var processors = session.GetAll<WmiProcessor>(CIMHelper.ClassNames.Processor);

            if (processors != null)
            {
                var list = new List<Processor>();

                foreach (var processor in processors)
                {
                    var proc = new Processor();

                    proc.AddressWidth = processor.AddressWidth;
                    proc.Architecture = EnumConverter<CpuArchitecture>.Convert(processor.Architecture);
                    proc.Availability = EnumConverter<CpuAvailability>.Convert(processor.Availability);
                    proc.CpuStatus = EnumConverter<CpuStatus>.Convert(processor.CpuStatus);
                    proc.CurrentClockSpeed = processor.CurrentClockSpeed;
                    proc.DataWidth = processor.DataWidth;
                    proc.Description = processor.Description;
                    proc.Manufacturer = processor.Manufacturer;
                    proc.MaxClockSpeed = processor.MaxClockSpeed;
                    proc.Name = processor.Name;
                    proc.NumberOfCores = processor.NumberOfCores;
                    proc.NumberOfLogicalProcessors = processor.NumberOfLogicalProcessors;
                    proc.ProcessorID = processor.ProcessorId;
                    proc.ProcessorType = EnumConverter<ProcessorType>.Convert(processor.ProcessorType);
                    proc.Role = processor.Role;
                    proc.SocketDesignation = processor.SocketDesignation;
                    proc.Status = processor.Status;

                    list.Add(proc);
                }

                return list.ToArray();
            }

            return null;
        }

        private static bool CheckDeviceGuardLicense()
        {
            const string propertyName = "CodeIntegrity-AllowConfigurablePolicy";

            // DeviceGuard is supported on all versions of PowerShell that execute on "full" SKUs
            if (Platform.IsWindows &&
                !(Platform.IsNanoServer || Platform.IsIoT))
            {
                try
                {
                    int policy = 0;

                    if (Native.SLGetWindowsInformationDWORD(propertyName, out policy) == Native.S_OK
                        && policy == 1)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // if we fail to load the native dll or if the call fails
                    // catastrophically there's not much we can do except to
                    // consider there to be no license.
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieve information related to Device Guard.
        /// </summary>
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object representing
        /// a CIM session.
        /// </param>
        /// <returns>
        /// A <see cref="DeviceGuard"/> object containing information related to
        /// the Device Guard feature
        /// </returns>
        private static DeviceGuardInfo GetDeviceGuard(CimSession session)
        {
            DeviceGuard guard = null;
            var status = DeviceGuardSmartStatus.Off;

            if (CheckDeviceGuardLicense())
            {
                var wmiGuard = session.GetFirst<WmiDeviceGuard>(CIMHelper.DeviceGuardNamespace,
                                                                CIMHelper.ClassNames.DeviceGuard);

                if (wmiGuard != null)
                {
                    var smartStatus = EnumConverter<DeviceGuardSmartStatus>.Convert((int?)wmiGuard.VirtualizationBasedSecurityStatus ?? 0);
                    if (smartStatus != null)
                    {
                        status = (DeviceGuardSmartStatus)smartStatus;
                    }

                    guard = wmiGuard.AsOutputType;
                }
            }

            return new DeviceGuardInfo
            {
                status = status,
                deviceGuard = guard
            };
        }

        /// <summary>
        /// A helper method used by GetHyperVisorInfo to retrieve a boolean
        /// property value.
        /// </summary>
        private static bool? GetBooleanProperty(CimInstance instance, string propertyName)
        {
            if (instance != null)
            {
                try
                {
                    var property = instance.CimInstanceProperties[propertyName];

                    if (property != null && property.Value != null)
                        return (bool)property.Value;
                }
                catch (Exception)
                {
                    // just in case the cast fails
                    // fall through to the null return
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieve information related to HyperVisor.
        /// </summary>
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object representing
        /// a CIM session.
        /// </param>
        /// <returns>
        /// A <see cref="HyperVInfo"/> object containing information related to
        /// HyperVisor
        /// </returns>
        private static HyperVInfo GetHyperVisorInfo(CimSession session)
        {
            HyperVInfo info = new();
            bool ok = false;
            CimInstance instance = null;

            using (instance = session.QueryFirstInstance(CIMHelper.WqlQueryAll(CIMHelper.ClassNames.ComputerSystem)))
            {
                if (instance != null)
                {
                    info.Present = GetBooleanProperty(instance, "HypervisorPresent");
                    ok = true;
                }
            }

            // don't bother checking requirements if the HyperV in present
            // when the HyperV is present, the requirements values are misleading
            if (ok && info.Present != null && info.Present.Value)
                return info;

            using (instance = session.QueryFirstInstance(CIMHelper.WqlQueryAll(CIMHelper.ClassNames.OperatingSystem)))
            {
                if (instance != null)
                {
                    info.DataExecutionPreventionAvailable = GetBooleanProperty(instance, "DataExecutionPrevention_Available");
                    ok = true;
                }
            }

            using (instance = session.QueryFirstInstance(CIMHelper.WqlQueryAll(CIMHelper.ClassNames.Processor)))
            {
                if (instance != null)
                {
                    info.SecondLevelAddressTranslation = GetBooleanProperty(instance, "SecondLevelAddressTranslationExtensions");
                    info.VirtualizationFirmwareEnabled = GetBooleanProperty(instance, "VirtualizationFirmwareEnabled");
                    info.VMMonitorModeExtensions = GetBooleanProperty(instance, "VMMonitorModeExtensions");
                    ok = true;
                }
            }

            return ok ? info : null;
        }

        /// <summary>
        /// Retrieve miscellaneous system information.
        /// </summary>
        /// <param name="session">
        /// A <see cref="Microsoft.Management.Infrastructure.CimSession"/> object representing
        /// a CIM session.
        /// </param>
        /// <returns>
        /// A <see cref="MiscInfoGroup"/> object containing miscellaneous
        /// system information
        /// </returns>
        private static MiscInfoGroup GetOtherInfo(CimSession session)
        {
            var rv = new MiscInfoGroup();

            // get platform role
            try
            {
                // TODO: Local machine only. Check for that?
                uint powerRole = Native.PowerDeterminePlatformRoleEx(Native.POWER_PLATFORM_ROLE_V2);
                if (powerRole >= (uint)PowerPlatformRole.MaximumEnumValue)
                    rv.powerPlatformRole = PowerPlatformRole.Unspecified;
                else
                    rv.powerPlatformRole = EnumConverter<PowerPlatformRole>.Convert((int)powerRole);
            }
            catch (Exception)
            {
                // probably failed to load the DLL with PowerDeterminePlatformRoleEx
                // either way, move on
            }

            // get secure-boot info
            // TODO: Local machine only? Check for that?
            rv.firmwareType = GetFirmwareType();

            // get amount of memory physically installed
            // TODO: Local machine only. Check for that?
            rv.physicallyInstalledMemory = GetPhysicallyInstalledSystemMemory();

            // get time zone
            // we'll use .Net's TimeZoneInfo for now. systeminfo uses Caption from Win32_TimeZone
            var tzi = TimeZoneInfo.Local;
            if (tzi != null)
                rv.timeZone = tzi.DisplayName;

            rv.logonServer = RegistryInfo.GetLogonServer();

            rv.keyboards = session.GetAll<WmiKeyboard>(CIMHelper.ClassNames.Keyboard);

            rv.hyperV = GetHyperVisorInfo(session);

            var serverLevels = RegistryInfo.GetServerLevels();
            uint value;

            if (serverLevels.TryGetValue("NanoServer", out value) && value == 1)
            {
                rv.serverLevel = ServerLevel.NanoServer;
            }
            else if (serverLevels.TryGetValue("ServerCore", out value) && value == 1)
            {
                rv.serverLevel = ServerLevel.ServerCore;

                if (serverLevels.TryGetValue("Server-Gui-Mgmt", out value) && value == 1)
                {
                    rv.serverLevel = ServerLevel.ServerCoreWithManagementTools;

                    if (serverLevels.TryGetValue("Server-Gui-Shell", out value) && value == 1)
                        rv.serverLevel = ServerLevel.FullServer;
                }
            }

            rv.deviceGuard = GetDeviceGuard(session);

            return rv;
        }

        /// <summary>
        /// Wrapper around the native GetFirmwareType function.
        /// </summary>
        /// <returns>
        /// null if unsuccessful, otherwise FirmwareType enum specifying
        /// the firmware type.
        /// </returns>
        private static FirmwareType? GetFirmwareType()
        {
            try
            {
                FirmwareType firmwareType;

                if (Native.GetFirmwareType(out firmwareType))
                    return firmwareType;
            }
            catch (Exception)
            {
                // Probably failed to load the DLL or to file the function entry point.
                // Fail silently
            }

            return null;
        }

        /// <summary>
        /// Wrapper around the native GetPhysicallyInstalledSystemMemory function.
        /// </summary>
        /// <returns>
        /// null if unsuccessful, otherwise the amount of physically installed memory.
        /// </returns>
        private static ulong? GetPhysicallyInstalledSystemMemory()
        {
            try
            {
                ulong memory;
                if (Native.GetPhysicallyInstalledSystemMemory(out memory))
                    return memory;
            }
            catch (Exception)
            {
                // Probably failed to load the DLL or to file the function entry point.
                // Fail silently
            }

            return null;
        }

        /// <summary>
        /// Create a new ComputerInfo object populated with the specified data objects.
        /// </summary>
        /// <param name="systemInfo">
        /// A <see cref="SystemInfoGroup"/> object containing system-related info
        /// such as BIOS, mother-board, computer system, etc.
        /// </param>
        /// <param name="osInfo">
        /// An <see cref="OSInfoGroup"/> object containing operating-system information.
        /// </param>
        /// <param name="otherInfo">
        /// A <see cref="MiscInfoGroup"/> object containing other information to be reported.
        /// </param>
        /// <returns>
        /// A new ComputerInfo object to be output to PowerShell.
        /// </returns>
        private static ComputerInfo CreateFullOutputObject(SystemInfoGroup systemInfo, OSInfoGroup osInfo, MiscInfoGroup otherInfo)
        {
            var output = new ComputerInfo();

            var regCurVer = osInfo.regCurVer;
            if (regCurVer != null)
            {
                output.WindowsBuildLabEx = regCurVer.BuildLabEx;
                output.WindowsCurrentVersion = regCurVer.CurrentVersion;
                output.WindowsEditionId = regCurVer.EditionId;
                output.WindowsInstallationType = regCurVer.InstallationType;
                output.WindowsInstallDateFromRegistry = regCurVer.InstallDate;
                output.WindowsProductId = regCurVer.ProductId;
                output.WindowsProductName = regCurVer.ProductName;
                output.WindowsRegisteredOrganization = regCurVer.RegisteredOrganization;
                output.WindowsRegisteredOwner = regCurVer.RegisteredOwner;
                output.WindowsSystemRoot = regCurVer.SystemRoot;
                output.WindowsVersion = regCurVer.ReleaseId;
                output.WindowsUBR = regCurVer.UBR;
            }

            var os = osInfo.os;
            if (os != null)
            {
                output.OsName = os.Caption;
                output.OsBootDevice = os.BootDevice;
                output.OsBuildNumber = os.BuildNumber;
                output.OsBuildType = os.BuildType;
                output.OsCodeSet = os.CodeSet;
                output.OsCountryCode = os.CountryCode;
                output.OsCSDVersion = os.CSDVersion;
                output.OsCurrentTimeZone = os.CurrentTimeZone;
                output.OsDataExecutionPreventionAvailable = os.DataExecutionPrevention_Available;
                output.OsDataExecutionPrevention32BitApplications = os.DataExecutionPrevention_32BitApplications;
                output.OsDataExecutionPreventionDrivers = os.DataExecutionPrevention_Drivers;
                output.OsDataExecutionPreventionSupportPolicy =
                    EnumConverter<DataExecutionPreventionSupportPolicy>.Convert(os.DataExecutionPrevention_SupportPolicy);
                output.OsDebug = os.Debug;

                output.OsDistributed = os.Distributed;
                output.OsEncryptionLevel = EnumConverter<OSEncryptionLevel>.Convert((int?)os.EncryptionLevel);
                output.OsForegroundApplicationBoost = EnumConverter<ForegroundApplicationBoost>.Convert(os.ForegroundApplicationBoost);
                output.OsTotalSwapSpaceSize = os.TotalSwapSpaceSize;
                output.OsTotalVisibleMemorySize = os.TotalVisibleMemorySize;
                output.OsFreePhysicalMemory = os.FreePhysicalMemory;
                output.OsFreeSpaceInPagingFiles = os.FreeSpaceInPagingFiles;
                output.OsTotalVirtualMemorySize = os.TotalVirtualMemorySize;
                output.OsFreeVirtualMemory = os.FreeVirtualMemory;
                if (os.TotalVirtualMemorySize != null && os.FreeVirtualMemory != null)
                    output.OsInUseVirtualMemory = os.TotalVirtualMemorySize - os.FreeVirtualMemory;
                output.OsInstallDate = os.InstallDate;
                output.OsLastBootUpTime = os.LastBootUpTime;
                output.OsLocalDateTime = os.LocalDateTime;
                output.OsLocaleID = os.Locale;
                output.OsManufacturer = os.Manufacturer;
                output.OsMaxNumberOfProcesses = os.MaxNumberOfProcesses;
                output.OsMaxProcessMemorySize = os.MaxProcessMemorySize;
                output.OsMuiLanguages = os.MUILanguages;
                output.OsNumberOfLicensedUsers = os.NumberOfLicensedUsers;
                output.OsNumberOfProcesses = os.NumberOfProcesses;
                output.OsNumberOfUsers = os.NumberOfUsers;
                output.OsOperatingSystemSKU = EnumConverter<OperatingSystemSKU>.Convert((int?)os.OperatingSystemSKU);
                output.OsOrganization = os.Organization;
                output.OsArchitecture = os.OSArchitecture;
                output.OsLanguage = os.LanguageName;
                output.OsProductSuites = os.ProductSuites;
                output.OsOtherTypeDescription = os.OtherTypeDescription;
                output.OsPAEEnabled = os.PAEEnabled;
                output.OsPortableOperatingSystem = os.PortableOperatingSystem;
                output.OsPrimary = os.Primary;
                output.OsProductType = EnumConverter<ProductType>.Convert((int?)os.ProductType);
                output.OsRegisteredUser = os.RegisteredUser;
                output.OsSerialNumber = os.SerialNumber;
                output.OsServicePackMajorVersion = os.ServicePackMajorVersion;
                output.OsServicePackMinorVersion = os.ServicePackMinorVersion;
                output.OsSizeStoredInPagingFiles = os.SizeStoredInPagingFiles;
                output.OsStatus = os.Status;
                output.OsSuites = os.Suites;
                output.OsSystemDevice = os.SystemDevice;
                output.OsSystemDirectory = os.SystemDirectory;
                output.OsSystemDrive = os.SystemDrive;
                output.OsType = EnumConverter<OSType>.Convert(os.OSType);
                output.OsVersion = os.Version;
                output.OsWindowsDirectory = os.WindowsDirectory;

                output.OsHardwareAbstractionLayer = osInfo.halVersion;
                output.OsLocale = os.GetLocale();
                output.OsUptime = osInfo.upTime;
                output.OsHotFixes = osInfo.hotFixes;

                var pageFileUsage = osInfo.pageFileUsage;
                if (pageFileUsage != null)
                {
                    output.OsPagingFiles = new string[pageFileUsage.Length];

                    for (int i = 0; i < pageFileUsage.Length; i++)
                        output.OsPagingFiles[i] = pageFileUsage[i].Caption;
                }
            }

            var bios = systemInfo.bios;
            if (bios != null)
            {
                output.BiosCharacteristics = bios.BiosCharacteristics;
                output.BiosBuildNumber = bios.BuildNumber;
                output.BiosBIOSVersion = bios.BIOSVersion;
                output.BiosCaption = bios.Caption;
                output.BiosCodeSet = bios.CodeSet;
                output.BiosCurrentLanguage = bios.CurrentLanguage;
                output.BiosDescription = bios.Description;
                output.BiosEmbeddedControllerMajorVersion = bios.EmbeddedControllerMajorVersion;
                output.BiosEmbeddedControllerMinorVersion = bios.EmbeddedControllerMinorVersion;
                output.BiosIdentificationCode = bios.IdentificationCode;
                output.BiosInstallableLanguages = bios.InstallableLanguages;
                output.BiosInstallDate = bios.InstallDate;
                output.BiosLanguageEdition = bios.LanguageEdition;
                output.BiosListOfLanguages = bios.ListOfLanguages;
                output.BiosManufacturer = bios.Manufacturer;
                output.BiosName = bios.Name;
                output.BiosOtherTargetOS = bios.OtherTargetOS;
                output.BiosPrimaryBIOS = bios.PrimaryBIOS;
                output.BiosReleaseDate = bios.ReleaseDate;
                output.BiosSerialNumber = bios.SerialNumber;
                output.BiosSMBIOSBIOSVersion = bios.SMBIOSBIOSVersion;
                output.BiosSMBIOSMajorVersion = bios.SMBIOSMajorVersion;
                output.BiosSMBIOSMinorVersion = bios.SMBIOSMinorVersion;
                output.BiosSMBIOSPresent = bios.SMBIOSPresent;
                output.BiosSoftwareElementState = EnumConverter<SoftwareElementState>.Convert(bios.SoftwareElementState);
                output.BiosStatus = bios.Status;
                output.BiosSystemBiosMajorVersion = bios.SystemBiosMajorVersion;
                output.BiosSystemBiosMinorVersion = bios.SystemBiosMinorVersion;
                output.BiosTargetOperatingSystem = bios.TargetOperatingSystem;
                output.BiosVersion = bios.Version;

                if (otherInfo != null)
                    output.BiosFirmwareType = otherInfo.firmwareType;
            }

            var computer = systemInfo.computer;
            if (computer != null)
            {
                output.CsAdminPasswordStatus = EnumConverter<HardwareSecurity>.Convert(computer.AdminPasswordStatus);
                output.CsAutomaticManagedPagefile = computer.AutomaticManagedPagefile;
                output.CsAutomaticResetBootOption = computer.AutomaticResetBootOption;
                output.CsAutomaticResetCapability = computer.AutomaticResetCapability;
                output.CsBootOptionOnLimit = EnumConverter<BootOptionAction>.Convert(computer.BootOptionOnLimit);
                output.CsBootOptionOnWatchDog = EnumConverter<BootOptionAction>.Convert(computer.BootOptionOnWatchDog);
                output.CsBootROMSupported = computer.BootROMSupported;
                output.CsBootStatus = computer.BootStatus;
                output.CsBootupState = computer.BootupState;
                output.CsCaption = computer.Caption;
                output.CsChassisBootupState = EnumConverter<SystemElementState>.Convert(computer.ChassisBootupState);
                output.CsChassisSKUNumber = computer.ChassisSKUNumber;
                output.CsCurrentTimeZone = computer.CurrentTimeZone;
                output.CsDaylightInEffect = computer.DaylightInEffect;
                output.CsDescription = computer.Description;
                output.CsDNSHostName = computer.DNSHostName;
                output.CsDomain = computer.Domain;
                output.CsDomainRole = EnumConverter<DomainRole>.Convert(computer.DomainRole);
                output.CsEnableDaylightSavingsTime = computer.EnableDaylightSavingsTime;
                output.CsFrontPanelResetStatus = EnumConverter<HardwareSecurity>.Convert(computer.FrontPanelResetStatus);
                output.CsHypervisorPresent = computer.HypervisorPresent;
                output.CsInfraredSupported = computer.InfraredSupported;
                output.CsInitialLoadInfo = computer.InitialLoadInfo;
                output.CsInstallDate = computer.InstallDate;
                output.CsKeyboardPasswordStatus = EnumConverter<HardwareSecurity>.Convert(computer.KeyboardPasswordStatus);
                output.CsLastLoadInfo = computer.LastLoadInfo;
                output.CsManufacturer = computer.Manufacturer;
                output.CsModel = computer.Model;
                output.CsName = computer.Name;
                output.CsNetworkAdapters = systemInfo.networkAdapters;
                output.CsNetworkServerModeEnabled = computer.NetworkServerModeEnabled;
                output.CsNumberOfLogicalProcessors = computer.NumberOfLogicalProcessors;
                output.CsNumberOfProcessors = computer.NumberOfProcessors;
                output.CsProcessors = systemInfo.processors;
                output.CsOEMStringArray = computer.OEMStringArray;
                output.CsPartOfDomain = computer.PartOfDomain;
                output.CsPauseAfterReset = computer.PauseAfterReset;
                output.CsPCSystemType = EnumConverter<PCSystemType>.Convert(computer.PCSystemType);
                output.CsPCSystemTypeEx = EnumConverter<PCSystemTypeEx>.Convert(computer.PCSystemTypeEx);
                output.CsPowerManagementCapabilities = computer.GetPowerManagementCapabilities();
                output.CsPowerManagementSupported = computer.PowerManagementSupported;
                output.CsPowerOnPasswordStatus = EnumConverter<HardwareSecurity>.Convert(computer.PowerOnPasswordStatus);
                output.CsPowerState = EnumConverter<PowerState>.Convert(computer.PowerState);
                output.CsPowerSupplyState = EnumConverter<SystemElementState>.Convert(computer.PowerSupplyState);
                output.CsPrimaryOwnerContact = computer.PrimaryOwnerContact;
                output.CsPrimaryOwnerName = computer.PrimaryOwnerName;
                output.CsResetCapability = EnumConverter<ResetCapability>.Convert(computer.ResetCapability);
                output.CsResetCount = computer.ResetCount;
                output.CsResetLimit = computer.ResetLimit;
                output.CsRoles = computer.Roles;
                output.CsStatus = computer.Status;
                output.CsSupportContactDescription = computer.SupportContactDescription;
                output.CsSystemFamily = computer.SystemFamily;
                output.CsSystemSKUNumber = computer.SystemSKUNumber;
                output.CsSystemType = computer.SystemType;
                output.CsThermalState = EnumConverter<SystemElementState>.Convert(computer.ThermalState);
                output.CsTotalPhysicalMemory = computer.TotalPhysicalMemory;
                output.CsUserName = computer.UserName;
                output.CsWakeUpType = EnumConverter<WakeUpType>.Convert(computer.WakeUpType);
                output.CsWorkgroup = computer.Workgroup;

                if (otherInfo != null)
                {
                    output.CsPhysicallyInstalledMemory = otherInfo.physicallyInstalledMemory;
                }
            }

            if (otherInfo != null)
            {
                output.TimeZone = otherInfo.timeZone;
                output.LogonServer = otherInfo.logonServer;
                output.PowerPlatformRole = otherInfo.powerPlatformRole;

                if (otherInfo.keyboards.Length > 0)
                {
                    // TODO: handle multiple keyboards?
                    // there might be several keyboards found. For the moment
                    // we display info for only one

                    string layout = otherInfo.keyboards[0].Layout;

                    output.KeyboardLayout = Conversion.GetLocaleName(layout);
                }

                if (otherInfo.hyperV != null)
                {
                    output.HyperVisorPresent = otherInfo.hyperV.Present;
                    output.HyperVRequirementDataExecutionPreventionAvailable = otherInfo.hyperV.DataExecutionPreventionAvailable;
                    output.HyperVRequirementSecondLevelAddressTranslation = otherInfo.hyperV.SecondLevelAddressTranslation;
                    output.HyperVRequirementVirtualizationFirmwareEnabled = otherInfo.hyperV.VirtualizationFirmwareEnabled;
                    output.HyperVRequirementVMMonitorModeExtensions = otherInfo.hyperV.VMMonitorModeExtensions;
                }

                output.OsServerLevel = otherInfo.serverLevel;

                var deviceGuardInfo = otherInfo.deviceGuard;
                if (deviceGuardInfo != null)
                {
                    output.DeviceGuardSmartStatus = deviceGuardInfo.status;

                    var deviceGuard = deviceGuardInfo.deviceGuard;
                    if (deviceGuard != null)
                    {
                        output.DeviceGuardRequiredSecurityProperties = deviceGuard.RequiredSecurityProperties;
                        output.DeviceGuardAvailableSecurityProperties = deviceGuard.AvailableSecurityProperties;
                        output.DeviceGuardSecurityServicesConfigured = deviceGuard.SecurityServicesConfigured;
                        output.DeviceGuardSecurityServicesRunning = deviceGuard.SecurityServicesRunning;
                        output.DeviceGuardCodeIntegrityPolicyEnforcementStatus = deviceGuard.CodeIntegrityPolicyEnforcementStatus;
                        output.DeviceGuardUserModeCodeIntegrityPolicyEnforcementStatus = deviceGuard.UserModeCodeIntegrityPolicyEnforcementStatus;
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Create a new PSObject, containing only those properties named in the
        /// namedProperties parameter.
        /// </summary>
        /// <param name="info">
        /// A <see cref="ComputerInfo"/> containing all the acquired system information
        /// </param>
        /// <param name="namedProperties">
        /// A list of property names to be included in the returned object
        /// </param>
        /// <returns>
        /// A new PSObject with the properties specified in the <paramref name="namedProperties"/>
        /// parameter
        /// </returns>
        private static PSObject CreateCustomOutputObject(ComputerInfo info, List<string> namedProperties)
        {
            var rv = new PSObject();

            if (info != null && namedProperties != null && namedProperties.Count > 0)
            {
                // Walk the list of named properties, find a matching property in the
                // info object, and create a new property on the results object
                // with the associated value.
                var type = info.GetType();

                foreach (var propertyName in namedProperties)
                {
                    var propInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

                    if (propInfo != null)
                    {
                        object value = propInfo.GetValue(info);
                        rv.Properties.Add(new PSNoteProperty(propertyName, value));
                    }
                }
            }

            return rv;
        }

        /// <summary>
        /// Get the names of all <see cref="ComputerInfo"/> properties. This is
        /// part of the processes of validating property names provided by the user.
        /// </summary>
        /// <returns></returns>
        private static List<string> GetComputerInfoPropertyNames()
        {
            var rv = new List<string>();
            var type = typeof(ComputerInfo);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                rv.Add(prop.Name);

            return rv;
        }

        /// <summary>
        /// Expand any wild-card patterns into known property names.
        /// </summary>
        /// <param name="propertyNames">
        /// List of known property names
        /// </param>
        /// <param name="pattern">
        /// The wild-card pattern used to perform globbing
        /// </param>
        /// <returns></returns>
        private static List<string> ExpandWildcardPropertyNames(List<string> propertyNames, string pattern)
        {
            var rv = new List<string>();

            var wcp = new WildcardPattern(pattern, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);

            foreach (var name in propertyNames)
                if (wcp.IsMatch(name))
                    rv.Add(name);

            return rv;
        }

        /// <summary>
        /// Produce a list of known, valid property names from property-name
        /// parameters. These parameter may use wild-card patterns and may
        /// contain invalid property names. This method expands wild-card
        /// patterns and filter out any invalid property names.
        /// </summary>
        /// <param name="requestedProperties"></param>
        /// <returns>
        /// </returns>
        private static List<string> CollectPropertyNames(string[] requestedProperties)
        {
            // A quick scan through the requested properties to make sure
            // we want to use user-specified properties
            foreach (var name in requestedProperties)
            {
                if (WildcardPattern.ContainsWildcardCharacters(name))
                {
                    if (name == "*")
                        return null;    // we treat a wild-card pattern of "*" as if no properties were named
                }
            }

            var availableProperties = GetComputerInfoPropertyNames();
            var rv = new List<string>();

            // walk though the requested properties again, expanding and collecting property names
            foreach (var name in requestedProperties)
            {
                if (WildcardPattern.ContainsWildcardCharacters(name))
                {
                    foreach (var matchedName in ExpandWildcardPropertyNames(availableProperties, name))
                        if (!rv.Contains(matchedName))
                            rv.Add(matchedName);
                }
                else
                {
                    // find a matching property name via case-insensitive string comparison
                    Predicate<string> pred = (s) =>
                                                {
                                                    return string.Equals(s,
                                                                          name,
                                                                          StringComparison.OrdinalIgnoreCase);
                                                };
                    var propertyName = availableProperties.Find(pred);

                    // add the properly-cased name, if found, to the list
                    if (propertyName != null && !rv.Contains(propertyName))
                        rv.Add(propertyName);
                }
            }

            return rv;
        }
        #endregion Private Methods
    }
    #endregion GetComputerInfoCommand cmdlet implementation

    #region Helper classes
    internal static class Conversion
    {
        /// <summary>
        /// Attempt to convert a string representation of a base-16 value
        /// into an integer.
        /// </summary>
        /// <param name="hexString">
        /// A string containing the text to be parsed.
        /// </param>
        /// <param name="value">
        /// An integer into which the parsed value is stored. If the string
        /// cannot be converted, this parameter is set to 0.
        /// </param>
        /// <returns>
        /// Returns true if the conversion was successful, false otherwise.
        /// </returns>
        /// <remarks>
        /// The hexString parameter must contain a hexadecimal value, with no
        /// base-indication prefix. For example, the string "0409" will be
        /// parsed into the base-10 integer value 1033, while the string "0x0409"
        /// will fail to parse due to the "0x" base-indication prefix.
        /// </remarks>
        internal static bool TryParseHex(string hexString, out uint value)
        {
            try
            {
                value = Convert.ToUInt32(hexString, 16);
                return true;
            }
            catch (Exception)
            {
                value = 0;
                return false;
            }
        }

        /// <summary>
        /// Attempt to create a <see cref="System.Globalization.CultureInfo"/>
        /// object from a locale string as retrieved from WMI.
        /// </summary>
        /// <param name="locale">
        /// A string containing WMI's notion (usually) of a locale.
        /// </param>
        /// <returns>
        /// A CultureInfo object if successful, null otherwise.
        /// </returns>
        /// <remarks>
        /// This method first tries to convert the string to a hex value
        /// and get the CultureInfo object from that value.
        /// Failing that it attempts to retrieve the CultureInfo object
        /// using the locale string as passed.
        /// </remarks>
        internal static string GetLocaleName(string locale)
        {
            CultureInfo culture = null;

            if (locale != null)
            {
                try
                {
                    // The "locale" must contain a hexadecimal value, with no
                    // base-indication prefix. For example, the string "0409" will be
                    // parsed into the base-10 integer value 1033, while the string "0x0409"
                    // will fail to parse due to the "0x" base-indication prefix.
                    if (UInt32.TryParse(locale, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint localeNum))
                    {
                        culture = CultureInfo.GetCultureInfo((int)localeNum);
                    }

                    if (culture == null)
                    {
                        // If TryParse failed we'll try using the original string as culture name
                        culture = CultureInfo.GetCultureInfo(locale);
                    }
                }
                catch (Exception)
                {
                    culture = null;
                }
            }

            return culture?.Name;
        }

        /// <summary>
        /// Convert a Unix time, expressed in seconds, to a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="seconds">Number of seconds since the Unix epoch.</param>
        /// <returns>
        /// A DateTime object representing the date and time represented by the
        /// <paramref name="seconds"/> parameter.
        /// </returns>
        internal static DateTime UnixSecondsToDateTime(long seconds)
        {
#if false   // requires .NET 4.6 or higher
            return DateTimeOffset.FromUnixTimeSeconds(seconds).DateTime;
#else
            const int DaysPerYear = 365;
            const int DaysPer4Years = DaysPerYear * 4 + 1;
            const int DaysPer100Years = DaysPer4Years * 25 - 1;
            const int DaysPer400Years = DaysPer100Years * 4 + 1;
            const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear;
            const long UnixEpochTicks = TimeSpan.TicksPerDay * DaysTo1970;

            long ticks = seconds * TimeSpan.TicksPerSecond + UnixEpochTicks;

            return new DateTimeOffset(ticks, TimeSpan.Zero).DateTime;
#endif
        }
    }

    /// <summary>
    /// The EnumConverter<typeparamref name="T"/> class contains a method
    /// for converting an integer to a nullable enum of the type specified
    /// in T.
    /// </summary>
    /// <typeparam name="T">
    /// The type of enum to be the destination of the conversion.
    /// </typeparam>
    internal static class EnumConverter<T> where T : struct, IConvertible
    {
        // The converter object
        private static readonly Func<int, T?> s_convert = MakeConverter();

        /// <summary>
        /// Convert an integer to a Nullable enum of type T.
        /// </summary>
        /// <param name="value">
        /// The integer value to be converted to the specified enum type.
        /// </param>
        /// <returns>
        /// A Nullable<typeparamref name="T"/> enum object. If the value
        /// is convertable to a valid enum value, the returned object's
        /// value will contain the converted value, otherwise the returned
        /// object will be null.
        /// </returns>
        internal static T? Convert(int? value)
        {
            try
            {
                if (value.HasValue)
                    return s_convert(value.Value);
            }
            catch (Exception)
            {
                // nothing should go wrong, but just in case
                // fall through to the return null below
            }

            return (T?)null;
        }

        /// <summary>
        /// Create a converter using Linq Expression classes.
        /// </summary>
        /// <returns>
        /// A generic Func{} object to convert an int to the specified enum type.
        /// </returns>
        internal static Func<int, T?> MakeConverter()
        {
            var param = Expression.Parameter(typeof(int));
            var method = Expression.Lambda<Func<int, T?>>
                            (Expression.Convert(param, typeof(T?)), param);

            return method.Compile();
        }
    }

    internal static class RegistryInfo
    {
        public static Dictionary<string, UInt32> GetServerLevels()
        {
            const string keyPath = @"Software\Microsoft\Windows NT\CurrentVersion\Server\ServerLevels";

            var rv = new Dictionary<string, UInt32>();

            using (var key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    foreach (var name in key.GetValueNames())
                    {
                        if (key.GetValueKind(name) == RegistryValueKind.DWord)
                        {
                            var val = key.GetValue(name);
                            rv.Add(name, Convert.ToUInt32(val));
                        }
                    }
                }
            }

            return rv;
        }

        public static string GetLogonServer()
        {
            const string valueName = "LOGONSERVER";
            const string keyPath = "Volatile Environment";

            using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key != null)
                    return (string)key.GetValue(valueName, null);
            }

            return null;
        }

        public static RegWinNtCurrentVersion GetWinNtCurrentVersion()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
            {
                if (key != null)
                {
                    object temp = key.GetValue("InstallDate");

                    return new RegWinNtCurrentVersion()
                    {
                        BuildLabEx = (string)key.GetValue("BuildLabEx"),
                        CurrentVersion = (string)key.GetValue("CurrentVersion"),
                        EditionId = (string)key.GetValue("EditionID"),
                        InstallationType = (string)key.GetValue("InstallationType"),
                        InstallDate = temp == null ? (DateTime?)null
                                                                : Conversion.UnixSecondsToDateTime((long)(int)temp),
                        ProductId = (string)key.GetValue("ProductId"),
                        ProductName = (string)key.GetValue("ProductName"),
                        RegisteredOrganization = (string)key.GetValue("RegisteredOrganization"),
                        RegisteredOwner = (string)key.GetValue("RegisteredOwner"),
                        SystemRoot = (string)key.GetValue("SystemRoot"),
                        ReleaseId = (string)key.GetValue("ReleaseId"),
                        UBR = (int?)key.GetValue("UBR")
                    };
                }
            }

            return null;
        }
    }
    #endregion Helper classes

    #region Intermediate WMI classes
    /// <summary>
    /// Base class for some of the other Intermediate WMI classes,
    /// providing some shared methods.
    /// </summary>
    internal abstract class WmiClassBase
    {
        /// <summary>
        /// Get a language name from a language identifier.
        /// </summary>
        /// <param name="lcid">
        /// A nullable integer containing the language ID for the desired language.
        /// </param>
        /// <returns>
        /// A string containing the display name of the language identified by
        /// the language parameter. If the language parameter is null or has a
        /// value that is not a valid language ID, the method returns null.
        /// </returns>
        protected static string GetLanguageName(uint? lcid)
        {
            if (lcid != null && lcid >= 0)
            {
                try
                {
                    return CultureInfo.GetCultureInfo((int)lcid.Value).Name;
                }
                catch
                {
                }
            }

            return null;
        }
    }

#pragma warning disable 649 // fields and properties in these class are assigned dynamically
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiBaseBoard
    {
        public string Caption;
        public string[] ConfigOptions;
        public float? Depth;
        public string Description;
        public float? Height;
        public bool? HostingBoard;
        public bool? HotSwappable;
        public DateTime? InstallDate;
        public string Manufacturer;
        public string Model;
        public string Name;
        public string OtherIdentifyingInfo;
        public string PartNumber;
        public bool? PoweredOn;
        public string Product;
        public bool? Removable;
        public bool? Replaceable;
        public string RequirementsDescription;
        public bool? RequiresDaughterBoard;
        public string SerialNumber;
        public string SKU;
        public string SlotLayout;
        public bool? SpecialRequirements;
        public string Status;
        public string Tag;
        public string Version;
        public float? Weight;
        public float? Width;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiBios : WmiClassBase
    {
        public UInt16[] BiosCharacteristics;
        public string[] BIOSVersion;
        public string BuildNumber;
        public string Caption;
        public string CodeSet;
        public string CurrentLanguage;
        public string Description;
        public byte? EmbeddedControllerMajorVersion;
        public byte? EmbeddedControllerMinorVersion;
        public string IdentificationCode;
        public ushort? InstallableLanguages;
        public DateTime? InstallDate;
        public string LanguageEdition;
        public string[] ListOfLanguages;
        public string Manufacturer;
        public string Name;
        public string OtherTargetOS;
        public bool? PrimaryBIOS;
        public DateTime? ReleaseDate;
        public string SerialNumber;
        public string SMBIOSBIOSVersion;
        public ushort? SMBIOSMajorVersion;
        public ushort? SMBIOSMinorVersion;
        public bool? SMBIOSPresent;
        public ushort? SoftwareElementState;
        public string Status;
        public byte? SystemBiosMajorVersion;
        public byte? SystemBiosMinorVersion;
        public ushort? TargetOperatingSystem;
        public string Version;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiComputerSystem
    {
        public ushort? AdminPasswordStatus;
        public bool? AutomaticManagedPagefile;
        public bool? AutomaticResetBootOption;
        public bool? AutomaticResetCapability;
        public ushort? BootOptionOnLimit;
        public ushort? BootOptionOnWatchDog;
        public bool? BootROMSupported;
        public string BootupState;
        public UInt16[] BootStatus;
        public string Caption;
        public ushort? ChassisBootupState;
        public string ChassisSKUNumber;
        public Int16? CurrentTimeZone;
        public bool? DaylightInEffect;
        public string Description;
        public string DNSHostName;
        public string Domain;
        public ushort? DomainRole;
        public bool? EnableDaylightSavingsTime;
        public ushort? FrontPanelResetStatus;
        public bool? HypervisorPresent;
        public bool? InfraredSupported;
        public string InitialLoadInfo;
        public DateTime? InstallDate;
        public ushort? KeyboardPasswordStatus;
        public string LastLoadInfo;
        public string Manufacturer;
        public string Model;
        public string Name;
        public bool? NetworkServerModeEnabled;
        public uint? NumberOfLogicalProcessors;
        public uint? NumberOfProcessors;
        public string[] OEMStringArray;
        public bool? PartOfDomain;
        public Int64? PauseAfterReset;
        public ushort? PCSystemType;
        public ushort? PCSystemTypeEx;
        public UInt16[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public ushort? PowerOnPasswordStatus;
        public ushort? PowerState;
        public ushort? PowerSupplyState;
        public string PrimaryOwnerContact;
        public string PrimaryOwnerName;
        public ushort? ResetCapability;
        public Int16? ResetCount;
        public Int16? ResetLimit;
        public string[] Roles;
        public string Status;
        public string[] SupportContactDescription;
        public string SystemFamily;
        public string SystemSKUNumber;
        public string SystemType;
        public ushort? ThermalState;
        public ulong? TotalPhysicalMemory;
        public string UserName;
        public ushort? WakeUpType;
        public string Workgroup;

        public PowerManagementCapabilities[] GetPowerManagementCapabilities()
        {
            if (PowerManagementCapabilities != null)
            {
                var list = new List<PowerManagementCapabilities>();

                foreach (var cap in PowerManagementCapabilities)
                {
                    var val = EnumConverter<PowerManagementCapabilities>.Convert(cap);

                    if (val != null)
                        list.Add(val.Value);
                }

                return list.ToArray();
            }

            return null;
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiDeviceGuard
    {
        public UInt32[] AvailableSecurityProperties;
        public uint? CodeIntegrityPolicyEnforcementStatus;
        public uint? UsermodeCodeIntegrityPolicyEnforcementStatus;
        public UInt32[] RequiredSecurityProperties;
        public UInt32[] SecurityServicesConfigured;
        public UInt32[] SecurityServicesRunning;
        public uint? VirtualizationBasedSecurityStatus;

        public DeviceGuard AsOutputType
        {
            get
            {
                var guard = new DeviceGuard();

                var status = EnumConverter<DeviceGuardSmartStatus>.Convert((int?)VirtualizationBasedSecurityStatus);
                if (status != null && status != DeviceGuardSmartStatus.Off)
                {
                    var listHardware = new List<DeviceGuardHardwareSecure>();
                    for (int i = 0; i < RequiredSecurityProperties.Length; i++)
                    {
                        var temp = EnumConverter<DeviceGuardHardwareSecure>.Convert((int?)RequiredSecurityProperties[i]);

                        if (temp != null)
                            listHardware.Add(temp.Value);
                    }

                    guard.RequiredSecurityProperties = listHardware.ToArray();

                    listHardware.Clear();
                    for (int i = 0; i < AvailableSecurityProperties.Length; i++)
                    {
                        var temp = EnumConverter<DeviceGuardHardwareSecure>.Convert((int?)AvailableSecurityProperties[i]);

                        if (temp != null)
                            listHardware.Add(temp.Value);
                    }

                    guard.AvailableSecurityProperties = listHardware.ToArray();

                    var listSoftware = new List<DeviceGuardSoftwareSecure>();
                    for (int i = 0; i < SecurityServicesConfigured.Length; i++)
                    {
                        var temp = EnumConverter<DeviceGuardSoftwareSecure>.Convert((int?)SecurityServicesConfigured[i]);

                        if (temp != null)
                            listSoftware.Add(temp.Value);
                    }

                    guard.SecurityServicesConfigured = listSoftware.ToArray();

                    listSoftware.Clear();
                    for (int i = 0; i < SecurityServicesRunning.Length; i++)
                    {
                        var temp = EnumConverter<DeviceGuardSoftwareSecure>.Convert((int?)SecurityServicesRunning[i]);

                        if (temp != null)
                            listSoftware.Add(temp.Value);
                    }

                    guard.SecurityServicesRunning = listSoftware.ToArray();
                }

                var configCiStatus = EnumConverter<DeviceGuardConfigCodeIntegrityStatus>.Convert((int?)CodeIntegrityPolicyEnforcementStatus);
                var userModeCiStatus = EnumConverter<DeviceGuardConfigCodeIntegrityStatus>.Convert((int?)UsermodeCodeIntegrityPolicyEnforcementStatus);
                guard.CodeIntegrityPolicyEnforcementStatus = configCiStatus;
                guard.UserModeCodeIntegrityPolicyEnforcementStatus = userModeCiStatus;

                return guard;
            }
        }
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiKeyboard
    {
        public ushort? Availability;
        public string Caption;
        public uint? ConfigManagerErrorCode;
        public bool? ConfigManagerUserConfig;
        public string Description;
        public string DeviceID;
        public bool? ErrorCleared;
        public string ErrorDescription;
        public DateTime? InstallDate;
        public bool? IsLocked;
        public uint? LastErrorCode;
        public string Layout;
        public string Name;
        public ushort? NumberOfFunctionKeys;
        public ushort? Password;
        public string PNPDeviceID;
        public UInt16[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public string Status;
        public ushort? StatusInfo;
        public string SystemCreationClassName;
        public string SystemName;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WMiLogicalMemory
    {
        // TODO: fill this in!!!
        public uint? TotalPhysicalMemory;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiMsftNetAdapter
    {
        public string Caption;
        public string Description;
        public DateTime? InstallDate;
        public string Name;
        public string Status;
        public ushort? Availability;
        public uint? ConfigManagerErrorCode;
        public bool? ConfigManagerUserConfig;
        public string DeviceID;
        public bool? ErrorCleared;
        public string ErrorDescription;
        public uint? LastErrorCode;
        public string PNPDeviceID;
        public UInt16[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public ushort? StatusInfo;
        public string SystemCreationClassName;
        public string SystemName;
        public ulong? Speed;
        public ulong? MaxSpeed;
        public ulong? RequestedSpeed;
        public ushort? UsageRestriction;
        public ushort? PortType;
        public string OtherPortType;
        public string OtherNetworkPortType;
        public ushort? PortNumber;
        public ushort? LinkTechnology;
        public string OtherLinkTechnology;
        public string PermanentAddress;
        public string[] NetworkAddresses;
        public bool? FullDuplex;
        public bool? AutoSense;
        public ulong? SupportedMaximumTransmissionUnit;
        public ulong? ActiveMaximumTransmissionUnit;
        public string InterfaceDescription;
        public string InterfaceName;
        public ulong? NetLuid;
        public string InterfaceGuid;
        public uint? InterfaceIndex;
        public string DeviceName;
        public uint? NetLuidIndex;
        public bool? Virtual;
        public bool? Hidden;
        public bool? NotUserRemovable;
        public bool? IMFilter;
        public uint? InterfaceType;
        public bool? HardwareInterface;
        public bool? WdmInterface;
        public bool? EndPointInterface;
        public bool? iSCSIInterface;
        public uint? State;
        public uint? NdisMedium;
        public uint? NdisPhysicalMedium;
        public uint? InterfaceOperationalStatus;
        public bool? OperationalStatusDownDefaultPortNotAuthenticated;
        public bool? OperationalStatusDownMediaDisconnected;
        public bool? OperationalStatusDownInterfacePaused;
        public bool? OperationalStatusDownLowPowerState;
        public uint? InterfaceAdminStatus;
        public uint? MediaConnectState;
        public uint? MtuSize;
        public ushort? VlanID;
        public ulong? TransmitLinkSpeed;
        public ulong? ReceiveLinkSpeed;
        public bool? PromiscuousMode;
        public bool? DeviceWakeUpEnable;
        public bool? ConnectorPresent;
        public uint? MediaDuplexState;
        public string DriverDate;
        public ulong? DriverDateData;
        public string DriverVersionString;
        public string DriverName;
        public string DriverDescription;
        public ushort? MajorDriverVersion;
        public ushort? MinorDriverVersion;
        public byte? DriverMajorNdisVersion;
        public byte? DriverMinorNdisVersion;
        public string PnPDeviceID;
        public string DriverProvider;
        public string ComponentID;
        public UInt32[] LowerLayerInterfaceIndices;
        public UInt32[] HigherLayerInterfaceIndices;
        public bool? AdminLocked;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiNetworkAdapter
    {
        public string AdapterType;
        public ushort? AdapterTypeID;
        public bool? AutoSense;
        public ushort? Availability;
        public string Caption;
        public uint? ConfigManagerErrorCode;
        public bool? ConfigManagerUserConfig;
        public string Description;
        public string DeviceID;
        public bool? ErrorCleared;
        public string ErrorDescription;
        public string GUID;
        public uint? Index;
        public DateTime? InstallDate;
        public bool? Installed;
        public uint? InterfaceIndex;
        public uint? LastErrorCode;
        public string MACAddress;
        public string Manufacturer;
        public uint? MaxNumberControlled;
        public ulong? MaxSpeed;
        public string Name;
        public string NetConnectionID;
        public ushort? NetConnectionStatus;
        public bool? NetEnabled;
        public string[] NetworkAddresses;
        public string PermanentAddress;
        public bool? PhysicalAdapter;
        public string PNPDeviceID;
        public UInt16[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public string ProductName;
        public string ServiceName;
        public ulong? Speed;
        public string Status;
        public ushort? StatusInfo;
        public string SystemCreationClassName;
        public string SystemName;
        public DateTime? TimeOfLastReset;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiNetworkAdapterConfiguration
    {
        public bool? ArpAlwaysSourceRoute;
        public bool? ArpUseEtherSNAP;
        public string Caption;
        public string DatabasePath;
        public bool? DeadGWDetectEnabled;
        public string[] DefaultIPGateway;
        public byte? DefaultTOS;
        public byte? DefaultTTL;
        public string Description;
        public bool? DHCPEnabled;
        public DateTime? DHCPLeaseExpires;
        public DateTime? DHCPLeaseObtained;
        public string DHCPServer;
        public string DNSDomain;
        public string[] DNSDomainSuffixSearchOrder;
        public bool? DNSEnabledForWINSResolution;
        public string DNSHostName;
        public string[] DNSServerSearchOrder;
        public bool? DomainDNSRegistrationEnabled;
        public uint? ForwardBufferMemory;
        public bool? FullDNSRegistrationEnabled;
        public UInt16[] GatewayCostMetric;
        public byte? IGMPLevel;
        public uint? Index;
        public uint? InterfaceIndex;
        public string[] IPAddress;
        public uint? IPConnectionMetric;
        public bool? IPEnabled;
        public bool? IPFilterSecurityEnabled;
        public bool? IPPortSecurityEnabled;
        public string[] IPSecPermitIPProtocols;
        public string[] IPSecPermitTCPPorts;
        public string[] IPSecPermitUDPPorts;
        public string[] IPSubnet;
        public bool? IPUseZeroBroadcast;
        public string IPXAddress;
        public bool? IPXEnabled;
        public UInt32[] IPXFrameType;
        public uint? IPXMediaType;
        public string[] IPXNetworkNumber;
        public string IPXVirtualNetNumber;
        public uint? KeepAliveInterval;
        public uint? KeepAliveTime;
        public string MACAddress;
        public uint? MTU;
        public uint? NumForwardPackets;
        public bool? PMTUBHDetectEnabled;
        public bool? PMTUDiscoveryEnabled;
        public string ServiceName;
        public string SettingID;
        public uint? TcpipNetbiosOptions;
        public uint? TcpMaxConnectRetransmissions;
        public uint? TcpMaxDataRetransmissions;
        public uint? TcpNumConnections;
        public bool? TcpUseRFC1122UrgentPointer;
        public ushort? TcpWindowSize;
        public bool? WINSEnableLMHostsLookup;
        public string WINSHostLookupFile;
        public string WINSPrimaryServer;
        public string WINSScopeID;
        public string WINSSecondaryServer;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiOperatingSystem : WmiClassBase
    {
        #region Fields
        public string BootDevice;
        public string BuildNumber;
        public string BuildType;
        public string Caption;
        public string CodeSet;
        public string CountryCode;
        public string CSDVersion;
        public string CSName;
        public Int16? CurrentTimeZone;
        public bool? DataExecutionPrevention_Available;
        public bool? DataExecutionPrevention_32BitApplications;
        public bool? DataExecutionPrevention_Drivers;
        public byte? DataExecutionPrevention_SupportPolicy;
        public bool? Debug;
        public string Description;
        public bool? Distributed;
        public uint? EncryptionLevel;
        public byte? ForegroundApplicationBoost;
        public ulong? FreePhysicalMemory;
        public ulong? FreeSpaceInPagingFiles;
        public ulong? FreeVirtualMemory;
        public DateTime? InstallDate;
        public DateTime? LastBootUpTime;
        public DateTime? LocalDateTime;
        public string Locale;
        public string Manufacturer;
        public uint? MaxNumberOfProcesses;
        public ulong? MaxProcessMemorySize;
        public string[] MUILanguages;
        public string Name;
        public uint? NumberOfLicensedUsers;
        public uint? NumberOfProcesses;
        public uint? NumberOfUsers;
        public uint? OperatingSystemSKU;
        public string Organization;
        public string OSArchitecture;
        public uint? OSLanguage;
        public uint? OSProductSuite;
        public ushort? OSType;
        public string OtherTypeDescription;
        public bool? PAEEnabled;
        public bool? PortableOperatingSystem;
        public bool? Primary;
        public uint? ProductType;
        public string RegisteredUser;
        public string SerialNumber;
        public ushort? ServicePackMajorVersion;
        public ushort? ServicePackMinorVersion;
        public ulong? SizeStoredInPagingFiles;
        public string Status;
        public uint? SuiteMask;
        public string SystemDevice;
        public string SystemDirectory;
        public string SystemDrive;
        public ulong? TotalSwapSpaceSize;
        public ulong? TotalVirtualMemorySize;
        public ulong? TotalVisibleMemorySize;
        public string Version;
        public string WindowsDirectory;
        #endregion Fields

        #region Public Properties
        public string LanguageName
        {
            get { return GetLanguageName(OSLanguage); }
        }

        public OSProductSuite[] ProductSuites
        {
            get { return MakeProductSuites(OSProductSuite); }
        }

        public OSProductSuite[] Suites
        {
            get { return MakeProductSuites(SuiteMask); }
        }
        #endregion Public Properties

        #region Public Methods
        public string GetLocale()
        {
            return Conversion.GetLocaleName(Locale);
        }
        #endregion Public Methods

        #region Private Methods
        private static OSProductSuite[] MakeProductSuites(uint? suiteMask)
        {
            if (suiteMask == null)
                return null;

            var mask = suiteMask.Value;
            var list = new List<OSProductSuite>();

            foreach (OSProductSuite suite in Enum.GetValues(typeof(OSProductSuite)))
                if ((mask & (UInt32)suite) != 0)
                    list.Add(suite);

            return list.ToArray();
        }
        #endregion Private Methods
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiPageFileUsage
    {
        public uint? AllocatedBaseSize;
        public string Caption;
        public uint? CurrentUsage;
        public string Description;
        public DateTime? InstallDate;
        public string Name;
        public uint? PeakUsage;
        public string Status;
        public bool? TempPageFile;
    }

    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated directly from a CIM instance")]
    internal class WmiProcessor
    {
        public ushort? AddressWidth;
        public ushort? Architecture;
        public string AssetTag;
        public ushort? Availability;
        public string Caption;
        public uint? Characteristics;
        public uint? ConfigManagerErrorCode;
        public bool? ConfigManagerUserConfig;
        public ushort? CpuStatus;
        public uint? CurrentClockSpeed;
        public ushort? CurrentVoltage;
        public ushort? DataWidth;
        public string Description;
        public string DeviceID;
        public bool? ErrorCleared;
        public string ErrorDescription;
        public uint? ExtClock;
        public ushort? Family;
        public DateTime? InstallDate;
        public uint? L2CacheSize;
        public uint? L2CacheSpeed;
        public uint? L3CacheSize;
        public uint? L3CacheSpeed;
        public uint? LastErrorCode;
        public ushort? Level;
        public ushort? LoadPercentage;
        public string Manufacturer;
        public uint? MaxClockSpeed;
        public string Name;
        public uint? NumberOfCores;
        public uint? NumberOfEnabledCore;
        public uint? NumberOfLogicalProcessors;
        public string OtherFamilyDescription;
        public string PartNumber;
        public string PNPDeviceID;
        public UInt16[] PowerManagementCapabilities;
        public bool? PowerManagementSupported;
        public string ProcessorId;
        public ushort? ProcessorType;
        public ushort? Revision;
        public string Role;
        public bool? SecondLevelAddressTranslationExtensions;
        public string SerialNumber;
        public string SocketDesignation;
        public string Status;
        public ushort? StatusInfo;
        public string Stepping;
        public string SystemName;
        public uint? ThreadCount;
        public string UniqueId;
        public ushort? UpgradeMethod;
        public string Version;
        public bool? VirtualizationFirmwareEnabled;
        public bool? VMMonitorModeExtensions;
        public uint? VoltageCaps;
    }

#pragma warning restore 649
    #endregion Intermediate WMI classes

    #region Other Intermediate classes
    internal class RegWinNtCurrentVersion
    {
        public string BuildLabEx;
        public string CurrentVersion;
        public string EditionId;
        public string InstallationType;
        public DateTime? InstallDate;
        public string ProductId;
        public string ProductName;
        public string RegisteredOrganization;
        public string RegisteredOwner;
        public string SystemRoot;
        public string ReleaseId;
        public int? UBR;
    }
    #endregion Other Intermediate classes

    #region Output components
    #region Classes comprising the output object
    /// <summary>
    /// Provides information about Device Guard.
    /// </summary>
    public class DeviceGuard
    {
        /// <summary>
        /// Array of required security properties.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardHardwareSecure[] RequiredSecurityProperties { get; internal set; }
        /// <summary>
        /// Array of available security properties.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardHardwareSecure[] AvailableSecurityProperties { get; internal set; }
        /// <summary>
        /// Indicates which security services have been configured.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardSoftwareSecure[] SecurityServicesConfigured { get; internal set; }
        /// <summary>
        /// Indicates which security services are running.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardSoftwareSecure[] SecurityServicesRunning { get; internal set; }
        /// <summary>
        /// Indicates the status of the Device Guard Code Integrity policy.
        /// </summary>
        public DeviceGuardConfigCodeIntegrityStatus? CodeIntegrityPolicyEnforcementStatus { get; internal set; }

        /// <summary>
        /// Indicates the status of the Device Guard user mode Code Integrity policy.
        /// </summary>
        public DeviceGuardConfigCodeIntegrityStatus? UserModeCodeIntegrityPolicyEnforcementStatus { get; internal set; }
    }

    /// <summary>
    /// Describes a Quick-Fix Engineering update.
    /// </summary>
    public class HotFix
    {
        /// <summary>
        /// Unique identifier associated with a particular update.
        /// </summary>
        public string HotFixID
        {
            get;
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Class is instantiated directly from a CIM instance")]
            internal set;
        }

        /// <summary>
        /// Description of the update.
        /// </summary>
        public string Description
        {
            get;
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Class is instantiated directly from a CIM instance")]
            internal set;
        }

        /// <summary>
        /// String containing the date that the update was installed.
        /// </summary>
        public string InstalledOn
        {
            get;
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Class is instantiated directly from a CIM instance")]
            internal set;
        }

        /// <summary>
        /// Additional comments that relate to the update.
        /// </summary>
        public string FixComments
        {
            get;
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Class is instantiated directly from a CIM instance")]
            internal set;
        }
    }

    /// <summary>
    /// Provides information about a network adapter.
    /// </summary>
    public class NetworkAdapter
    {
        /// <summary>
        /// Description of the network adapter.
        /// </summary>
        public string Description { get; internal set; }
        /// <summary>
        /// Name of the network connection as it appears in the Network
        /// Connections Control Panel program.
        /// </summary>
        public string ConnectionID { get; internal set; }
        /// <summary>
        /// Indicates whether the DHCP server automatically assigns an IP address
        /// to the computer system when establishing a network connection.
        /// </summary>
        public bool? DHCPEnabled { get; internal set; }
        /// <summary>
        /// IP Address of the DHCP server.
        /// </summary>
        public string DHCPServer { get; internal set; }
        /// <summary>
        /// State of the network adapter connection to the network.
        /// </summary>
        public NetConnectionStatus ConnectionStatus { get; internal set; }
        /// <summary>
        /// Array of all of the IP addresses associated with the current network adapter.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] IPAddresses { get; internal set; }
    }

    /// <summary>
    /// Describes a processor on the computer.
    /// </summary>
    public class Processor
    {
        /// <summary>
        /// Name of the processor.
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Name of the processor manufacturer.
        /// </summary>
        public string Manufacturer { get; internal set; }
        /// <summary>
        /// Description of the processor.
        /// </summary>
        public string Description { get; internal set; }
        /// <summary>
        /// Processor architecture used by the platform.
        /// </summary>
        public CpuArchitecture? Architecture { get; internal set; }
        /// <summary>
        /// Address width of the processor.
        /// </summary>
        public ushort? AddressWidth { get; internal set; }
        /// <summary>
        /// Data width of the processor.
        /// </summary>
        public ushort? DataWidth { get; internal set; }
        /// <summary>
        /// Maximum speed of the processor, in MHz.
        /// </summary>
        public uint? MaxClockSpeed { get; internal set; }
        /// <summary>
        /// Current speed of the processor, in MHz.
        /// </summary>
        public uint? CurrentClockSpeed { get; internal set; }
        /// <summary>
        /// Number of cores for the current instance of the processor.
        /// </summary>
        /// <remarks>
        /// A core is a physical processor on the integrated circuit
        /// </remarks>
        public uint? NumberOfCores { get; internal set; }
        /// <summary>
        /// Number of logical processors for the current instance of the processor.
        /// </summary>
        /// <remarks>
        /// For processors capable of hyperthreading, this value includes only the
        /// processors which have hyperthreading enabled
        /// </remarks>
        public uint? NumberOfLogicalProcessors { get; internal set; }
        /// <summary>
        /// Processor information that describes the processor features.
        /// </summary>
        /// <remarks>
        /// For an x86 class CPU, the field format depends on the processor support
        /// of the CPUID instruction. If the instruction is supported, the property
        /// contains 2 (two) DWORD formatted values. The first is an offset of 08h-0Bh,
        /// which is the EAX value that a CPUID instruction returns with input EAX set
        /// to 1. The second is an offset of 0Ch-0Fh, which is the EDX value that the
        /// instruction returns. Only the first two bytes of the property are significant
        /// and contain the contents of the DX register at CPU reset—all others are set
        /// to 0 (zero), and the contents are in DWORD format
        /// </remarks>
        public string ProcessorID { get; internal set; }
        /// <summary>
        /// Type of chip socket used on the circuit.
        /// </summary>
        public string SocketDesignation { get; internal set; }
        /// <summary>
        /// Primary function of the processor.
        /// </summary>
        public ProcessorType? ProcessorType { get; internal set; }
        /// <summary>
        /// Role of the processor.
        /// </summary>
        public string Role { get; internal set; }
        /// <summary>
        /// Current status of the processor.
        /// </summary>
        public string Status { get; internal set; }
        /// <summary>
        /// Current status of the processor.
        /// Status changes indicate processor usage, but not the physical
        /// condition of the processor.
        /// </summary>
        public CpuStatus? CpuStatus { get; internal set; }
        /// <summary>
        /// Availability and status of the processor.
        /// </summary>
        public CpuAvailability? Availability { get; internal set; }
    }

    /// <summary>
    /// The ComputerInfo class is output to the PowerShell pipeline.
    /// </summary>
    public class ComputerInfo
    {
        #region Registry
        /// <summary>
        /// Windows build lab information, from the Windows Registry.
        /// </summary>
        public string WindowsBuildLabEx { get; internal set; }

        /// <summary>
        /// Windows version number, from the Windows Registry.
        /// </summary>
        public string WindowsCurrentVersion { get; internal set; }

        /// <summary>
        /// Windows edition, from the Windows Registry.
        /// </summary>
        public string WindowsEditionId { get; internal set; }

        /// <summary>
        /// Windows installation type, from the Windows Registry.
        /// </summary>
        public string WindowsInstallationType { get; internal set; }

        /// <summary>
        /// The data Windows was installed, from the Windows Registry.
        /// </summary>
        public DateTime? WindowsInstallDateFromRegistry { get; internal set; }

        /// <summary>
        /// The Windows product ID, from the Windows Registry.
        /// </summary>
        public string WindowsProductId { get; internal set; }

        /// <summary>
        /// The Windows product name, from the Windows Registry.
        /// </summary>
        public string WindowsProductName { get; internal set; }

        /// <summary>
        /// Name of the organization that this installation of Windows is registered to, from the Windows Registry.
        /// </summary>
        public string WindowsRegisteredOrganization { get; internal set; }

        /// <summary>
        /// Name of the registered owner of this installation of Windows, from the Windows Registry.
        /// </summary>
        public string WindowsRegisteredOwner { get; internal set; }

        /// <summary>
        /// Path to the operating system's root directory, from the Windows Registry.
        /// </summary>
        public string WindowsSystemRoot { get; internal set; }

        /// <summary>
        /// The Windows ReleaseId, from the Windows Registry.
        /// </summary>
        public string WindowsVersion { get; internal set; }

        /// <summary>
        /// The Windows Update Build Revision (UBR), from the Windows Registry.
        /// </summary>
        public int? WindowsUBR { get; internal set; }
        #endregion Registry

        #region BIOS
        /// <summary>
        /// Array of BIOS characteristics supported by the system as defined by
        /// the System Management BIOS Reference Specification.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public UInt16[] BiosCharacteristics { get; internal set; }

        /// <summary>
        /// Array of the complete system BIOS information. In many computers
        /// there can be several version strings that are stored in the registry
        /// and represent the system BIOS information.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] BiosBIOSVersion { get; internal set; }

        /// <summary>
        /// Internal identifier for this compilation of the BIOS firmware.
        /// </summary>
        public string BiosBuildNumber { get; internal set; }

        /// <summary>
        /// Short description of the BIOS.
        /// </summary>
        public string BiosCaption { get; internal set; }

        /// <summary>
        /// Code set used by the BIOS.
        /// </summary>
        public string BiosCodeSet { get; internal set; }

        /// <summary>
        /// Name of the current BIOS language.
        /// </summary>
        public string BiosCurrentLanguage { get; internal set; }

        /// <summary>
        /// Description of the BIOS.
        /// </summary>
        public string BiosDescription { get; internal set; }

        /// <summary>
        /// Major version of the embedded controller firmware.
        /// </summary>
        public Int16? BiosEmbeddedControllerMajorVersion { get; internal set; }

        /// <summary>
        /// Minor version of the embedded controller firmware.
        /// </summary>
        public Int16? BiosEmbeddedControllerMinorVersion { get; internal set; }

        /// <summary>
        /// Firmware type of the local computer.
        /// </summary>
        /// <remarks>
        /// This is acquired via the GetFirmwareType Windows API function
        /// </remarks>
        public FirmwareType? BiosFirmwareType { get; internal set; }

        /// <summary>
        /// Manufacturer's identifier for this software element.
        /// Often this will be a stock keeping unit (SKU) or a part number.
        /// </summary>
        public string BiosIdentificationCode { get; internal set; }

        /// <summary>
        /// Number of languages available for installation on this system.
        /// Language may determine properties such as the need for Unicode and bidirectional text.
        /// </summary>
        public ushort? BiosInstallableLanguages { get; internal set; }

        /// <summary>
        /// Date and time the object was installed.
        /// </summary>
        // TODO: do we want this? On my system this is null
        public DateTime? BiosInstallDate { get; internal set; }

        /// <summary>
        /// Language edition of the BIOS firmware.
        /// The language codes defined in ISO 639 should be used.
        /// Where the software element represents a multilingual or international
        /// version of a product, the string "multilingual" should be used.
        /// </summary>
        public string BiosLanguageEdition { get; internal set; }

        /// <summary>
        /// Array of names of available BIOS-installable languages.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] BiosListOfLanguages { get; internal set; }

        /// <summary>
        /// Manufacturer of the BIOS.
        /// </summary>
        public string BiosManufacturer { get; internal set; }

        /// <summary>
        /// Name used to identify the BIOS.
        /// </summary>
        public string BiosName { get; internal set; }

        /// <summary>
        /// Records the manufacturer and operating system type for the BIOS when
        /// the BiosTargetOperatingSystem property has a value of 1 (Other).
        /// When TargetOperatingSystem has a value of 1, BiosOtherTargetOS must
        /// have a nonnull value. For all other values of BiosTargetOperatingSystem,
        /// BiosOtherTargetOS is NULL.
        /// </summary>
        public string BiosOtherTargetOS { get; internal set; }

        /// <summary>
        /// If true, this is the primary BIOS of the computer system.
        /// </summary>
        public bool? BiosPrimaryBIOS { get; internal set; }

        /// <summary>
        /// Release date of the Windows BIOS.
        /// </summary>
        public DateTime? BiosReleaseDate { get; internal set; }

        /// <summary>
        /// Assigned serial number of the BIOS.
        /// </summary>
        public string BiosSerialNumber { get; internal set; }

        /// <summary>
        /// BIOS version as reported by SMBIOS.
        /// </summary>
        public string BiosSMBIOSBIOSVersion { get; internal set; }

        /// <summary>
        /// SMBIOS major version number. This property is null if SMBIOS is not found.
        /// </summary>
        public ushort? BiosSMBIOSMajorVersion { get; internal set; }

        /// <summary>
        /// SMBIOS minor version number. This property is null if SMBIOS is not found.
        /// </summary>
        public ushort? BiosSMBIOSMinorVersion { get; internal set; }

        /// <summary>
        /// If true, the SMBIOS is available on this computer system.
        /// </summary>
        public bool? BiosSMBIOSPresent { get; internal set; }

        /// <summary>
        /// State of a BIOS software element.
        /// </summary>
        public SoftwareElementState? BiosSoftwareElementState { get; internal set; }

        /// <summary>
        /// Status of the BIOS.
        /// </summary>
        public string BiosStatus { get; internal set; }

        /// <summary>
        /// Major elease of the System BIOS.
        /// </summary>
        public ushort? BiosSystemBiosMajorVersion { get; internal set; }

        /// <summary>
        /// Minor release of the System BIOS.
        /// </summary>
        public ushort? BiosSystemBiosMinorVersion { get; internal set; }

        /// <summary>
        /// Target operating system.
        /// </summary>
        public ushort? BiosTargetOperatingSystem { get; internal set; }

        /// <summary>
        /// Version of the BIOS.
        /// This string is created by the BIOS manufacturer.
        /// </summary>
        public string BiosVersion { get; internal set; }
        #endregion BIOS

        #region Computer System
        /// <summary>
        /// System hardware security settings for administrator password status.
        /// </summary>
        // public AdminPasswordStatus? CsAdminPasswordStatus { get; internal set; }

        public HardwareSecurity? CsAdminPasswordStatus { get; internal set; }

        /// <summary>
        /// If true, the system manages the page file.
        /// </summary>
        public bool? CsAutomaticManagedPagefile { get; internal set; }

        /// <summary>
        /// If True, the automatic reset boot option is enabled.
        /// </summary>
        public bool? CsAutomaticResetBootOption { get; internal set; }

        /// <summary>
        /// If True, the automatic reset is enabled.
        /// </summary>
        public bool? CsAutomaticResetCapability { get; internal set; }

        /// <summary>
        /// Boot option limit is ON. Identifies the system action when the
        /// CsResetLimit value is reached.
        /// </summary>
        public BootOptionAction? CsBootOptionOnLimit { get; internal set; }

        /// <summary>
        /// Type of reboot action after the time on the watchdog timer is elapsed.
        /// </summary>
        public BootOptionAction? CsBootOptionOnWatchDog { get; internal set; }

        /// <summary>
        /// If true, indicates whether a boot ROM is supported.
        /// </summary>
        public bool? CsBootROMSupported { get; internal set; }

        /// <summary>
        /// Status and Additional Data fields that identify the boot status.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public UInt16[] CsBootStatus { get; internal set; }

        /// <summary>
        /// System is started. Fail-safe boot bypasses the user startup files—also called SafeBoot.
        /// </summary>
        public string CsBootupState { get; internal set; }

        /// <summary>
        /// The name of this computer.
        /// </summary>
        public string CsCaption { get; internal set; }  // TODO: remove this? Same as CsName???

        /// <summary>
        /// Boot up state of the chassis.
        /// </summary>
        // public ChassisBootupState? CsChassisBootupState { get; internal set; }

        public SystemElementState? CsChassisBootupState { get; internal set; }

        /// <summary>
        /// The chassis or enclosure SKU number as a string.
        /// </summary>
        public string CsChassisSKUNumber { get; internal set; }

        /// <summary>
        /// Amount of time the unitary computer system is offset from Coordinated
        /// Universal Time (UTC).
        /// </summary>
        public Int16? CsCurrentTimeZone { get; internal set; }

        /// <summary>
        /// If True, the daylight savings mode is ON.
        /// </summary>
        public bool? CsDaylightInEffect { get; internal set; }

        /// <summary>
        /// Description of the computer system.
        /// </summary>
        public string CsDescription { get; internal set; }

        /// <summary>
        /// Name of local computer according to the domain name server.
        /// </summary>
        public string CsDNSHostName { get; internal set; }

        /// <summary>
        /// Name of the domain to which a computer belongs.
        /// </summary>
        /// <remarks>
        /// If the computer is not part of a domain, then the name of the workgroup is returned
        /// </remarks>
        public string CsDomain { get; internal set; }

        /// <summary>
        /// Role of a computer in an assigned domain workgroup. A domain workgroup
        /// is a collection of computers on the same network. For example,
        /// a DomainRole property may show that a computer is a member workstation.
        /// </summary>
        public DomainRole? CsDomainRole { get; internal set; }

        /// <summary>
        /// Enables daylight savings time on a computer. A value of True indicates
        /// that the system time changes to an hour ahead or behind when DST starts
        /// or ends. A value of False indicates that the system time does not change
        /// to an hour ahead or behind when DST starts or ends. A value of NULL
        /// indicates that the DST status is unknown on a system.
        /// </summary>
        public bool? CsEnableDaylightSavingsTime { get; internal set; }

        /// <summary>
        /// Hardware security setting for the reset button on a computer.
        /// </summary>
        // public FrontPanelResetStatus? CsFrontPanelResetStatus { get; internal set; }

        public HardwareSecurity? CsFrontPanelResetStatus { get; internal set; }

        /// <summary>
        /// If True, a hypervisor is present.
        /// </summary>
        public bool? CsHypervisorPresent { get; internal set; }

        /// <summary>
        /// If True, an infrared port exists on a computer system.
        /// </summary>
        public bool? CsInfraredSupported { get; internal set; }

        /// <summary>
        /// Data required to find the initial load device or boot service to request that the operating system start up.
        /// </summary>
        public string CsInitialLoadInfo { get; internal set; }

        /// <summary>
        /// Object is installed. An object does not need a value to indicate that it is installed.
        /// </summary>
        public DateTime? CsInstallDate { get; internal set; }

        /// <summary>
        /// System hardware security setting for Keyboard Password Status.
        /// </summary>
        // public KeyboardPasswordStatus? CsKeyboardPasswordStatus { get; internal set; }

        public HardwareSecurity? CsKeyboardPasswordStatus { get; internal set; }

        /// <summary>
        /// Array entry of the CsInitialLoadInfo property that contains the data
        /// to start the loaded operating system.
        /// </summary>
        public string CsLastLoadInfo { get; internal set; }

        /// <summary>
        /// Name of the computer manufacturer.
        /// </summary>
        public string CsManufacturer { get; internal set; }

        /// <summary>
        /// Product name that a manufacturer gives to a computer.
        /// </summary>
        public string CsModel { get; internal set; }

        /// <summary>
        /// Key of a CIM_System instance in an enterprise environment.
        /// </summary>
        public string CsName { get; internal set; }

        /// <summary>
        /// An array of <see cref="NetworkAdapter"/> objects describing any
        /// network adapters on the system.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public NetworkAdapter[] CsNetworkAdapters { get; internal set; }

        /// <summary>
        /// If True, the network Server Mode is enabled.
        /// </summary>
        public bool? CsNetworkServerModeEnabled { get; internal set; }

        /// <summary>
        /// Number of logical processors available on the computer.
        /// </summary>
        public uint? CsNumberOfLogicalProcessors { get; internal set; }

        /// <summary>
        /// Number of physical processors currently available on a system.
        /// </summary>
        /// <remarks>
        /// This is the number of enabled processors for a system, which
        /// does not include the disabled processors. If a computer system
        /// has two physical processors each containing two logical processors,
        /// then the value of CsNumberOfProcessors is 2 and CsNumberOfLogicalProcessors
        /// is 4. The processors may be multicore or they may be hyperthreading processors
        /// </remarks>
        public uint? CsNumberOfProcessors { get; internal set; }

        /// <summary>
        /// Array of <see cref="Processor"/> objects describing each processor on the system.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public Processor[] CsProcessors { get; internal set; }

        /// <summary>
        /// Array of free-form strings that an OEM defines.
        /// For example, an OEM defines the part numbers for system reference
        /// documents, manufacturer contact information, and so on.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CsOEMStringArray { get; internal set; }

        /// <summary>
        /// If True, the computer is part of a domain.
        /// If the value is NULL, the computer is not in a domain or the status is unknown.
        /// </summary>
        public bool? CsPartOfDomain { get; internal set; }

        /// <summary>
        /// Time delay before a reboot is initiated, in milliseconds.
        /// It is used after a system power cycle, local or remote system reset,
        /// and automatic system reset. A value of –1 (minus one) indicates that
        /// the pause value is unknown.
        /// </summary>
        public Int64? CsPauseAfterReset { get; internal set; }

        /// <summary>
        /// Type of the computer in use, such as laptop, desktop, or tablet.
        /// </summary>
        public PCSystemType? CsPCSystemType { get; internal set; }

        /// <summary>
        /// Type of the computer in use, such as laptop, desktop, or tablet.
        /// </summary>
        public PCSystemTypeEx? CsPCSystemTypeEx { get; internal set; }

        /// <summary>
        /// Array of the specific power-related capabilities of a logical device.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public PowerManagementCapabilities[] CsPowerManagementCapabilities { get; internal set; }

        /// <summary>
        /// If True, device can be power-managed, for example, a device can be
        /// put into suspend mode, and so on.
        /// </summary>
        /// <remarks>
        /// This property does not indicate that power management features are
        /// enabled currently, but it does indicate that the logical device is
        /// capable of power management
        /// </remarks>
        public bool? CsPowerManagementSupported { get; internal set; }

        /// <summary>
        /// System hardware security setting for Power-On Password Status.
        /// </summary>
        // public PowerOnPasswordStatus? CsPowerOnPasswordStatus { get; internal set; }

        public HardwareSecurity? CsPowerOnPasswordStatus { get; internal set; }

        /// <summary>
        /// Current power state of a computer and its associated operating system.
        /// </summary>
        public PowerState? CsPowerState { get; internal set; }

        /// <summary>
        /// State of the power supply or supplies when last booted.
        /// </summary>
        // public PowerSupplyState? CsPowerSupplyState { get; internal set; }

        public SystemElementState? CsPowerSupplyState { get; internal set; }

        /// <summary>
        /// Contact information for the primary system owner.
        /// For example, phone number, email address, and so on.
        /// </summary>
        public string CsPrimaryOwnerContact { get; internal set; }

        /// <summary>
        /// Name of the primary system owner.
        /// </summary>
        public string CsPrimaryOwnerName { get; internal set; }

        /// <summary>
        /// Indicates if the computer system can be reset.
        /// </summary>
        public ResetCapability? CsResetCapability { get; internal set; }

        /// <summary>
        /// Number of automatic resets since the last reset.
        /// A value of –1 (minus one) indicates that the count is unknown.
        /// </summary>
        public Int16? CsResetCount { get; internal set; }

        /// <summary>
        /// Number of consecutive times a system reset is attempted.
        /// A value of –1 (minus one) indicates that the limit is unknown.
        /// </summary>
        public Int16? CsResetLimit { get; internal set; }

        /// <summary>
        /// Array that specifies the roles of a system in the information
        /// technology environment.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CsRoles { get; internal set; }

        /// <summary>
        /// Statis pf the computer system.
        /// </summary>
        public string CsStatus { get; internal set; }

        /// <summary>
        /// Array of the support contact information for the Windows operating system.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CsSupportContactDescription { get; internal set; }

        /// <summary>
        /// The family to which a particular computer belongs.
        /// A family refers to a set of computers that are similar but not
        /// identical from a hardware or software point of view.
        /// </summary>
        public string CsSystemFamily { get; internal set; }

        /// <summary>
        /// Identifies a particular computer configuration for sale.
        /// It is sometimes also called a product ID or purchase order number.
        /// </summary>
        public string CsSystemSKUNumber { get; internal set; }

        /// <summary>
        /// System running on the Windows-based computer.
        /// </summary>
        public string CsSystemType { get; internal set; }

        /// <summary>
        /// Thermal state of the system when last booted.
        /// </summary>
        // public ThermalState? CsThermalState { get; internal set; }

        public SystemElementState? CsThermalState { get; internal set; }

        /// <summary>
        /// Total size of physical memory.
        /// </summary>
        /// <remarks>
        /// Be aware that, under some circumstances, this property may not
        /// return an accurate value for the physical memory. For example,
        /// it is not accurate if the BIOS is using some of the physical memory
        /// </remarks>
        public ulong? CsTotalPhysicalMemory { get; internal set; }

        /// <summary>
        /// Size of physically installed memory, as reported by the Windows API
        /// function GetPhysicallyInstalledSystemMemory.
        /// </summary>
        public ulong? CsPhysicallyInstalledMemory { get; internal set; }

        /// <summary>
        /// Name of a user that is logged on currently.
        /// </summary>
        /// <remarks>
        /// In a terminal services session, CsUserName is the name of the user
        /// that is logged on to the console—not the user logged on during the
        /// terminal service session
        /// </remarks>
        public string CsUserName { get; internal set; }

        /// <summary>
        /// Event that causes the system to power up.
        /// </summary>
        public WakeUpType? CsWakeUpType { get; internal set; }

        /// <summary>
        /// Name of the workgroup for this computer.
        /// </summary>
        public string CsWorkgroup { get; internal set; }
        #endregion Computer System

        #region Operating System
        /// <summary>
        /// Name of the operating system.
        /// </summary>
        public string OsName { get; internal set; }

        /// <summary>
        /// Type of operating system.
        /// </summary>
        public OSType? OsType { get; internal set; }

        /// <summary>
        /// SKU number for the operating system.
        /// </summary>
        public OperatingSystemSKU? OsOperatingSystemSKU { get; internal set; }

        /// <summary>
        /// Version number of the operating system.
        /// </summary>
        public string OsVersion { get; internal set; }

        /// <summary>
        /// String that indicates the latest service pack installed on a computer.
        /// If no service pack is installed, the string is NULL.
        /// </summary>
        public string OsCSDVersion { get; internal set; }

        /// <summary>
        /// Build number of the operating system.
        /// </summary>
        public string OsBuildNumber { get; internal set; }

        /// <summary>
        /// Array of <see cref="HotFix"/> objects containing information about
        /// any Quick-Fix Engineering patches (Hot Fixes) applied to the operating
        /// system.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public HotFix[] OsHotFixes { get; internal set; }

        /// <summary>
        /// Name of the disk drive from which the Windows operating system starts.
        /// </summary>
        public string OsBootDevice { get; internal set; }

        /// <summary>
        /// Physical disk partition on which the operating system is installed.
        /// </summary>
        public string OsSystemDevice { get; internal set; }

        /// <summary>
        /// System directory of the operating system.
        /// </summary>
        public string OsSystemDirectory { get; internal set; }

        /// <summary>
        /// Letter of the disk drive on which the operating system resides.
        /// </summary>
        public string OsSystemDrive { get; internal set; }

        /// <summary>
        /// Windows directory of the operating system.
        /// </summary>
        public string OsWindowsDirectory { get; internal set; }

        /// <summary>
        /// Code for the country/region that an operating system uses.
        /// </summary>
        /// <remarks>
        /// Values are based on international phone dialing prefixes—also
        /// referred to as IBM country/region codes
        /// </remarks>
        public string OsCountryCode { get; internal set; }

        /// <summary>
        /// Number, in minutes, an operating system is offset from Greenwich
        /// mean time (GMT). The number is positive, negative, or zero.
        /// </summary>
        public Int16? OsCurrentTimeZone { get; internal set; }

        /// <summary>
        /// Language identifier used by the operating system.
        /// </summary>
        /// <remarks>
        /// A language identifier is a standard international numeric abbreviation
        /// for a country/region. Each language has a unique language identifier (LANGID),
        /// a 16-bit value that consists of a primary language identifier and a secondary
        /// language identifier
        /// </remarks>
        public string OsLocaleID { get; internal set; }   // From Win32_OperatingSystem.Locale

        /// <summary>
        /// The culture name, such as "en-US", derived from the <see cref="OsLocaleID"/> property.
        /// </summary>
        public string OsLocale { get; internal set; }

        /// <summary>
        /// Operating system version of the local date and time-of-day.
        /// </summary>
        public DateTime? OsLocalDateTime { get; internal set; }

        /// <summary>
        /// Date and time the operating system was last restarted.
        /// </summary>
        public DateTime? OsLastBootUpTime { get; internal set; }

        /// <summary>
        /// The interval between the time the operating system was last
        /// restarted and the current time.
        /// </summary>
        public TimeSpan? OsUptime { get; internal set; }

        /// <summary>
        /// Type of build used for the operating system.
        /// </summary>
        public string OsBuildType { get; internal set; }

        /// <summary>
        /// Code page value the operating system uses.
        /// </summary>
        public string OsCodeSet { get; internal set; }

        /// <summary>
        /// If true, then the data execution prevention hardware feature is available.
        /// </summary>
        public bool? OsDataExecutionPreventionAvailable { get; internal set; }

        /// <summary>
        /// When the data execution prevention hardware feature is available,
        /// this property indicates that the feature is set to work for 32-bit
        /// applications if true.
        /// </summary>
        public bool? OsDataExecutionPrevention32BitApplications { get; internal set; }

        /// <summary>
        /// When the data execution prevention hardware feature is available,
        /// this property indicates that the feature is set to work for drivers
        /// if true.
        /// </summary>
        public bool? OsDataExecutionPreventionDrivers { get; internal set; }

        /// <summary>
        /// Indicates which Data Execution Prevention (DEP) setting is applied.
        /// The DEP setting specifies the extent to which DEP applies to 32-bit
        /// applications on the system. DEP is always applied to the Windows kernel.
        /// </summary>
        public DataExecutionPreventionSupportPolicy? OsDataExecutionPreventionSupportPolicy { get; internal set; }

        /// <summary>
        /// If true, the operating system is a checked (debug) build.
        /// </summary>
        public bool? OsDebug { get; internal set; }

        /// <summary>
        /// If True, the operating system is distributed across several computer
        /// system nodes. If so, these nodes should be grouped as a cluster.
        /// </summary>
        public bool? OsDistributed { get; internal set; }

        /// <summary>
        /// Encryption level for secure transactions: 40-bit, 128-bit, or n-bit.
        /// </summary>
        public OSEncryptionLevel? OsEncryptionLevel { get; internal set; }

        /// <summary>
        /// Increased priority given to the foreground application.
        /// </summary>
        public ForegroundApplicationBoost? OsForegroundApplicationBoost { get; internal set; }

        /// <summary>
        /// Total amount, in kilobytes, of physical memory available to the
        /// operating system.
        /// </summary>
        /// <remarks>
        /// This value does not necessarily indicate the true amount of
        /// physical memory, but what is reported to the operating system
        /// as available to it.
        /// </remarks>
        public ulong? OsTotalVisibleMemorySize { get; internal set; }

        /// <summary>
        /// Number, in kilobytes, of physical memory currently unused and available.
        /// </summary>
        public ulong? OsFreePhysicalMemory { get; internal set; }

        /// <summary>
        /// Number, in kilobytes, of virtual memory.
        /// </summary>
        public ulong? OsTotalVirtualMemorySize { get; internal set; }

        /// <summary>
        /// Number, in kilobytes, of virtual memory currently unused and available.
        /// </summary>
        public ulong? OsFreeVirtualMemory { get; internal set; }

        /// <summary>
        /// Number, in kilobytes, of virtual memory currently in use.
        /// </summary>
        public ulong? OsInUseVirtualMemory { get; internal set; }

        /// <summary>
        /// Total swap space in kilobytes.
        /// </summary>
        /// <remarks>
        /// This value may be NULL (unspecified) if the swap space is not
        /// distinguished from page files. However, some operating systems
        /// distinguish these concepts. For example, in UNIX, whole processes
        /// can be swapped out when the free page list falls and remains below
        /// a specified amount
        /// </remarks>
        public ulong? OsTotalSwapSpaceSize { get; internal set; }

        /// <summary>
        /// Total number of kilobytes that can be stored in the operating system
        /// paging files—0 (zero) indicates that there are no paging files.
        /// Be aware that this number does not represent the actual physical
        /// size of the paging file on disk.
        /// </summary>
        public ulong? OsSizeStoredInPagingFiles { get; internal set; }

        /// <summary>
        /// Number, in kilobytes, that can be mapped into the operating system
        /// paging files without causing any other pages to be swapped out.
        /// </summary>
        public ulong? OsFreeSpaceInPagingFiles { get; internal set; }

        /// <summary>
        /// Array of fiel paths to the operating system's paging files.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] OsPagingFiles { get; internal set; }

        /// <summary>
        /// Version of the operating system's Hardware Abstraction Layer (HAL)
        /// </summary>
        public string OsHardwareAbstractionLayer { get; internal set; }

        /// <summary>
        /// Indicates the install date.
        /// </summary>
        public DateTime? OsInstallDate { get; internal set; }

        /// <summary>
        /// Name of the operating system manufacturer.
        /// For Windows-based systems, this value is "Microsoft Corporation"
        /// </summary>
        public string OsManufacturer { get; internal set; }

        /// <summary>
        /// Maximum number of process contexts the operating system can support.
        /// </summary>
        public uint? OsMaxNumberOfProcesses { get; internal set; }

        /// <summary>
        /// Maximum number, in kilobytes, of memory that can be allocated to a process.
        /// </summary>
        public ulong? OsMaxProcessMemorySize { get; internal set; }

        /// <summary>
        /// Array of Multilingual User Interface Pack (MUI Pack) languages installed
        /// on the computer.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] OsMuiLanguages { get; internal set; }

        /// <summary>
        /// Number of user licenses for the operating system.
        /// </summary>
        public uint? OsNumberOfLicensedUsers { get; internal set; }

        /// <summary>
        /// Number of process contexts currently loaded or running on the operating system.
        /// </summary>
        public uint? OsNumberOfProcesses { get; internal set; }

        /// <summary>
        /// Number of user sessions for which the operating system is storing
        /// state information currently.
        /// </summary>
        public uint? OsNumberOfUsers { get; internal set; }

        /// <summary>
        /// Company name for the registered user of the operating system.
        /// </summary>
        public string OsOrganization { get; internal set; }

        /// <summary>
        /// Architecture of the operating system, as opposed to the processor.
        /// </summary>
        public string OsArchitecture { get; internal set; }

        /// <summary>
        /// Language version of the operating system installed.
        /// </summary>
        public string OsLanguage { get; internal set; }

        /// <summary>
        /// Array of <see cref="OSProductSuite"/> objects indicating installed
        /// and licensed product additions to the operating system.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public OSProductSuite[] OsProductSuites { get; internal set; }

        /// <summary>
        /// Additional description for the current operating system version.
        /// </summary>
        public string OsOtherTypeDescription { get; internal set; }

        /// <summary>
        /// If True, the physical address extensions (PAE) are enabled by the
        /// operating system running on Intel processors.
        /// </summary>
        public bool? OsPAEEnabled { get; internal set; }

        /// <summary>
        /// Specifies whether the operating system booted from an external USB device.
        /// If true, the operating system has detected it is booting on a supported
        /// locally connected storage device.
        /// </summary>
        public bool? OsPortableOperatingSystem { get; internal set; }

        /// <summary>
        /// Specifies whether this is the primary operating system.
        /// </summary>
        public bool? OsPrimary { get; internal set; }

        /// <summary>
        /// Additional system information.
        /// </summary>
        public ProductType? OsProductType { get; internal set; }

        /// <summary>
        /// Name of the registered user of the operating system.
        /// </summary>
        public string OsRegisteredUser { get; internal set; }

        /// <summary>
        /// Operating system product serial identification number.
        /// </summary>
        public string OsSerialNumber { get; internal set; }

        /// <summary>
        /// Major version of the service pack installed on the computer system.
        /// </summary>
        public ushort? OsServicePackMajorVersion { get; internal set; }

        /// <summary>
        /// Minor version of the service pack installed on the computer system.
        /// </summary>
        public ushort? OsServicePackMinorVersion { get; internal set; }

        /// <summary>
        /// Current status.
        /// </summary>
        public string OsStatus { get; internal set; }

        /// <summary>
        /// Product suites available on the operating system.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public OSProductSuite[] OsSuites { get; internal set; }

        /// <summary>
        /// Server level of the operating system, if the operating system is a server.
        /// </summary>
        public ServerLevel? OsServerLevel { get; internal set; }
        #endregion Operating System

        #region Misc Info
        /// <summary>
        /// Layout of the (first) keyboard attached to the system.
        /// </summary>
        public string KeyboardLayout { get; internal set; }

        /// <summary>
        /// Name of the system's current time zone.
        /// </summary>
        public string TimeZone { get; internal set; }

        /// <summary>
        /// Path to the system's logon server.
        /// </summary>
        public string LogonServer { get; internal set; }

        /// <summary>
        /// Power platform role.
        /// </summary>
        public PowerPlatformRole? PowerPlatformRole { get; internal set; }

        /// <summary>
        /// If true, a HyperVisor was detected.
        /// </summary>
        public bool? HyperVisorPresent { get; internal set; }

        /// <summary>
        /// If a HyperVisor is not present, indicates the state of the
        /// requirement that the Data Execution Prevention feature is available.
        /// </summary>
        public bool? HyperVRequirementDataExecutionPreventionAvailable { get; internal set; }

        /// <summary>
        /// If a HyperVisor is not present, indicates the state of the
        /// requirement that the processor supports address translation
        /// extensions used for virtualization.
        /// </summary>
        public bool? HyperVRequirementSecondLevelAddressTranslation { get; internal set; }

        /// <summary>
        /// If a HyperVisor is not present, indicates the state of the
        /// requirement that the firmware has enabled virtualization
        /// extensions.
        /// </summary>
        public bool? HyperVRequirementVirtualizationFirmwareEnabled { get; internal set; }

        /// <summary>
        /// If a HyperVisor is not present, indicates the state of the
        /// requirement that the processor supports Intel or AMD Virtual
        /// Machine Monitor extensions.
        /// </summary>
        public bool? HyperVRequirementVMMonitorModeExtensions { get; internal set; }

        /// <summary>
        /// Indicates the status of the Device Guard features.
        /// </summary>
        public DeviceGuardSmartStatus? DeviceGuardSmartStatus { get; internal set; }

        /// <summary>
        /// Required Device Guard security properties.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardHardwareSecure[] DeviceGuardRequiredSecurityProperties { get; internal set; }

        /// <summary>
        /// Available Device Guard security properties.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardHardwareSecure[] DeviceGuardAvailableSecurityProperties { get; internal set; }

        /// <summary>
        /// Configured Device Guard security services.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardSoftwareSecure[] DeviceGuardSecurityServicesConfigured { get; internal set; }

        /// <summary>
        /// Running Device Guard security services.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public DeviceGuardSoftwareSecure[] DeviceGuardSecurityServicesRunning { get; internal set; }

        /// <summary>
        /// Status of the Device Guard Code Integrity policy enforcement.
        /// </summary>
        public DeviceGuardConfigCodeIntegrityStatus? DeviceGuardCodeIntegrityPolicyEnforcementStatus { get; internal set; }

        /// <summary>
        /// Status of the Device Guard user mode Code Integrity policy enforcement.
        /// </summary>
        public DeviceGuardConfigCodeIntegrityStatus? DeviceGuardUserModeCodeIntegrityPolicyEnforcementStatus { get; internal set; }
        #endregion Misc Info
    }
    #endregion Classes comprising the output object

    #region Enums used in the output objects
    /// <summary>
    /// System hardware security settings for administrator password status.
    /// </summary>
    public enum AdminPasswordStatus
    {
        /// <summary>
        /// Feature is disabled.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Feature is Enabled.
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Feature is not implemented.
        /// </summary>
        NotImplemented = 2,

        /// <summary>
        /// Status is unknown.
        /// </summary>
        Unknown = 3
    }

    /// <summary>
    /// Actions related to the BootOptionOn* properties of the Win32_ComputerSystem
    /// CIM class.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum BootOptionAction
    {
        //  <summary>
        //  This value is reserved
        //  </summary>
        // Reserved = 0,

        /// <summary>
        /// Boot into operating system.
        /// </summary>
        OperatingSystem = 1,

        /// <summary>
        /// Boot into system utilities.
        /// </summary>
        SystemUtilities = 2,

        /// <summary>
        /// Do not reboot.
        /// </summary>
        DoNotReboot = 3
    }

    /// <summary>
    /// Indicates the state of a system element.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum SystemElementState
    {
        /// <summary>
        /// The element state is something other than those in this Enum.
        /// </summary>
        Other = 1,

        /// <summary>
        /// The element state is unknown.
        /// </summary>
        Unknown = 2,

        /// <summary>
        /// The element is in Safe state.
        /// </summary>
        Safe = 3,

        /// <summary>
        /// The element is in Warning state.
        /// </summary>
        Warning = 4,

        /// <summary>
        /// The element is in Critical state.
        /// </summary>
        Critical = 5,

        /// <summary>
        /// The element is in Non-Recoverable state.
        /// </summary>
        NonRecoverable = 6
    }

    /// <summary>
    /// Specifies the processor architecture.
    /// </summary>
    public enum CpuArchitecture
    {
        /// <summary>
        /// Architecture is Intel x86.
        /// </summary>
        x86 = 0,

        /// <summary>
        /// Architecture is MIPS.
        /// </summary>
        MIPs = 1,

        /// <summary>
        /// Architecture is DEC Alpha.
        /// </summary>
        Alpha = 2,

        /// <summary>
        /// Architecture is Motorola PowerPC.
        /// </summary>
        PowerPC = 3,

        /// <summary>
        /// Architecture is ARM.
        /// </summary>
        ARM = 5,

        /// <summary>
        /// Architecture is Itanium-based 64-bit.
        /// </summary>
        ia64 = 6,

        /// <summary>
        /// Architecture is Intel 64-bit.
        /// </summary>
        x64 = 9
    }

    /// <summary>
    /// Specifies a CPU's availability and status.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum CpuAvailability
    {
        /// <summary>
        /// A state other than those specified in CpuAvailability.
        /// </summary>
        Other = 1,

        /// <summary>
        /// Availability status is unknown.
        /// </summary>
        Unknown = 2,

        /// <summary>
        /// The device is running or at full power.
        /// </summary>
        RunningOrFullPower = 3,

        /// <summary>
        /// Device is in a Warning state.
        /// </summary>
        Warning = 4,

        /// <summary>
        /// Availability status is In Test.
        /// </summary>
        InTest = 5,

        /// <summary>
        /// Status is not applicable to this device.
        /// </summary>
        NotApplicable = 6,

        /// <summary>
        /// The device is powered off.
        /// </summary>
        PowerOff = 7,

        /// <summary>
        /// Availability status is Offline.
        /// </summary>
        OffLine = 8,

        /// <summary>
        /// Availability status is Off-Duty.
        /// </summary>
        OffDuty = 9,

        /// <summary>
        /// Availability status is Degraded.
        /// </summary>
        Degraded = 10,

        /// <summary>
        /// Availability status is Not Installed.
        /// </summary>
        NotInstalled = 11,

        /// <summary>
        /// Availability status is Install Error.
        /// </summary>
        InstallError = 12,

        /// <summary>
        /// The device is known to be in a power save state, but its exact status is unknown.
        /// </summary>
        PowerSaveUnknown = 13,

        /// <summary>
        /// The device is in a power save state, but is still functioning,
        /// and may exhibit decreased performance.
        /// </summary>
        PowerSaveLowPowerMode = 14,

        /// <summary>
        /// The device is not functioning, but can be brought to full power quickly.
        /// </summary>
        PowerSaveStandby = 15,

        /// <summary>
        /// The device is in a power-cycle state.
        /// </summary>
        PowerCycle = 16,

        /// <summary>
        /// The device is in a warning state, though also in a power save state.
        /// </summary>
        PowerSaveWarning = 17,

        /// <summary>
        /// The device is paused.
        /// </summary>
        Paused = 18,

        /// <summary>
        /// The device is not ready.
        /// </summary>
        NotReady = 19,

        /// <summary>
        /// The device is not configured.
        /// </summary>
        NotConfigured = 20,

        /// <summary>
        /// The device is quiet.
        /// </summary>
        Quiesced = 21
    }

    /// <summary>
    /// Specifies that current status of the processor.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "The underlying MOF definition is not a bit field.")]
    public enum CpuStatus
    {
        /// <summary>
        /// CPU status is Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// CPU status is Enabled.
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// CPU status is Disabled by User via BIOS Setup.
        /// </summary>
        DisabledByUser = 2,

        /// <summary>
        /// CPU status is Disabled by BIOS.
        /// </summary>
        DisabledByBIOS = 3,

        /// <summary>
        /// CPU is Idle.
        /// </summary>
        Idle = 4,

        // <summary>
        // This value is reserved
        // </summary>
        // Reserved_5 = 5,

        // <summary>
        // This value is reserved
        // </summary>
        // Reserved_6 = 6,

        /// <summary>
        /// CPU is in another state.
        /// </summary>
        Other = 7
    }

    /// <summary>
    /// Data Execution Prevention (DEP) settings.
    /// </summary>
    public enum DataExecutionPreventionSupportPolicy
    {
        // Unknown     = -1,

        /// <summary>
        /// DEP is turned off for all 32-bit applications on the computer with no exceptions.
        /// </summary>
        AlwaysOff = 0,

        /// <summary>
        /// DEP is enabled for all 32-bit applications on the computer.
        /// </summary>
        AlwaysOn = 1,

        /// <summary>
        /// DEP is enabled for a limited number of binaries, the kernel, and all
        /// Windows-based services. However, it is off by default for all 32-bit
        /// applications. A user or administrator must explicitly choose either
        /// the Always On or the Opt Out setting before DEP can be applied to
        /// 32-bit applications.
        /// </summary>
        OptIn = 2,

        /// <summary>
        /// DEP is enabled by default for all 32-bit applications. A user or
        /// administrator can explicitly remove support for a 32-bit
        /// application by adding the application to an exceptions list.
        /// </summary>
        OptOut = 3
    }

    /// <summary>
    /// Status of the Device Guard feature.
    /// </summary>
    public enum DeviceGuardSmartStatus
    {
        /// <summary>
        /// Device Guard is off.
        /// </summary>
        Off = 0,

        /// <summary>
        /// Device Guard is Configured.
        /// </summary>
        Configured = 1,

        /// <summary>
        /// Device Guard is Running.
        /// </summary>
        Running = 2
    }

    /// <summary>
    /// Configuration status of the Device Guard Code Integrity.
    /// </summary>
    public enum DeviceGuardConfigCodeIntegrityStatus
    {
        /// <summary>
        /// Code Integrity is off.
        /// </summary>
        Off = 0,

        /// <summary>
        /// Code Integrity uses Audit mode.
        /// </summary>
        AuditMode = 1,

        /// <summary>
        /// Code Integrity uses Enforcement mode.
        /// </summary>
        EnforcementMode = 2
    }

    /// <summary>
    /// Device Guard hardware security properties.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum DeviceGuardHardwareSecure
    {
        /// <summary>
        /// Base Virtualization Support.
        /// </summary>
        BaseVirtualizationSupport = 1,

        /// <summary>
        /// Secure Boot.
        /// </summary>
        SecureBoot = 2,

        /// <summary>
        /// DMA Protection.
        /// </summary>
        DMAProtection = 3,

        /// <summary>
        /// Secure Memory Overwrite.
        /// </summary>
        SecureMemoryOverwrite = 4
    }

    /// <summary>
    /// Device Guard software security properties.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum DeviceGuardSoftwareSecure
    {
        /// <summary>
        /// Credential Guard.
        /// </summary>
        CredentialGuard = 1,

        /// <summary>
        /// Hypervisor enforced Code Integrity.
        /// </summary>
        HypervisorEnforcedCodeIntegrity = 2
    }

    /// <summary>
    /// Role of a computer in an assigned domain workgroup.
    /// </summary>
    public enum DomainRole
    {
        /// <summary>
        /// Standalone Workstation.
        /// </summary>
        StandaloneWorkstation = 0,

        /// <summary>
        /// Member Workstation.
        /// </summary>
        MemberWorkstation = 1,

        /// <summary>
        /// Standalone Server.
        /// </summary>
        StandaloneServer = 2,

        /// <summary>
        /// Member Server.
        /// </summary>
        MemberServer = 3,

        /// <summary>
        /// Backup Domain Controller.
        /// </summary>
        BackupDomainController = 4,

        /// <summary>
        /// Primary Domain Controller.
        /// </summary>
        PrimaryDomainController = 5
    }

    /// <summary>
    /// Specifies a firmware type.
    /// </summary>
    public enum FirmwareType
    {
        /// <summary>
        /// The firmware type is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The computer booted in legacy BIOS mode.
        /// </summary>
        Bios = 1,

        /// <summary>
        /// The computer booted in UEFI mode.
        /// </summary>
        Uefi = 2,

        /// <summary>
        /// Not implemented.
        /// </summary>
        Max = 3
    }

    /// <summary>
    /// Increase in priority given to the foreground application.
    /// </summary>
    public enum ForegroundApplicationBoost
    {
        /// <summary>
        /// The system boosts the quantum length by 6.
        /// </summary>
        None = 0,

        /// <summary>
        /// The system boosts the quantum length by 12.
        /// </summary>
        Minimum = 1,

        /// <summary>
        /// The system boosts the quantum length by 18.
        /// </summary>
        Maximum = 2
    }

    /// <summary>
    /// Hardware security settings for the reset button on a computer.
    /// </summary>
    public enum FrontPanelResetStatus
    {
        /// <summary>
        /// Reset button is disabled.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Reset button is enabled.
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Hardware security settings are not implement.
        /// </summary>
        NotImplemented = 2,

        /// <summary>
        /// Unknown security setting.
        /// </summary>
        Unknown = 3
    }

    /// <summary>
    /// Indicates a hardware security setting.
    /// </summary>
    public enum HardwareSecurity
    {
        /// <summary>
        /// Hardware security is disabled.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Hardware security is enabled.
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Hardware security is not implemented.
        /// </summary>
        NotImplemented = 2,

        /// <summary>
        /// Hardware security setting is unknown.
        /// </summary>
        Unknown = 3
    }

    /// <summary>
    /// State of the network adapter connection to the network.
    /// </summary>
    public enum NetConnectionStatus
    {
        /// <summary>
        /// Adapter is disconnected.
        /// </summary>
        Disconnected = 0,

        /// <summary>
        /// Adapter is connecting.
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// Adapter is connected.
        /// </summary>
        Connected = 2,

        /// <summary>
        /// Adapter is disconnecting.
        /// </summary>
        Disconnecting = 3,

        /// <summary>
        /// Adapter hardware is not present.
        /// </summary>
        HardwareNotPresent = 4,

        /// <summary>
        /// Adapter hardware is disabled.
        /// </summary>
        HardwareDisabled = 5,

        /// <summary>
        /// Adapter has a hardware malfunction.
        /// </summary>
        HardwareMalfunction = 6,

        /// <summary>
        /// Media is disconnected.
        /// </summary>
        MediaDisconnected = 7,

        /// <summary>
        /// Adapter is authenticating.
        /// </summary>
        Authenticating = 8,

        /// <summary>
        /// Authentication has succeeded.
        /// </summary>
        AuthenticationSucceeded = 9,

        /// <summary>
        /// Authentication has failed.
        /// </summary>
        AuthenticationFailed = 10,

        /// <summary>
        /// Address is invalid.
        /// </summary>
        InvalidAddress = 11,

        /// <summary>
        /// Credentials are required.
        /// </summary>
        CredentialsRequired = 12,

        /// <summary>
        /// Other unspecified state.
        /// </summary>
        Other = 13
    }

    /// <summary>
    /// Encryption level for secure transactions: 40-bit, 128-bit, or n-bit.
    /// </summary>
    public enum OSEncryptionLevel
    {
        /// <summary>
        /// 40-bit encryption.
        /// </summary>
        Encrypt40Bits = 0,

        /// <summary>
        /// 128-bit encryption.
        /// </summary>
        Encrypt128Bits = 1,

        /// <summary>
        /// N-bit encryption.
        /// </summary>
        EncryptNBits = 2
    }

    /// <summary>
    /// Indicates installed and licensed system product additions to the operating system.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    [FlagsAttribute]
    public enum OSProductSuite
    {
        /// <summary>
        /// Microsoft Small Business Server was once installed, but may have
        /// been upgraded to another version of Windows.
        /// </summary>
        SmallBusinessServer = 0x0001,

        /// <summary>
        /// Windows Server 2008 Enterprise is installed.
        /// </summary>
        Server2008Enterprise = 0x0002,

        /// <summary>
        /// Windows BackOffice components are installed.
        /// </summary>
        BackOfficeComponents = 0x0004,

        /// <summary>
        /// Communication Server is installed.
        /// </summary>
        CommunicationsServer = 0x0008,

        /// <summary>
        /// Terminal Services is installed.
        /// </summary>
        TerminalServices = 0x0010,

        /// <summary>
        /// Microsoft Small Business Server is installed with the restrictive
        /// client license.
        /// </summary>
        SmallBusinessServerRestricted = 0x0020,

        /// <summary>
        /// Windows Embedded is installed.
        /// </summary>
        WindowsEmbedded = 0x0040,

        /// <summary>
        /// A Datacenter edition is installed.
        /// </summary>
        DatacenterEdition = 0x0080,

        /// <summary>
        /// Terminal Services is installed, but only one interactive session is supported.
        /// </summary>
        TerminalServicesSingleSession = 0x0100,

        /// <summary>
        /// Windows Home Edition is installed.
        /// </summary>
        HomeEdition = 0x0200,

        /// <summary>
        /// Web Server Edition is installed.
        /// </summary>
        WebServerEdition = 0x0400,

        /// <summary>
        /// Storage Server Edition is installed.
        /// </summary>
        StorageServerEdition = 0x2000,

        /// <summary>
        /// Compute Cluster Edition is installed.
        /// </summary>
        ComputeClusterEdition = 0x4000
    }

    /// <summary>
    /// Indicates the operating system Stock Keeping Unit (SKU)
    /// </summary>
    public enum OperatingSystemSKU
    {
        /// <summary>
        /// The SKU is undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// SKU is Ultimate Edition.
        /// </summary>
        UltimateEdition = 1,

        /// <summary>
        /// SKU is Home Basic Edition.
        /// </summary>
        HomeBasicEdition = 2,

        /// <summary>
        /// SKU is Home Premium Edition.
        /// </summary>
        HomePremiumEdition = 3,

        /// <summary>
        /// SKU is Enterprise Edition.
        /// </summary>
        EnterpriseEdition = 4,

        /// <summary>
        /// SKU is Home Basic N Edition.
        /// </summary>
        HomeBasicNEdition = 5,

        /// <summary>
        /// SKU is Business Edition.
        /// </summary>
        BusinessEdition = 6,

        /// <summary>
        /// SKU is Standard Server Edition.
        /// </summary>
        StandardServerEdition = 7,

        /// <summary>
        /// SKU is Datacenter Server Edition.
        /// </summary>
        DatacenterServerEdition = 8,

        /// <summary>
        /// SKU is Small Business Server Edition.
        /// </summary>
        SmallBusinessServerEdition = 9,

        /// <summary>
        /// SKU is Enterprise Server Edition.
        /// </summary>
        EnterpriseServerEdition = 10,

        /// <summary>
        /// SKU is Starter Edition.
        /// </summary>
        StarterEdition = 11,

        /// <summary>
        /// SKU is Datacenter Server Core Edition.
        /// </summary>
        DatacenterServerCoreEdition = 12,

        /// <summary>
        /// SKU is Standard Server Core Edition.
        /// </summary>
        StandardServerCoreEdition = 13,

        /// <summary>
        /// SKU is Enterprise Server Core Edition.
        /// </summary>
        EnterpriseServerCoreEdition = 14,

        /// <summary>
        /// SKU is Enterprise Server IA64 Edition.
        /// </summary>
        EnterpriseServerIA64Edition = 15,

        /// <summary>
        /// SKU is Business N Edition.
        /// </summary>
        BusinessNEdition = 16,

        /// <summary>
        /// SKU is Web Server Edition.
        /// </summary>
        WebServerEdition = 17,

        /// <summary>
        /// SKU is Cluster Server Edition.
        /// </summary>
        ClusterServerEdition = 18,

        /// <summary>
        /// SKU is Home Server Edition.
        /// </summary>
        HomeServerEdition = 19,

        /// <summary>
        /// SKU is Storage Express Server Edition.
        /// </summary>
        StorageExpressServerEdition = 20,

        /// <summary>
        /// SKU is Storage Standard Server Edition.
        /// </summary>
        StorageStandardServerEdition = 21,

        /// <summary>
        /// SKU is Storage Workgroup Server Edition.
        /// </summary>
        StorageWorkgroupServerEdition = 22,

        /// <summary>
        /// SKU is Storage Enterprise Server Edition.
        /// </summary>
        StorageEnterpriseServerEdition = 23,

        /// <summary>
        /// SKU is Server For Small Business Edition.
        /// </summary>
        ServerForSmallBusinessEdition = 24,

        /// <summary>
        /// SKU is Small Business Server Premium Edition.
        /// </summary>
        SmallBusinessServerPremiumEdition = 25,

        /// <summary>
        /// SKU is to be determined.
        /// </summary>
        TBD = 26,

        /// <summary>
        /// SKU is Windows Enterprise.
        /// </summary>
        WindowsEnterprise = 27,

        /// <summary>
        /// SKU is Windows Ultimate.
        /// </summary>
        WindowsUltimate = 28,

        /// <summary>
        /// SKU is Web Server (core installation)
        /// </summary>
        WebServerCore = 29,

        /// <summary>
        /// SKU is Server Foundation.
        /// </summary>
        ServerFoundation = 33,

        /// <summary>
        /// SKU is Windows Home Server.
        /// </summary>
        WindowsHomeServer = 34,

        /// <summary>
        /// SKU is Windows Server Standard without Hyper-V.
        /// </summary>
        WindowsServerStandardNoHyperVFull = 36,

        /// <summary>
        /// SKU is Windows Server Datacenter without Hyper-V (full installation)
        /// </summary>
        WindowsServerDatacenterNoHyperVFull = 37,

        /// <summary>
        /// SKU is Windows Server Enterprise without Hyper-V (full installation)
        /// </summary>
        WindowsServerEnterpriseNoHyperVFull = 38,

        /// <summary>
        /// SKU is Windows Server Datacenter without Hyper-V (core installation)
        /// </summary>
        WindowsServerDatacenterNoHyperVCore = 39,

        /// <summary>
        /// SKU is Windows Server Standard without Hyper-V (core installation)
        /// </summary>
        WindowsServerStandardNoHyperVCore = 40,

        /// <summary>
        /// SKU is Windows Server Enterprise without Hyper-V (core installation)
        /// </summary>
        WindowsServerEnterpriseNoHyperVCore = 41,

        /// <summary>
        /// SKU is Microsoft Hyper-V Server.
        /// </summary>
        MicrosoftHyperVServer = 42,

        /// <summary>
        /// SKU is Storage Server Express (core installation)
        /// </summary>
        StorageServerExpressCore = 43,

        /// <summary>
        /// SKU is Storage Server Standard (core installation)
        /// </summary>
        StorageServerStandardCore = 44,

        /// <summary>
        /// SKU is Storage Server Workgroup (core installation)
        /// </summary>
        StorageServerWorkgroupCore = 45,

        /// <summary>
        /// SKU is Storage Server Enterprise (core installation)
        /// </summary>
        StorageServerEnterpriseCore = 46,

        /// <summary>
        /// SKU is Windows Small Business Server 2011 Essentials.
        /// </summary>
        WindowsSmallBusinessServer2011Essentials = 50,

        /// <summary>
        /// SKU is Small Business Server Premium (core installation)
        /// </summary>
        SmallBusinessServerPremiumCore = 63,

        /// <summary>
        /// SKU is Windows Server Hyper Core V.
        /// </summary>
        WindowsServerHyperCoreV = 64,

        /// <summary>
        /// SKU is Windows Thin PC.
        /// </summary>
        WindowsThinPC = 87,

        /// <summary>
        /// SKU is Windows Embedded Industry.
        /// </summary>
        WindowsEmbeddedIndustry = 89,

        /// <summary>
        /// SKU is Windows RT.
        /// </summary>
        WindowsRT = 97,

        /// <summary>
        /// SKU is Windows Home.
        /// </summary>
        WindowsHome = 101,

        /// <summary>
        /// SKU is Windows Professional with Media Center.
        /// </summary>
        WindowsProfessionalWithMediaCenter = 103,

        /// <summary>
        /// SKU is Windows Mobile.
        /// </summary>
        WindowsMobile = 104,

        /// <summary>
        /// SKU is Windows Embedded Handheld.
        /// </summary>
        WindowsEmbeddedHandheld = 118,

        /// <summary>
        /// SKU is Windows IoT (Internet of Things) Core.
        /// </summary>
        WindowsIotCore = 123
    }

    /// <summary>
    /// Type of operating system.
    /// </summary>
    public enum OSType
    {
        /// <summary>
        /// OS is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// OS is one other than covered by this Enum.
        /// </summary>
        Other = 1,

        /// <summary>
        /// OS is MacOS.
        /// </summary>
        MACROS = 2,

        /// <summary>
        /// OS is AT&amp;T UNIX.
        /// </summary>
        ATTUNIX = 3,

        /// <summary>
        /// OS is DG/UX.
        /// </summary>
        DGUX = 4,

        /// <summary>
        /// OS is DECNT.
        /// </summary>
        DECNT = 5,

        /// <summary>
        /// OS is Digital UNIX.
        /// </summary>
        DigitalUNIX = 6,

        /// <summary>
        /// OS is OpenVMS.
        /// </summary>
        OpenVMS = 7,

        /// <summary>
        /// OS is HP-UX.
        /// </summary>
        HPUX = 8,

        /// <summary>
        /// OS is AIX.
        /// </summary>
        AIX = 9,

        /// <summary>
        /// OS is MVS.
        /// </summary>
        MVS = 10,

        /// <summary>
        /// OS is OS/400.
        /// </summary>
        OS400 = 11,

        /// <summary>
        /// OS is OS/2.
        /// </summary>
        OS2 = 12,

        /// <summary>
        /// OS is Java Virtual Machine.
        /// </summary>
        JavaVM = 13,

        /// <summary>
        /// OS is MS-DOS.
        /// </summary>
        MSDOS = 14,

        /// <summary>
        /// OS is Windows 3x.
        /// </summary>
        WIN3x = 15,

        /// <summary>
        /// OS is Windows 95.
        /// </summary>
        WIN95 = 16,

        /// <summary>
        /// OS is Windows 98.
        /// </summary>
        WIN98 = 17,

        /// <summary>
        /// OS is Windows NT.
        /// </summary>
        WINNT = 18,

        /// <summary>
        /// OS is Windows CE.
        /// </summary>
        WINCE = 19,

        /// <summary>
        /// OS is NCR System 3000.
        /// </summary>
        NCR3000 = 20,

        /// <summary>
        /// OS is NetWare.
        /// </summary>
        NetWare = 21,

        /// <summary>
        /// OS is OSF.
        /// </summary>
        OSF = 22,

        /// <summary>
        /// OS is DC/OS.
        /// </summary>
        DC_OS = 23,

        /// <summary>
        /// OS is Reliant UNIX.
        /// </summary>
        ReliantUNIX = 24,

        /// <summary>
        /// OS is SCO UnixWare.
        /// </summary>
        SCOUnixWare = 25,

        /// <summary>
        /// OS is SCO OpenServer.
        /// </summary>
        SCOOpenServer = 26,

        /// <summary>
        /// OS is Sequent.
        /// </summary>
        Sequent = 27,

        /// <summary>
        /// OS is IRIX.
        /// </summary>
        IRIX = 28,

        /// <summary>
        /// OS is Solaris.
        /// </summary>
        Solaris = 29,

        /// <summary>
        /// OS is SunOS.
        /// </summary>
        SunOS = 30,

        /// <summary>
        /// OS is U6000.
        /// </summary>
        U6000 = 31,

        /// <summary>
        /// OS is ASERIES.
        /// </summary>
        ASERIES = 32,

        /// <summary>
        /// OS is Tandem NSK.
        /// </summary>
        TandemNSK = 33,

        /// <summary>
        /// OS is Tandem NT.
        /// </summary>
        TandemNT = 34,

        /// <summary>
        /// OS is BS2000.
        /// </summary>
        BS2000 = 35,

        /// <summary>
        /// OS is Linux.
        /// </summary>
        LINUX = 36,

        /// <summary>
        /// OS is Lynx.
        /// </summary>
        Lynx = 37,

        /// <summary>
        /// OS is XENIX.
        /// </summary>
        XENIX = 38,

        /// <summary>
        /// OS is VM/ESA.
        /// </summary>
        VM_ESA = 39,

        /// <summary>
        /// OS is Interactive UNIX.
        /// </summary>
        InteractiveUNIX = 40,

        /// <summary>
        /// OS is BSD UNIX.
        /// </summary>
        BSDUNIX = 41,

        /// <summary>
        /// OS is FreeBSD.
        /// </summary>
        FreeBSD = 42,

        /// <summary>
        /// OS is NetBSD.
        /// </summary>
        NetBSD = 43,

        /// <summary>
        /// OS is GNU Hurd.
        /// </summary>
        GNUHurd = 44,

        /// <summary>
        /// OS is OS 9.
        /// </summary>
        OS9 = 45,

        /// <summary>
        /// OS is Mach Kernel.
        /// </summary>
        MACHKernel = 46,

        /// <summary>
        /// OS is Inferno.
        /// </summary>
        Inferno = 47,

        /// <summary>
        /// OS is QNX.
        /// </summary>
        QNX = 48,

        /// <summary>
        /// OS is EPOC.
        /// </summary>
        EPOC = 49,

        /// <summary>
        /// OS is IxWorks.
        /// </summary>
        IxWorks = 50,

        /// <summary>
        /// OS is VxWorks.
        /// </summary>
        VxWorks = 51,

        /// <summary>
        /// OS is MiNT.
        /// </summary>
        MiNT = 52,

        /// <summary>
        /// OS is BeOS.
        /// </summary>
        BeOS = 53,

        /// <summary>
        /// OS is HP MPE.
        /// </summary>
        HP_MPE = 54,

        /// <summary>
        /// OS is NextStep.
        /// </summary>
        NextStep = 55,

        /// <summary>
        /// OS is PalmPilot.
        /// </summary>
        PalmPilot = 56,

        /// <summary>
        /// OS is Rhapsody.
        /// </summary>
        Rhapsody = 57,

        /// <summary>
        /// OS is Windows 2000.
        /// </summary>
        Windows2000 = 58,

        /// <summary>
        /// OS is Dedicated.
        /// </summary>
        Dedicated = 59,

        /// <summary>
        /// OS is OS/390.
        /// </summary>
        OS_390 = 60,

        /// <summary>
        /// OS is VSE.
        /// </summary>
        VSE = 61,

        /// <summary>
        /// OS is TPF.
        /// </summary>
        TPF = 62
    }

    /// <summary>
    /// Specifies the type of the computer in use, such as laptop, desktop, or Tablet.
    /// </summary>
    public enum PCSystemType
    {
        /// <summary>
        /// System type is unspecified.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// System is a desktop.
        /// </summary>
        Desktop = 1,

        /// <summary>
        /// System is a mobile device.
        /// </summary>
        Mobile = 2,

        /// <summary>
        /// System is a workstation.
        /// </summary>
        Workstation = 3,

        /// <summary>
        /// System is an Enterprise Server.
        /// </summary>
        EnterpriseServer = 4,

        /// <summary>
        /// System is a Small Office and Home Office (SOHO) Server.
        /// </summary>
        SOHOServer = 5,

        /// <summary>
        /// System is an appliance PC.
        /// </summary>
        AppliancePC = 6,

        /// <summary>
        /// System is a performance server.
        /// </summary>
        PerformanceServer = 7,

        /// <summary>
        /// Maximum enum value.
        /// </summary>
        Maximum = 8
    }

    /// <summary>
    /// Specifies the type of the computer in use, such as laptop, desktop, or Tablet.
    /// This is an extended version of PCSystemType.
    /// </summary>
    // TODO: conflate these two enums???
    public enum PCSystemTypeEx
    {
        /// <summary>
        /// System type is unspecified.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// System is a desktop.
        /// </summary>
        Desktop = 1,

        /// <summary>
        /// System is a mobile device.
        /// </summary>
        Mobile = 2,

        /// <summary>
        /// System is a workstation.
        /// </summary>
        Workstation = 3,

        /// <summary>
        /// System is an Enterprise Server.
        /// </summary>
        EnterpriseServer = 4,

        /// <summary>
        /// System is a Small Office and Home Office (SOHO) Server.
        /// </summary>
        SOHOServer = 5,

        /// <summary>
        /// System is an appliance PC.
        /// </summary>
        AppliancePC = 6,

        /// <summary>
        /// System is a performance server.
        /// </summary>
        PerformanceServer = 7,

        /// <summary>
        /// System is a Slate.
        /// </summary>
        Slate = 8,

        /// <summary>
        /// Maximum enum value.
        /// </summary>
        Maximum = 9
    }

    /// <summary>
    /// Specifies power-related capabilities of a logical device.
    /// </summary>
    public enum PowerManagementCapabilities
    {
        /// <summary>
        /// Unknown capability.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Power management not supported.
        /// </summary>
        NotSupported = 1,

        /// <summary>
        /// Power management features are currently disabled.
        /// </summary>
        Disabled = 2,

        /// <summary>
        /// The power management features are currently enabled,
        /// but the exact feature set is unknown or the information is unavailable.
        /// </summary>
        Enabled = 3,

        /// <summary>
        /// The device can change its power state based on usage or other criteria.
        /// </summary>
        PowerSavingModesEnteredAutomatically = 4,

        /// <summary>
        /// The power state may be set through the Win32_LogicalDevice class.
        /// </summary>
        PowerStateSettable = 5,

        /// <summary>
        /// Power may be done through the Win32_LogicalDevice class.
        /// </summary>
        PowerCyclingSupported = 6,

        /// <summary>
        /// Timed power-on is supported.
        /// </summary>
        TimedPowerOnSupported = 7
    }

    /// <summary>
    /// Specified power states.
    /// </summary>
    public enum PowerState
    {
        /// <summary>
        /// Power state is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Full power.
        /// </summary>
        FullPower = 1,

        /// <summary>
        /// Power Save - Low Power mode.
        /// </summary>
        PowerSaveLowPowerMode = 2,

        /// <summary>
        /// Power Save - Standby.
        /// </summary>
        PowerSaveStandby = 3,

        /// <summary>
        /// Unknown Power Save mode.
        /// </summary>
        PowerSaveUnknown = 4,

        /// <summary>
        /// Power Cycle.
        /// </summary>
        PowerCycle = 5,

        /// <summary>
        /// Power Off.
        /// </summary>
        PowerOff = 6,

        /// <summary>
        /// Power Save - Warning.
        /// </summary>
        PowerSaveWarning = 7,

        /// <summary>
        /// Power Save - Hibernate.
        /// </summary>
        PowerSaveHibernate = 8,

        /// <summary>
        /// Power Save - Soft off.
        /// </summary>
        PowerSaveSoftOff = 9
    }

    /// <summary>
    /// Specifies the primary function of a processor.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum ProcessorType
    {
        /// <summary>
        /// Processor ype is other than provided in these enumeration values.
        /// </summary>
        Other = 1,

        /// <summary>
        /// Processor type is.
        /// </summary>
        Unknown = 2,

        /// <summary>
        /// Processor is a Central Processing Unit (CPU)
        /// </summary>
        CentralProcessor = 3,

        /// <summary>
        /// Processor is a Math processor.
        /// </summary>
        MathProcessor = 4,

        /// <summary>
        /// Processor is a Digital Signal processor (DSP)
        /// </summary>
        DSPProcessor = 5,

        /// <summary>
        /// Processor is a Video processor.
        /// </summary>
        VideoProcessor = 6
    }

    /// <summary>
    /// Specifies a computer's reset capability.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum ResetCapability
    {
        /// <summary>
        /// Capability is a value other than provided in these enumerated values.
        /// </summary>
        Other = 1,

        /// <summary>
        /// Reset capability is unknown.
        /// </summary>
        Unknown = 2,

        /// <summary>
        /// Capability is disabled.
        /// </summary>
        Disabled = 3,

        /// <summary>
        /// Capability is enabled.
        /// </summary>
        Enabled = 4,

        /// <summary>
        /// Capability is not implemented.
        /// </summary>
        NotImplemented = 5
    }

    /// <summary>
    /// Specifies the kind of event that causes a computer to power up.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "The underlying MOF definition does not contain a zero value. The converter method will handle it appropriately.")]
    public enum WakeUpType
    {
        // <summary>
        // This value is reserved
        // </summary>
        // Reserved = 0,

        /// <summary>
        /// An event other than specified in this enumeration.
        /// </summary>
        Other = 1,

        /// <summary>
        /// Event type is unknown.
        /// </summary>
        Unknown = 2,

        /// <summary>
        /// Event is APM timer.
        /// </summary>
        APMTimer = 3,

        /// <summary>
        /// Event is a Modem Ring.
        /// </summary>
        ModemRing = 4,

        /// <summary>
        /// Event is a LAN Remove.
        /// </summary>
        LANRemote = 5,

        /// <summary>
        /// Event is a power switch.
        /// </summary>
        PowerSwitch = 6,

        /// <summary>
        /// Event is a PCI PME# signal.
        /// </summary>
        PCIPME = 7,

        /// <summary>
        /// AC power was restored.
        /// </summary>
        ACPowerRestored = 8
    }

    /// <summary>
    /// Indicates the OEM's preferred power management profile.
    /// </summary>
    public enum PowerPlatformRole
    {
        /// <summary>
        /// The OEM did not specify a specific role.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// The OEM specified a desktop role.
        /// </summary>
        Desktop = 1,

        /// <summary>
        /// The OEM specified a mobile role (for example, a laptop)
        /// </summary>
        Mobile = 2,

        /// <summary>
        /// The OEM specified a workstation role.
        /// </summary>
        Workstation = 3,

        /// <summary>
        /// The OEM specified an enterprise server role.
        /// </summary>
        EnterpriseServer = 4,

        /// <summary>
        /// The OEM specified a single office/home office (SOHO) server role.
        /// </summary>
        SOHOServer = 5,

        /// <summary>
        /// The OEM specified an appliance PC role.
        /// </summary>
        AppliancePC = 6,

        /// <summary>
        /// The OEM specified a performance server role.
        /// </summary>
        PerformanceServer = 7,    // v1 last supported

        /// <summary>
        /// The OEM specified a tablet form factor role.
        /// </summary>
        Slate = 8,    // v2 last supported

        /// <summary>
        /// Max enum value.
        /// </summary>
        MaximumEnumValue
    }

    /// <summary>
    /// Additional system information, from Win32_OperatingSystem.
    /// </summary>
    public enum ProductType
    {
        /// <summary>
        /// Product type is unknown.
        /// </summary>
        Unknown = 0,    // this value is not specified in Win32_OperatingSystem, but may prove useful

        /// <summary>
        /// System is a workstation.
        /// </summary>
        WorkStation = 1,

        /// <summary>
        /// System is a domain controller.
        /// </summary>
        DomainController = 2,

        /// <summary>
        /// System is a server.
        /// </summary>
        Server = 3
    }

    /// <summary>
    /// Specifies the system server level.
    /// </summary>
    public enum ServerLevel
    {
        /// <summary>
        /// An unknown or unrecognized level was detected.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Nano server.
        /// </summary>
        NanoServer,

        /// <summary>
        /// Server core.
        /// </summary>
        ServerCore,

        /// <summary>
        /// Server core with management tools.
        /// </summary>
        ServerCoreWithManagementTools,

        /// <summary>
        /// Full server.
        /// </summary>
        FullServer
    }

    /// <summary>
    /// State of a software element.
    /// </summary>
    public enum SoftwareElementState
    {
        /// <summary>
        /// Software element is deployable.
        /// </summary>
        Deployable = 0,

        /// <summary>
        /// Software element is installable.
        /// </summary>
        Installable = 1,

        /// <summary>
        /// Software element is executable.
        /// </summary>
        Executable = 2,

        /// <summary>
        /// Software element is running.
        /// </summary>
        Running = 3
    }
    #endregion Enums used in the output objects
    #endregion Output components

    #region Native
    internal static class Native
    {
        private static class PInvokeDllNames
        {
            public const string GetPhysicallyInstalledSystemMemoryDllName = "api-ms-win-core-sysinfo-l1-2-1.dll";
            public const string PowerDeterminePlatformRoleExDllName = "api-ms-win-power-base-l1-1-0.dll";
            public const string GetFirmwareTypeDllName = "api-ms-win-core-kernel32-legacy-l1-1-1";
        }

        public const int LOCALE_NAME_MAX_LENGTH = 85;
        public const uint POWER_PLATFORM_ROLE_V1 = 0x1;
        public const uint POWER_PLATFORM_ROLE_V2 = 0x2;

        public const UInt32 S_OK = 0;

        /// <summary>
        /// Import WINAPI function PowerDeterminePlatformRoleEx.
        /// </summary>
        /// <param name="version">The version of the POWER_PLATFORM_ROLE enumeration for the platform.</param>
        /// <returns>POWER_PLATFORM_ROLE enumeration.</returns>
        [DllImport(PInvokeDllNames.PowerDeterminePlatformRoleExDllName, EntryPoint = "PowerDeterminePlatformRoleEx", CharSet = CharSet.Ansi)]
        public static extern uint PowerDeterminePlatformRoleEx(uint version);

        /// <summary>
        /// Retrieve the amount of RAM physically installed in the computer.
        /// </summary>
        /// <param name="MemoryInKilobytes"></param>
        /// <returns></returns>
        [DllImport(PInvokeDllNames.GetPhysicallyInstalledSystemMemoryDllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPhysicallyInstalledSystemMemory(out ulong MemoryInKilobytes);

        /// <summary>
        /// Retrieve the firmware type of the local computer.
        /// </summary>
        /// <param name="firmwareType">
        /// A reference to a <see cref="FirmwareType"/> enumeration to contain
        /// the resultant firmware type
        /// </param>
        /// <returns></returns>
        [DllImport(PInvokeDllNames.GetFirmwareTypeDllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFirmwareType(out FirmwareType firmwareType);

        /// <summary>
        /// Gets the data specified for the passed in property name from the
        /// Software Licensing API.
        /// </summary>
        /// <param name="licenseProperty">Name of the licensing property to get.</param>
        /// <param name="propertyValue">Out parameter for the value.</param>
        /// <returns>An hresult indicating success or failure.</returns>
        [DllImport("slc.dll", CharSet = CharSet.Unicode)]
        internal static extern int SLGetWindowsInformationDWORD(string licenseProperty, out int propertyValue);
    }
    #endregion Native
}

#endif
