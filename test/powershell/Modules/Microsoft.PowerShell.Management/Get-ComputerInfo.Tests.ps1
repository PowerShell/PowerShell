#
# TEST SPECIFIC HELPER METHODS FOR TESTING Get-ComputerInfo cmdlet
#

$computerInfoAll > $null

function Get-ComputerInfoForTest
{
    param([string[]] $properties = $null, [bool] $forceRefresh = $false) # NOTE: $forceRefresh only applies to the case where $properties is null
    
    $computerInfo = $null >$null # RETURN VALUE
    if ( $properties )
    {
        return Get-ComputerInfo -Property $properties
    }
    else
    {
        if ( $forceRefresh -or $script:computerInfoAll -eq $null)
        {
            $script:computerInfoAll = Get-ComputerInfo
        }
        return $script:computerInfoAll
    }
}

function Get-PropertyNamesForComputerInfoTest
{
    $propertyNames = @()

    $propertyNames += @("BiosBIOSVersion",
        "BiosBuildNumber",
        "BiosCaption",
        "BiosCharacteristics",
        "BiosCodeSet",
        "BiosCurrentLanguage",
        "BiosDescription",
        "BiosEmbeddedControllerMajorVersion",
        "BiosEmbeddedControllerMinorVersion",
        "BiosFirmwareType",
        "BiosIdentificationCode",
        "BiosInstallableLanguages",
        "BiosInstallDate",
        "BiosLanguageEdition",
        "BiosListOfLanguages",
        "BiosManufacturer",
        "BiosName",
        "BiosOtherTargetOS",
        "BiosPrimaryBIOS",
        "BiosReleaseDate",
        "BiosSeralNumber",
        "BiosSMBIOSBIOSVersion",
        "BiosSMBIOSPresent",
        "BiosSMBIOSMajorVersion",
        "BiosSMBIOSMinorVersion",
        "BiosSoftwareElementState",
        "BiosStatus",
        "BiosTargetOperatingSystem",
        "BiosVersion")

    $propertyNames += @("CsAdminPasswordStatus",
        "CsAutomaticManagedPagefile",
        "CsAutomaticResetBootOption",
        "CsAutomaticResetCapability",
        "CsBootOptionOnLimit",
        "CsBootOptionOnWatchDog",
        "CsBootROMSupported",
        "CsBootStatus",
        "CsBootupState",
        "CsCaption",
        "CsChassisBootupState",
        "CsChassisSKUNumber",
        "CsCurrentTimeZone",
        "CsDaylightInEffect",
        "CsDescription",
        "CsDNSHostName",
        "CsDomain",
        "CsDomainRole",
        "CsEnableDaylightSavingsTime",
        "CsFrontPanelResetStatus",
        "CsHypervisorPresent",
        "CsInfraredSupported",
        "CsInitialLoadInfo",
        "CsInstallDate",
        "CsKeyboardPasswordStatus",
        "CsLastLoadInfo",
        "CsManufacturer",
        "CsModel",
        "CsName",
        "CsNetworkAdapters",
        "CsNetworkServerModeEnabled",
        "CsNumberOfLogicalProcessors",
        "CsNumberOfProcessors",
        "CsOEMStringArray",
        "CsPartOfDomain",
        "CsPauseAfterReset",
        "CsPCSystemType",
        "CsPCSystemTypeEx",
        "CsPhyicallyInstalledMemory",
        "CsPowerManagementCapabilities",
        "CsPowerManagementSupported",
        "CsPowerOnPasswordStatus",
        "CsPowerState",
        "CsPowerSupplyState",
        "CsPrimaryOwnerContact",
        "CsPrimaryOwnerName",
        "CsProcessors",
        "CsResetCapability",
        "CsResetCount",
        "CsResetLimit",
        "CsRoles",
        "CsStatus",
        "CsSupportContactDescription",
        "CsSystemFamily",
        "CsSystemSKUNumber",
        "CsSystemType",
        "CsThermalState",
        "CsTotalPhysicalMemory",
        "CsUserName",
        "CsWakeUpType",
        "CsWorkgroup")

    $propertyNames += @("DeviceGuardAvailableSecurityProperties",
        "DeviceGuardCodeIntegrityPolicyEnforcementStatus",
        "DeviceGuardRequiredSecurityProperties",
        "DeviceGuardSecurityServicesConfigured",
        "DeviceGuardSecurityServicesRunning",
        "DeviceGuardSmartStatus",
        "DeviceGuardUserModeCodeIntegrityPolicyEnforcementStatus")

    $propertyNames += @("HyperVisorPresent",
        "HyperVRequirementDataExecutionPreventionAvailable",
        "HyperVRequirementSecondLevelAddressTranslation",
        "HyperVRequirementVirtualizationFirmwareEnabled",
        "HyperVRequirementVMMonitorModeExtensions")

    $propertyNames += @("OsArchitecture",
        "OsBootDevice",
        "OsBuildNumber",
        "OsBuildType",
        "OsCodeSet",
        "OsCountryCode",
        "OsCSDVersion",
        "OsCurrentTimeZone",
        "OsDataExecutionPrevention32BitApplications",
        "OsDataExecutionPreventionAvailable",
        "OsDataExecutionPreventionDrivers",
        "OsDataExecutionPreventionSupportPolicy",
        "OsDebug",
        "OsDistributed",
        "OsEncryptionLevel",
        "OsForegroundApplicationBoost",
        "OsHardwareAbstractionLayer",
        "OsHotFixes",
        "OsInstallDate",
        "OsLanguage",
        "OsLastBootUpTime",
        "OsLocale",
        "OsLocaleID",
        "OsManufacturer",
        "OsMaxProcessMemorySize",
        "OsMuiLanguages",
        "OsName",
        "OsNumberOfLicensedUsers",
        "OsNumberOfUsers",
        "OsOperatingSystemSKU",
        "OsOrganization",
        "OsOtherTypeDescription",
        "OsPAEEnabled",
        "OsPagingFiles",
        "OsPortableOperatingSystem",
        "OsPrimary",
        "OsProductSuites",
        "OsProductType",
        "OsRegisteredUser",
        "OsSerialNumber",
        "OsServerLevel",
        "OsServicePackMajorVersion",
        "OsServicePackMinorVersion",
        "OsSizeStoredInPagingFiles",
        "OsStatus",
        "OsSuites",
        "OsSystemDevice",
        "OsSystemDirectory",
        "OsSystemDrive",
        "OsTotalSwapSpaceSize",
        "OsTotalVirtualMemorySize",
        "OsTotalVisibleMemorySize",
        "OsType",
        "OsVersion",
        "OsWindowsDirectory")

    $propertyNames += @("KeyboardLayout",
        "LogonServer",
        "PowerPlatformRole",
        "TimeZone")

    $WindowsPropertyArray = @("WindowsBuildLabEx",
        "WindowsCurrentVersion",
        "WindowsEditionId",
        "WindowsInstallationType",
        "WindowsProductId",
        "WindowsProductName",
        "WindowsRegisteredOrganization",
        "WindowsRegisteredOwner",
        "WindowsSystemRoot",
        "WindowsVersion")
   
    if ([System.Management.Automation.Platform]::IsIoT)
    {
        Write-Verbose -Verbose -Message "WindowsInstallDateFromRegistry is not supported on IoT."
    }
    else
    {
        $WindowsPropertyArray += "WindowsInstallDateFromRegistry"
    }

    $propertyNames += $WindowsPropertyArray
    
    return $propertyNames
}

function New-ExpectedComputerInfo
{
    param([string[]]$propertyNames)

    # P-INVOKE TYPE DEF START ******************************************
    function Get-FirmwareType
    {
$signature = @"
[DllImport("kernel32.dll")]
public static extern bool GetFirmwareType(ref uint firmwareType);
"@
        Add-Type -MemberDefinition $signature -Name "Win32BiosFirmwareType" -Namespace Win32Functions -PassThru
    }

    function Get-PhysicallyInstalledSystemMemory
    {
$signature = @"
[DllImport("kernel32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
public static extern bool GetPhysicallyInstalledSystemMemory(out ulong MemoryInKilobytes);
"@
        Add-Type -MemberDefinition $signature -Name "Win32PhyicallyInstalledMemory" -Namespace Win32Functions -PassThru
    }

    function Get-PhysicallyInstalledSystemMemoryCore
    {
$signature = @"
[DllImport("api-ms-win-core-sysinfo-l1-2-1.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
public static extern bool GetPhysicallyInstalledSystemMemory(out ulong MemoryInKilobytes);
"@
        Add-Type -MemberDefinition $signature -Name "Win32PhyicallyInstalledMemory" -Namespace Win32Functions -PassThru
    }            

    function Get-PowerDeterminePlatformRole
    {
$signature = @"
[DllImport("Powrprof", EntryPoint = "PowerDeterminePlatformRoleEx", CharSet = CharSet.Ansi)]
public static extern uint PowerDeterminePlatformRoleEx(uint version);
"@
        Add-Type -MemberDefinition $signature -Name "Win32PowerDeterminePlatformRole" -Namespace Win32Functions -PassThru
    }

    function Get-LCIDToLocaleName
    {
$signature = @"
[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
public static extern int LCIDToLocaleName(uint localeID, System.Text.StringBuilder localeName, int localeNameSize, int flags); 
"@
        Add-Type -MemberDefinition $signature -Name "Win32LCIDToLocaleNameDllName" -Namespace Win32Functions -PassThru
    }

    # P-INVOKE TYPE DEF END ******************************************

    # HELPER METHODS RELYING ON P-INVOKE BEGIN ******************************************
    function Get-BiosFirmwareType
    {
        [int]$firmwareType = 0
        (Get-FirmwareType)::GetFirmwareType([ref]$firmwareType)
        return $firmwareType
    }

    function Get-CsPhysicallyInstalledSystemMemory
    {
        param([bool]$isCore = $false)

        # TODO: we need to add support for tests running on core
        #   but for now, test is for non-core
        [int] $memoryInKilobytes = 0
        if ($isCore)
        {
            (Get-PhysicallyInstalledSystemMemoryCore)::GetPhysicallyInstalledSystemMemory([ref]$memoryInKilobytes)
        }
        else
        {
            (Get-PhysicallyInstalledSystemMemory)::GetPhysicallyInstalledSystemMemory([ref]$memoryInKilobytes)
        }
            
        return $memoryInKilobytes
    }

    function Get-PowerPlatformRole
    {
        $version = 0x2
        $powerRole = (Get-PowerDeterminePlatformRole)::PowerDeterminePlatformRoleEx($version)
        if ($powerRole -gt 9)
        {
            $powerRole = 0
        }
        return $powerRole
    }
    # HELPER METHODS RELYING ON P-INVOKE END ******************************************

    $cimClassList = @{}
    function Get-CimClass
    {
        param([string]$className, [string] $namespace = "root\cimv2")
            
        if (-not $cimClassList.ContainsKey($className))
        {
            $cimClassInstance = Get-CimInstance -ClassName $className -Namespace $namespace
            $cimClassList.Add($className, $cimClassInstance)
        }

        return $cimClassList.Get_Item($className)
    }

    function Get-CimClassPropVal
    {
        param([string]$className, [string]$propertyName, [string] $namespace = "root\cimv2")

        $cimClassInstance = Get-CimClass $className $namespace
        $cimClassInstance.$propertyName
    }

    function Get-DeviceGuard
    {
        param([string]$propertyName)
        $returnValue = $null
        try
        {
            $returnValue = Get-CimClassPropVal Win32_DeviceGuard $propertyName -namespace 'root\Microsoft\Windows\DeviceGuard' -ErrorAction Stop
        }
        catch
        {
            #swallow this
        }
        if (($propertyName -eq 'SmartStatus') -and ($returnValue -eq $null)){
            $returnValue = 0
        }
        return $returnValue
    }

    function Get-CsNetworkAdapters
    {
        $networkAdapters = @()

        $adapters = Get-CimClass Win32_NetworkAdapter
        $configs = Get-CimClass Win32_NetworkAdapterConfiguration
        # easy-out: no adapters or configs
        if (!$adapters -or !$configs) { return $null }
            
        # build config hashtable
        $configHash = @{} 
        foreach ($config in $configs)
        {
            if ($config.Index -ne $null)
            {
                $configHash.Add([string]$config.Index,$config)
            }
        }
        # easy-out: no config hash items
        if ($configHash.Count -eq 0)  { return $null }

        foreach ($adapter in $adapters)
        {
            # Easy skip: adapters that have a null connection status or null index
            if (!$adapter.NetConnectionStatus) { continue }

            # Easy skip: configHash does not contain adapter
            if (!$configHash.ContainsKey([string]$adapter.Index))  { continue }

            $connectionStatus = 13 # default NetConnectionStatus.Other
            if ($adapter.NetConnectionStatus) { $connectionStatus = $adapter.NetConnectionStatus}
                

            $config =$configHash.Item([string]$adapter.Index)

            $dHCPEnabled = $null
            $dHCPServer = $null
            $ipAddresses = $null
            if ($connectionStatus -eq 2) # 2 = NetConnectionStatus.Connected
            {
                $dHCPEnabled = $config.DHCPEnabled
                $dHCPServer = $config.DHCPServer;
                $ipAddresses = $config.IPAddress;
            }

            # new-up one adapter object
            $properties = 
                @{
                    'Description'=$adapter.Description;
                    'ConnectionID'=$adapter.NetConnectionID;
                    'ConnectionStatus' = $connectionStatus;
                    'DHCPEnabled' = $dHCPEnabled;
                    'DHCPServer' = $dHCPServer;
                    'IPAddresses' = $ipAddresses;

                }
            $networkAdapter = New-Object -TypeName PSObject -Prop $properties
                
            # add adapter to list
            $networkAdapters += $networkAdapter
        }
        return $networkAdapters
    }

    function Get-CsProcessors
    {
        $processors = Get-CimClass Win32_Processor
        if (!$processors) {return $null }
        $csProcessors = @()
        foreach ($processor in $processors)
        {
            # new-up one adapter object
            $properties = 
                @{
                    'Name'=$processor.Name;
                    'Manufacturer'=$processor.Manufacturer;
                    'Description'=$processor.Description;
                    'Architecture'=$processor.Architecture;
                    'AddressWidth'=$processor.AddressWidth;
                        
                        
                    'Availability'=$processor.Availability;
                    'CpuStatus'=$processor.CpuStatus;
                    'CurrentClockSpeed'=$processor.CurrentClockSpeed;
                    'DataWidth'=$processor.DataWidth;

                    'MaxClockSpeed'=$processor.MaxClockSpeed;
                    'NumberOfCores'=$processor.NumberOfCores;
                    'NumberOfLogicalProcessors'=$processor.NumberOfLogicalProcessors;
                    'ProcessorID'=$processor.ProcessorID;
                    'ProcessorType'=$processor.ProcessorType;
                    'Role'=$processor.Role;
                    'SocketDesignation'=$processor.SocketDesignation;
                    'Status'=$processor.Status;
                }
            $csProcessor = New-Object -TypeName PSObject -Prop $properties
                
            # add adapter to list
            $csProcessors += $csProcessor
        }
        $csProcessors
    }

    function Get-OsHardwareAbstractionLayer
    {
        $hal = $null
        $systemDirectory =  Get-CimClassPropVal Win32_OperatingSystem SystemDirectory
        $halPath = Join-Path -path $systemDirectory -ChildPath "hal.dll"
        $query = 'SELECT * FROM CIM_DataFile Where Name="C:\WINDOWS\system32\hal.dll"'
        $query = $query -replace '\\','\\'
        $instance = Get-CimInstance -Query $query
        if ($instance)
        {
            $hal = [string]$instance[0].CimInstanceProperties["Version"].Value
        }
        return $hal
    }

    function Get-OsHotFixes
    {
        $hotfixes = Get-CimClass Win32_QuickFixEngineering | Select-Object -Property HotFixID,Description,InstalledOn,FixComments
        if (!$hotfixes) {return $null }

        $osHotFixes = @()
            
        foreach ($hotfix in $hotfixes)
        {
            $installedOn = $null
            if ($hotfix.InstalledOn)
            {
                $installedOn = $hotfix.InstalledOn.ToString("M/d/yyyy")
            }
            # new-up one adapter object
            $properties = 
                @{
                    'HotFixID'=$hotfix.HotFixID;
                    'Description'=$hotfix.Description;
                    'InstalledOn'=$installedOn;
                    'FixComments'=$hotfix.FixComments;
                }
            $osHotFix = New-Object -TypeName PSObject -Prop $properties
                
            # add adapter to list
            $osHotFixes += $osHotFix
        }
        $osHotFixes
    }

    function Get-OsInUseVirtualMemory 
    {
        $osInUseVirtualMemory  = $null
        $os = Get-CimClass Win32_OperatingSystem
        $totalVirtualMemorySize = $os.TotalVirtualMemorySize
        $freeVirtualMemory = $os.FreeVirtualMemory

        if (($totalVirtualMemorySize) -and ($freeVirtualMemory))
        {
            $osInUseVirtualMemory = $totalVirtualMemorySize - $freeVirtualMemory
        }
        return $osInUseVirtualMemory
    }

    function Get-OsServerLevel
    {
        # translated from cmldet logic (1) RegistryInfo.GetServerLevels; and (2) os.GetOtherInfo()
        $subkey = 'Software\Microsoft\Windows NT\CurrentVersion\Server\ServerLevels'
        $regKey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($subkey)
        $serverLevels = @{}
        try
        {
            if ($regKey -ne $null)
            {
                $serverLevelNames = $regKey.GetValueNames()
                
                foreach ($serverLevelName in $serverLevelNames)
                {
                    if ($regKey.GetValueKind($serverLevelName) -eq 4) # RegistryValueKind.DWord == 4
                    {
                        $val = $regKey.GetValue($serverLevelName)
                        $serverLevels.Add($serverLevelName, [System.Convert]::ToUInt32($val))
                    }
                }
            }
        }
        finally
        {
            if ($regKey -ne $null) { $regKey.Dispose()}
        }

        if ($serverLevels -eq $null -or $serverLevels.Count -eq 0)
        {
            return $null
        }

        [uint32]$rv
        # computerinfo enum ServerLevel
        #    0 = Unknown
        #    1 = NanoServer
        #    2 = ServerCore
        #    3 = ServerCoreWithManagementTools
        #    4 = FullServer
        if ($serverLevels.ContainsKey("NanoServer") -and $serverLevels["NanoServer"] -eq 1)
        {
            $rv = 1 # NanoServer
        }
        elseif ($serverLevels.ContainsKey("ServerCore") -and $serverLevels["ServerCore"] -eq 1)
        {
            $rv = 2 # ServerCore
            if ($serverLevels.ContainsKey("Server-Gui-Mgmt") -and $serverLevels["Server-Gui-Mgmt"] -eq 1)
            {
                $rv = 3 # ServerCoreWithManagementTools
                if ($serverLevels.ContainsKey("Server-Gui-Shell") -and $serverLevels["Server-Gui-Shell"] -eq 1)
                {
                    $rv = 4 # FullServer
                }
            }
        }
        
        return $rv
    }

    function Get-OsSuites
    {
        param($propertyName)

        $osProductSuites = @()
        $suiteMask = Get-CimClassPropVal Win32_OperatingSystem $propertyName 
        if ($suiteMask)
        {
            foreach($suite in [System.Enum]::GetValues('Microsoft.PowerShell.Commands.OSProductSuite'))
            {
                if (($suiteMask -band $suite) -ne 0)
                {
                    $osProductSuites += $suite
                }
            }
                
        }
        return $osProductSuites
    }

    function Get-HyperVProperty
    {
        param([string]$propertyName)

        $hypervisorPresent = Get-CimClassPropVal Win32_ComputerSystem HypervisorPresent
          
        $dataExecutionPrevention_Available = $null
        $secondLevelAddressTranslationExtensions  = $null
        $virtualizationFirmwareEnabled  = $null
        $vMMonitorModeExtensions  = $null

        if (($hypervisorPresent -ne $null) -and ($hypervisorPresent -ne $true))
        {
            $dataExecutionPrevention_Available = Get-CimClassPropVal Win32_OperatingSystem DataExecutionPrevention_Available

            $secondLevelAddressTranslationExtensions = Get-CimClassPropVal Win32_Processor SecondLevelAddressTranslationExtensions
            $virtualizationFirmwareEnabled = Get-CimClassPropVal Win32_Processor VirtualizationFirmwareEnabled
            $vMMonitorModeExtensions = Get-CimClassPropVal Win32_Processor VMMonitorModeExtensions
        }
        switch ($propertyName) 
        { 
            "HyperVisorPresent" { return $hypervisorPresent }
            "HyperVRequirementDataExecutionPreventionAvailable" { return $dataExecutionPrevention_Available }
            "HyperVRequirementSecondLevelAddressTranslation"{ return $secondLevelAddressTranslationExtensions }
            "HyperVRequirementVirtualizationFirmwareEnabled"{ return $virtualizationFirmwareEnabled }
            "HyperVRequirementVMMonitorModeExtensions" { return $vMMonitorModeExtensions }
        }
    }

    function Get-KeyboardLayout
    {
        $keyboards = Get-CimClass Win32_Keyboard 
        $result = $null
        if ($keyboards)
        {
            # cmdlet code comment TODO: handle multiple keyboards?
            #   there might be several keyboards found. For the moment
            #   we display info for only one
            $layout = $keyboards[0].Layout
            try
            {
                $layoutAsHex = [System.Convert]::ToUInt32($layout, 16)
                if ($layoutAsHex -ne $null)
                {
                    $result = Convert-LocaleIdToLocaleName $layoutAsHex
                }
            }
            catch
            {
              #swallow
            }
        }
        return $result
    }

    function Get-OsLanguageName
    {
        # updated 21May 2016 to follow updated logic from cmdlet
        $localeID = Get-CimClassPropVal Win32_OperatingSystem OSLanguage
        return Convert-LocaleIdToLocaleName $localeID
    }

    function Convert-LocaleIdToLocaleName
    {
        # This is a migrated/translated version of the cmdlet method = LocaleIdToLocaleName()
        # THIS is the comment from the cmdlet
        #    CoreCLR's System.Globalization.Culture does not appear to have a constructor
        #    that accepts an integer LocalID (LCID) value, so we'll PInvoke native code
        #    to get a locale name from an LCID value
        param($localeID)

        $sb = (New-Object System.Text.StringBuilder([int]85)) # 85 = Native.LOCALE_NAME_MAX_LENGTH
        $len = (Get-LCIDToLocaleName)::LCIDToLocaleName($localeID, $sb, $sb.Capacity, 0)
        if (($len -gt 0) -and ($sb.Length -gt 0))
        {
            return $sb.ToString()
        }
        return $null
    }

    function Get-Locale
    {
        # This is a migrated/translated version of the cmdlet method = Conversion.MakeLocale()
        # This method first tries to convert the string to a hex value
        #  and get the CultureInfo object from that value.
        #  Failing that it attempts to retrieve the CultureInfo object
        #  using the locale string as passed.

        $localeName = $null

        $locale =  Get-CimClassPropVal Win32_OperatingSystem Locale

        if ($locale -ne $null)
        {
            #$localeAsHex = $locale -as [hex]
            $localeAsHex = [System.Convert]::ToUInt32($locale, 16)
            if ($localeAsHex -ne $null)
            {
                
                try 
                {
                    $localeName = Convert-LocaleIdToLocaleName $localeAsHex
                }
                catch 
                {
                    # swallow this
                        # DEBUGGING
                        #return $_.Exception.Message
                }
            }
                
            if ($localeName -eq $null)
            {
                try 
                {
                    $cultureInfo = (New-Object System.Globalization.CultureInfo($locale))
                    $localeName = $cultureInfo.Name 
                }
                catch 
                {
                    # swallow this
                }
            }
        }
        return $localeName
    }

    function Get-OsPagingFiles
    {
        $osPagingFiles = @()
        $pageFileUsage =  Get-CimClass Win32_PageFileUsage
        if ($pageFileUsage -ne $null)
        {
            foreach ($pageFileItem in $pageFileUsage)
            {
                $osPagingFiles += $pageFileItem.Caption
            }
        }

        return [string[]]$osPagingFiles
    }

    function Get-UnixSecondsToDateTime
    {
        param([string]$seconds)
            
        $origin = New-Object -Type DateTime -ArgumentList 1970, 1, 1, 0, 0, 0, 0
        $origin.AddSeconds($seconds)
    }
    
    function Get-WinNtCurrentVersion
    {
        # This method was translated/converted from cmdlet impl method = RegistryInfo.GetWinNtCurrentVersion();
        param([string]$propertyName)

        $key = 'HKLM:\Software\Microsoft\Windows NT\CurrentVersion\'
        $regValue = (Get-ItemProperty -Path $key -Name $propertyName).$propertyName
        if ($propertyName -eq "InstallDate")
        {
            # more complicated case: InstallDate
            if ($regValue)
            {
                return Get-UnixSecondsToDateTime $regValue
            }
        }
        else
        {
            return $regValue
        }
    }

    function Get-ExpectedComputerInfoValue
    {
        param([string]$propertyName)

        switch ($propertyName) 
        { 
            "BiosBIOSVersion" {return Get-CimClassPropVal Win32_bios BiosVersion}
            "BiosBuildNumber" {return Get-CimClassPropVal Win32_bios BuildNumber}
            "BiosCaption" {return Get-CimClassPropVal Win32_bios Caption}
            "BiosCharacteristics" {return Get-CimClassPropVal Win32_bios BiosCharacteristics}
            "BiosCodeSet" {return Get-CimClassPropVal Win32_bios CodeSet}
            "BiosCurrentLanguage" {return Get-CimClassPropVal Win32_bios CurrentLanguage}
            "BiosDescription" {return Get-CimClassPropVal Win32_bios Description}
            "BiosEmbeddedControllerMajorVersion" {return Get-CimClassPropVal Win32_bios EmbeddedControllerMajorVersion}
            "BiosEmbeddedControllerMinorVersion" {return Get-CimClassPropVal Win32_bios EmbeddedControllerMinorVersion}
            "BiosFirmwareType" {return Get-BiosFirmwareType} 
            "BiosIdentificationCode" {return Get-CimClassPropVal Win32_bios IdentificationCode}
            "BiosInstallableLanguages" {return Get-CimClassPropVal Win32_bios InstallableLanguages}
            "BiosInstallDate" {return Get-CimClassPropVal Win32_bios InstallDate}
            "BiosLanguageEdition" {return Get-CimClassPropVal Win32_bios LanguageEdition}
            "BiosListOfLanguages" {return Get-CimClassPropVal Win32_bios ListOfLanguages}
            "BiosManufacturer" {return Get-CimClassPropVal Win32_bios Manufacturer}
            "BiosName" {return Get-CimClassPropVal Win32_bios Name}
            "BiosOtherTargetOS" {return Get-CimClassPropVal Win32_bios OtherTargetOS}
            "BiosPrimaryBIOS" {return Get-CimClassPropVal Win32_bios PrimaryBIOS}
            "BiosReleaseDate" {return Get-CimClassPropVal Win32_bios ReleaseDate}
            "BiosSeralNumber" {return Get-CimClassPropVal Win32_bios SerialNumber}
            "BiosSMBIOSBIOSVersion" {return Get-CimClassPropVal Win32_bios SMBIOSBIOSVersion}
            "BiosSMBIOSPresent" {return Get-CimClassPropVal Win32_bios SMBIOSPresent}
            "BiosSMBIOSMajorVersion" {return Get-CimClassPropVal Win32_bios SMBIOSMajorVersion}
            "BiosSMBIOSMinorVersion" {return Get-CimClassPropVal Win32_bios SMBIOSMinorVersion}
            "BiosSoftwareElementState" {return Get-CimClassPropVal Win32_bios SoftwareElementState}
            "BiosStatus" {return Get-CimClassPropVal Win32_bios Status}
            "BiosSystemBiosMajorVersion" {return Get-CimClassPropVal Win32_bios SystemBiosMajorVersion}
            "BiosSystemBiosMinorVersion" {return Get-CimClassPropVal Win32_bios SystemBiosMinorVersion}
            "BiosTargetOperatingSystem" {return Get-CimClassPropVal Win32_bios TargetOperatingSystem}
            "BiosVersion" {return Get-CimClassPropVal Win32_bios Version}
                
            "CsAdminPasswordStatus" {return Get-CimClassPropVal Win32_ComputerSystem AdminPasswordStatus}
            "CsAutomaticManagedPagefile" {return Get-CimClassPropVal Win32_ComputerSystem AutomaticManagedPagefile}
            "CsAutomaticResetBootOption" {return Get-CimClassPropVal Win32_ComputerSystem AutomaticResetBootOption}
            "CsAutomaticResetCapability" {return Get-CimClassPropVal Win32_ComputerSystem AutomaticResetCapability}
            "CsBootOptionOnLimit" {return Get-CimClassPropVal Win32_ComputerSystem BootOptionOnLimit}
            "CsBootOptionOnWatchDog" {return Get-CimClassPropVal Win32_ComputerSystem BootOptionOnWatchDog}
            "CsBootROMSupported" {return Get-CimClassPropVal Win32_ComputerSystem BootROMSupported}
            "CsBootStatus" {return Get-CimClassPropVal Win32_ComputerSystem BootStatus}
            "CsBootupState" {return Get-CimClassPropVal Win32_ComputerSystem BootupState}
            "CsCaption" {return Get-CimClassPropVal Win32_ComputerSystem Caption}
            "CsChassisBootupState" {return Get-CimClassPropVal Win32_ComputerSystem ChassisBootupState}
            "CsChassisSKUNumber" {return Get-CimClassPropVal Win32_ComputerSystem ChassisSKUNumber}
            "CsCurrentTimeZone" {return Get-CimClassPropVal Win32_ComputerSystem CurrentTimeZone}
            "CsDaylightInEffect" {return Get-CimClassPropVal Win32_ComputerSystem DaylightInEffect}
            "CsDescription" {return Get-CimClassPropVal Win32_ComputerSystem Description}
            "CsDNSHostName" {return Get-CimClassPropVal Win32_ComputerSystem DNSHostName}
            "CsDomain" {return Get-CimClassPropVal Win32_ComputerSystem Domain}
            "CsDomainRole" {return Get-CimClassPropVal Win32_ComputerSystem DomainRole}
            "CsEnableDaylightSavingsTime" {return Get-CimClassPropVal Win32_ComputerSystem EnableDaylightSavingsTime}
            "CsFrontPanelResetStatus" {return Get-CimClassPropVal Win32_ComputerSystem FrontPanelResetStatus}
            "CsHypervisorPresent" {return Get-CimClassPropVal Win32_ComputerSystem HypervisorPresent}
            "CsInfraredSupported" {return Get-CimClassPropVal Win32_ComputerSystem InfraredSupported}
            "CsInitialLoadInfo" {return Get-CimClassPropVal Win32_ComputerSystem InitialLoadInfo}
            "CsInstallDate" {return Get-CimClassPropVal Win32_ComputerSystem InstallDate}
            "CsKeyboardPasswordStatus" {return Get-CimClassPropVal Win32_ComputerSystem KeyboardPasswordStatus}
            "CsLastLoadInfo" {return Get-CimClassPropVal Win32_ComputerSystem LastLoadInfo}
            "CsManufacturer" {return Get-CimClassPropVal Win32_ComputerSystem Manufacturer}
            "CsModel" {return Get-CimClassPropVal Win32_ComputerSystem Model}
            "CsName" {return Get-CimClassPropVal Win32_ComputerSystem Name}
            "CsNetworkAdapters" { return Get-CsNetworkAdapters }
            "CsNetworkServerModeEnabled" {return Get-CimClassPropVal Win32_ComputerSystem NetworkServerModeEnabled}
            "CsNumberOfLogicalProcessors" {return [System.Environment]::GetEnvironmentVariable("NUMBER_OF_PROCESSORS")}
            "CsNumberOfProcessors" {return Get-CimClassPropVal Win32_ComputerSystem NumberOfProcessors }
            "CsOEMStringArray" {return Get-CimClassPropVal Win32_ComputerSystem OEMStringArray}
            "CsPartOfDomain" {return Get-CimClassPropVal Win32_ComputerSystem PartOfDomain}
            "CsPauseAfterReset" {return Get-CimClassPropVal Win32_ComputerSystem PauseAfterReset}
            "CsPCSystemType" {return Get-CimClassPropVal Win32_ComputerSystem PCSystemType}
            "CsPCSystemTypeEx" {return Get-CimClassPropVal Win32_ComputerSystem PCSystemTypeEx}
            "CsPhyicallyInstalledMemory" {return Get-CsPhysicallyInstalledSystemMemory}
            "CsPowerManagementCapabilities" {return Get-CimClassPropVal Win32_ComputerSystem PowerManagementCapabilities}
            "CsPowerManagementSupported" {return Get-CimClassPropVal Win32_ComputerSystem PowerManagementSupported}
            "CsPowerOnPasswordStatus" {return Get-CimClassPropVal Win32_ComputerSystem PowerOnPasswordStatus}
            "CsPowerState" {return Get-CimClassPropVal Win32_ComputerSystem PowerState}
            "CsPowerSupplyState" {return Get-CimClassPropVal Win32_ComputerSystem PowerSupplyState}
            "CsPrimaryOwnerContact" {return Get-CimClassPropVal Win32_ComputerSystem PrimaryOwnerContact}
            "CsPrimaryOwnerName" {return Get-CimClassPropVal Win32_ComputerSystem PrimaryOwnerName}
            "CsProcessors" { return Get-CsProcessors }
            "CsResetCapability" {return Get-CimClassPropVal Win32_ComputerSystem ResetCapability}
            "CsResetCount" {return Get-CimClassPropVal Win32_ComputerSystem ResetCount}
            "CsResetLimit" {return Get-CimClassPropVal Win32_ComputerSystem ResetLimit}
            "CsRoles" {return Get-CimClassPropVal Win32_ComputerSystem Roles}
            "CsStatus" {return Get-CimClassPropVal Win32_ComputerSystem Status}
            "CsSupportContactDescription" {return Get-CimClassPropVal Win32_ComputerSystem SupportContactDescription}
            "CsSystemFamily" {return Get-CimClassPropVal Win32_ComputerSystem SystemFamily}
            "CsSystemSKUNumber" {return Get-CimClassPropVal Win32_ComputerSystem SystemSKUNumber}
            "CsSystemType" {return Get-CimClassPropVal Win32_ComputerSystem SystemType}
            "CsThermalState" {return Get-CimClassPropVal Win32_ComputerSystem ThermalState}
            "CsTotalPhysicalMemory" {return Get-CimClassPropVal Win32_ComputerSystem TotalPhysicalMemory}
            "CsUserName" {return Get-CimClassPropVal Win32_ComputerSystem UserName}
            "CsWakeUpType" {return Get-CimClassPropVal Win32_ComputerSystem WakeUpType}
            "CsWorkgroup" {return Get-CimClassPropVal Win32_ComputerSystem Workgroup}

            "DeviceGuardAvailableSecurityProperties" {return Get-DeviceGuard AvailableSecurityProperties}
            "DeviceGuardCodeIntegrityPolicyEnforcementStatus" {return Get-DeviceGuard CodeIntegrityPolicyEnforcementStatus}
            "DeviceGuardRequiredSecurityProperties" {return Get-DeviceGuard RequiredSecurityProperties}
            "DeviceGuardSecurityServicesConfigured" {return Get-DeviceGuard SecurityServicesConfigured}
            "DeviceGuardSecurityServicesRunning" {return Get-DeviceGuard SecurityServicesRunning}
            "DeviceGuardSmartStatus" {return Get-DeviceGuard SmartStatus}
            "DeviceGuardUserModeCodeIntegrityPolicyEnforcementStatus" {return Get-DeviceGuard UserModeCodeIntegrityPolicyEnforcementStatus}	
              
            "HyperVisorPresent" {return Get-HyperVProperty $propertyName}
            "HyperVRequirementDataExecutionPreventionAvailable" {return Get-HyperVProperty $propertyName}
            "HyperVRequirementSecondLevelAddressTranslation" {return Get-HyperVProperty $propertyName}
            "HyperVRequirementVirtualizationFirmwareEnabled" {return Get-HyperVProperty $propertyName}
            "HyperVRequirementVMMonitorModeExtensions" {return Get-HyperVProperty $propertyName}
            "KeyboardLayout" {return Get-KeyboardLayout}
            "LogonServer" {return [Microsoft.Win32.Registry]::GetValue("HKEY_Current_User\Volatile Environment", "LOGONSERVER", "")}
             
            "OsArchitecture" {return Get-CimClassPropVal Win32_OperatingSystem OsArchitecture}
            "OsBootDevice" {return Get-CimClassPropVal Win32_OperatingSystem BootDevice}
            "OsBuildNumber" {return Get-CimClassPropVal Win32_OperatingSystem BuildNumber}
            "OsBuildType" {return Get-CimClassPropVal Win32_OperatingSystem BuildType}
            "OsCodeSet" {return Get-CimClassPropVal Win32_OperatingSystem CodeSet}
            "OsCountryCode" {return Get-CimClassPropVal Win32_OperatingSystem CountryCode}
            "OsCSDVersion" {return Get-CimClassPropVal Win32_OperatingSystem CSDVersion}
            "OsCurrentTimeZone" {return Get-CimClassPropVal Win32_OperatingSystem CurrentTimeZone}
            "OsDataExecutionPrevention32BitApplications" {return Get-CimClassPropVal Win32_OperatingSystem DataExecutionPrevention_32BitApplications}
            "OsDataExecutionPreventionAvailable" {return Get-CimClassPropVal Win32_OperatingSystem DataExecutionPrevention_Available}
            "OsDataExecutionPreventionDrivers" {return Get-CimClassPropVal Win32_OperatingSystem DataExecutionPrevention_Drivers}
            "OsDataExecutionPreventionSupportPolicy" {return Get-CimClassPropVal Win32_OperatingSystem DataExecutionPrevention_SupportPolicy}
            "OsDebug" {return Get-CimClassPropVal Win32_OperatingSystem Debug}
            "OsDistributed" {return Get-CimClassPropVal Win32_OperatingSystem Distributed}
            "OsEncryptionLevel" {return Get-CimClassPropVal Win32_OperatingSystem EncryptionLevel}
            "OsForegroundApplicationBoost" {return Get-CimClassPropVal Win32_OperatingSystem ForegroundApplicationBoost}

            # OsFreePhysicalMemory => fragile test: fluid/dynamic (see special cases)
            #"OsFreeSpaceInPagingFiles" {return Get-CimClassPropVal Win32_OperatingSystem FreePhysicalMemory}
            # OsFreeSpaceInPagingFiles => fragile test: fluid/dynamic (see special cases)
            #"OsFreeSpaceInPagingFiles" {return Get-CimClassPropVal Win32_OperatingSystem FreeSpaceInPagingFiles}
            # OsFreeVirtualMemory => fragile test: fluid/dynamic (see special cases)
            #"OsFreeVirtualMemory" {return Get-CimClassPropVal Win32_OperatingSystem FreeVirtualMemory}
                
            "OsHardwareAbstractionLayer" {return Get-OsHardwareAbstractionLayer}
            "OsHotFixes" {return Get-OsHotFixes }
            "OsInstallDate" {return Get-CimClassPropVal Win32_OperatingSystem InstallDate}
            "OsInUseVirtualMemory"  { return Get-OsInUseVirtualMemory }
            "OsLanguage" {return Get-OsLanguageName}
            "OsLastBootUpTime" {return Get-CimClassPropVal Win32_OperatingSystem LastBootUpTime}

            # OsLocalDateTime => fragile test: fluid/dynamic  (see special cases)
            #"OsLocalDateTime" {return Get-CimClassPropVal Win32_OperatingSystem LocalDateTime}

            "OsLocale" {return Get-Locale}
            "OsLocaleID" {return Get-CimClassPropVal Win32_OperatingSystem Locale}
            "OsManufacturer" {return Get-CimClassPropVal Win32_OperatingSystem Manufacturer}
            "OsMaxNumberOfProcesses" {return Get-CimClassPropVal Win32_OperatingSystem MaxNumberOfProcesses}
            "OsMaxProcessMemorySize" {return Get-CimClassPropVal Win32_OperatingSystem MaxProcessMemorySize}
            "OsMuiLanguages" {return Get-CimClassPropVal Win32_OperatingSystem MuiLanguages}
            "OsName" {return Get-CimClassPropVal Win32_OperatingSystem Caption}
            "OsNumberOfLicensedUsers" {return Get-CimClassPropVal Win32_OperatingSystem NumberOfLicensedUsers}

            # OsNumberOfProcesses => fragile test: fluid/dynamic
            #"OsNumberOfProcesses" {return Get-CimClassPropVal Win32_OperatingSystem NumberOfProcesses}

            "OsNumberOfUsers" {return Get-CimClassPropVal Win32_OperatingSystem NumberOfUsers}
            "OsOperatingSystemSKU" {return Get-CimClassPropVal Win32_OperatingSystem OperatingSystemSKU}
            "OsOrganization" {return Get-CimClassPropVal Win32_OperatingSystem Organization}
            "OsOtherTypeDescription" {return Get-CimClassPropVal Win32_OperatingSystem OtherTypeDescription}
            "OsPAEEnabled" {return Get-CimClassPropVal Win32_OperatingSystem PAEEnabled}
            "OsPagingFiles" {return Get-OsPagingFiles}
            "OsPortableOperatingSystem" {return Get-CimClassPropVal Win32_OperatingSystem PortableOperatingSystem}
            "OsPrimary" {return Get-CimClassPropVal Win32_OperatingSystem Primary}
            "OsProductSuites" {return Get-OsSuites OSProductSuite }
            "OsProductType" {return Get-CimClassPropVal Win32_OperatingSystem ProductType}

            "OsRegisteredUser" {return Get-CimClassPropVal Win32_OperatingSystem RegisteredUser}
            "OsSerialNumber" {return Get-CimClassPropVal Win32_OperatingSystem SerialNumber}

            "OsServerLevel" {return Get-OsServerLevel}

            "OsServicePackMajorVersion" {return Get-CimClassPropVal Win32_OperatingSystem ServicePackMajorVersion}
            "OsServicePackMinorVersion" {return Get-CimClassPropVal Win32_OperatingSystem ServicePackMinorVersion}

            "OsSizeStoredInPagingFiles" {return Get-CimClassPropVal Win32_OperatingSystem SizeStoredInPagingFiles}
            "OsStatus" {return Get-CimClassPropVal Win32_OperatingSystem Status}
            "OsSuites" {return Get-OsSuites SuiteMask }
            "OsSystemDevice" {return Get-CimClassPropVal Win32_OperatingSystem SystemDevice}
            "OsSystemDirectory" {return Get-CimClassPropVal Win32_OperatingSystem SystemDirectory}
            "OsSystemDrive" {return Get-CimClassPropVal Win32_OperatingSystem SystemDrive}
            "OsTotalSwapSpaceSize" {return Get-CimClassPropVal Win32_OperatingSystem TotalSwapSpaceSize}
            "OsTotalVirtualMemorySize" {return Get-CimClassPropVal Win32_OperatingSystem TotalVirtualMemorySize}
            "OsTotalVisibleMemorySize" {return Get-CimClassPropVal Win32_OperatingSystem TotalVisibleMemorySize}
            "OsType" {return Get-CimClassPropVal Win32_OperatingSystem OSType }

            # OsUptime => fragile test: fluid/dynamic
            #"OsUptime" {return Get-CimClassPropVal Win32_OperatingSystem Uptime}

            "OsVersion" {return Get-CimClassPropVal Win32_OperatingSystem Version}
            "OsWindowsDirectory" {return [System.Environment]::GetEnvironmentVariable("windir")}

            "PowerPlatformRole" { return Get-PowerPlatformRole }
            "TimeZone" {return ([System.TimeZoneInfo]::Local).DisplayName}
              
            "WindowsBuildLabEx" { return Get-WinNtCurrentVersion BuildLabEx }
            "WindowsCurrentVersion" { return Get-WinNtCurrentVersion CurrentVersion}
            "WindowsEditionId" { return Get-WinNtCurrentVersion EditionID}
            "WindowsInstallationType" { return Get-WinNtCurrentVersion InstallationType}
            "WindowsInstallDateFromRegistry" { return Get-WinNtCurrentVersion InstallDate}
            "WindowsProductId" { return Get-WinNtCurrentVersion ProductId}
            "WindowsProductName" { return Get-WinNtCurrentVersion ProductName}
            "WindowsRegisteredOrganization" {return Get-WinNtCurrentVersion RegisteredOrganization}
            "WindowsRegisteredOwner" {return Get-WinNtCurrentVersion RegisteredOwner}
            "WindowsVersion" {return Get-WinNtCurrentVersion ReleaseId}

            "WindowsSystemRoot" {return [System.Environment]::GetEnvironmentVariable("SystemRoot")}

            default {return "Unknown/unsupported propertyName = $propertyName"}
        }
    }

    $expected = New-Object -TypeName PSObject
    foreach ($propertyName in [string[]]$propertyNames)
    {
        $expected | Add-Member -MemberType NoteProperty -Name $propertyName -Value (Get-ExpectedComputerInfoValue $propertyName)
    }
    return $expected
}

#
# COMMON TEST HELPER METHODS
#
function Build-TestCases
{
    param($observed, $expected)

    $propertNames = Get-CommonProperties $observed $expected
    $testCases = @()
    foreach ($propertyName in [string[]]$propertNames)
    {
        $expectedValue = $expected.PsObject.Properties.Item($propertyName).Value
        $observedValue = $observed.PsObject.Properties.Item($propertyName).Value

        $testCase = @{            
            "Expected" = $expectedValue;          
            "Observed" = $observedValue;           
            "PropertyName" = $propertyName}
        $testCases += $testCase 
    }
    $testCases      
}

function Get-CommonProperties
{
    param($observed,$expected)
   
    if (!$observed) { return $null }
    if (!$expected) { return $null }

    $propListObserved = $observed | Get-Member -MemberType Properties | Select-Object -ExpandProperty Name | Select-Object -Unique 
    $propListExpected = $expected | Get-Member -MemberType Properties | Select-Object -ExpandProperty Name | Select-Object -Unique 

    $propCount = [math]::max($propListObserved.Count,$propListExpected.Count)
    $syncWinNum = [math]::round(($propCount/2),0)

    $commonProp = Compare-Object -SyncWindow $syncWinNum -ReferenceObject $propListExpected -DifferenceObject $propListObserved -ExcludeDifferent -IncludeEqual
    return $commonProp | Select-Object -ExpandProperty InputObject 
}

function Assert-Properties
{
    param($refObject, [string[]] $propListExpected)

    $propListObserved = $refObject | Get-Member -MemberType Properties | Select-Object -ExpandProperty Name | Select-Object -Unique 
    $compResult = Compare-Object $propListObserved $propListExpected | Select-Object -ExpandProperty InputObject
    if ($compResult)
    {
        $observedList = ([string]::Join("|",$propListObserved))
        $expectedList = ([string]::Join("|",$propListExpected))
        $observedList | Should Be $expectedList
    }
}

function Assert-ListsSame
{
    param([object[]] $expected, [object[]] $observed)

    $compResult = Compare-Object $observed $expected | Select-Object -ExpandProperty InputObject
    if ($compResult)
    {
        $observedList = ([string]::Join("|",$observed))
        $expectedList = ([string]::Join("|",$expected))
        $observedList | Should Be $expectedList
    }
}

function Assert-NoProperties
{
    param($refObject)

    if ($refObject)
    {
        $propListObserved = $refObject | Get-Member -MemberType Properties | Select-Object -ExpandProperty Name | Select-Object -Unique 
        $propListObserved.Count | Should Be 0
    }
}

function Assert-Default
{
    param($observed,$expected)
    
    if (($observed) -and ($observed.GetType().Name -eq "string"))
    {
        # we do NOT want to do case-sensitive comparisons for strings
        $observed | Should Be $expected
    }
    else
    {
        $observed | Should BeExactly $expected
    } 
}

function Assert-ObjectsHaveSamePropertyValues
{
    param($i,$observed,$expected)

    $items = Build-TestCases $observed $expected
    foreach($item in $items)
    {
        try
        {
            Assert-Default $item.Observed $item.Expected
        }
        catch
        {
            $propertyName = $item.PropertyName
            $exception = New-Object System.Exception ("Failure in Assert-ListsHavePropertyValues for list item index = $i and Property = $propertyName",$_.Exception)
		    throw $exception 
        }
    }
}

function Assert-ListsHaveSamePropertyValues
{
    param($observed,$expected)
    
    if ($expected.Count)
    {
        $observed.Count | Should Be $expected.Count
    }
    for ($i=0; $i -lt $observed.Count; $i++) 
    {
        $itemObserved = $observed[$i]
        $itemExpected = $null
        if ( $itemExpected.Count ) { $itemExpected  = $expected[$i] }
        Assert-ObjectsHaveSamePropertyValues $i $itemObserved $itemExpected
    }
}

function Exec-OneTestPass
{
    param($testName, $propertyNames, $propertyFilter, $expectedProperties = $null, $forceRefresh = $false)
   
    if ($IsWindows) 
    { 
        if ($propertyFilter)
        {
            $observed  = Get-ComputerInfoForTest $propertyFilter
        }
        else
        {
            $observed  = Get-ComputerInfoForTest $null $forceRefresh
        }
        $observed | Should Not BeNullOrEmpty
    }

    # if property filter passed-in, validate properties of observed object
    if ($propertyFilter)
    {
        It "[$testName] Validate Property Filter" {
            if ($expectedProperties)
            {
                Assert-Properties $observed $expectedProperties
            }
            else
            {
               Assert-NoProperties $observed
            }
        } 
    }

    if ($expectedProperties)
    {
        if ($IsWindows) 
        {
            $expected = New-ExpectedComputerInfo $propertyNames
            $expected | Should Not BeNullOrEmpty

            $testCases = Build-TestCases $observed $expected
        }

        It "[$testName] Init common Test objects validation" {
            $testCases | Should Not BeNullOrEmpty
        }   

        It "[$testName] Compare observed to expected for property = <PropertyName>" -TestCases $testCases {
            param($observed, $expected, $propertyName)
        
            switch($propertyName)
            {
                "CsNetworkAdapters" { Assert-ListsHaveSamePropertyValues $observed $expected }
                "CsProcessors"      { Assert-ListsHaveSamePropertyValues $observed $expected }
                "OsHotFixes"        { Assert-ListsHaveSamePropertyValues $observed $expected }
                default             { Assert-Default $observed $expected }
            }
        }
    }
}

try {
    #skip all tests on non-windows platform
    $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:skip"] = !$IsWindows

    Describe "Tests for Get-ComputerInfo: Ensure Type returned" -tags "CI", "RequireAdminOnWindows" {

        It "Verify type returned by Get-ComputerInfo" {
            $computerInfo = Get-ComputerInfo
            $computerInfo.GetType().Name | Should Be "ComputerInfo"
        } 
    }

    Describe "Tests for Get-ComputerInfo" -tags "Feature", "RequireAdminOnWindows" {

        #
        # Test 01. Standard Property test - No property filter applied
        #
        $propertyNames = Get-PropertyNamesForComputerInfoTest
        $expectedProperties = $propertyNames
        $testName = "Test 01. Standard Property test - No property filter applied"
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.001 Filter Property - Property filter with one valid item
        #
        $testName = "Test 02.001 Filter Property - Property filter with one valid item"
        $propertyNames =  @("BiosBIOSVersion")
        $expectedProperties = @("BiosBIOSVersion")
        $propertyFilter = "BiosBIOSVersion"
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.002 Filter Property - Property filter with three valid items
        #
        $testName = "Test 02.002 Filter Property - Property filter with three valid items"
        $propertyNames =  @("BiosBIOSVersion","BiosBuildNumber","BiosCaption")
        $expectedProperties = @("BiosBIOSVersion","BiosBuildNumber","BiosCaption")
        $propertyFilter = @("BiosBIOSVersion","BiosBuildNumber","BiosCaption")
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.003 Filter Property - Property filter with one invalid item
        #
        $testName = "Test 02.003 Filter Property - Property filter with one invalid item"
        $propertyNames =  $null
        $expectedProperties = $null
        $propertyFilter = @("BiosBIOSVersionXXX")
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.004 Filter Property - Property filter with four invalid items
        #
        $testName = "Test 02.004 Filter Property - Property filter with four invalid items"
        $propertyNames =  $null
        $expectedProperties = $null
        $propertyFilter = @("BiosBIOSVersionXXX","InvalidProperty1","InvalidProperty2","InvalidProperty3")
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.005 Filter Property - Property filter with valid and invalid items: ver #1
        #
        $testName = "Test 02.005 Filter Property - Property filter with valid and invalid items: ver #1"
        $propertyNames =  @("BiosCodeSet","BiosCurrentLanguage","BiosDescription")
        $expectedProperties = @("BiosCodeSet","BiosCurrentLanguage","BiosDescription")
        $propertyFilter = @("InvalidProperty1","BiosCodeSet","BiosCurrentLanguage","BiosDescription")
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.006 Filter Property - Property filter with valid and invalid items: ver #2
        #
        $testName = "Test 02.006 Filter Property - Property filter with valid and invalid items: ver #2"
        $propertyNames =  @("BiosCodeSet","BiosCurrentLanguage","BiosDescription")
        $expectedProperties = @("BiosCodeSet","BiosCurrentLanguage","BiosDescription")
        $propertyFilter = @("BiosCodeSet","InvalidProperty1","BiosCurrentLanguage","BiosDescription","InvalidProperty2")
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.007 Filter Property - Property filter with wild card: ver #1
        #
        $testName = "02.007 Filter Property - Property filter with wild card: ver #1"
        $propertyNames =  @("BiosCaption","BiosCharacteristics","BiosCodeSet","BiosCurrentLanguage")
        $expectedProperties = @("BiosCaption","BiosCharacteristics","BiosCodeSet","BiosCurrentLanguage")
        $propertyFilter = @("BiosC*")
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.008 Filter Property - Property filter with wild card and fixed
        #
        $testName = "Test 02.008 Filter Property - Property filter with wild card and fixed"
        $propertyNames =  @("BiosCaption","BiosCharacteristics","BiosCodeSet","BiosCurrentLanguage","CsCaption")
        $expectedProperties = @("BiosCaption","BiosCharacteristics","BiosCodeSet","BiosCurrentLanguage","CsCaption")
        $propertyFilter = @("BiosC*","CsCaption")
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.009 Filter Property - Property filter with wild card, fixed and invalid
        #
        $testName = "Test 02.009 Filter Property - Property filter with wild card, fixed and invalid"
        $propertyNames =  @("BiosCaption","BiosCharacteristics","BiosCodeSet","BiosCurrentLanguage","CsCaption")
        $expectedProperties = @("BiosCaption","BiosCharacteristics","BiosCodeSet","BiosCurrentLanguage","CsCaption")
        $propertyFilter = @("CsCaption","InvalidProperty1","BiosC*")
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties

        #
        # Test 02.010 Filter Property - Property filter with wild card invalid
        #
        $testName = "Test 02.010 Filter Property - Property filter with wild card invalid"
        $propertyNames =  $null
        $expectedProperties = $null
        $propertyFilter = @("BiosBIOSVersionX*")
        Exec-OneTestPass $testName $propertyNames $propertyFilter $expectedProperties
    }

    Describe "Special Case Tests for Get-ComputerInfo" -tags "Feature", "RequireAdminOnWindows" {

        BeforeAll {
            if ($IsWindows) 
            {
                $observed = Get-ComputerInfoForTest
                $observed | Should Not BeNullOrEmpty
            }
        }

        It "Verify that alias 'gin' exists" {
            $result = (Get-Alias -Name "gin").Name
            $result | Should Be "gin"
        }

        #
        # TESTS FOR SPECIAL CASE PROPERTIES (i.e. those that are fluid/changing
        #

        It "(special case) Test for property = OsFreePhysicalMemory" {
            ($observed.OsFreePhysicalMemory -gt 0) | Should Be $true
        } 


        It "(special case) Test for property = OsFreeSpaceInPagingFiles" -Skip:([System.Management.Automation.Platform]::IsIoT) {
            ($observed.OsFreeSpaceInPagingFiles -gt 0) | Should Be $true
        } 

        It "(special case) Test for property = OsFreeVirtualMemory" {
            ($observed.OsFreeVirtualMemory -gt 0) | Should Be $true
        } 

        It "(special case) Test for property = OsLocalDateTime" {
            $computerInfo = Get-ComputerInfo
            $computerInfo.GetType().Name | Should Be "ComputerInfo"
        } 

        It "(special case) Test for property = OsMaxNumberOfProcesses" {
            ($observed.OsMaxNumberOfProcesses -gt 0) | Should Be $true
        } 

        It "(special case) Test for property = OsNumberOfProcesses" {
            ($observed.OsNumberOfProcesses -gt 0) | Should Be $true
        }

        It "(special case) Test for property = OsUptime" {
            ($observed.OsUptime.Ticks -gt 0) | Should Be $true
        }     

        It "(special case) Test for property = OsInUseVirtualMemory" {
            ($observed.OsInUseVirtualMemory -gt 0) | Should Be $true
        } 
        
        
        It "(special case) Test for Filter Property - Property filter with special wild card * and fixed" {
            $propertyFilter = @("BiosC*","*")
            $computerInfo = Get-ComputerInfo -Property $propertyFilter
            $computerInfo.GetType().Name | Should Be "ComputerInfo"
        }
    }
}
finally 
{
    $global:PSDefaultParameterValues = $originalDefaultParameterValues
}
