# Use the .NET Core APIs to determine the current platform; if a runtime
# exception is thrown, we are on FullCLR, not .NET Core.
try {
    $Runtime = [System.Runtime.InteropServices.RuntimeInformation]
    $OSPlatform = [System.Runtime.InteropServices.OSPlatform]

    $IsCoreCLR = $true
    $IsLinux = $Runtime::IsOSPlatform($OSPlatform::Linux)
    $IsOSX = $Runtime::IsOSPlatform($OSPlatform::OSX)
    $IsWindows = $Runtime::IsOSPlatform($OSPlatform::Windows)
} catch {
    # If these are already set, then they're read-only and we're done
    try {
        $IsCoreCLR = $false
        $IsLinux = $false
        $IsOSX = $false
        $IsWindows = $true
    }
    catch { }
}

if ($IsLinux) {
    $LinuxInfo = Get-Content /etc/os-release | ConvertFrom-StringData

    $IsUbuntu = $LinuxInfo.ID -match 'ubuntu'
    $IsUbuntu14 = $IsUbuntu -and $LinuxInfo.VERSION_ID -match '14.04'
    $IsUbuntu16 = $IsUbuntu -and $LinuxInfo.VERSION_ID -match '16.04'
    $IsCentOS = $LinuxInfo.ID -match 'centos' -and $LinuxInfo.VERSION_ID -match '7'
}


function Start-PSBuild {
    [CmdletBinding(DefaultParameterSetName='CoreCLR')]
    param(
        # When specified this switch will stops running dev powershell
        # to help avoid compilation error, because file are in use.
        [switch]$StopDevPowerShell,

        [switch]$NoPath,
        [switch]$Restore,
        [string]$Output,
        [switch]$ResGen,
        [switch]$TypeGen,
        [switch]$Clean,

        # this switch will re-build only System.Management.Automation.dll
        # it's useful for development, to do a quick changes in the engine
        [switch]$SMAOnly,

        # These runtimes must match those in project.json
        # We do not use ValidateScript since we want tab completion
        [ValidateSet("ubuntu.14.04-x64",
                     "ubuntu.16.04-x64",
                     "debian.8-x64",
                     "centos.7-x64",
                     "win7-x64",
                     "win81-x64",
                     "win10-x64",
                     "osx.10.11-x64")]
        [Parameter(ParameterSetName='CoreCLR')]
        [string]$Runtime,

        [Parameter(ParameterSetName='FullCLR', Mandatory=$true)]
        [switch]$FullCLR,

        [Parameter(ParameterSetName='FullCLR')]
        [switch]$XamlGen,

        [Parameter(ParameterSetName='FullCLR')]
        [ValidateSet('x86', 'x64')] # TODO: At some point, we need to add ARM support to match CoreCLR
        [string]$NativeHostArch = "x64",

        [ValidateSet('Linux', 'Debug', 'Release', '')] # We might need "Checked" as well
        [string]$Configuration,

        [Parameter(ParameterSetName='CoreCLR')]
        [switch]$Publish,

        [Parameter(ParameterSetName='CoreCLR')]
        [switch]$CrossGen
    )

    function Stop-DevPowerShell {
        Get-Process powershell* |
            Where-Object {
                $_.Modules |
                Where-Object {
                    $_.FileName -eq (Resolve-Path $script:Options.Output).Path
                }
            } |
        Stop-Process -Verbose
    }

    if ($CrossGen -and !$Publish) {
        # By specifying -CrossGen, we implicitly set -Publish to $true, if not already specified.
        $Publish = $true
    }

    if ($Clean) {
        log "Cleaning your working directory. You can also do it with 'git clean -fdX'"
        Push-Location $PSScriptRoot
        try {
            git clean -fdX
        } finally {
            Pop-Location
        }
    }

    # save Git description to file for PowerShell to include in PSVersionTable
    git --git-dir="$PSScriptRoot/.git" describe --dirty --abbrev=60 > "$psscriptroot/powershell.version"

    # simplify ParameterSetNames
    if ($PSCmdlet.ParameterSetName -eq 'FullCLR') {
        $FullCLR = $true
    }

    # Add .NET CLI tools to PATH
    Find-Dotnet

    # verify we have all tools in place to do the build
    $precheck = precheck 'dotnet' "Build dependency 'dotnet' not found in PATH. Run Start-PSBootstrap. Also see: https://dotnet.github.io/getting-started/"

    if ($IsWindows) {
        # use custom package store - this value is also defined in nuget.config under config/repositoryPath
        # dotnet restore uses this value as the target for installing the assemblies for referenced nuget packages.
        # dotnet build does not currently consume the  config value but will consume env:NUGET_PACKAGES to resolve these dependencies
        $env:NUGET_PACKAGES="$PSScriptRoot\Packages"

        # cmake is needed to build powershell.exe
        $precheck = $precheck -and (precheck 'cmake' 'cmake not found. Run Start-PSBootstrap. You can also install it from https://chocolatey.org/packages/cmake')

        Use-MSBuild

        #mc.exe is Message Compiler for native resources
        $mcexe = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\" -Recurse -Filter 'mc.exe' | ? {$_.FullName -match 'x64'} | select -First 1 | % {$_.FullName}
        if (-not $mcexe) {
            throw 'mc.exe not found. Run Start-PSBootstrap or install Microsoft Windows 10 SDK from https://developer.microsoft.com/en-US/windows/downloads/windows-10-sdk'
        }

        $vcVarsPath = (Get-Item(Join-Path -Path "$env:VS140COMNTOOLS" -ChildPath '../../vc')).FullName
        if ((Test-Path -Path $vcVarsPath\vcvarsall.bat) -eq $false) {
            throw "Could not find Visual Studio vcvarsall.bat at $vcVarsPath. Please ensure the optional feature 'Common Tools for Visual C++' is installed."
        }

        # setup msbuild configuration
        if ($Configuration -eq 'Debug' -or $Configuration -eq 'Release') {
            $msbuildConfiguration = $Configuration
        } else {
            $msbuildConfiguration = 'Release'
        }

        # setup cmakeGenerator
        if ($NativeHostArch -eq 'x86') {
            $cmakeGenerator = 'Visual Studio 14 2015'
        } else {
            $cmakeGenerator = 'Visual Studio 14 2015 Win64'
        }

    } elseif ($IsLinux -or $IsOSX) {
        foreach ($Dependency in 'cmake', 'make', 'g++') {
            $precheck = $precheck -and (precheck $Dependency "Build dependency '$Dependency' not found. Run Start-PSBootstrap.")
        }
    }

    # Abort if any precheck failed
    if (-not $precheck) {
        return
    }

    # set output options
    $OptionsArguments = @{
        Publish=$Publish
        Output=$Output
        FullCLR=$FullCLR
        Runtime=$Runtime
        Configuration=$Configuration
        Verbose=$true
        SMAOnly=[bool]$SMAOnly
    }
    $script:Options = New-PSOptions @OptionsArguments

    if ($StopDevPowerShell) {
        Stop-DevPowerShell
    }

    # setup arguments
    $Arguments = @()
    if ($Publish -or $FullCLR) {
        $Arguments += "publish"
    } else {
        $Arguments += "build"
    }
    if ($Output) {
        $Arguments += "--output", (Join-Path $PSScriptRoot $Output)
    }
    elseif ($SMAOnly) {
        $Arguments += "--output", (Split-Path $script:Options.Output)
    }

    $Arguments += "--configuration", $Options.Configuration
    $Arguments += "--framework", $Options.Framework
    $Arguments += "--runtime", $Options.Runtime

    # handle Restore
    if ($Restore -or -not (Test-Path "$($Options.Top)/project.lock.json")) {
        log "Run dotnet restore"

        $RestoreArguments = @("--verbosity")
        if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
            $RestoreArguments += "Info"
        } else {
            $RestoreArguments += "Warning"
        }

        $RestoreArguments += "$PSScriptRoot"

        Start-NativeExecution { dotnet restore $RestoreArguments }

        # .NET Core's crypto library needs brew's OpenSSL libraries added to its rpath
        if ($IsOSX) {
            # This is the restored library used to build
            # This is allowed to fail since the user may have already restored
            Write-Warning ".NET Core links the incorrect OpenSSL, correcting NuGet package libraries..."
            find $env:HOME/.nuget -name System.Security.Cryptography.Native.dylib | xargs sudo install_name_tool -add_rpath /usr/local/opt/openssl/lib
        }
    }

    # handle ResGen
    # Heuristic to run ResGen on the fresh machine
    if ($ResGen -or -not (Test-Path "$PSScriptRoot/src/Microsoft.PowerShell.ConsoleHost/gen")) {
        log "Run ResGen (generating C# bindings for resx files)"
        Start-ResGen
    }

    # handle xaml files
    # Heuristic to resolve xaml on the fresh machine
    if ($FullCLR -and ($XamlGen -or -not (Test-Path "$PSScriptRoot/src/Microsoft.PowerShell.Activities/gen/*.g.cs"))) {
        log "Run XamlGen (generating .g.cs and .resources for .xaml files)"
        Start-XamlGen -MSBuildConfiguration $msbuildConfiguration
    }

    # Build native components
    if (($IsLinux -or $IsOSX) -and -not $SMAOnly) {
        $Ext = if ($IsLinux) {
            "so"
        } elseif ($IsOSX) {
            "dylib"
        }

        $Native = "$PSScriptRoot/src/libpsl-native"
        $Lib = "$($Options.Top)/libpsl-native.$Ext"
        log "Start building $Lib"

        try {
            Push-Location $Native
            Start-NativeExecution { cmake -DCMAKE_BUILD_TYPE=Debug . }
            Start-NativeExecution { make -j }
            Start-NativeExecution { ctest --verbose }
        } finally {
            Pop-Location
        }

        if (-not (Test-Path $Lib)) {
            throw "Compilation of $Lib failed"
        }
    } elseif ($IsWindows -and (-not $SMAOnly)) {
        log "Start building native Windows binaries"

        try {
            Push-Location "$PSScriptRoot\src\powershell-native"

            # Compile native resources
            $currentLocation = Get-Location
            @("nativemsh/pwrshplugin") | % {
                $nativeResourcesFolder = $_
                Get-ChildItem $nativeResourcesFolder -Filter "*.mc" | % {
                    $command = @"
cmd.exe /C cd /d "$currentLocation" "&" "$($vcVarsPath)\vcvarsall.bat" "$NativeHostArch" "&" mc.exe -o -d -c -U "$($_.FullName)" -h "$nativeResourcesFolder" -r "$nativeResourcesFolder"
"@
                    log "  Executing mc.exe Command: $command"
                    Start-NativeExecution { Invoke-Expression -Command:$command 2>&1 }
                }
            }

            function Build-NativeWindowsBinaries {
                param(
                    # Describes wither it should build the CoreCLR or FullCLR version
                    [ValidateSet("ON", "OFF")]
                    [string]$OneCoreValue,
                    
                    # Array of file names to copy from the local build directory to the packaging directory
                    [string[]]$FilesToCopy
                )

# Disabling until I figure out if it is necessary
#                $overrideFlags = "-DCMAKE_USER_MAKE_RULES_OVERRIDE=$PSScriptRoot\src\powershell-native\windows-compiler-override.txt"
                $overrideFlags = ""
                $location = Get-Location

                $command = @"
cmd.exe /C cd /d "$location" "&" "$($vcVarsPath)\vcvarsall.bat" "$NativeHostArch" "&" cmake "$overrideFlags" -DBUILD_ONECORE=$OneCoreValue -G "$cmakeGenerator" . "&" msbuild ALL_BUILD.vcxproj "/p:Configuration=$msbuildConfiguration"
"@
                log "  Executing Build Command: $command"
                Start-NativeExecution { Invoke-Expression -Command:$command }

                $clrTarget = "FullClr"
                if ($OneCoreValue -eq "ON")
                {
                    $clrTarget = "CoreClr"
                }

                # Copy the binaries from the local build directory to the packaging directory
                $dstPath = ($script:Options).Top
                $FilesToCopy | % {
                    $srcPath = Join-Path (Join-Path (Join-Path (Get-Location) "bin") $msbuildConfiguration) "$clrTarget/$_"
                    log "  Copying $srcPath to $dstPath"
                    Copy-Item $srcPath $dstPath
                }
            }
        
            if ($FullCLR) {
                $fullBinaries = @(  
                    'powershell.exe',
                    'powershell.pdb',
                    'pwrshplugin.dll',
                    'pwrshplugin.pdb'
                )
                Build-NativeWindowsBinaries "OFF" $fullBinaries 
            }
            else
            {
                $coreClrBinaries = @(  
                    'pwrshplugin.dll',
                    'pwrshplugin.pdb'
                )
                Build-NativeWindowsBinaries "ON" $coreClrBinaries

                # Place the remoting configuration script in the same directory
                # as the binary so it will get published.
                Copy-Item .\Install-PowerShellRemoting.ps1 ($script:Options).Top
            }
        } finally {
            Pop-Location
        }
    }

    # handle TypeGen
    if ($TypeGen -or -not (Test-Path "$PSScriptRoot/src/Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs")) {
        log "Run TypeGen (generating CorePsTypeCatalog.cs)"
        Start-TypeGen
    }

    try {
        # Relative paths do not work well if cwd is not changed to project
        Push-Location $Options.Top
        log "Run dotnet $Arguments from $pwd"
        Start-NativeExecution { dotnet $Arguments }

        if ($CrossGen) {
            $publishPath = Split-Path $Options.Output
            Start-CrossGen -PublishPath $publishPath
            log "PowerShell.exe with ngen binaries is available at: $($Options.Output)"
        } else {
            log "PowerShell output: $($Options.Output)"
        }
    } finally {
        Pop-Location
    }
}

function New-PSOptions {
    [CmdletBinding()]
    param(
        [ValidateSet("Linux", "Debug", "Release", "")]
        [string]$Configuration,

        [ValidateSet("netcoreapp1.0", "net451")]
        [string]$Framework,

        # These are duplicated from Start-PSBuild
        # We do not use ValidateScript since we want tab completion
        [ValidateSet("",
                     "ubuntu.14.04-x64",
                     "ubuntu.16.04-x64",
                     "debian.8-x64",
                     "centos.7-x64",
                     "win7-x64",
                     "win81-x64",
                     "win10-x64",
                     "osx.10.11-x64")]
        [string]$Runtime,

        [switch]$Publish,
        [string]$Output,

        [switch]$FullCLR,

        [switch]$SMAOnly
    )

    # Add .NET CLI tools to PATH
    Find-Dotnet

    if (-not $Configuration) {
        $Configuration = if ($IsLinux -or $IsOSX) {
            "Linux"
        } elseif ($IsWindows) {
            "Debug"
        }
        Write-Verbose "Using configuration '$Configuration'"
    }

    if ($FullCLR) {
        $Top = "$PSScriptRoot/src/powershell-win-full"
    } else {
        if ($Configuration -eq 'Linux')
        {
            $Top = "$PSScriptRoot/src/powershell-unix"
        }
        else
        {
            $Top = "$PSScriptRoot/src/powershell-win-core"
        }
    }
    Write-Verbose "Top project directory is $Top"


    if (-not $Framework) {
        $Framework = if ($FullCLR) {
            "net451"
        } else {
            "netcoreapp1.0"
        }
        Write-Verbose "Using framework '$Framework'"
    }

    if (-not $Runtime) {
        $Runtime = dotnet --info | % {
            if ($_ -match "RID") {
                $_ -split "\s+" | Select-Object -Last 1
            }
        }

        if (-not $Runtime) {
            Throw "Could not determine Runtime Identifier, please update dotnet"
        } else {
            Write-Verbose "Using runtime '$Runtime'"
        }
    }

    $Executable = if ($IsLinux -or $IsOSX) {
        "powershell"
    } elseif ($IsWindows) {
        "powershell.exe"
    }

    # Build the Output path
    if ($Output) {
        $Output = Join-Path $PSScriptRoot $Output
    } else {
        $Output = [IO.Path]::Combine($Top, "bin", $Configuration, $Framework, $Runtime)

        # Publish injects the publish directory
        if ($Publish -or $FullCLR) {
            $Output = [IO.Path]::Combine($Output, "publish")
        }

        $Output = [IO.Path]::Combine($Output, $Executable)
    }

    $RealFramework = $Framework
    if ($SMAOnly)
    {
        $Top = "$PSScriptRoot/src/System.Management.Automation"
        if ($Framework -match 'netcoreapp')
        {
            $RealFramework = 'netstandard1.6'
        }
    }

    return @{ Top = $Top;
              Configuration = $Configuration;
              Framework = $RealFramework;
              Runtime = $Runtime;
              Output = $Output }
}


function Get-PSOutput {
    [CmdletBinding()]param(
        [hashtable]$Options
    )
    if ($Options) {
        return $Options.Output
    } elseif ($script:Options) {
        return $script:Options.Output
    } else {
        return (New-PSOptions).Output
    }
}


function Get-PesterTag {
    param ( [Parameter(Position=0)][string]$testbase = "$PSScriptRoot/test/powershell" )
    $alltags = @{}
    $warnings = @()

    get-childitem -Recurse $testbase -File |?{$_.name -match "tests.ps1"}| %{
        $fullname = $_.fullname
        $tok = $err = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($FullName, [ref]$tok,[ref]$err)
        $des = $ast.FindAll({$args[0] -is "System.Management.Automation.Language.CommandAst" -and $args[0].CommandElements[0].Value -eq "Describe"},$true)
        foreach( $describe in $des) {
            $elements = $describe.CommandElements
            $lineno = $elements[0].Extent.StartLineNumber
            $foundTag = $false
            for ( $i = 0; $i -lt $elements.Count; $i++) {
                if ( $elements[$i].extent.text -match "^-t" ) {
                    $vAst = $elements[$i+1]
                    if ( $vAst.FindAll({$args[0] -is "System.Management.Automation.Language.VariableExpressionAst"},$true) ) {
                        $warnings += "TAGS must be static strings, error in ${fullname}, line $lineno"
                    }
                    $values = $vAst.FindAll({$args[0] -is "System.Management.Automation.Language.StringConstantExpressionAst"},$true).Value
                    $values | %{
                        if ( $_ -notmatch "CI|FEATURE|SCENARIO|SLOW" ) {
                            $warnings += "${fullname} includes improper tag '$_', line '$lineno'"
                        }
                        $alltags[$_]++
                        }
                    $foundTag = $true
                }
            }
            if ( ! $foundTag ) {
                $warnings += "${fullname} does not include -Tag in Describe, line '$lineno'"
            }
        }
    }
    if ( $Warnings.Count -gt 0 ) {
        $alltags['Result'] = "Fail"
    }
    else {
        $alltags['Result'] = "Pass"
    }
    $alltags['Warnings'] = $warnings
    $o = [pscustomobject]$alltags
    $o.psobject.TypeNames.Add("DescribeTagsInUse")
    $o
}

function Start-PSPester {
    [CmdletBinding()]
    param(
        [string]$OutputFormat = "NUnitXml",
        [string]$OutputFile = "pester-tests.xml",
        [string[]]$ExcludeTag = "Slow",
        [string[]]$Tag = "CI",
        [string]$Path = "$PSScriptRoot/test/powershell",
        [switch]$ThrowOnFailure,
        [switch]$FullCLR,
        [string]$binDir = (Split-Path (New-PSOptions -FullCLR:$FullCLR).Output),
        [string]$powershell = (Join-Path $binDir 'powershell'),
        [string]$Pester = ([IO.Path]::Combine($binDir, "Modules", "Pester"))
    )

    Write-Verbose "Running pester tests at '$path' with tag '$($Tag -join ''', ''')' and ExcludeTag '$($ExcludeTag -join ''', ''')'" -Verbose
    # All concatenated commands/arguments are suffixed with the delimiter (space)
    $Command = ""

    # Windows needs the execution policy adjusted
    if ($IsWindows) {
        $Command += "Set-ExecutionPolicy -Scope Process Unrestricted; "
    }
    $startParams = @{binDir=$binDir}

    if(!$FullCLR)
    {
        $Command += "Import-Module '$Pester'; "
    }
    $Command += "Invoke-Pester "

    $Command += "-OutputFormat ${OutputFormat} -OutputFile ${OutputFile} "
    if ($ExcludeTag -and ($ExcludeTag -ne "")) {
        $Command += "-ExcludeTag @('" + (${ExcludeTag} -join "','") + "') "
    }
    if ($Tag) {
        $Command += "-Tag @('" + (${Tag} -join "','") + "') "
    }

    $Command += "'" + $Path + "'"
        
    Write-Verbose $Command

    # To ensure proper testing, the module path must not be inherited by the spawned process
    if($FullCLR)
    {
        Start-DevPowerShell -binDir $binDir -FullCLR -NoNewWindow -ArgumentList '-noprofile', '-noninteractive' -Command $command
    }
    else {
        try {
            $originalModulePath = $env:PSMODULEPATH
            
            & $powershell -noprofile -c $Command
        } finally {
            $env:PSMODULEPATH = $originalModulePath
        }        
    }
    if($ThrowOnFailure)
    {
        Test-PSPesterResults -TestResultsFile $OutputFile
    }
}

#
# Read the test result file and
# Throw if a test failed 
function Test-PSPesterResults
{
    param(
        [string]$TestResultsFile = "pester-tests.xml",
        [string] $TestArea = 'test/powershell'
    )

    if(!(Test-Path $TestResultsFile))
    {
        throw "Test result file '$testResultsFile' not found for $TestArea."
    } 

    $x = [xml](Get-Content -raw $testResultsFile)
    if ([int]$x.'test-results'.failures -gt 0)
    {
        throw "$($x.'test-results'.failures) tests in $TestArea failed"
    }
}


function Start-PSxUnit {
    [CmdletBinding()]param()

    log "xUnit tests are currently disabled pending fixes due to API and AssemblyLoadContext changes - @andschwa"
    return

    if ($IsWindows) {
        throw "xUnit tests are only currently supported on Linux / OS X"
    }

    if ($IsOSX) {
        log "Not yet supported on OS X, pretending they passed..."
        return
    }

    # Add .NET CLI tools to PATH
    Find-Dotnet

    $Arguments = "--configuration", "Linux", "-parallel", "none"
    if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) {
        $Arguments += "-verbose"
    }

    $Content = Split-Path -Parent (Get-PSOutput)
    if (-not (Test-Path $Content)) {
        throw "PowerShell must be built before running tests!"
    }

    try {
        Push-Location $PSScriptRoot/test/csharp
        # Path manipulation to obtain test project output directory
        $Output = Join-Path $pwd ((Split-Path -Parent (Get-PSOutput)) -replace (New-PSOptions).Top)
        Write-Verbose "Output is $Output"

        Copy-Item -ErrorAction SilentlyContinue -Recurse -Path $Content/* -Include Modules,libpsl-native* -Destination $Output
        Start-NativeExecution { dotnet test $Arguments }

        if ($LASTEXITCODE -ne 0) {
            throw "$LASTEXITCODE xUnit tests failed"
        }
    } finally {
        Pop-Location
    }
}


function Install-Dotnet {
    [CmdletBinding()]
    param(
        [string]$Channel = "rel-1.0.0",
        [string]$Version = "latest",
        [switch]$NoSudo
    )

    # This allows sudo install to be optional; needed when running in containers / as root
    # Note that when it is null, Invoke-Expression (but not &) must be used to interpolate properly
    $sudo = if (!$NoSudo) { "sudo" }

    $obtainUrl = "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain"

    # Install for Linux and OS X
    if ($IsLinux -or $IsOSX) {
        # Uninstall all previous dotnet packages
        $uninstallScript = if ($IsUbuntu) {
            "dotnet-uninstall-debian-packages.sh"
        } elseif ($IsOSX) {
            "dotnet-uninstall-pkgs.sh"
        }

        if ($uninstallScript) {
            Start-NativeExecution {
                curl -sO $obtainUrl/uninstall/$uninstallScript
                Invoke-Expression "$sudo bash ./$uninstallScript"
            }
        } else {
            Write-Warning "This script only removes prior versions of dotnet for Ubuntu 14.04 and OS X"
        }

        # Install new dotnet 1.0.0 preview packages
        $installScript = "dotnet-install.sh"
        Start-NativeExecution {
            curl -sO $obtainUrl/$installScript
            bash ./$installScript -c $Channel -v $Version
        }

        # .NET Core's crypto library needs brew's OpenSSL libraries added to its rpath
        if ($IsOSX) {
            # This is the library shipped with .NET Core
            # This is allowed to fail as the user may have installed other versions of dotnet
            Write-Warning ".NET Core links the incorrect OpenSSL, correcting .NET CLI libraries..."
            find $env:HOME/.dotnet -name System.Security.Cryptography.Native.dylib | xargs sudo install_name_tool -add_rpath /usr/local/opt/openssl/lib
        }
    } elseif ($IsWindows) {
        Remove-Item -ErrorAction SilentlyContinue -Recurse -Force ~\AppData\Local\Microsoft\dotnet
        $installScript = "dotnet-install.ps1"
        Invoke-WebRequest -Uri $obtainUrl/$installScript -OutFile $installScript
        & ./$installScript -c $Channel -v $Version
    }
}


function Start-PSBootstrap {
    [CmdletBinding(
        SupportsShouldProcess=$true,
        ConfirmImpact="High")]
    param(
        [string]$Channel = "rel-1.0.0",
        [string]$Version = "latest",
        [switch]$Package,
        [switch]$NoSudo,
        [switch]$Force
    )

    log "Installing PowerShell build dependencies"

    Push-Location $PSScriptRoot/tools

    # This allows sudo install to be optional; needed when running in containers / as root
    # Note that when it is null, Invoke-Expression (but not &) must be used to interpolate properly
    $sudo = if (!$NoSudo) { "sudo" }

    try {
        # Update googletest submodule for linux native cmake
        if ($IsLinux -or $IsOSX) {
            try {
                Push-Location $PSScriptRoot
                $Submodule = "$PSScriptRoot/src/libpsl-native/test/googletest"
                Remove-Item -Path $Submodule -Recurse -Force -ErrorAction SilentlyContinue
                git submodule update --init -- $submodule
            } finally {
                Pop-Location
            }
        }

        # Install ours and .NET's dependencies
        $Deps = @()
        if ($IsUbuntu) {
            # Build tools
            $Deps += "curl", "g++", "cmake", "make"

            # .NET Core required runtime libraries
            $Deps += "libunwind8"
            if ($IsUbuntu14) { $Deps += "libicu52" }
            elseif ($IsUbuntu16) { $Deps += "libicu55" }

            # Packaging tools
            if ($Package) { $Deps += "ruby-dev", "groff" }

            # Install dependencies
            Start-NativeExecution {
                Invoke-Expression "$sudo apt-get update"
                Invoke-Expression "$sudo apt-get install -y -qq $Deps"
            }
        } elseif ($IsCentOS) {
            # Build tools
            $Deps += "which", "curl", "gcc-c++", "cmake", "make"

            # .NET Core required runtime libraries
            $Deps += "libicu", "libunwind"

            # Packaging tools
            if ($Package) { $Deps += "ruby-devel", "rpm-build", "groff" }

            # Install dependencies
            Start-NativeExecution {
                Invoke-Expression "$sudo yum install -y -q $Deps"
            }
        } elseif ($IsOSX) {
            precheck 'brew' "Bootstrap dependency 'brew' not found, must install Homebrew! See http://brew.sh/"

            # Build tools
            $Deps += "curl", "cmake"

            # .NET Core required runtime libraries
            $Deps += "openssl"

            # Install dependencies
            Start-NativeExecution { brew install $Deps }
        }

        # Install [fpm](https://github.com/jordansissel/fpm) and [ronn](https://github.com/rtomayko/ronn)
        if ($Package) {
            try {
                # We cannot guess if the user wants to run gem install as root
                Start-NativeExecution { gem install fpm ronn }
            } catch {
                Write-Warning "Installation of fpm and ronn gems failed! Must resolve manually."
            }
        }

        $DotnetArguments = @{ Channel=$Channel; Version=$Version; NoSudo=$NoSudo }
        Install-Dotnet @DotnetArguments

        # Install for Windows
        if ($IsWindows) {
            $machinePath = [Environment]::GetEnvironmentVariable('Path', 'MACHINE')
            $newMachineEnvironmentPath = $machinePath

            $cmakePresent = precheck 'cmake' $null
            $win10sdkBinPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64"
            $sdkPresent = Test-Path -Path $win10sdkBinPath -PathType Container

            # Install chocolatey
            $chocolateyPath = "$env:AllUsersProfile\chocolatey\bin"

            if(precheck 'choco' $null) {
                log "Chocolatey is already installed. Skipping installation."
            }
            elseif(($cmakePresent -eq $false) -or ($sdkPresent -eq $false)) {
                log "Chocolatey not present. Installing chocolatey."
                if ($Force -or $PSCmdlet.ShouldProcess("Install chocolatey via https://chocolatey.org/install.ps1")) {
                    Invoke-Expression ((new-object net.webclient).DownloadString('https://chocolatey.org/install.ps1'))
                    if (-not ($machinePath.ToLower().Contains($chocolateyPath.ToLower()))) {
                        log "Adding $chocolateyPath to Path environment variable"
                        $env:Path += ";$chocolateyPath"
                        $newMachineEnvironmentPath += ";$chocolateyPath"
                    } else {
                        log "$chocolateyPath already present in Path environment variable"
                    }
                } else {
                    Write-Error "Chocolatey is required to install missing dependencies. Please install it from https://chocolatey.org/ manually. Alternatively, install cmake and Windows 10 SDK."
                    return $null
                }
            } else {
                log "Skipping installation of chocolatey, cause both cmake and Win 10 SDK are present."
            }

            # Install cmake
            $cmakePath = "${env:ProgramFiles}\CMake\bin"
            if($cmakePresent) {
                log "Cmake is already installed. Skipping installation."
            } else {
                log "Cmake not present. Installing cmake."
                choco install cmake -y --version 3.6.0
                if (-not ($machinePath.ToLower().Contains($cmakePath.ToLower()))) {
                    log "Adding $cmakePath to Path environment variable"
                    $env:Path += ";$cmakePath"
                    $newMachineEnvironmentPath = "$cmakePath;$newMachineEnvironmentPath"
                } else {
                    log "$cmakePath already present in Path environment variable"
                }
            }

            # Install Windows 10 SDK
            $packageName = "windows-sdk-10.0"

            if (-not $sdkPresent) {
                log "Windows 10 SDK not present. Installing $packageName."
                choco install windows-sdk-10.0 -y
            } else {
                log "Windows 10 SDK present. Skipping installation."
            }

            # Update path machine environment variable
            if ($newMachineEnvironmentPath -ne $machinePath) {
                log "Updating Path machine environment variable"
                if ($Force -or $PSCmdlet.ShouldProcess("Update Path machine environment variable to $newMachineEnvironmentPath")) {
                    [Environment]::SetEnvironmentVariable('Path', $newMachineEnvironmentPath, 'MACHINE')
                }
            }

        }
    } finally {
        Pop-Location
    }
}


function Start-PSPackage {
    [CmdletBinding()]param(
        # PowerShell packages use Semantic Versioning http://semver.org/
        [string]$Version,

        # Package name
        [ValidatePattern("^powershell")]
        [string]$Name = "powershell",

        # Ubuntu, CentOS, and OS X, and Windows packages are supported
        [ValidateSet("deb", "osxpkg", "rpm", "msi", "appx")]
        [string[]]$Types
    )

    # Use Git tag if not given a version
    if (-not $Version) {
        $Version = (git --git-dir="$PSScriptRoot/.git" describe) -Replace '^v'
    }

    if ($IsWindows) {
        Write-Warning "Please ensure you have previously run Start-PSBuild -Clean -CrossGen -Configuration Release!"
        $Source = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -Publish -Configuration Release))
    } else {
        Write-Warning "Please ensure you have previously run Start-PSBuild -Clean -CrossGen!"
        $Source = Split-Path -Parent (Get-PSOutput -Options (New-PSOptions -Publish))
    }

    Write-Verbose "Packaging $Source"

    # Decide package output type
    if (-not $Types) {
        $Types = if ($IsLinux) {
            if ($LinuxInfo.ID -match "ubuntu") {
                "deb"
            } elseif ($LinuxInfo.ID -match "centos") {
                "rpm"
            } else {
                throw "Building packages for $($LinuxInfo.PRETTY_NAME) is unsupported!"
            }
        } elseif ($IsOSX) {
            "osxpkg"
        } elseif ($IsWindows) {
            "msi", "appx"
        }
        Write-Warning "-Types was not specified, continuing with $Types!"
    }

    switch ($Types) {
        "msi" {
            $Arguments = @{
                ProductSourcePath = $Source;
                ProductVersion = $Version;
                AssetsPath = "$PSScriptRoot\assets";
                LicenseFilePath = "$PSScriptRoot\assets\license.rtf";
                # Product Guid needs to be unique for every PowerShell version to allow SxS install
                ProductGuid = [Guid]::NewGuid()
            }
            New-MSIPackage @Arguments
        }
        "appx" {
            $Arguments = @{
                PackageVersion = $Version;
                SourcePath = $Source;
                AssetsPath = "$PSScriptRoot\assets"
            }
            New-AppxPackage @Arguments
        }
        default {
            $Arguments = @{
                Type = $_;
                Source = $Source;
                Name = $Name;
                Version = $Version
            }
            New-UnixPackage @Arguments
        }
    }
}


function New-UnixPackage {
    [CmdletBinding()]param(
        [Parameter(Mandatory)]
        [ValidateSet("deb", "osxpkg", "rpm")]
        [string]$Type,

        [Parameter(Mandatory)]
        [string]$Source,

        # Must start with 'powershell' but may have any suffix
        [Parameter(Mandatory)]
        [ValidatePattern("^powershell")]
        [string]$Name,

        [Parameter(Mandatory)]
        [string]$Version,

        # Package iteration version (rarely changed)
        # This is a string because strings are appended to it
        [string]$Iteration = "1"
    )

    # Validate platform
    $ErrorMessage = "Must be on {0} to build '$Type' packages!"
    switch ($Type) {
        "deb" {
            $WarningMessage = "Building for Ubuntu {0}.04!"
            if (!$IsUbuntu) {
                    throw ($ErrorMessage -f "Ubuntu")
                } elseif ($IsUbuntu14) {
                    Write-Warning ($WarningMessage -f "14")
                } elseif ($IsUbuntu16) {
                    Write-Warning ($WarningMessage -f "16")
                }
        }
        "rpm" {
            if (!$IsCentOS) {
                throw ($ErrorMessage -f "CentOS")
            }
        }
        "osxpkg" {
            if (!$IsOSX) {
                throw ($ErrorMessage -f "OS X")
            }
        }
    }

    foreach ($Dependency in "fpm", "ronn") {
        if (!(precheck $Dependency "Package dependency '$Dependency' not found. Run Start-PSBootstrap -Publish")) {
            throw "Dependency precheck failed!"
        }
    }

    $Description = @"
PowerShell is an automation and configuration management platform.
It consists of a cross-platform command-line shell and associated scripting language.
"@

    # Suffix is used for side-by-side package installation
    $Suffix = $Name -replace "^powershell"
    if (!$Suffix) {
        Write-Warning "Suffix not given, building primary PowerShell package!"
        $Suffix = $Version
    }

    # Setup staging directory so we don't change the original source directory
    $Staging = "$PSScriptRoot/staging"
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $Staging
    Copy-Item -Recurse $Source $Staging

    # Rename files to given name if not "powershell"
    if ($Name -ne "powershell") {
        $Files = @("powershell",
                   "powershell.dll",
                   "powershell.deps.json",
                   "powershell.pdb",
                   "powershell.runtimeconfig.json",
                   "powershell.xml")

        foreach ($File in $Files) {
            $NewName = $File -replace "^powershell", $Name
            Move-Item "$Staging/$File" "$Staging/$NewName"
        }
    }

    # Follow the Filesystem Hierarchy Standard for Linux and OS X
    $Destination = if ($IsLinux) {
        "/opt/microsoft/powershell/$Suffix"
    } elseif ($IsOSX) {
        "/usr/local/microsoft/powershell/$Suffix"
    }

    # Destination for symlink to powershell executable
    $Link = if ($IsLinux) {
        "/usr/bin"
    } elseif ($IsOSX) {
        "/usr/local/bin"
    }

    New-Item -Force -ItemType SymbolicLink -Path "/tmp/$Name" -Target "$Destination/$Name" >$null

    if ($IsCentos) {
        $AfterInstallScript = [io.path]::GetTempFileName()
        $AfterRemoveScript = [io.path]::GetTempFileName()
        @'
#!/bin/sh
if [ ! -f /etc/shells ] ; then
    echo "{0}" > /etc/shells
else
    grep -q "^{0}$" /etc/shells || echo "{0}" >> /etc/shells
fi
'@ -f "$Link/$Name" | Out-File -FilePath $AfterInstallScript -Encoding ascii

        @'
if [ "$1" = 0 ] ; then
    if [ -f /etc/shells ] ; then
        TmpFile=`/bin/mktemp /tmp/.powershellmXXXXXX`
        grep -v '^{0}$' /etc/shells > $TmpFile
        cp -f $TmpFile /etc/shells
        rm -f $TmpFile
    fi
fi
'@ -f "$Link/$Name" | Out-File -FilePath $AfterRemoveScript -Encoding ascii
    }
    elseif ($IsUbuntu) {
        $AfterInstallScript = [io.path]::GetTempFileName()
        $AfterRemoveScript = [io.path]::GetTempFileName()
        @'
#!/bin/sh
set -e
case "$1" in
    (configure)
        add-shell "{0}"
    ;;
    (abort-upgrade|abort-remove|abort-deconfigure)
        exit 0
    ;;
    (*)
        echo "postinst called with unknown argument '$1'" >&2
        exit 0
    ;;
esac
'@ -f "$Link/$Name" | Out-File -FilePath $AfterInstallScript -Encoding ascii

        @'
#!/bin/sh
set -e
case "$1" in
        (remove)
        remove-shell "{0}"
        ;;
esac
'@ -f "$Link/$Name" | Out-File -FilePath $AfterRemoveScript -Encoding ascii
    }


    # there is a weird bug in fpm
    # if the target of the powershell symlink exists, `fpm` aborts
    # with a `utime` error on OS X.
    # so we move it to make symlink broken
    $symlink_dest = "$Destination/$Name"
    $hack_dest = "./_fpm_symlink_hack_powershell"
    if ($IsOSX) {
        if (Test-Path $symlink_dest) {
            Write-Warning "Move $symlink_dest to $hack_dest (fpm utime bug)"
            Move-Item $symlink_dest $hack_dest
        }
    }

    # run ronn to convert man page to roff
    $RonnFile = Join-Path $PSScriptRoot "/assets/powershell.1.ronn"
    $RoffFile = $RonnFile -replace "\.ronn$"

    # Run ronn on assets file
    # Run does not play well with files named powershell6.0.1, so we generate and then rename
    Start-NativeExecution { ronn --roff $RonnFile }

    # Setup for side-by-side man pages (noop if primary package)
    $FixedRoffFile = $RoffFile -replace "powershell.1$", "$Name.1"
    if ($Name -ne "powershell") {
        Move-Item $RoffFile $FixedRoffFile
    }

    # gzip in assets directory
    $GzipFile = "$FixedRoffFile.gz"
    Start-NativeExecution { gzip -f $FixedRoffFile }

    $ManFile = Join-Path "/usr/local/share/man/man1" (Split-Path -Leaf $GzipFile)

    # Change permissions for packaging
    Start-NativeExecution {
        find $Staging -type d | xargs chmod 755
        find $Staging -type f | xargs chmod 644
        chmod 644 $GzipFile
        chmod 755 "$Staging/$Name" # only the executable should be executable
    }

    $libunwind = switch ($Type) {
        "deb" { "libunwind8" }
        "rpm" { "libunwind" }
    }

    $libicu = switch ($Type) {
        "deb" {
            if ($IsUbuntu14) { "libicu52" }
            elseif ($IsUbuntu16) { "libicu55" }
        }
        "rpm" { "libicu" }
    }

    # iteration is "debian_revision"
    # usage of this to differentiate distributions is allowed by non-standard
    if ($IsUbuntu14) {
        $Iteration += "ubuntu1.14.04.1"
    } elseif ($IsUbuntu16) {
        $Iteration += "ubuntu1.16.04.1"
    }

    # We currently only support CentOS 7
    # https://fedoraproject.org/wiki/Packaging:DistTag
    $rpm_dist = "el7.centos"

    $Arguments = @(
        "--force", "--verbose",
        "--name", $Name,
        "--version", $Version,
        "--iteration", $Iteration,
        "--rpm-dist", $rpm_dist,
        "--maintainer", "PowerShell Team <PowerShellTeam@hotmail.com>",
        "--vendor", "Microsoft Corporation",
        "--url", "https://microsoft.com/powershell",
        "--license", "MIT License",
        "--description", $Description,
        "--category", "shells",
        "--rpm-os", "linux",
        "--depends", $libunwind,
        "--depends", $libicu,
        "-t", $Type,
        "-s", "dir"
    )
    if ($AfterInstallScript) {
       $Arguments += @("--after-install", $AfterInstallScript)
    }
    if ($AfterRemoveScript) {
       $Arguments += @("--after-remove", $AfterRemoveScript)
    }
    $Arguments += @(
        "$Staging/=$Destination/",
        "$GzipFile=$ManFile",
        "/tmp/$Name=$Link"
    )
    # Build package
    try {
        $Output = Start-NativeExecution { fpm $Arguments }
    } finally {
        if ($IsOSX) {
            # this is continuation of a fpm hack for a weird bug
            if (Test-Path $hack_dest) {
                Write-Warning "Move $hack_dest to $symlink_dest (fpm utime bug)"
                Move-Item $hack_dest $symlink_dest
            }
        }
        if ($AfterInstallScript) {
           Remove-Item -erroraction 'silentlycontinue' $AfterInstallScript
        }
        if ($AfterRemoveScript) {
           Remove-Item -erroraction 'silentlycontinue' $AfterRemoveScript
        }
    }

    # Magic to get path output
    return Get-Item (Join-Path $PSScriptRoot (($Output[-1] -split ":path=>")[-1] -replace '["{}]'))
}


function Publish-NuGetFeed
{
    param(
        [string]$OutputPath = "$PSScriptRoot/nuget-artifacts",
        [Parameter(Mandatory=$true)]
        [string]$VersionSuffix
    )

    # Add .NET CLI tools to PATH
    Find-Dotnet

    @(
'Microsoft.PowerShell.Commands.Management',
'Microsoft.PowerShell.Commands.Utility',
'Microsoft.PowerShell.ConsoleHost',
'Microsoft.PowerShell.Security',
'System.Management.Automation',
'Microsoft.PowerShell.CoreCLR.AssemblyLoadContext',
'Microsoft.PowerShell.CoreCLR.Eventing',
'Microsoft.WSMan.Management',
'Microsoft.WSMan.Runtime',
'Microsoft.PowerShell.SDK'
    ) | % {
        if ($VersionSuffix) {
            dotnet pack "src/$_" --output $OutputPath --version-suffix $VersionSuffix
        } else {
            dotnet pack "src/$_" --output $OutputPath
        }
    }
}

function Start-DevPowerShell {
    param(
        [switch]$FullCLR,
        [switch]$ZapDisable,
        [string[]]$ArgumentList = '',
        [switch]$LoadProfile,
        [string]$binDir = (Split-Path (New-PSOptions -FullCLR:$FullCLR).Output),
        [switch]$NoNewWindow,
        [string]$Command,
        [switch]$KeepPSModulePath
    )

    try {
        if ((-not $NoNewWindow) -and ($IsCoreCLR)) {
            Write-Warning "Start-DevPowerShell -NoNewWindow is currently implied in PowerShellCore edition https://github.com/PowerShell/PowerShell/issues/1543"
            $NoNewWindow = $true
        }

        if (-not $LoadProfile) {
            $ArgumentList = @('-noprofile') + $ArgumentList
        }

        if (-not $KeepPSModulePath) {
            if (-not $Command) {
                $ArgumentList = @('-NoExit') + $ArgumentList
            }
            $Command = '$env:PSMODULEPATH = Join-Path $env:DEVPATH Modules; ' + $Command
        }

        if ($Command) {
            $ArgumentList = $ArgumentList + @("-command $Command")
        }

        $env:DEVPATH = $binDir
        if ($ZapDisable) {
            $env:COMPLUS_ZapDisable = 1
        }

        if ($FullCLR -and (-not (Test-Path $binDir\powershell.exe.config))) {
            $configContents = @"
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
<runtime>
<developmentMode developerInstallation="true"/>
</runtime>
</configuration>
"@
            $configContents | Out-File -Encoding Ascii $binDir\powershell.exe.config
        }

        # splatting for the win
        $startProcessArgs = @{
            FilePath = "$binDir\powershell"
            ArgumentList = "$ArgumentList"
        }

        if ($NoNewWindow) {
            $startProcessArgs.NoNewWindow = $true
            $startProcessArgs.Wait = $true
        }

        Start-Process @startProcessArgs
    } finally {
        if($env:DevPath)
        {
            Remove-Item env:DEVPATH
        }
        
        if ($ZapDisable) {
            Remove-Item env:COMPLUS_ZapDisable
        }
    }
}


<#
.EXAMPLE
PS C:> Copy-MappedFiles -PslMonadRoot .\src\monad

copy files FROM .\src\monad (old location of submodule) TO src/<project> folders
#>
function Copy-MappedFiles {

    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true)]
        [string[]]$Path = "$PSScriptRoot",
        [Parameter(Mandatory=$true)]
        [string]$PslMonadRoot,
        [switch]$Force,
        [switch]$WhatIf
    )

    begin {
        function MaybeTerminatingWarning {
            param([string]$Message)

            if ($Force) {
                Write-Warning "$Message : ignoring (-Force)"
            } elseif ($WhatIf) {
                Write-Warning "$Message : ignoring (-WhatIf)"
            } else {
                throw "$Message : use -Force to ignore"
            }
        }

        if (-not (Test-Path -PathType Container $PslMonadRoot)) {
            throw "$pslMonadRoot is not a valid folder"
        }

        # Do some intelligence to prevent shooting us in the foot with CL management

        # finding base-line CL
        $cl = git --git-dir="$PSScriptRoot/.git" tag | % {if ($_ -match 'SD.(\d+)$') {[int]$Matches[1]} } | Sort-Object -Descending | Select-Object -First 1
        if ($cl) {
            log "Current base-line CL is SD:$cl (based on tags)"
        } else {
            MaybeTerminatingWarning "Could not determine base-line CL based on tags"
        }

        try {
            Push-Location $PslMonadRoot
            if (git status --porcelain -uno) {
                MaybeTerminatingWarning "$pslMonadRoot has changes"
            }

            if (git log --grep="SD:$cl" HEAD^..HEAD) {
                log "$pslMonadRoot HEAD matches [SD:$cl]"
            } else {
                Write-Warning "Try to checkout this commit in $pslMonadRoot :"
                git log --grep="SD:$cl" | Write-Warning
                MaybeTerminatingWarning "$pslMonadRoot HEAD doesn't match [SD:$cl]"
            }
        } finally {
            Pop-Location
        }

        $map = @{}
    }

    process {
        $map += Get-Mappings $Path -Root $PslMonadRoot
    }

    end {
        $map.GetEnumerator() | % {
            New-Item -ItemType Directory (Split-Path $_.Value) -ErrorAction SilentlyContinue > $null
            Copy-Item $_.Key $_.Value -Verbose:([bool]$PSBoundParameters['Verbose']) -WhatIf:$WhatIf
        }
    }
}

function Get-Mappings
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true)]
        [string[]]$Path = "$PSScriptRoot",
        [string]$Root,
        [switch]$KeepRelativePaths
    )

    begin {
        $mapFiles = @()
    }

    process {
        Write-Verbose "Discovering map files in $Path"
        $count = $mapFiles.Count

        if (-not (Test-Path $Path)) {
            throw "Mapping file not found in $mappingFilePath"
        }

        if (Test-Path -PathType Container $Path) {
            $mapFiles += Get-ChildItem -Recurse $Path -Filter 'map.json' -File
        } else {
            # it exists and it's a file, don't check the name pattern
            $mapFiles += Get-ChildItem $Path
        }

        Write-Verbose "Found $($mapFiles.Count - $count) map files in $Path"
    }

    end {
        $map = @{}
        $mapFiles | % {
            $file = $_
            try {
                $rawHashtable = $_ | Get-Content -Raw | ConvertFrom-Json | Convert-PSObjectToHashtable
            } catch {
                Write-Error "Exception, when processing $($file.FullName): $_"
            }

            $mapRoot = Split-Path $_.FullName
            if ($KeepRelativePaths) {
                # not very elegant way to find relative for the current directory path
                $mapRoot = $mapRoot.Substring($PSScriptRoot.Length + 1)
                # keep original unix-style paths for git
                $mapRoot = $mapRoot.Replace('\', '/')
            }

            $rawHashtable.GetEnumerator() | % {
                $newKey = if ($Root) { Join-Path $Root $_.Key } else { $_.Key }
                $newValue = if ($KeepRelativePaths) { ($mapRoot + '/' + $_.Value) } else { Join-Path $mapRoot $_.Value }
                $map[$newKey] = $newValue
            }
        }

        return $map
    }
}


<#
.EXAMPLE Send-GitDiffToSd -diffArg1 32b90c048aa0c5bc8e67f96a98ea01c728c4a5be~1 -diffArg2 32b90c048aa0c5bc8e67f96a98ea01c728c4a5be -AdminRoot d:\e\ps_dev\admin
Apply a single commit to admin folder
#>
function Send-GitDiffToSd {
    param(
        [Parameter(Mandatory)]
        [string]$diffArg1,
        [Parameter(Mandatory)]
        [string]$diffArg2,
        [Parameter(Mandatory)]
        [string]$AdminRoot,
        [switch]$WhatIf
    )

    # this is only for windows, because you cannot have SD enlistment on Linux
    $patchPath = (ls (Join-Path (get-command git).Source '..\..') -Recurse -Filter 'patch.exe').FullName
    $m = Get-Mappings -KeepRelativePaths -Root $AdminRoot
    $affectedFiles = git diff --name-only $diffArg1 $diffArg2
    $affectedFiles | % {
        log "Changes in file $_"
    }

    $rev = Get-InvertedOrderedMap $m
    foreach ($file in $affectedFiles) {
        if ($rev.Contains) {
            $sdFilePath = $rev[$file]
            if (-not $sdFilePath)
            {
                Write-Warning "Cannot find mapped file for $file, skipping"
                continue
            }

            $diff = git diff $diffArg1 $diffArg2 -- $file
            if ($diff) {
                log "Apply patch to $sdFilePath"
                Set-Content -Value $diff -Path $env:TEMP\diff -Encoding Ascii
                if ($WhatIf) {
                    log "Patch content"
                    Get-Content $env:TEMP\diff
                } else {
                    & $patchPath --binary -p1 $sdFilePath $env:TEMP\diff
                }
            } else {
                log "No changes in $file"
            }
        } else {
            log "Ignore changes in $file, because there is no mapping for it"
        }
    }
}

function Start-TypeGen
{
    [CmdletBinding()]
    param()

    # Add .NET CLI tools to PATH
    Find-Dotnet

    Push-Location "$PSScriptRoot/src/TypeCatalogParser"
    try {
        dotnet run
    } finally {
        Pop-Location
    }

    Push-Location "$PSScriptRoot/src/TypeCatalogGen"
    try {
        dotnet run ../Microsoft.PowerShell.CoreCLR.AssemblyLoadContext/CorePsTypeCatalog.cs powershell.inc
    } finally {
        Pop-Location
    }
}

function Start-ResGen
{
    [CmdletBinding()]
    param()

    # Add .NET CLI tools to PATH
    Find-Dotnet

    Push-Location "$PSScriptRoot/src/ResGen"
    try {
        Start-NativeExecution { dotnet run } | Write-Verbose
    } finally {
        Pop-Location
    }
}


function Find-Dotnet() {
    $originalPath = $env:PATH
    $dotnetPath = if ($IsWindows) {
        "$env:LocalAppData\Microsoft\dotnet"
    } else {
        "$env:HOME/.dotnet"
    }

    if (-not (precheck 'dotnet' "Could not find 'dotnet', appending $dotnetPath to PATH.")) {
        $env:PATH += [IO.Path]::PathSeparator + $dotnetPath
    }

    if (-not (precheck 'dotnet' "Still could not find 'dotnet', restoring PATH.")) {
        $env:PATH = $originalPath
    }
}

<#
    This is one-time conversion. We use it for to turn GetEventResources.txt into GetEventResources.resx

    .EXAMPLE Convert-TxtResourceToXml -Path Microsoft.PowerShell.Commands.Diagnostics\resources
#>
function Convert-TxtResourceToXml
{
    param(
        [string[]]$Path
    )

    process {
        $Path | % {
            Get-ChildItem $_ -Filter "*.txt" | % {
                $txtFile = $_.FullName
                $resxFile = Join-Path (Split-Path $txtFile) "$($_.BaseName).resx"
                $resourceHashtable = ConvertFrom-StringData (Get-Content -Raw $txtFile)
                $resxContent = $resourceHashtable.GetEnumerator() | % {
@'
  <data name="{0}" xml:space="preserve">
    <value>{1}</value>
  </data>
'@ -f $_.Key, $_.Value
                } | Out-String
                Set-Content -Path $resxFile -Value ($script:RESX_TEMPLATE -f $resxContent)
            }
        }
    }
}

function Start-XamlGen
{
    [CmdletBinding()]
    param(
        [Parameter()]
        [ValidateSet("Debug", "Release")]
        [string]
        $MSBuildConfiguration = "Release"
    )

    Use-MSBuild
    Get-ChildItem -Path "$PSScriptRoot/src" -Directory | % {
        $XamlDir = Join-Path -Path $_.FullName -ChildPath Xamls
        if ((Test-Path -Path $XamlDir -PathType Container) -and
            (@(Get-ChildItem -Path "$XamlDir\*.xaml").Count -gt 0)) {
            $OutputDir = Join-Path -Path $env:TEMP -ChildPath "_Resolve_Xaml_"
            Remove-Item -Path $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
            mkdir -Path $OutputDir -Force > $null

            # we will get failures, but it's ok: we only need to copy *.g.cs files in the dotnet cli project.
            $SourceDir = ConvertFrom-Xaml -Configuration $MSBuildConfiguration -OutputDir $OutputDir -XamlDir $XamlDir -IgnoreMsbuildFailure:$true
            $DestinationDir = Join-Path -Path $_.FullName -ChildPath gen

            New-Item -ItemType Directory $DestinationDir -ErrorAction SilentlyContinue > $null
            $filesToCopy = Get-Item "$SourceDir\*.cs", "$SourceDir\*.g.resources"
            if (-not $filesToCopy) {
                throw "No .cs or .g.resources files are generated for $XamlDir, something went wrong. Run 'Start-XamlGen -Verbose' for details."
            }

            $filesToCopy | % {
                $sourcePath = $_.FullName
                Write-Verbose "Copy generated xaml artifact: $sourcePath -> $DestinationDir"
                Copy-Item -Path $sourcePath -Destination $DestinationDir
            }
        }
    }
}

$Script:XamlProj = @"
<Project DefaultTargets="ResolveAssemblyReferences;MarkupCompilePass1;PrepareResources" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
        <Language>C#</Language>
        <AssemblyName>Microsoft.PowerShell.Activities</AssemblyName>
        <OutputType>library</OutputType>
        <Configuration>{0}</Configuration>
        <Platform>Any CPU</Platform>
        <OutputPath>{1}</OutputPath>
        <Do_CodeGenFromXaml>true</Do_CodeGenFromXaml>
    </PropertyGroup>

    <Import Project="`$(MSBuildBinPath)\Microsoft.CSharp.targets" />
    <Import Project="`$(MSBuildBinPath)\Microsoft.WinFX.targets" Condition="'`$(TargetFrameworkVersion)' == 'v2.0' OR '`$(TargetFrameworkVersion)' == 'v3.0' OR '`$(TargetFrameworkVersion)' == 'v3.5'" />

    <ItemGroup>
{2}
        <Reference Include="WindowsBase.dll">
            <Private>False</Private>
        </Reference>
        <Reference Include="PresentationCore.dll">
            <Private>False</Private>
        </Reference>
        <Reference Include="PresentationFramework.dll">
            <Private>False</Private>
        </Reference>
    </ItemGroup>
</Project>
"@

$Script:XamlProjPage = @'
        <Page Include="{0}" />

'@

function script:ConvertFrom-Xaml {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [string] $Configuration,

        [Parameter(Mandatory=$true)]
        [string] $OutputDir,

        [Parameter(Mandatory=$true)]
        [string] $XamlDir,

        [switch] $IgnoreMsbuildFailure
    )

    log "ConvertFrom-Xaml for $XamlDir"

    $Pages = ""
    Get-ChildItem -Path "$XamlDir\*.xaml" | % {
        $Page = $Script:XamlProjPage -f $_.FullName
        $Pages += $Page
    }

    $XamlProjContent = $Script:XamlProj -f $Configuration, $OutputDir, $Pages
    $XamlProjPath = Join-Path -Path $OutputDir -ChildPath xaml.proj
    Set-Content -Path $XamlProjPath -Value $XamlProjContent -Encoding Ascii -NoNewline -Force

    msbuild $XamlProjPath | Write-Verbose

    if ($LASTEXITCODE -ne 0) {
        $message = "When processing $XamlDir 'msbuild $XamlProjPath > `$null' failed with exit code $LASTEXITCODE"
        if ($IgnoreMsbuildFailure) {
            Write-Verbose $message
        } else {
            throw $message
        }
    }

    return (Join-Path -Path $OutputDir -ChildPath "obj\Any CPU\$Configuration")
}


function script:Use-MSBuild {
    # TODO: we probably should require a particular version of msbuild, if we are taking this dependency
    # msbuild v14 and msbuild v4 behaviors are different for XAML generation
    $frameworkMsBuildLocation = "${env:SystemRoot}\Microsoft.Net\Framework\v4.0.30319\msbuild"

    $msbuild = get-command msbuild -ErrorAction SilentlyContinue
    if ($msbuild) {
        # all good, nothing to do
        return
    }

    if (-not (Test-Path $frameworkMsBuildLocation)) {
        throw "msbuild not found in '$frameworkMsBuildLocation'. Install Visual Studio 2015."
    }

    Set-Alias msbuild $frameworkMsBuildLocation -Scope Script
}


function script:log([string]$message) {
    Write-Host -Foreground Green $message
    #reset colors for older package to at return to default after error message on a compilation error
    [console]::ResetColor()
}

function script:precheck([string]$command, [string]$missedMessage) {
    $c = Get-Command $command -ErrorAction SilentlyContinue
    if (-not $c) {
        if ($missedMessage -ne $null)
        {
            Write-Warning $missedMessage
        }
        return $false
    } else {
        return $true
    }
}


function script:Get-InvertedOrderedMap {
    param(
        $h
    )
    $res = [ordered]@{}
    foreach ($q in $h.GetEnumerator()) {
        if ($res.Contains($q.Value)) {
            throw "Cannot invert hashtable: duplicated key $($q.Value)"
        }

        $res[$q.Value] = $q.Key
    }
    return $res
}


## this function is from Dave Wyatt's answer on
## http://stackoverflow.com/questions/22002748/hashtables-from-convertfrom-json-have-different-type-from-powershells-built-in-h
function script:Convert-PSObjectToHashtable {
    param (
        [Parameter(ValueFromPipeline)]
        $InputObject
    )

    process {
        if ($null -eq $InputObject) { return $null }

        if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string]) {
            $collection = @(
                foreach ($object in $InputObject) { Convert-PSObjectToHashtable $object }
            )

            Write-Output -NoEnumerate $collection
        } elseif ($InputObject -is [psobject]) {
            $hash = @{}

            foreach ($property in $InputObject.PSObject.Properties) {
                $hash[$property.Name] = Convert-PSObjectToHashtable $property.Value
            }

            $hash
        } else {
            $InputObject
        }
    }
}

# this function wraps native command Execution
# for more information, read https://mnaoumov.wordpress.com/2015/01/11/execution-of-external-commands-in-powershell-done-right/
function script:Start-NativeExecution([scriptblock]$sb)
{
    $backupEAP = $script:ErrorActionPreference
    $script:ErrorActionPreference = "Continue"
    try {
        & $sb
        # note, if $sb doesn't have a native invocation, $LASTEXITCODE will
        # point to the obsolete value
        if ($LASTEXITCODE -ne 0) {
            throw "Execution of {$sb} failed with exit code $LASTEXITCODE"
        }
    } finally {
        $script:ErrorActionPreference = $backupEAP
    }
}

# Builds coming out of this project can have version number as 'a.b.c' OR 'a.b.c-d-f'
# This function converts the above version into major.minor[.build[.revision]] format
function Get-PackageVersionAsMajorMinorBuildRevision
{
    [CmdletBinding()]
    param (
        # Version of the Package
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $Version
        )

    Write-Verbose "Extract the version in the form of major.minor[.build[.revision]] for $Version"
    $packageVersionTokens = $Version.Split('-')
    $packageVersion = ([regex]::matches($Version, "\d+(\.\d+)+"))[0].value

    if (1 -eq $packageVersionTokens.Count) {
        # In case the input is of the form a.b.c, add a '0' at the end for revision field
        $packageVersion = $packageVersion + '.0'
    } elseif (1 -lt $packageVersionTokens.Count) {
        # We have all the four fields
        $packageBuildTokens = ([regex]::Matches($packageVersionTokens[1], "\d+"))[0].value
        $packageVersion = $packageVersion + '.' + $packageBuildTokens[0]
    }

    return $packageVersion
}

function New-MSIPackage
{
    [CmdletBinding()]
    param (
    
        # Name of the Product
        [ValidateNotNullOrEmpty()]
        [string] $ProductName = 'PowerShell', 

        # Version of the Product
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductVersion,

        # Product Guid needs to change for every version to support SxS install
        [ValidateNotNullOrEmpty()]
        [string] $ProductGuid = 'a5249933-73a1-4b10-8a4c-13c98bdc16fe',

        # Source Path to the Product Files - required to package the contents into an MSI
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $ProductSourcePath,

        # File describing the MSI Package creation semantics
        [ValidateNotNullOrEmpty()]
        [string] $ProductWxsPath = (Join-Path $pwd '\assets\Product.wxs'),

        # Path to Assets folder containing artifacts such as icons, images
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $AssetsPath, 

        # Path to license.rtf file - for the EULA
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $LicenseFilePath

    )    

    $wixToolsetBinPath = "${env:ProgramFiles(x86)}\WiX Toolset v3.10\bin"

    Write-Verbose "Ensure Wix Toolset is present on the machine @ $wixToolsetBinPath"
    if (-not (Test-Path $wixToolsetBinPath))
    {
        throw "Install Wix Toolset prior to running this script - https://wix.codeplex.com/downloads/get/1540240"
    }

    Write-Verbose "Initialize Wix executables - Heat.exe, Candle.exe, Light.exe"
    $wixHeatExePath = Join-Path $wixToolsetBinPath "Heat.exe"
    $wixCandleExePath = Join-Path $wixToolsetBinPath "Candle.exe"
    $wixLightExePath = Join-Path $wixToolsetBinPath "Light.exe"

    $ProductVersion = Get-PackageVersionAsMajorMinorBuildRevision -Version $ProductVersion -Verbose
    
    $assetsInSourcePath = "$ProductSourcePath" + '\assets'
    New-Item $assetsInSourcePath -type directory -Force | Write-Verbose

    $assetsInSourcePath = Join-Path $ProductSourcePath 'assets'

    Write-Verbose "Place dependencies such as icons to $assetsInSourcePath" 
    Copy-Item "$AssetsPath\*.ico" $assetsInSourcePath -Force
    
    $productVersionWithName = $ProductName + "_" + $ProductVersion
    Write-Verbose "Create MSI for Product $productVersionWithName"

    [Environment]::SetEnvironmentVariable("ProductSourcePath", $ProductSourcePath, "Process")
    [Environment]::SetEnvironmentVariable("ProductName", $ProductName, "Process")
    [Environment]::SetEnvironmentVariable("ProductGuid", $ProductGuid, "Process")
    [Environment]::SetEnvironmentVariable("ProductVersion", $ProductVersion, "Process")
    [Environment]::SetEnvironmentVariable("ProductVersionWithName", $productVersionWithName, "Process")

    $wixFragmentPath = (Join-path $env:Temp "Fragment.wxs")
    $wixObjProductPath = (Join-path $env:Temp "Product.wixobj")
    $wixObjFragmentPath = (Join-path $env:Temp "Fragment.wixobj")
    
    $msiLocationPath = Join-Path $pwd "$productVersionWithName.msi"    
    Remove-Item -ErrorAction SilentlyContinue $msiLocationPath -Force

    & $wixHeatExePath dir  $ProductSourcePath -dr  $productVersionWithName -cg $productVersionWithName -gg -sfrag -srd -scom -sreg -out $wixFragmentPath -var env.ProductSourcePath -v | Write-Verbose
    & $wixCandleExePath  "$ProductWxsPath"  "$wixFragmentPath" -out (Join-Path "$env:Temp" "\\") -arch x64 -v | Write-Verbose
    & $wixLightExePath -out "$productVersionWithName.msi" $wixObjProductPath $wixObjFragmentPath -ext WixUIExtension -dWixUILicenseRtf="$LicenseFilePath" -v | Write-Verbose
    
    Remove-Item -ErrorAction SilentlyContinue *.wixpdb -Force

    Write-Verbose "You can find the MSI @ $msiLocationPath"
    return $msiLocationPath
}

# Function to create an Appx package compatible with Windows 8.1 and above
function New-AppxPackage
{
    [CmdletBinding()]
    param (

        # Name of the Package
        [ValidateNotNullOrEmpty()]
        [string] $PackageName = 'PowerShell',

        # Version of the Package
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $PackageVersion,

        # Source Path to the Binplaced Files
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $SourcePath,

        # Path to Assets folder containing Appx specific artifacts
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string] $AssetsPath
    )

    $PackageVersion = Get-PackageVersionAsMajorMinorBuildRevision -Version $PackageVersion -Verbose
    Write-Verbose "Package Version is $PackageVersion"

    $win10sdkBinPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64"

    Write-Verbose "Ensure Win10 SDK is present on the machine @ $win10sdkBinPath"
    if (-not (Test-Path $win10sdkBinPath)) {
        throw "Install Win10 SDK prior to running this script - https://go.microsoft.com/fwlink/p/?LinkID=698771"
    }

    Write-Verbose "Ensure Source Path is valid - $SourcePath"
    if (-not (Test-Path $SourcePath)) {
        throw "Invalid SourcePath - $SourcePath"
    }

    Write-Verbose "Ensure Assets Path is valid - $AssetsPath"
    if (-not (Test-Path $AssetsPath)) {
        throw "Invalid AssetsPath - $AssetsPath"
    }

    Write-Verbose "Initialize MakeAppx executable path"
    $makeappxExePath = Join-Path $win10sdkBinPath "MakeAppx.exe"

    $appxManifest = @"
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities">
  <Identity Name="Microsoft.PowerShell" ProcessorArchitecture="x64" Publisher="CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" Version="#VERSION#" />
  <Properties>
    <DisplayName>PowerShell</DisplayName>
    <PublisherDisplayName>Microsoft Corporation</PublisherDisplayName>
    <Logo>#LOGO#</Logo>
  </Properties>
  <Resources>
    <Resource Language="en-us" />
  </Resources>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.14257.0" MaxVersionTested="12.0.0.0" />
    <TargetDeviceFamily Name="Windows.Server" MinVersion="10.0.14257.0" MaxVersionTested="12.0.0.0" />
  </Dependencies>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
  <Applications>
    <Application Id="PowerShell" Executable="powershell.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="PowerShell" Description="PowerShell for every system" BackgroundColor="transparent" Square150x150Logo="#SQUARE150x150LOGO#" Square44x44Logo="#SQUARE44x44LOGO#">
      </uap:VisualElements>
    </Application>
  </Applications>
</Package>
"@

    $appxManifest = $appxManifest.Replace('#VERSION#', $PackageVersion)
    $appxManifest = $appxManifest.Replace('#LOGO#', 'Assets\Powershell_256.png')
    $appxManifest = $appxManifest.Replace('#SQUARE150x150LOGO#', 'Assets\Powershell_256.png')
    $appxManifest = $appxManifest.Replace('#SQUARE44x44LOGO#', 'Assets\Powershell_48.png')

    Write-Verbose "Place Appx Manifest in $SourcePath"
    $appxManifest | Out-File "$SourcePath\AppxManifest.xml" -Force

    $assetsInSourcePath = "$SourcePath" + '\Assets'
    New-Item $assetsInSourcePath -type directory -Force | Out-Null

    $assetsInSourcePath = Join-Path $SourcePath 'Assets'

    Write-Verbose "Place AppxManifest dependencies such as images to $assetsInSourcePath"
    Copy-Item "$AssetsPath\*.png" $assetsInSourcePath -Force

    $appxPackageName = $PackageName + "_" + $PackageVersion
    $appxPackagePath = "$pwd\$appxPackageName.appx"
    Write-Verbose "Calling MakeAppx from $makeappxExePath to create the package @ $appxPackagePath"
    & $makeappxExePath pack /o /v /d $SourcePath  /p $appxPackagePath | Write-Verbose

    Write-Verbose "Clean-up Appx artifacts and Assets from $SourcePath"
    Remove-Item $assetsInSourcePath -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "$SourcePath\AppxManifest.xml" -Force -ErrorAction SilentlyContinue

    return $appxPackagePath
}

function Start-CrossGen {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory= $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $PublishPath
    )

    function Generate-CrossGenAssembly {
        param (
            [Parameter(Mandatory= $true)]
            [ValidateNotNullOrEmpty()]
            [String]
            $AssemblyPath,
            [Parameter(Mandatory= $true)]
            [ValidateNotNullOrEmpty()]
            [String]
            $CrossgenPath
        )

        $outputAssembly = $AssemblyPath.Replace(".dll", ".ni.dll")
        $platformAssembliesPath = Split-Path $AssemblyPath -Parent
        $crossgenFolder = Split-Path $CrossgenPath
        $niAssemblyName = Split-Path $outputAssembly -Leaf

        try {
            Push-Location $crossgenFolder

            # Generate the ngen assembly
            Write-Verbose "Generating assembly $niAssemblyName"
            Start-NativeExecution {
                & $CrossgenPath /MissingDependenciesOK /in $AssemblyPath /out $outputAssembly /Platform_Assemblies_Paths $platformAssembliesPath
            } | Write-Verbose

            <#
            # TODO: Generate the pdb for the ngen binary - currently, there is a hard dependency on diasymreader.dll, which is available at %windir%\Microsoft.NET\Framework\v4.0.30319.
            # However, we still need to figure out the prerequisites on Linux.
            Start-NativeExecution {
                & $CrossgenPath /Platform_Assemblies_Paths $platformAssembliesPath  /CreatePDB $platformAssembliesPath /lines $platformAssembliesPath $niAssemblyName
            } | Write-Verbose
            #>
        } finally {
            Pop-Location
        }
    }

    if (-not (Test-Path $PublishPath)) {
        throw "Path '$PublishPath' does not exist."
    }

    # Get the path to crossgen
    $crossGenSearchPath = if ($IsWindows) {
        "$PSScriptRoot\Packages\*crossgen.exe"
    } else {
        "~/.nuget/packages/*crossgen"
    }

    # The crossgen tool is only published for these particular runtimes
    $crossGenRuntime = if ($IsWindows) {
        "win7-x64"
    } elseif ($IsLinux) {
        if ($IsUbuntu) {
            "ubuntu.14.04-x64"
        } elseif ($IsCentOS) {
            "rhel.7-x64"
        }
    } elseif ($IsOSX) {
        "osx.10.10-x64"
    }

    if (-not $crossGenRuntime) {
        throw "crossgen is not available for this platform"
    }

    $crossGenPath = Get-ChildItem $crossGenSearchPath -Recurse | ? { $_.FullName -match $crossGenRuntime } | select -First 1 | % { $_.FullName }
    if (-not $crossGenPath) {
        throw "Unable to find latest version of crossgen.exe. 'Please run Start-PSBuild -Clean' first, and then try again."
    }

    # Crossgen.exe requires the following assemblies:
    # mscorlib.dll
    # System.Private.CoreLib.dll
    # clrjit.dll on Windows or libclrjit.so/dylib on Linux/OS X
    $crossGenRequiredAssemblies = @("mscorlib.dll", "System.Private.CoreLib.dll")

    $crossGenRequiredAssemblies += if ($IsWindows) {
         "clrjit.dll"
    } elseif ($IsLinux) {
        "libclrjit.so"
    } elseif ($IsOSX) {
        "libclrjit.dylib"
    }

    # Make sure that all dependencies required by crossgen are at the directory.
    $crossGenFolder = Split-Path $crossGenPath
    foreach ($assemblyName in $crossGenRequiredAssemblies) {
        if (-not (Test-Path "$crossGenFolder\$assemblyName")) {
            Copy-Item -Path "$PublishPath\$assemblyName" -Destination $crossGenFolder -Force -ErrorAction Stop
        }
    }

    # Common PowerShell libraries to crossgen
    $psCoreAssemblyList = @(
        "Microsoft.PowerShell.Commands.Utility.dll",
        "Microsoft.PowerShell.Commands.Management.dll",
        "Microsoft.PowerShell.Security.dll",
        "Microsoft.PowerShell.CoreCLR.AssemblyLoadContext.dll",
        "Microsoft.PowerShell.CoreCLR.Eventing.dll",
        "Microsoft.PowerShell.ConsoleHost.dll",
        "Microsoft.PowerShell.PackageManagement.dll",
        "Microsoft.PowerShell.PSReadLine.dll",
        "System.Management.Automation.dll"
    )

    # Add Windows specific libraries
    if ($IsWindows) {
        $psCoreAssemblyList += @(
            "Microsoft.WSMan.Management.dll",
            "Microsoft.WSMan.Runtime.dll",
            "Microsoft.PowerShell.LocalAccounts.dll",
            "Microsoft.PowerShell.Commands.Diagnostics.dll",
            "Microsoft.Management.Infrastructure.CimCmdlets.dll"
        )
    }

    foreach ($assemblyName in $psCoreAssemblyList) {
        $assemblyPath = Join-Path $PublishPath $assemblyName
        Generate-CrossGenAssembly -CrossgenPath $crossGenPath -AssemblyPath $assemblyPath
    }

    Write-Verbose "PowerShell Ngen assemblies have been generated, deleting ILs..." -Verbose
    foreach ($assemblyName in $psCoreAssemblyList) {
        # Remove the IL assembly and its symbols.
        $assemblyPath = Join-Path $PublishPath $assemblyName
        $symbolsPath = $assemblyPath.Replace(".dll", ".pdb")
        Remove-Item $assemblyPath -Force -ErrorAction SilentlyContinue
        Remove-Item $symbolsPath -Force -ErrorAction SilentlyContinue
    }
}

# Cleans the PowerShell repo
# by default everything but the root folder and the Packages folder
# if you specify -IncludePackages it will clean the Packages folder
function Clear-PSRepo
{
    [CmdletBinding()]
    param(
        [switch] $IncludePackages
    )
        Get-ChildItem $PSScriptRoot\* -Directory -Exclude 'Packages' | ForEach-Object {
        Write-Verbose "Cleaning $_ ..." 
        git clean -fdX $_
    }

    if($IncludePackages)
    {
        remove-item $RepoRoot\Packages\ -Recurse -Force  -ErrorAction SilentlyContinue
    }
}


$script:RESX_TEMPLATE = @'
<?xml version="1.0" encoding="utf-8"?>
<root>
  <!--
    Microsoft ResX Schema

    Version 2.0

    The primary goals of this format is to allow a simple XML format
    that is mostly human readable. The generation and parsing of the
    various data types are done through the TypeConverter classes
    associated with the data types.

    Example:

    ... ado.net/XML headers & schema ...
    <resheader name="resmimetype">text/microsoft-resx</resheader>
    <resheader name="version">2.0</resheader>
    <resheader name="reader">System.Resources.ResXResourceReader, System.Windows.Forms, ...</resheader>
    <resheader name="writer">System.Resources.ResXResourceWriter, System.Windows.Forms, ...</resheader>
    <data name="Name1"><value>this is my long string</value><comment>this is a comment</comment></data>
    <data name="Color1" type="System.Drawing.Color, System.Drawing">Blue</data>
    <data name="Bitmap1" mimetype="application/x-microsoft.net.object.binary.base64">
        <value>[base64 mime encoded serialized .NET Framework object]</value>
    </data>
    <data name="Icon1" type="System.Drawing.Icon, System.Drawing" mimetype="application/x-microsoft.net.object.bytearray.base64">
        <value>[base64 mime encoded string representing a byte array form of the .NET Framework object]</value>
        <comment>This is a comment</comment>
    </data>

    There are any number of "resheader" rows that contain simple
    name/value pairs.

    Each data row contains a name, and value. The row also contains a
    type or mimetype. Type corresponds to a .NET class that support
    text/value conversion through the TypeConverter architecture.
    Classes that don't support this are serialized and stored with the
    mimetype set.

    The mimetype is used for serialized objects, and tells the
    ResXResourceReader how to depersist the object. This is currently not
    extensible. For a given mimetype the value must be set accordingly:

    Note - application/x-microsoft.net.object.binary.base64 is the format
    that the ResXResourceWriter will generate, however the reader can
    read any of the formats listed below.

    mimetype: application/x-microsoft.net.object.binary.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            : and then encoded with base64 encoding.

    mimetype: application/x-microsoft.net.object.soap.base64
    value   : The object must be serialized with
            : System.Runtime.Serialization.Formatters.Soap.SoapFormatter
            : and then encoded with base64 encoding.

    mimetype: application/x-microsoft.net.object.bytearray.base64
    value   : The object must be serialized into a byte array
            : using a System.ComponentModel.TypeConverter
            : and then encoded with base64 encoding.
    -->
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="metadata">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" />
              </xsd:sequence>
              <xsd:attribute name="name" use="required" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="assembly">
            <xsd:complexType>
              <xsd:attribute name="alias" type="xsd:string" />
              <xsd:attribute name="name" type="xsd:string" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>2.0</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
{0}
</root>
'@
