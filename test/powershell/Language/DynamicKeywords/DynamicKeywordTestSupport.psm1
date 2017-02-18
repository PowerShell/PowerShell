$assetPath = Join-Path -Path $PSScriptRoot -ChildPath "assets"
$executableName = if ($IsWindows) { "powershell.exe" } else { "powershell" }
$powershellExe = Join-Path -Path $PSHOME -ChildPath $executableName

function New-TestDllModule
{
    param([string] $TestDrive, [string] $ModuleName)

    $assetPath = Join-Path -Path $PSScriptRoot -ChildPath "assets"

    $references = @(
        "System",
        "System.Collections",
        "System.Management.Automation"
    )

    $dllDirPath = Join-Path -Path $TestDrive -ChildPath $ModuleName
    $dllPath = Join-Path -Path $dllDirPath -ChildPath "$ModuleName.dll"

    # Assume that if the DLL exists, we don't want to recompile it
    if (Test-Path $dllPath)
    {
        return
    }

    # Make the directory if it doesn't exist
    if (-not (Test-Path $dllDirPath))
    {
        $null = New-Item -ItemType Directory $dllDirPath
    }

    $csSourcePath = Join-Path -Path $assetPath -ChildPath "$ModuleName.cs"

    Add-Type -Path $csSourcePath -OutputAssembly $dllPath -ReferencedAssemblies $references
}

function Get-ScriptBlockResultInNewProcess
{
    param([string] $TestDrive, [string[]] $ModuleName, [scriptblock] $ScriptBlock, [object[]] $Arguments)

    foreach ($module in $ModuleName)
    {
        New-TestDllModule -TestDrive $TestDrive -ModuleName $module
    }

    $result = & $powershellExe -NoProfile -NonInteractive -OutputFormat XML -Command $ScriptBlock -args $Arguments
    $result
}

function Convert-TestCasesToSerialized
{
    param([hashtable[]] $TestCases, [string[]] $Keys, [string] $TableSeparator=';', [string] $EntrySeparator=',')

    ($TestCases | ForEach-Object { $tmp = $_; ($Keys | ForEach-Object { $tmp.$_ }) -join $EntrySeparator }) -join $TableSeparator
}

function New-PathEntry
{
    param($PathString, $ModulePath)

    $ModulePath,$PathString -join [System.IO.Path]::PathSeparator
}