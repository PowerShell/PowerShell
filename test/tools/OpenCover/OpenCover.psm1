﻿#region privateFunctions

function Get-AssemblyCoverageData([xml.xmlelement] $element)
{
    $AssemblyCoverageData = New-Object System.Management.Automation.PSObject
    $AssemblyCoverageData | Add-Member -MemberType NoteProperty -Name AssemblyName -TypeName [string] -Value ([string]::Empty)
    $AssemblyCoverageData | Add-Member -MemberType NoteProperty -Name Branch -TypeName [double] -Value -1
    $AssemblyCoverageData | Add-Member -MemberType NoteProperty -Name Sequence -TypeName [double] -Value -1
    $AssemblyCoverageData | Add-Member -MemberType NoteProperty -Name CoverageSummary -TypeName [PSObject] -Value $null
    $AssemblyCoverageData | Add-Member -MemberType ScriptMethod -Name ToString -Value { "{0} ({1})" -f $this.AssemblyName,$this.CoverageSummary.BranchCoverage } -Force

    $AssemblyCoverageData.AssemblyName = $element.ModuleName    
    $AssemblyCoverageData.CoverageSummary = (Get-CoverageSummary -element $element.Summary)
    $AssemblyCoverageData.Branch = $AssemblyCoverageData.CoverageSummary.BranchCoverage
    $AssemblyCoverageData.Sequence = $AssemblyCoverageData.CoverageSummary.SequenceCoverage
    
    return $AssemblyCoverageData
}

function Get-CodeCoverageChange($r1, $r2)
{
    $CoverageChange = New-Object System.Management.Automation.PSObject
    $CoverageChange | Add-Member -MemberType NoteProperty -Name Run1 -TypeName [psobject] -Value $null
    $CoverageChange | Add-Member -MemberType NoteProperty -Name Run2 -TypeName [psobject] -Value $null
    $CoverageChange | Add-Member -MemberType NoteProperty -Name Deltas -TypeName [psobject[]] -Value $null
    $CoverageChange | Add-Member -MemberType NoteProperty -Name Branch -TypeName [double] -Value -1
    $CoverageChange | Add-Member -MemberType NoteProperty -Name BranchDelta -TypeName [double] -Value -1
    $CoverageChange | Add-Member -MemberType NoteProperty -Name Sequence -TypeName [double] -Value -1
    $CoverageChange | Add-Member -MemberType NoteProperty -Name SequenceDelta -TypeName [double] -Value -1

    $CoverageChange.Run1 = $r1
    $CoverageChange.Run2 = $r2
    $CoverageChange.Branch = $r2.Summary.BranchCoverage
    $CoverageChange.Sequence = $r2.Summary.SequenceCoverage
    $CoverageChange.BranchDelta = $r2.Summary.BranchCoverage - $r1.Summary.BranchCoverage
    $CoverageChange.SequenceDelta = $r2.Summary.SequenceCoverage - $r1.Summary.SequenceCoverage
    if ( compare-object ($r2.Assembly.AssemblyName|sort-object)  ($r2.Assembly.AssemblyName|sort-object) ) {
        Write-Warning "Assembly list differs from run1 to run2"
    }
    $h = @{}
    $CoverageChange.Deltas = new-object "System.Collections.ArrayList"

    $r1.assembly | % { $h[$_.assemblyname] = @($_) }
    $r2.assembly | % { 
                        if($h.ContainsKey($_.assemblyname))
                        { 
                            $h[$_.assemblyname] += $_ 
                        }
                        else
                        {
                            $h[$_.assemblyname] = @($_)
                        }
                     }
    
    foreach($kvPair in $h.GetEnumerator())
    {
        $runs = @($h[$kvPair.Name])
        $assemblyCoverageChange = Get-AssemblyCoverageChange -r1 $runs[0] -r2 $runs[1]
        $CoverageChange.Deltas.Add($assemblyCoverageChange) | Out-Null
    }

    return $CoverageChange
}

function Get-AssemblyCoverageChange($r1, $r2)
{
    if($r1 -eq $null -and $r2 -ne $null)
    {
        $r1 = @{ AssemblyName = $r2.AssemblyName ; Branch = 0 ; Sequence = 0 }
    }
    elseif($r2 -eq $null -and $r1 -ne $null)
    {
        $r2 = @{ AssemblyName = $r1.AssemblyName ; Branch = 0 ; Sequence = 0 }
    }

    if ( compare-object $r1.assemblyname $r2.assemblyname ) { throw "different assemblies" }

    $AssemblyCoverageChange = New-Object System.Management.Automation.PSObject
    $AssemblyCoverageChange | Add-Member -MemberType NoteProperty -Name AssemblyName -TypeName [string] -Value $null
    $AssemblyCoverageChange | Add-Member -MemberType NoteProperty -Name Branch -TypeName [double] -Value $null
    $AssemblyCoverageChange | Add-Member -MemberType NoteProperty -Name BranchDelta -TypeName [double] -Value $null
    $AssemblyCoverageChange | Add-Member -MemberType NoteProperty -Name Sequence -TypeName [double] -Value $null
    $AssemblyCoverageChange | Add-Member -MemberType NoteProperty -Name SequenceDelta -TypeName [double] -Value $null
    
    $AssemblyCoverageChange.AssemblyName = $r1.AssemblyName
    $AssemblyCoverageChange.Branch = $r2.Branch
    $AssemblyCoverageChange.BranchDelta = $r2.Branch - $r1.Branch
    $AssemblyCoverageChange.Sequence = $r2.Sequence
    $AssemblyCoverageChange.SequenceDelta = $r2.Sequence - $r1.Sequence

    return $AssemblyCoverageChange
}

function Get-CoverageData($xmlPath)
{
    [xml]$CoverageXml = get-content -readcount 0 $xmlPath
    if ( $CoverageXml.CoverageSession -eq $null ) { throw "CoverageSession data not found" }        

    $CoverageData = New-Object System.Management.Automation.PSObject
    $CoverageData | Add-Member -MemberType NoteProperty -Name CoverageSummary -TypeName [PSObject] -Value $null
    $CoverageData | Add-Member -MemberType NoteProperty -Name Assembly -TypeName [System.Collections.ArrayList] -Value (New-Object "System.Collections.ArrayList")    
    $CoverageData | Add-Member -MemberType ScriptMethod -Name GetCoverageSummary -Value { return $this.CoverageSummary }
    $CoverageData | Add-Member -MemberType ScriptMethod -Name GetAssembly -Value { return $this.Assembly }
            
    foreach( $module in $CoverageXml.CoverageSession.modules.module|?{$_.skippedDueTo -ne "MissingPdb"}) {
        $CoverageData.Assembly.Add((Get-AssemblyCoverageData -element $module)) | Out-Null
    }

    $CoverageData.CoverageSummary = (Get-CoverageSummary -element $CoverageXml.CoverageSession.Summary)

    remove-variable CoverageXml
    [gc]::Collect()
    
    return $CoverageData
}

function Get-CoverageSummary([xml.xmlelement] $element)
{
    $CoverageSummary = New-Object System.Management.Automation.PSObject
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name NumSequencePoints -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name VisitedSequencePoints -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name NumBranchPoints -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name VisitedBranchPoints -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name MaxCyclomaticComplexity -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name MinCyclomaticComplexity -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name VisitedClasses -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name NumClasses -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name VisitedMethods -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name NumMethods -TypeName [int] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name SequenceCoverage -TypeName [double] -Value -1
    $CoverageSummary | Add-Member -MemberType NoteProperty -Name BranchCoverage -TypeName [double] -Value -1
    $CoverageSummary | Add-Member -MemberType ScriptMethod -Name ToString -Value { "Branch:{0,3} Sequence:{1,3}" -f $this.BranchCoverage,$this.SequenceCoverage } -Force
        
    $CoverageSummary.numSequencePoints = $element.numSequencePoints
    $CoverageSummary.visitedSequencePoints = $element.visitedSequencePoints
    $CoverageSummary.numBranchPoints = $element.numBranchPoints
    $CoverageSummary.visitedBranchPoints = $element.visitedBranchPoints
    $CoverageSummary.sequenceCoverage = $element.sequenceCoverage
    $CoverageSummary.branchCoverage = $element.branchCoverage
    $CoverageSummary.maxCyclomaticComplexity = $element.maxCyclomaticComplexity
    $CoverageSummary.minCyclomaticComplexity = $element.minCyclomaticComplexity
    $CoverageSummary.visitedClasses = $element.visitedClasses
    $CoverageSummary.numClasses = $element.numClasses
    $CoverageSummary.visitedMethods = $element.visitedMethods
    $CoverageSummary.numMethods = $element.numMethods     
        
    return $CoverageSummary
}

function Expand-ZipArchive([string] $Path, [string] $DestinationPath)
{
    try
    {
        Add-Type -AssemblyName System.IO.Compression
        Add-Type -AssemblyName System.IO.Compression.FileSystem

        $fileStream = New-Object System.IO.FileStream -ArgumentList @($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read) 
        $zipArchive = New-Object System.IO.Compression.ZipArchive -ArgumentList @($fileStream, [System.IO.Compression.ZipArchiveMode]::Read, $false)

        foreach($entry in $zipArchive.Entries)
        {
            $extractPath = (Join-Path $DestinationPath $entry.FullName)

            $fileInfo = New-Object System.IO.FileInfo -ArgumentList $extractPath
            if(-not $fileInfo.Directory.Exists) { New-Item -Path $fileInfo.Directory.FullName -ItemType Directory | Out-Null }

            try
            {
                $newfileStream = [System.IO.File]::Create($extractPath)
                $entry.Open().CopyTo($newfileStream)
            }
            finally
            {                
                if($newfileStream) { $newfileStream.Dispose() }
            }
        }
    }
    finally
    {
        if($zipArchive) { $zipArchive.Dispose() }
        if($fileStream) { $fileStream.Dispose() }
    }    
}

#endregion

<#
.Synopsis
   Get code coverage information for the supplied coverage file. 
.Description
   Coverage information from the supplied OpenCover XML file is displayed. The output object has options to show assembly coverage and summary.
.EXAMPLE
   PS> $coverage = Get-CodeCoverage -CoverageXmlFile .\opencover.xml
   PS> $cov.assembly

   AssemblyName                                        Branch Sequence CoverageSummary            
   ------------                                        ------ -------- ---------------            
   powershell                                          100    100      Branch:100 Sequence:100    
   Microsoft.PowerShell.CoreCLR.AssemblyLoadContext    53.66  95.31    Branch:53.66 Sequence:95.31
   Microsoft.PowerShell.ConsoleHost                    36.53  38.40    Branch:36.53 Sequence:38.40
   System.Management.Automation                        42.18  44.11    Branch:42.18 Sequence:44.11
   Microsoft.PowerShell.CoreCLR.Eventing               28.70  36.23    Branch:28.70 Sequence:36.23
   Microsoft.PowerShell.Security                       15.17  18.16    Branch:15.17 Sequence:18.16
   Microsoft.PowerShell.Commands.Management            18.84  21.70    Branch:18.84 Sequence:21.70
   Microsoft.PowerShell.Commands.Utility               62.38  64.54    Branch:62.38 Sequence:64.54
   Microsoft.WSMan.Management                          3.93   4.45     Branch:3.93 Sequence:4.45  
   Microsoft.WSMan.Runtime                             0      0        Branch:  0 Sequence:  0    
   Microsoft.PowerShell.Commands.Diagnostics           44.96  49.93    Branch:44.96 Sequence:49.93
   Microsoft.PowerShell.PSReadLine                     7.12   9.94     Branch:7.12 Sequence:9.94  
   Microsoft.PowerShell.PackageManagement              59.77  62.04    Branch:59.77 Sequence:62.04
   Microsoft.PackageManagement                         41.73  44.47    Branch:41.73 Sequence:44.47
   Microsoft.Management.Infrastructure.CimCmdlets      13.20  17.01    Branch:13.20 Sequence:17.01
   Microsoft.PowerShell.LocalAccounts                  73.15  84.32    Branch:73.15 Sequence:84.32
   Microsoft.PackageManagement.MetaProvider.PowerShell 54.79  57.90    Branch:54.79 Sequence:57.90
   Microsoft.PackageManagement.NuGetProvider           62.36  65.37    Branch:62.36 Sequence:65.37
   Microsoft.PackageManagement.CoreProviders           7.08   7.96     Branch:7.08 Sequence:7.96  
   Microsoft.PackageManagement.ArchiverProviders       0.53   0.56     Branch:0.53 Sequence:0.56 
.EXAMPLE
   PS> $coverage = Get-CodeCoverage -CoverageXmlFile .\opencover.xml
   PS> $cov.CoverageSummary

   NumSequencePoints       : 337052
   VisitedSequencePoints   : 143209
   NumBranchPoints         : 115193
   VisitedBranchPoints     : 46132
   MaxCyclomaticComplexity : 398
   MinCyclomaticComplexity : 1
   VisitedClasses          : 2465
   NumClasses              : 3894
   VisitedMethods          : 17792
   NumMethods              : 37832
   SequenceCoverage        : 42.49
   BranchCoverage          : 40.05   
#>
function Get-CodeCoverage
{
    param ( [string]$CoverageXmlFile )
    $xmlPath = (get-item $CoverageXmlFile).Fullname
    (Get-CoverageData -xmlPath $xmlPath)
}


<#
.Synopsis
   Compare results between two coverage runs. 
.Description
   Coverage information from the supplied OpenCover XML file is displayed. The output object has options to show assembly coverage and summary.
.EXAMPLE
   $comp = Compare-CodeCoverage -RunFile1 .\OpenCover.xml -RunFile2 .\OpenCover.xml
   $comp.Deltas | sort-object assemblyname | format-table

   AssemblyName                                        Branch BranchDelta Sequence SequenceDelta
   ------------                                        ------ ----------- -------- -------------
   Microsoft.Management.Infrastructure.CimCmdlets      13.20            0 17.01                0
   Microsoft.PackageManagement                         41.73            0 44.47                0
   Microsoft.PackageManagement.ArchiverProviders       0.53             0 0.56                 0
   Microsoft.PackageManagement.CoreProviders           7.08             0 7.96                 0
   Microsoft.PackageManagement.MetaProvider.PowerShell 54.79            0 57.90                0
   Microsoft.PackageManagement.NuGetProvider           62.36            0 65.37                0
   Microsoft.PowerShell.Commands.Diagnostics           44.96            0 49.93                0
   Microsoft.PowerShell.Commands.Management            18.84            0 21.70                0
   Microsoft.PowerShell.Commands.Utility               62.38            0 64.54                0
   Microsoft.PowerShell.ConsoleHost                    36.53            0 38.40                0
   Microsoft.PowerShell.CoreCLR.AssemblyLoadContext    53.66            0 95.31                0
   Microsoft.PowerShell.CoreCLR.Eventing               28.70            0 36.23                0
   Microsoft.PowerShell.LocalAccounts                  73.15            0 84.32                0
   Microsoft.PowerShell.PackageManagement              59.77            0 62.04                0
   Microsoft.PowerShell.PSReadLine                     7.12             0 9.94                 0
   Microsoft.PowerShell.Security                       15.17            0 18.16                0
   Microsoft.WSMan.Management                          3.93             0 4.45                 0
   Microsoft.WSMan.Runtime                             0                0 0                    0
   powershell                                          100              0 100                  0
   System.Management.Automation                        42.18            0 44.11                0
   
.EXAMPLE 
   $comp = Compare-CodeCoverage -Run1 $c -Run2 $c
   $comp.Deltas | sort-object assemblyname | format-table

   AssemblyName                                        Branch BranchDelta Sequence SequenceDelta
   ------------                                        ------ ----------- -------- -------------
   Microsoft.Management.Infrastructure.CimCmdlets      13.20            0 17.01                0
   Microsoft.PackageManagement                         41.73            0 44.47                0
   Microsoft.PackageManagement.ArchiverProviders       0.53             0 0.56                 0
   Microsoft.PackageManagement.CoreProviders           7.08             0 7.96                 0
   Microsoft.PackageManagement.MetaProvider.PowerShell 54.79            0 57.90                0
   Microsoft.PackageManagement.NuGetProvider           62.36            0 65.37                0
   Microsoft.PowerShell.Commands.Diagnostics           44.96            0 49.93                0
   Microsoft.PowerShell.Commands.Management            18.84            0 21.70                0
   Microsoft.PowerShell.Commands.Utility               62.38            0 64.54                0
   Microsoft.PowerShell.ConsoleHost                    36.53            0 38.40                0
   Microsoft.PowerShell.CoreCLR.AssemblyLoadContext    53.66            0 95.31                0
   Microsoft.PowerShell.CoreCLR.Eventing               28.70            0 36.23                0
   Microsoft.PowerShell.LocalAccounts                  73.15            0 84.32                0
   Microsoft.PowerShell.PackageManagement              59.77            0 62.04                0
   Microsoft.PowerShell.PSReadLine                     7.12             0 9.94                 0
   Microsoft.PowerShell.Security                       15.17            0 18.16                0
   Microsoft.WSMan.Management                          3.93             0 4.45                 0
   Microsoft.WSMan.Runtime                             0                0 0                    0
   powershell                                          100              0 100                  0
   System.Management.Automation                        42.18            0 44.11                0
#>
function Compare-CodeCoverage
{
    [CmdletBinding()]
    param ( 
        [Parameter(Mandatory=$true,Position=0,ParameterSetName="file")][string]$RunFile1, 
        [Parameter(Mandatory=$true,Position=1,ParameterSetName="file")][string]$RunFile2,
        [Parameter(Mandatory=$true,Position=0,ParameterSetName="coverage")][Object]$Run1, 
        [Parameter(Mandatory=$true,Position=1,ParameterSetName="coverage")][Object]$Run2
        )

    if ( $PSCmdlet.ParameterSetName -eq "file" ) 
    {
        [string]$xmlPath1 = (get-item $Run1File).Fullname        
        $Run1 = (Get-CoverageData -xmlPath $xmlPath1)

        [string]$xmlPath2 = (get-item $Run1File).Fullname
        $Run2 = (Get-CoverageData -xmlPath $xmlPath2)
    }
    
    (Get-CodeCoverageChange -r1 $Run1 -r2 $Run2)
    [gc]::Collect()
}

## We are taking dependency on private build of OpenCover till a new release is available.
## This is required for collecting code coverage numbers with portable PDBs.  
<#
.Synopsis
   Install OpenCover by downloading the 4.6.589 version.  
.Description
   Install OpenCover version 4.6.589. This version supports debugType as portable.
#>
function Install-OpenCover
{
    param ( 
        [parameter()][string]$version = "4.6.589",
        [parameter()][string]$targetDirectory = $PWD,
        [parameter()][switch]$force
        )

    $webclient = [System.Net.WebClient]::New()
    $filename =  "opencover.${version}.zip"
    $packageUrl = "https://ci.appveyor.com/api/buildjobs/xj78v6dac42uob8q/artifacts/main%2Fbin%2Fzip%2Fopencover.4.6.589.zip"
    if ( test-path $PWD/$Filename ) 
    {
        if ( $force ) 
        {
            remove-item -force "$PWD/$Filename"
        }
        else 
        {
            throw "package already exists, not downloading"
        }
    }
    if ( test-path "$targetDirectory/OpenCover" ) 
    {
        if ( $force ) 
        {
            remove-item -recurse -force "$targetDirectory/OpenCover"
        }
        else 
        {
            throw "$targetDirectory/OpenCover exists"
        }
    }

    $webclient.DownloadFile($packageUrl, "$PWD/$filename")
    if ( ! (test-path $filename) ) 
    {
        throw "Download failed: $packageUrl"
    }
    
    import-module Microsoft.PowerShell.Archive
    Expand-ZipArchive -Path "$PWD/$filename" -DestinationPath "$targetDirectory/OpenCover"
}

<#
.Synopsis
   Invoke-OpenCover runs tests under OpenCover to collect code coverage.
.Description
   Invoke-OpenCover runs tests under OpenCover by executing tests on PowerShell.exe located at $PowerShellExeDirectory.
.EXAMPLE
   Invoke-OpenCover -TestDirectory $pwd/test/powershell -PowerShellExeDirectory $pwd/src/powershell-win-core/bin/debug/netcoreapp1.0/win10-x64 
#>
function Invoke-OpenCover
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param ( 
        [parameter()]$OutputLog = "$pwd/OpenCover.xml",
        [parameter(Mandatory=$true)]$TestDirectory,
        [parameter()]$OpenCoverPath = "$HOME\AppData\Local\Apps\OpenCover", # this is the default install location for open cover
        [parameter(Mandatory=$true)]$PowerShellExeDirectory,
        [switch]$CIOnly
        )

    # check to be sure that OpenCover is present
    $OpenCoverBin = "$OpenCoverPath\opencover.console.exe"
    if ( ! (test-path $OpenCoverBin)) 
    {
        throw "$OpenCoverBin does not exist"
    }

    # check to be sure that powershell.exe is present
    $target = "${PowerShellExeDirectory}\powershell.exe"
    if ( ! (test-path $target) ) 
    {
        throw "$target does not exist"
    }

    # create the arguments for OpenCover
    $targetArgs = "-c", "Set-ExecutionPolicy Bypass -Force;", "Invoke-Pester","${TestDirectory}" 
    
    if ( $CIOnly ) 
    {
        $targetArgs += "-excludeTag @('Feature','Scenario','Slow','RequireAdminOnWindows')"
    }

    $targetArgString = $targetArgs -join " "
    # the order seems to be important
    $openCoverArgs = "-target:$target","-targetargs:""$targetArgString""","-register:user","-output:${outputLog}","-nodefaultfilters","-oldstyle","-hideskipped:all"

    if ( $PSCmdlet.ShouldProcess("$OpenCoverBin $openCoverArgs")  )
    {
        try 
        {
            # check to be sure that the module path is present
            # this isn't done earlier because there's no need to change env:psmodulepath unless we're going to really run tests
            $saveModPath = $env:psmodulepath
            $env:psmodulepath = "${PowerShellExeDirectory}\Modules"
            if ( ! (test-path $env:psmodulepath) ) 
            {
                throw "${env:psmodulepath} does not exist"
            }
            
            # invoke OpenCover
            & $OpenCoverBin $openCoverArgs
        }
        finally 
        {
            # set it back
            $env:PSModulePath = $saveModPath
        }
    }
}