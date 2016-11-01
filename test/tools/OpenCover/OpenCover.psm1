
#region Classes
Class CoverageSummary {
    [int]$NumSequencePoints
    [int]$VisitedSequencePoints
    [int]$NumBranchPoints
    [int]$VisitedBranchPoints
    [double]$SequenceCoverage
    [double]$BranchCoverage
    [int]$MaxCyclomaticComplexity
    [int]$MinCyclomaticComplexity
    [int]$VisitedClasses
    [int]$NumClasses
    [int]$VisitedMethods
    [int]$NumMethods
    CoverageSummary([xml.xmlelement]$x) {
        $this.numSequencePoints = $x.numSequencePoints
        $this.visitedSequencePoints = $x.visitedSequencePoints
        $this.numBranchPoints = $x.numBranchPoints
        $this.visitedBranchPoints = $x.visitedBranchPoints
        $this.sequenceCoverage = $x.sequenceCoverage
        $this.branchCoverage = $x.branchCoverage
        $this.maxCyclomaticComplexity = $x.maxCyclomaticComplexity
        $this.minCyclomaticComplexity = $x.minCyclomaticComplexity
        $this.visitedClasses = $x.visitedClasses
        $this.numClasses = $x.numClasses
        $this.visitedMethods = $x.visitedMethods
        $this.numMethods = $x.numMethods
    }
    [string]ToString() {
        return "Branch:{0,3} Sequence:{1,3}" -f $this.BranchCoverage,$this.SequenceCoverage
    }
}

Class AssemblyCoverageData {
    [string]$AssemblyName
    [double]$Branch
    [double]$Sequence
    [CoverageSummary]$Coverage
    [string]ToString() { return "{0} ({1})" -f $this.AssemblyName,$this.Coverage.BranchCoverage }
    AssemblyCoverageData([xml.xmlelement]$x) {
        $this.AssemblyName = $x.ModuleName
        $this.Coverage = [CoverageSummary]::new($x.Summary)
        $this.Branch = $this.Coverage.BranchCoverage
        $this.Sequence = $this.Coverage.SequenceCoverage
    }
}

Class CoverageData {
    [CoverageSummary]$Summary
    [System.Collections.ArrayList]$Assembly
    CoverageData([xml.xmldocument]$CoverageXml) {
        if ( $CoverageXml.CoverageSession -eq $null ) { throw "CoverageSession data not found" }
        $this.Assembly = [System.Collections.ArrayList]::new()
        $this.Summary = [CoverageSummary]::new($CoverageXml.CoverageSession.Summary)
        foreach( $module in $CoverageXml.CoverageSession.modules.module|?{$_.skippedDueTo -ne "MissingPdb"}) {
            $this.Assembly.Add([AssemblyCoverageData]::New($module))
        }
    }
    CoverageData([string]$path) {
        [xml]$CoverageXml = get-content -readcount 0 $path
        if ( $CoverageXml.CoverageSession -eq $null ) { throw "CoverageSession data not found" }
        $this.Assembly = [System.Collections.ArrayList]::new()
        $this.Summary = [CoverageSummary]::new($CoverageXml.CoverageSession.Summary)
        foreach( $module in $CoverageXml.CoverageSession.modules.module|?{$_.skippedDueTo -ne "MissingPdb"}) {
            $this.Assembly.Add([AssemblyCoverageData]::New($module))
        }
        remove-variable CoverageXml
        [gc]::Collect()
    }
}
Class CoverageChange {
    [CoverageData]$Run1
    [CoverageData]$Run2
    [AssemblyCoverageChange[]]$Deltas
    [double]$Branch
    [double]$BranchDelta
    [double]$Sequence
    [double]$SequenceDelta
    CoverageChange([CoverageData]$r1,[CoverageData]$r2) {
        $this.Run1 = $r1
        $this.Run2 = $r2
        $this.Branch = $r2.Summary.BranchCoverage
        $this.Sequence = $r2.Summary.SequenceCoverage
        $this.BranchDelta = $r2.Summary.BranchCoverage - $r1.Summary.BranchCoverage
        $this.SequenceDelta = $r2.Summary.SequenceCoverage - $r1.Summary.SequenceCoverage
        if ( compare-object ($r2.Assembly.AssemblyName|sort-object)  ($r2.Assembly.AssemblyName|sort-object) ) {
            Write-Warning "Assembly list differs from run1 to run2"
        }
        $h = @{}
        $r1.assembly | % { $h[$_.assemblyname] = @($_) }
        $r2.assembly | % { $h[$_.assemblyname] += $_ }
        $this.Deltas = $h.keys | %{ [AssemblyCoverageChange]::new(@($h[$_])[0],@($h[$_])[1]) }
    }
}
Class AssemblyCoverageChange {
    [string]$AssemblyName
    [double]$Branch
    [double]$BranchDelta
    [double]$Sequence
    [double]$SequenceDelta
    AssemblyCoverageChange([AssemblyCoverageData]$r1, [AssemblyCoverageData]$r2) {
        if ( compare-object $r1.assemblyname $r2.assemblyname ) { throw "different assemblies" }
        $this.AssemblyName = $r1.AssemblyName
        $this.Branch = $r2.Branch
        $this.BranchDelta = $r2.Branch - $r1.Branch
        $this.Sequence = $r2.Sequence
        $this.SequenceDelta = $r2.Sequence - $r1.Sequence
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
   AssemblyName                                     Branch Sequence Coverage
   ------------                                     ------ -------- --------
   powershell                                          100      100 Branch:100 Sequence:100
   Microsoft.PowerShell.CoreCLR.AssemblyLoadContext   37.2     93.3 Branch:37.2 Sequence:93.3
   Microsoft.PowerShell.ConsoleHost                  12.54    13.35 Branch:12.54 Sequence:13.35
   System.Management.Automation                      20.35    21.15 Branch:20.35 Sequence:21.15
   Microsoft.PowerShell.CoreCLR.Eventing               4.3     5.01 Branch:4.3 Sequence:5.01
   Microsoft.PowerShell.Security                      1.08     1.86 Branch:1.08 Sequence:1.86
   Microsoft.PowerShell.Commands.Management           5.04     5.95 Branch:5.04 Sequence:5.95
   Microsoft.PowerShell.Commands.Utility              5.92     6.19 Branch:5.92 Sequence:6.19
   Microsoft.Management.Infrastructure.CimCmdlets    28.99    36.08 Branch:28.99 Sequence:36.08
   Microsoft.WSMan.Management                         0.36     0.65 Branch:0.36 Sequence:0.65
   Microsoft.WSMan.Runtime                               0        0 Branch:  0 Sequence:  0
#>
function Get-CodeCoverage
{
    param ( [string]$CoverageXmlFile )
    $xmlPath = (get-item $CoverageXmlFile).Fullname
    [CoverageData]::new($xmlPath)
}

<#
.Synopsis
   Compare results between two coverage runs. 
.Description
   Coverage information from the supplied OpenCover XML file is displayed. The output object has options to show assembly coverage and summary.
.EXAMPLE
   Compare-CodeCoverage
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
    if ( $PSCmdlet.ParameterSetName -eq "file" ) {
        [string]$xmlPath1 = (get-item $Run1File).Fullname
        $Run1 = [CoverageData]::new($xmlPath1)
        $xmlPath2 = (get-item $Run1File).Fullname
        $Run2 = [CoverageData]::new($xmlPath2)
    }
    
    [CoverageChange]::new($run1,$run2)    
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
    if ( test-path $PWD/$Filename ) {
        if ( $force ) {
            remove-item -force "$PWD/$Filename"
        }
        else {
            throw "package already exists, not downloading"
        }
    }
    if ( test-path "$targetDirectory/OpenCover" ) {
        if ( $force ) {
            remove-item -recurse -force "$targetDirectory/OpenCover"
        }
        else {
            throw "$targetDirectory/OpenCover exists"
        }
    }
    $webclient.DownloadFile($packageUrl, "$PWD/$filename")
    if ( ! (test-path $filename) ) {
        throw "Download failed: $packageUrl"
    }
    import-module Microsoft.PowerShell.Archive
    Expand-Archive -Path "$PWD/$filename" -DestinationPath "$targetDirectory/OpenCover"
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
    if ( ! (test-path $OpenCoverBin)) {
        throw "$OpenCoverBin does not exist"
    }
    # check to be sure that powershell.exe is present
    $target = "${PowerShellExeDirectory}\powershell.exe"
    if ( ! (test-path $target) ) {
        throw "$target does not exist"
    }

    # create the arguments for OpenCover
    $targetArgs = "-c","Invoke-Pester","${TestDirectory}" 
    if ( $CIOnly ) {
        $targetArgs += "-excludeTag @('Feature','Scenario','Slow','RequireAdminOnWindows')"
    }
    $targetArgString = $targetArgs -join " "
    # the order seems to be important
    $openCoverArgs = "-target:$target","-targetargs:""$targetArgString""","-register:user","-output:${outputLog}","-nodefaultfilters","-oldstyle","-hideskipped:all"

    if ( $PSCmdlet.ShouldProcess("$OpenCoverBin $openCoverArgs")  )
    {
        try {
            # check to be sure that the module path is present
            # this isn't done earlier because there's no need to change env:psmodulepath unless we're going to really run tests
            $saveModPath = $env:psmodulepath
            $env:psmodulepath = "${PowerShellExeDirectory}\Modules"
            if ( ! (test-path $env:psmodulepath) ) {
                throw "${env:psmodulepath} does not exist"
            }
            # invoke OpenCover
            & $OpenCoverBin $openCoverArgs
        }
        finally {
            # set it back
            $env:PSModulePath = $saveModPath
        }
    }
}