param(
    [string]$config = "release",
    [string]$outputdir,
    [string]$outputfile,
    
    [string]$framework = "dnxcore50",
    [string]$name = "Microsoft.Management.Infrastructure.Native"

)

#region 
function Get-FileInfo {
    param($exe)

    if( $exe -and (test-path $exe) ) {
        [int32]$MACHINE_OFFSET = 4
        [int32]$PE_POINTER_OFFSET = 60

        [byte[]]$data = New-Object -TypeName System.Byte[] -ArgumentList 4096
        $stream = New-Object -TypeName System.IO.FileStream -ArgumentList ($exe, 'Open', 'Read')
        $stream.Read($data, 0, 4096) | Out-Null

        [int32]$PE_HEADER_ADDR = [System.BitConverter]::ToInt32($data, $PE_POINTER_OFFSET)
        [int32]$machineUint = [System.BitConverter]::ToUInt16($data, $PE_HEADER_ADDR + $MACHINE_OFFSET)

        $result = "" | select FullName, Arch, Version
        $result.FullName = $exe
        $result.Arch = 'Unknown'
        $result.Version = try { [system.version]((dir "$exe" -ea 0).VersionInfo.ProductVersion) } catch { 0 }

        switch ($machineUint)
        {
            0      { $result.Arch = 'Native' }
            0x014c { $result.Arch = 'x86' }
            0x0200 { $result.Arch = 'Itanium' }
            0x8664 { $result.Arch = 'x64' }
        }
        return $result
    }
}

function Any {
    [CmdletBinding()]
    param( [Parameter(Mandatory = $True)] $Condition, [Parameter(Mandatory = $True, ValueFromPipeline = $True)] $Item )
    begin { $isMatch = $False }
    process { if (& $Condition $Item) { $isMatch = $true } }
    end { $isMatch }
}


function All {
    [CmdletBinding()]
    param( [Parameter(Mandatory = $True)] $Condition, [Parameter(Mandatory = $True, ValueFromPipeline = $True)] $Item )
    begin { $isMatch = $true }
    process { if (-not ($isMatch -and (& $Condition $Item)) ) {$isMatch = $false} }
    end { $isMatch }
}

function Includes { param( [string] $i, [string[]] $all ) $all | All { $i -match $_ } }
function Excludes { param( [string] $i, [string[]] $all )  $all | All { -not ($i -match $_) } }

function Validate {
param( 
    [string] $exe,
    [string] $arch,
    [string[]] $include,
    [string[]] $exclude,
    [system.version] $minimumVersion 
)
    if( -not $exe ) {
        return $false
    }
    
    $file = dir $exe
    
    if( -not $file )  {
        return $false
    }
    if( -not (Includes $file $include) ) {
        return $false
    }
    
    if( -not (Excludes $file $exclude) ) {
        return $false
    }
    
    $info = Get-FileInfo $file
    
    if( $arch -and ($info.Arch -ne $arch) ) {
        return $false
    }    
    
    if( $minimumVersion -and ($minimumVersion -gt $info.Version ) ) {
        return $false
    }    
   
    return $info
}

function Convert-ToHashtable{ 
    param( $object )
    $keys = ($object| get-member -MemberType NoteProperty).Name
    
    $result = @{}
    $keys |% { $result[$_] = $object.($_) }
    return $result
}

function Find-Exe {
    param( 
    [string] $exe,
    [string] $arch,
    [string[]] $folders = @("${env:ProgramFiles(x86)}","${env:ProgramFiles}"),
    [string[]] $include = @(),
    [string[]] $exclude= @('arm','_x86','x86_'),
    [string] $minimumVersion = "0.0"
) 

    # find exe on path
    $onPath = (get-command $exe -ea 0).Source
    if( $onPath ) {
        $result = Validate -exe $onPath $arch $include $exclude $minimumVersion

        if( $result ) {
            return $result.FullName
        } 
    }
        # not in path. check registry
    $kt = "HKCU:\Software\KnownTools"
    if( $arch ) {
        $kt += "\$arch"
    }

    if( $minimumVersion ) {
        $kt += "\$minimumVersion"
        try { $minimumVersion = [system.version]$minimumVersion }  catch {
            try {
                $minimumVersion = [system.version]($minimumVersion + ".0")
            } catch {
                write-error "Bad Version $minimumVersion"
                return;
            }
        }
        
    }

    $result = Validate -exe ((Get-ItemProperty -Path "$kt\$exe" -Name Path -ea 0).Path) $arch $include $exclude $minimumVersion
    if( $result ) {
        return $result.FullName
    } 

    if( -not $result -or -not (test-path $result )) { 
        write-host -fore yellow "Searching for $exe "
        $result = ($folders |% {cmd "/c dir /s/b `"$_\$exe`" 2>nul"  | dir }) `
            |% { Validate -exe $_ $arch $include $exclude $minimumVersion } `
            |? { $_ -ne $false } `
            | Sort-Object -Descending { $_.Version } `
            | Select-Object -first 1 
        
        if( $result ) { 
            $result = $result.FullName 
            $null = mkdir -Path "$kt\$exe" -Force
            $null = New-ItemProperty -Path "$kt\$exe" -Name Path -Value $result -force 
        } 
    } 

    if( -not $result ) {
        write-error "Can not find $exe"
        return;
    }

    return $result
}
#endregion

# start First, adjust the $framework.
if( $framework -eq "DNXCore Version=v5.0" ) {
    $framework = "DNXCore50"
}

if( $framework -eq "DNX Version=v4.5.1" ) {
    $framework = "dnx451"
}


try {
    # make sure we can get back here.
    pushd $PSScriptRoot
    
    # make sure we can find our System.Security.SecureString implementation 
    if($framework -eq "dnxcore50" ) {    
        if( (test-path "$outputdir\System.Security.SecureString.dll") ) {
            $sss = "$outputdir\System.Security.SecureString.dll"
        } else {
            if( (test-path "..\System.Security.SecureString\bin\$config\$framework\System.Security.SecureString.dll") ) {
                $sss = "..\System.Security.SecureString\bin\$config\$framework\System.Security.SecureString.dll"
            } else {
                write-error "Can't find System.Security.SecureString.dll"
                return 1;
            }
        }
    }
    # Stuff we borrowed from SD
    $mmin = resolve-path "$PSScriptRoot\..\psl-mmin\"
    $SrcDir = resolve-path "$mmin\DotNetAPI\cpp\"
    
    # tools we need for this stuff
    $cl = Find-exe cl.exe x64 -exclude 'arm','_x86','x86_'
    $link = Find-exe link.exe x64 -exclude 'arm','_x86','x86_'
    $rc = (dir (Find-exe rc.exe x64 -exclude 'arm','_x86','x86_')).Directory
    $asmRefRewriter = "$mmin\tools\asmrefrewriter.exe"

    # add rc.exe to the path so link can find it.
    $env:path = "$env:path;$rc"

    # where are the packages installed 
    $PackageDir= "$env:USERPROFILE\.nuget\packages\"
    
    # a convenient place to work (cleaned up at the end)
    $IntDir = "$PSScriptRoot\intermediate"
    $WorkDir = "$IntDir\$config\$framework"
    
    # clean our intermediate directory first
    rmdir -recurse -force $IntDir -ea 0
    $null = mkdir $WorkDir

    # oh, and remove the original stubs that the c# created
    erase "$outputdir\Microsoft.Management.Infrastructure.Native.pdb" -force         
    erase "$outputdir\Microsoft.Management.Infrastructure.Native.dll" -force 

    # back to this directory.
    cd $PSScriptRoot

    # compiler command line
    $arg = @(
        "/clr:pure"
        ,"/Fo""$WorkDir\\"""
        ,"/Fd""$WorkDir\Microsoft.Management.Infrastructure.Native.pdb"""
        ,"/FU""C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll"""
        ,"/I""$mmin\include"""
        ,"/I""$mmin\DotNetAPI\unmanaged"""
        ,"/I""$mmin\admin\inc"""
        ,"/I""$mmin\admin\inc\codec"""
        ,"/I""C:\Program Files (x86)\Windows Kits\10\include\10.0.10240.0\um"""
        ,"/I""C:\Program Files (x86)\Windows Kits\10\include\10.0.10240.0\ucrt"""
        ,"/I""C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\INCLUDE"""
        ,"/I""C:\Program Files (x86)\Windows Kits\10\include\10.0.10240.0\shared"""
        ,"/c"
        ,"/AI""C:\Program Files (x86)\Windows Kits\10\References"""
        ,"/Zi"
        ,"/nologo"
        ,"/W3"
        ,"/WX-"
        ,"/Od"
        ,"/D","_WINDOWS"
        ,"/D","_USRDLL"
        ,"/D","_WINDLL"
        ,"/D","_UNICODE"
        ,"/D","UNICODE"
        ,"/D","_AMD64_"
        ,"/D","_APISET_MINCORE_VERSION=0x0104"
        ,"/D","_APISET_MINWIN_VERSION=0x0105"
        ,"/D","_APISET_WINDOWS_VERSION=0x601"
        ,"/D","_CONTROL_FLOW_GUARD_SVCTAB=1"
        ,"/D","_CONTROL_FLOW_GUARD=1"
        ,"/D","_CRT_SECURE_NO_WARNINGS"
        ,"/D","_UNICODE"
        ,"/D","_USE_DECLSPECS_FOR_SAL=1"
        ,"/D","_WIN32_WINNT=0x0A00"
        ,"/D","_WIN64"
        ,"/D","AMD64"
        ,"/D","CORECLR"
        ,"/D","_CORECLR"
        ,"/D","NTDDI_VERSION=0x0A000001"
        ,"/D","UNICODE"
        ,"/D","WIN32_LEAN_AND_MEAN=1"
        ,"/D","WINBLUE_KBSPRING14"
        ,"/D","WINNT=1"
        ,"/D","WINVER=0x0A00"
        ,"/D","_MANAGED_PURE"
        ,"/D","MOFCODEC"
        ,"/D","_CRT_STDIO_IMP="
        ,"/D","_CRT_STDIO_IMP_ALT="
        ,"/D","_FULL_IOBUF"
        ,"/D","_STATIC_MGDLIB"
        ,"/EHa"
        ,"/MDd"
        ,"/GS"
        ,"/fp:precise"
        ,"/Zc:wchar_t"
        ,"/Zc:forScope"
        ,"/Zc:inline"
        ,"/TP"
        ,"/errorReport:prompt"
        ,"/clr:nostdlib"
        ,"/d1clr:nostdlib"
        ,"/d1clr:nomscorlib"
        ,"/d2AllowCompatibleILVersions"
        ,"/d1clrNoPureCRT"
        ,"$SrcDir\AssemblyInfo.cpp"
        ,"$SrcDir\dangerousHandleAccessor.cpp" 
        ,"$SrcDir\Enums.cpp" 
        ,"$SrcDir\Helpers.cpp" 
        ,"$SrcDir\marshalCore.cpp" 
        ,"$SrcDir\nativeApplication.cpp" 
        ,"$SrcDir\nativeApplicationInternal.cpp" 
        ,"$SrcDir\nativeClass.cpp" 
        ,"$SrcDir\nativeCredential.cpp" 
        ,"$SrcDir\nativeDeserializer.cpp" 
        ,"$SrcDir\nativeDeserializerCallbacksInternal.cpp" 
        ,"$SrcDir\nativeDeserializerInternal.cpp" 
        ,"$SrcDir\nativeDestinationOptions.cpp" 
        ,"$SrcDir\nativeInstance.cpp" 
        ,"$SrcDir\nativeOperation.cpp" 
        ,"$SrcDir\nativeOperationCallbacks.cpp" 
        ,"$SrcDir\nativeOperationOptions.cpp" 
        ,"$SrcDir\nativeSerializer.cpp" 
        ,"$SrcDir\nativeSession.cpp" 
        ,"$SrcDir\nativeSubscriptionDeliveryOptions.cpp"
    )

    # Defines for coreclr
    if( $framework -eq "dnxcore50" ) {
        $arg += @(
            "/D","CORECLR"
            ,"/D","_CORECLR"
        )
    }

    # build the assembly using C++/CLI
    write-host -fore yellow "`nCompiling Assembly"
    write-host -fore DarkMagenta $cl $arg
    & $cl $arg

    # Linker args
    $arg = @(
        "/NODEFAULTLIB"
        ,"/INCREMENTAL:NO"
        ,"/NOLOGO"
    #    ,"/Debug"
    #    ,"/ASSEMBLYDEBUG"
        ,"/SUBSYSTEM:WINDOWS"
        ,"/TLBID:1"
        ,"/DYNAMICBASE"
        ,"/FIXED:NO"
        ,"/MANIFEST"
        ,"/MANIFESTUAC:""level='asInvoker' uiAccess='false'"""
        ,"/manifest:embed"
        ,"/NXCOMPAT"
        ,"/MACHINE:X64"
        ,"/DLL"
        # ,"/VERBOSE"
        ,"/LIBPATH:""$mmin\libs"""
        ,"/LIBPATH:""C:\Program Files (x86)\Windows Kits\10\Lib\10.0.10240.0\um\x64"""
        ,"/LIBPATH:""C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC\lib\onecore\amd64"""
        ,"/PDB:""$WorkDir\Microsoft.Management.Infrastructure.Native.pdb"""
        ,"/OUT:""$WorkDir\Microsoft.Management.Infrastructure.Native.dll"""
        ,"libcummt40.lib"
        ,"mi.lib"
        ,"mincore.lib"
        ,"mimofcodec.lib"
        ,"Microsoft.Management.Infrastructure.Native.Unmanaged.lib"
        ,"$WorkDir\AssemblyInfo.obj"
        ,"$WorkDir\dangerousHandleAccessor.obj"
        ,"$WorkDir\Enums.obj"
        ,"$WorkDir\Helpers.obj"
        ,"$WorkDir\marshalCore.obj"
        ,"$WorkDir\nativeApplication.obj"
        ,"$WorkDir\nativeApplicationInternal.obj"
        ,"$WorkDir\nativeClass.obj"
        ,"$WorkDir\nativeCredential.obj"
        ,"$WorkDir\nativeDeserializer.obj"
        ,"$WorkDir\nativeDeserializerCallbacksInternal.obj"
        ,"$WorkDir\nativeDeserializerInternal.obj"
        ,"$WorkDir\nativeDestinationOptions.obj"
        ,"$WorkDir\nativeInstance.obj"
        ,"$WorkDir\nativeOperation.obj"
        ,"$WorkDir\nativeOperationCallbacks.obj"
        ,"$WorkDir\nativeOperationOptions.obj"
        ,"$WorkDir\nativeSerializer.obj"
        ,"$WorkDir\nativeSession.obj"
        ,"$WorkDir\nativeSubscriptionDeliveryOptions.obj"
    );

    # Link the assembly
    write-host -fore yellow "`nLinking Assembly"
    write-host -fore DarkMagenta $link $arg
    & $link $arg
    
    # For CoreCLR, we have to rewrite the references in the assembly to use our CoreCLR implementations
    if( $framework -eq "DNXCore50" ) {
        write-host -fore yellow "`nRewriting Assembly References to CoreClr assemblies"

        $project = (convertfrom-json (get-content -raw "$PSScriptRoot\project.json" ))
        $deps =  Convert-ToHashtable $project.frameworks
        $deps =  Convert-ToHashtable $deps[$framework].dependencies
        
        $refs = $deps.Keys |% { 
            $pk = $_
            $ver = $deps[$_] 
            $pdir = "$PackageDir\$pk\$ver\ref"
            if( test-path $pdir ) {
                $refdir = "$pdir\dotnet5.4"
                if( test-path $refdir ) {
                    return """$refdir\$pk.dll"""
                }
                $refdir = "$pdir\netcore50\"
                if( test-path $refdir ) {
                    return """$refdir\$pk.dll"""
                }
                $refdir = "$pdir\dotnet5.4\"
                if( test-path $refdir ) {
                    return """$refdir\$pk.dll"""
                }
            }
        }
    
        # args for rewriting assembly 
        $arg = @( 
            """$workdir\Microsoft.Management.Infrastructure.Native.dll"""  
            ,"/out", """$IntDir\Microsoft.Management.Infrastructure.Native.dll"""
            ,"/ref", """$sss"""
        )

        # add the references from project.json
        $arg += ($refs |% { 
            "/ref" 
            $_
        } )
        
        write-host -fore DarkMagenta $asmRefRewriter $arg
        & $asmRefRewriter $arg

        write-host -fore yellow "`nCopying files to folder required by dotnet-* "
        $shh = mkdir $outputdir -ea 0
        
        # copy the files where the C# compiler would think they should be.
        copy "$intdir\Microsoft.Management.Infrastructure.Native.pdb" "$outputdir" -force 
        copy "$intdir\Microsoft.Management.Infrastructure.Native.dll" "$outputdir" -force 
    } else {
        write-host -fore yellow "`nCopying files to folder required by dotnet-* "
        $shh = mkdir $outputdir -ea 0

        # copy the files where the C# compiler would think they should be.
        copy "$workdir\Microsoft.Management.Infrastructure.Native.pdb" "$outputdir" -force 
        copy "$workdir\Microsoft.Management.Infrastructure.Native.dll" "$outputdir" -force 
    }
    rmdir -recurse -force $IntDir -ea 0
} finally {
    popd   
}