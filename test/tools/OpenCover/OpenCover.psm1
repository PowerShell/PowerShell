# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#region privateFunctions

$script:psRepoPath = [string]::Empty
if ($null -ne (Get-Command -Name 'git' -ErrorAction Ignore)) {
    $script:psRepoPath = git rev-parse --show-toplevel
}

function Get-AssemblyCoverageData([xml.xmlelement] $element)
{
    $coverageSummary = (Get-CoverageSummary -element $element.Summary)
    $classCoverage = Get-ClassCoverageData $element
    $AssemblyCoverageData = [PSCustomObject] @{
        AssemblyName = $element.ModuleName
        CoverageSummary = $coverageSummary
        Branch = $coverageSummary.BranchCoverage
        Sequence = $coverageSummary.SequenceCoverage
        ClassCoverage = $classCoverage
    }

    $AssemblyCoverageData | Add-Member -MemberType ScriptMethod -Name ToString -Value { "{0} ({1})" -f $this.AssemblyName,$this.CoverageSummary.BranchCoverage } -Force
    $AssemblyCoverageData.PSTypeNames.Insert(0,"OpenCover.AssemblyCoverageData")

    return $AssemblyCoverageData
}

function Get-ClassCoverageData([xml.xmlelement]$element)
{
    $classes = [system.collections.arraylist]::new()
    foreach ( $class in $element.classes.class )
    {
        # skip classes with names like <>f__AnonymousType6`4
        if ( $class.fullname -match "<>" ) { continue }
        $name = $class.fullname
        $branch = $class.summary.branchcoverage
        $sequence = $class.summary.sequenceCoverage
        $o = [pscustomobject]@{ ClassName = $name; Branch = $branch; Sequence = $sequence}
        $o.psobject.TypeNames.Insert(0, "ClassCoverageData")
        $null = $classes.Add($o)
    }
    return $classes
}

#region FileCoverage

class FileCoverage
{
    [string]$Path
    [Collections.Generic.HashSet[int]]$Hit
    [Collections.Generic.HashSet[int]]$Miss
    [int]$SequencePointCount = 0
    [Double]$Coverage
    FileCoverage([string]$p) {
        $this.Path = $p
        $this.Hit = [Collections.Generic.HashSet[int]]::new()
        $this.Miss = [Collections.Generic.HashSet[int]]::new()
    }
}

<#
.Synopsis
   Format the coverage data for a file
.Description
   Show the lines which were hit or not in a specific file. Line numbers are included in the output.
   If a line was hit during a test run a '+' will follow the line number, if a line was missed, a '-'
   will follow the line number. If a line is not hittable, it will not show '+' or '-'.

   You can map file locations with the -oldBase and -newBase parameters (see example below), so you can
   view coverage on a system with a different file layout. It is obvious to note that if files are different
   between the systems, the results will be misleading
.EXAMPLE
   PS> $coverage = Get-CodeCoverage -CoverageXmlFile .\opencover.xml
   PS> Format-FileCoverage -FileCoverageData $coverage.FileCoverage -filter "CredSSP.cs"

   ...
   0790                   try
   0791 +                 {
   0792                       // ServiceController.Start will return before the service is actually started
   0793                       // This API will wait forever
   0794 +                     serviceController.WaitForStatus(
   0795 +                         targetStatus,
   0796 +                         new TimeSpan(20000000) // 2 seconds
   0797 +                         );
   0798 +                     return true; // service reached target status
   0799                   }
   0800 -                 catch (System.ServiceProcess.TimeoutException) // still waiting
   0801 -                 {
   0802 -                     if (serviceController.Status != pendingStatus
   ...

.EXAMPLE
   Map the file location from C:\projects\powershell-f975h to /users/james/src
   PS> $coverage = Get-CodeCoverage -CoverageXmlFile .\opencover.xml
   PS> $formatArgs = @{
       FileCoverageData = $coverage.FileCoverage
       filter = "Service.cs"
       oldBase = "C:\\projects\\powershell-f975h"
       newBase = "/users/james/src"
   }
   PS> Format-FileCoverage @formatArgs

   ...
   0790                   try
   0791 +                 {
   0792                       // ServiceController.Start will return before the service is actually started
   0793                       // This API will wait forever
   0794 +                     serviceController.WaitForStatus(
   0795 +                         targetStatus,
   0796 +                         new TimeSpan(20000000) // 2 seconds
   0797 +                         );
   0798 +                     return true; // service reached target status
   0799                   }
   0800 -                 catch (System.ServiceProcess.TimeoutException) // still waiting
   0801 -                 {
   0802 -                     if (serviceController.Status != pendingStatus
   ...
#>
function Format-FileCoverage
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true,Position=0,ValueFromPipeline=$true)]$CoverageData,
        [Parameter()][string]$oldBase = "",
        [Parameter()][string]$newBase = ""
    )

    PROCESS {
        $file = $CoverageData.Path
        $filepath = $file -replace "$oldBase","${newBase}"
        if ( Test-Path $filepath ) {
            $content = Get-Content $filepath
            for($i = 0; $i -lt $content.length; $i++ ) {
                if ( $CoverageData.Hit -contains ($i+1)) {
                    $sign = "+"
                }
                elseif ( $CoverageData.Miss -contains ($i+1)) {
                    $sign = "-"
                }
                else {
                    $sign = " "
                }
                $outputline = "{0:0000} {1} {2}" -f ($i+1),$sign,$content[$i]
                if ( $sign -eq "+" ) { Write-Host -fore green $outputline }
                elseif ( $sign -eq "-" ) { Write-Host -fore red $outputline }
                else { Write-Host -fore white $outputline }
            }
        }
        else {
            Write-Error "Cannot find $filepath"
        }
    }
}

function Get-FileCoverageData([xml]$CoverageData)
{
    $result = [Collections.Generic.Dictionary[string,FileCoverage]]::new()
    $count = 0
    Write-Progress "collecting files"
    $filehash = $CoverageData.SelectNodes(".//File") | ForEach-Object { $h = @{} } { $h[$_.uid] = $_.fullpath } { $h }
    Write-Progress "collecting sequence points"
    $nodes = $CoverageData.SelectNodes(".//SequencePoint")
    $ncount = $nodes.count
    Write-Progress "scanning sequence points"
    foreach($point in $nodes) {
        $fileid = $point.fileid
        $filepath = $filehash[$fileid]
        $s = [int]$point.sl
        $e = [int]$point.el
        $filedata = $null
        if ( ! $result.TryGetValue($filepath, [ref]$filedata) ) {
            $filedata = [FileCoverage]::new($filepath)
            $null = $result.Add($filepath, $filedata)
        }

        for($i = $s; $i -le $e; $i++) {
            if ( $point.vc -eq "0" ) {
                $null = $filedata.Miss.Add($i)
            }
            else {
                $null = $filedata.Hit.Add($i)
            }
        }
        if ( (++$count % 50000) -eq 0 ) { Write-Progress "$count of $ncount" }
    }

    # Almost done, we're looking at two runs, and one run might have missed a line that
    # was hit in another run, so go throw each one of the collections and remove any
    # hit from the miss collection
    Write-Progress "Cleanup up collections"
    foreach ( $key in $result.keys ) {
        $collection = $null
        if ( $result.TryGetValue($key, [ref]$collection ) ) {
            foreach ( $hit in $collection.hit ) {
                $null = $collection.miss.remove($hit)
            }
            $collection.SequencePointCount = $collection.Hit.Count + $Collection.Miss.Count
            $collection.Coverage = $collection.Hit.Count/$collection.SequencePointCount*100
        }
        else {
            Write-Error "Could not find '$key'"
        }
    }
    # now return $result
    $result
}
#endregion
function Get-CodeCoverageChange($r1, $r2, [string[]]$ClassName)
{
    $h = @{}
    $Deltas = New-Object "System.Collections.ArrayList"

    if ( $ClassName ) {
        foreach ( $Class in $ClassName ) {
            $c1 = $r1.Assembly.ClassCoverage | Where-Object {$_.ClassName -eq $Class }
            $c2 = $r2.Assembly.ClassCoverage | Where-Object {$_.ClassName -eq $Class }
            $ClassCoverageChange = [pscustomobject]@{
                ClassName     = $Class
                Branch        = $c2.Branch
                BranchDelta   = $c2.Branch - $c1.Branch
                Sequence      = $c2.Sequence
                SequenceDelta = $c2.Sequence - $c1.sequence
            }
            $ClassCoverageChange.psobject.typenames.insert(0,"ClassCoverageDelta")
            Write-Output $ClassCoverageChange
        }
        return
    }

    $r1.assembly | ForEach-Object { $h[$_.assemblyname] = @($_) }
    $r2.assembly | ForEach-Object {
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
        $assemblyCoverageChange = (Get-AssemblyCoverageChange -r1 $runs[0] -r2 $runs[1])
        $null = $Deltas.Add($assemblyCoverageChange)
    }

    $CoverageChange = [PSCustomObject] @{
        Run1 = $r1
        Run2 = $r2
        Branch = $r2.CoverageSummary.BranchCoverage
        Sequence = $r2.CoverageSummary.SequenceCoverage
        BranchDelta = [double] ($r2.CoverageSummary.BranchCoverage - $r1.CoverageSummary.BranchCoverage)
        SequenceDelta = [double] ($r2.CoverageSummary.SequenceCoverage - $r1.CoverageSummary.SequenceCoverage)
        Deltas = $Deltas
    }
    $CoverageChange.PSTypeNames.Insert(0,"OpenCover.CoverageChange")

    return $CoverageChange
}

function Get-AssemblyCoverageChange($r1, $r2)
{
    if($null -eq $r1 -and $null -ne $r2)
    {
        $r1 = @{ AssemblyName = $r2.AssemblyName ; Branch = 0 ; Sequence = 0 }
    }
    elseif($null -eq $r2 -and $null -ne $r1)
    {
        $r2 = @{ AssemblyName = $r1.AssemblyName ; Branch = 0 ; Sequence = 0 }
    }

    if ( Compare-Object $r1.assemblyname $r2.assemblyname ) { throw "different assemblies" }

    $AssemblyCoverageChange = [pscustomobject] @{
        AssemblyName = $r1.AssemblyName
        Branch = $r2.Branch
        BranchDelta = $r2.Branch - $r1.Branch
        Sequence = $r2.Sequence
        SequenceDelta = $r2.Sequence - $r1.Sequence
    }
    $AssemblyCoverageChange.PSTypeNames.Insert(0,"OpenCover.AssemblyCoverageChange")
    return $AssemblyCoverageChange
}

function Get-CoverageData($xmlPath)
{
    [xml]$CoverageXml = Get-Content -ReadCount 0 $xmlPath
    if ( $null -eq $CoverageXml.CoverageSession ) { throw "CoverageSession data not found" }

    $assemblies = New-Object System.Collections.ArrayList

    foreach( $module in $CoverageXml.CoverageSession.modules.module| Where-Object {$_.skippedDueTo -ne "MissingPdb"}) {
        $assemblies.Add((Get-AssemblyCoverageData -element $module)) | Out-Null
    }

    $CoverageData = [PSCustomObject] @{
        CoverageLogFile = $xmlPath
        CoverageSummary = (Get-CoverageSummary -element $CoverageXml.CoverageSession.Summary)
        Assembly = $assemblies
        FileCoverage = Get-FileCoverageData $CoverageXml
    }
    $CoverageData.PSTypeNames.Insert(0,"OpenCover.CoverageData")
    Add-Member -InputObject $CoverageData -MemberType ScriptMethod -Name GetClassCoverage -Value { param ( $name ) $this.assembly.classcoverage | Where-Object {$_.classname -match $name } }
    $null = $CoverageXml

    Add-Member -InputObject $CoverageData -MemberType ScriptMethod -Name GetFileCoverage -Value { param ( $name = ".*" ) @($this.FileCoverage.Values) | Where-Object {$_.Path -match "$name"} }

    ## Adding explicit garbage collection as the $CoverageXml object tends to be very large, in order of 1 GB.
    [gc]::Collect()

    return $CoverageData
}

function Get-CoverageSummary([xml.xmlelement] $element)
{
    $CoverageSummary = [PSCustomObject] @{
        NumSequencePoints = $element.numSequencePoints
        VisitedSequencePoints = $element.visitedSequencePoints
        NumBranchPoints = $element.numBranchPoints
        VisitedBranchPoints = $element.visitedBranchPoints
        SequenceCoverage = $element.sequenceCoverage
        BranchCoverage = $element.branchCoverage
        MaxCyclomaticComplexity = $element.maxCyclomaticComplexity
        MinCyclomaticComplexity = $element.minCyclomaticComplexity
        VisitedClasses = $element.visitedClasses
        NumClasses = $element.numClasses
        VisitedMethods = $element.visitedMethods
        NumMethods = $element.numMethods
    }

    $CoverageSummary | Add-Member -MemberType ScriptMethod -Name ToString -Value { "Branch:{0,3} Sequence:{1,3}" -f $this.BranchCoverage,$this.SequenceCoverage } -Force
    $CoverageSummary.PSTypeNames.Insert(0,"OpenCover.CoverageSummary")

    return $CoverageSummary
}

# needed for PowerShell v4 as Archive module isn't available by default
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
    param ( [string]$CoverageXmlFile = "$HOME/Documents/OpenCover.xml" )
    $xmlPath = (Get-Item $CoverageXmlFile).Fullname
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
        [Parameter(Mandatory=$true,Position=1,ParameterSetName="coverage")][Object]$Run2,
        [Parameter()][String[]]$ClassName,
        [Parameter()][switch]$Summary
        )

    if ( $PSCmdlet.ParameterSetName -eq "file" )
    {
        [string]$xmlPath1 = (Get-Item $Run1File).Fullname
        $Run1 = (Get-CoverageData -xmlPath $xmlPath1)

        [string]$xmlPath2 = (Get-Item $Run1File).Fullname
        $Run2 = (Get-CoverageData -xmlPath $xmlPath2)
    }

    $change = Get-CodeCoverageChange -r1 $Run1 -r2 $Run2 -Class $ClassName
    if ( $Summary -or $ClassName )
    {
        $change
    }
    else
    {
        $change.Deltas
    }
}

function Compare-FileCoverage
{
    param (
        [Parameter(Position=0,Mandatory=$true)]$ReferenceCoverage,
        [Parameter(Position=1,Mandatory=$true)]$DifferenceCoverage,
        [Parameter(Position=2,Mandatory=$true)]$FileName
    )
    # create a couple of hashtables where the key is the path
    # so we can compare file coverage
    $reference = $ReferenceCoverage.GetFileCoverage($FileName) | ForEach-Object { $h = @{} } { $h[$_.path] = $_ } {$h}
    $difference = $differenceCoverage.GetFileCoverage($FileName) | ForEach-Object { $h = @{}}{ $h[$_.path] = $_ }{$h }
    # based on the paths, create objects which show the difference between the two runs
    $reference.Keys | Sort-Object | ForEach-Object {
        $referenceObject = $reference[$_]
        $differenceObject = $difference[$_]
        if ( $differenceObject )
        {
            $fileCoverageObject = [pscustomobject]@{
                FileName = [io.path]::GetFileName($_)
                FilePath = "$_"
                ReferenceCoverage = $ReferenceObject.Coverage
                DifferenceCoverage = $DifferenceObject.Coverage
                CoverageDelta = $DifferenceObject.Coverage - $ReferenceObject.Coverage
            }
            $fileCoverageObject.psobject.typenames.Insert(0,"FileCoverageComparisonObject")
            $fileCoverageObject
        }
        else
        {
            Write-Warning "skipping '$_', not found in difference"
        }
    }
}

<#
.Synopsis
   Install OpenCover by downloading the 4.6.519 version.
.Description
   Install OpenCover version 4.6.519.
#>
function Install-OpenCover
{
    param (
        [parameter()][string]$Version = "4.6.519",
        [parameter()][string]$TargetDirectory = "$HOME",
        [parameter()][switch]$Force
        )

    $filename =  "opencover.${version}.zip"
    $tempPath = "$env:TEMP/$Filename"
    $packageUrl = "https://github.com/OpenCover/opencover/releases/download/${version}/${filename}"
    if ( Test-Path $tempPath )
    {
        if ( $force )
        {
            Remove-Item -Force $tempPath
        }
        else
        {
            throw "Package already exists at $tempPath, not continuing.  Use -force to re-install"
        }
    }
    if ( Test-Path "$TargetDirectory/OpenCover" )
    {
        if ( $force )
        {
            Remove-Item -Recurse -Force "$TargetDirectory/OpenCover"
        }
        else
        {
            throw "$TargetDirectory/OpenCover exists, not continuing.  Use -force to re-install"
        }
    }

    Invoke-WebRequest -Uri $packageUrl -OutFile "$tempPath"
    if ( ! (Test-Path $tempPath) )
    {
        throw "Download failed: $packageUrl"
    }

    ## We add ErrorAction as we do not have this module on PS v4 and below. Calling import-module will throw an error otherwise.
    Import-Module Microsoft.PowerShell.Archive -ErrorAction SilentlyContinue

    if ($null -ne (Get-Command Expand-Archive -ErrorAction Ignore)) {
        Expand-Archive -Path $tempPath -DestinationPath "$TargetDirectory/OpenCover"
    } else {
        Expand-ZipArchive -Path $tempPath -DestinationPath "$TargetDirectory/OpenCover"
    }
    Remove-Item -Force $tempPath
}

<#
.Synopsis
   Invoke-OpenCover runs tests under OpenCover to collect code coverage.
.Description
   Invoke-OpenCover runs tests under OpenCover by executing tests on PowerShell located at $PowerShellExeDirectory.
.EXAMPLE
   Invoke-OpenCover -TestPath $PWD/test/powershell -PowerShellExeDirectory $PWD/src/powershell-win-core/bin/CodeCoverage/7.0/win7-x64
#>
function Invoke-OpenCover
{
    [CmdletBinding(SupportsShouldProcess=$true)]
    param (
        [parameter()]$OutputLog = "$HOME/Documents/OpenCover.xml",
        [parameter()]$TestPath = "${script:psRepoPath}/test/powershell",
        [parameter()]$OpenCoverPath = "$HOME/OpenCover",
        [parameter()]$PowerShellExeDirectory = "${script:psRepoPath}/src/powershell-win-core/bin/CodeCoverage/net10.0/win7-x64/publish",
        [parameter()]$PesterLogElevated = "$HOME/Documents/TestResultsElevated.xml",
        [parameter()]$PesterLogUnelevated = "$HOME/Documents/TestResultsUnelevated.xml",
        [parameter()]$PesterLogFormat = "NUnitXml",
        [parameter()]$TestToolsModulesPath = "${script:psRepoPath}/test/tools/Modules",
        [switch]$CIOnly,
        [switch]$SuppressQuiet
        )

    # check for elevation
    $identity  = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object System.Security.Principal.WindowsPrincipal($identity)
    $isElevated = $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)

    if(-not $isElevated)
    {
        throw 'Please run from an elevated PowerShell.'
    }

    # check to be sure that OpenCover is present

    $OpenCoverBin = "$OpenCoverPath\opencover.console.exe"

    if ( ! (Test-Path $OpenCoverBin))
    {
        # see if it's somewhere else in the path
        $openCoverBin = (Get-Command -Name 'opencover.console' -ErrorAction Ignore).Source
        if ($null -eq $openCoverBin) {
            throw "$OpenCoverBin does not exist, use Install-OpenCover"
        }
    }

    # check to be sure that pwsh.exe is present
    $target = "${PowerShellExeDirectory}\pwsh.exe"
    if ( ! (Test-Path $target) )
    {
        throw "$target does not exist, use 'Start-PSBuild -configuration CodeCoverage'"
    }

    # create the arguments for OpenCover

    $updatedEnvPath = "${PowerShellExeDirectory}\Modules;$TestToolsModulesPath"
    $testToolsExePath = (Resolve-Path(Join-Path $TestPath -ChildPath "..\tools\TestExe\bin")).Path
    $testServiceExePath = (Resolve-Path(Join-Path $TestPath -ChildPath "..\tools\TestService\bin")).Path
    $updatedProcessEnvPath = "${testServiceExePath};${testToolsExePath};${env:PATH}"

    $startupArgs =  "Set-ExecutionPolicy Bypass -Force -Scope Process; `$env:PSModulePath = '${updatedEnvPath}'; `$env:Path = '${updatedProcessEnvPath}';"
    $targetArgs = "${startupArgs}", "Invoke-Pester","${TestPath}","-OutputFormat $PesterLogFormat"

    if ( $CIOnly )
    {
        $targetArgsElevated = $targetArgs + @("-excludeTag @('Feature','Scenario','Slow')", "-Tag @('RequireAdminOnWindows')")
        $targetArgsUnelevated = $targetArgs + @("-excludeTag @('Feature','Scenario','Slow','RequireAdminOnWindows')")
    }
    else
    {
        $targetArgsElevated = $targetArgs + @("-Tag @('RequireAdminOnWindows')")
        $targetArgsUnelevated = $targetArgs + @("-excludeTag @('RequireAdminOnWindows')")
    }

    $targetArgsElevated += @("-OutputFile $PesterLogElevated")
    $targetArgsUnelevated += @("-OutputFile $PesterLogUnelevated")

    if(-not $SuppressQuiet)
    {
        $targetArgsElevated += @("-Show None")
        $targetArgsUnelevated += @("-Show None")
    }

    $cmdlineElevated = CreateOpenCoverCmdline -target $target -outputLog $OutputLog -targetArgs $targetArgsElevated
    $cmdlineUnelevated = CreateOpenCoverCmdline -target $target -outputLog $OutputLog -targetArgs $targetArgsUnelevated

    if ( $PSCmdlet.ShouldProcess("$OpenCoverBin $cmdlineUnelevated") )
    {
        try
        {
            # invoke OpenCover elevated
            # Write the command line to a file and then invoke file.
            # '&' invoke caused issues with cmdline parameters for opencover.console.exe
            $elevatedFile = "$env:temp\elevated.ps1"
            "$OpenCoverBin $cmdlineElevated" | Out-File -FilePath $elevatedFile -Force
            powershell.exe -file $elevatedFile

            # invoke OpenCover unelevated and poll for completion
            $unelevatedFile = "$env:temp\unelevated.ps1"
            "$openCoverBin $cmdlineUnelevated" | Out-File -FilePath $unelevatedFile -Force
            runas.exe /trustlevel:0x20000 "powershell.exe -file $unelevatedFile"
            # poll for process exit every 60 seconds
            # timeout of 12 hours
            # Runs currently take about 8-9 hours, we picked 12 hours to be substantially larger.
            $timeOut = ([datetime]::Now).AddHours(12)

            $openCoverExited = $false

            while([datetime]::Now -lt $timeOut)
            {
                Start-Sleep -Seconds 60
                $openCoverProcess = Get-Process "OpenCover.Console" -ErrorAction SilentlyContinue

                if(-not $openCoverProcess)
                {
                    #run must have completed.
                    $openCoverExited = $true
                    break
                }
            }

            if(-not $openCoverExited)
            {
                throw "Opencover has not exited in 12 hours"
            }
        }
        finally
        {
            Remove-Item $elevatedFile -Force -ErrorAction SilentlyContinue
            Remove-Item $unelevatedFile -Force -ErrorAction SilentlyContinue
        }
    }
}

function CreateOpenCoverCmdline($target, $outputLog, $targetArgs)
{
    $targetArgString = $targetArgs -join " "

    $bytes = [System.Text.Encoding]::Unicode.GetBytes($targetArgString)
    $base64targetArgs = [convert]::ToBase64String($bytes)

    # the order seems to be important. Always keep -targetargs as the last parameter.
    $cmdline = "-target:$target",
        "-register:user",
        "-output:${outputLog}",
        "-nodefaultfilters",
        "-oldstyle",
        "-hideskipped:all",
        "-mergeoutput",
        "-filter:`"+[*]* -[Microsoft.PowerShell.PSReadLine]*`"",
        "-targetargs:`"-NoProfile -EncodedCommand $base64targetArgs`""

    $cmdlineAsString = $cmdline -join " "

    return $cmdlineAsString

}
