param(
    [Parameter(ParameterSetName='Build')]
    [ValidatePattern("^v\d+\.\d+\.\d+(-\w+\.\d+)?$")]
    [string]$ReleaseTag
)

DynamicParam {
    # Add a dynamic parameter '-Name' which specifies the name of the build to run

    # Get the names of the builds.
    $buildJsonPath = (Join-Path -path $PSScriptRoot -ChildPath 'build.json')
    $build = Get-Content -Path $buildJsonPath | ConvertFrom-Json
    $names = @($build.Windows.Name)
    foreach($name in $build.Linux.Name)
    {
        $names += $name
    }

    # Create the parameter attributs
    $ParameterAttr = New-Object "System.Management.Automation.ParameterAttribute"
    $ValidateSetAttr = New-Object "System.Management.Automation.ValidateSetAttribute" -ArgumentList $names
    $Attributes = New-Object "System.Collections.ObjectModel.Collection``1[System.Attribute]"
    $Attributes.Add($ParameterAttr) > $null
    $Attributes.Add($ValidateSetAttr) > $null
    
    # Create the parameter
    $Parameter = New-Object "System.Management.Automation.RuntimeDefinedParameter" -ArgumentList ("Name", [string], $Attributes)
    $Dict = New-Object "System.Management.Automation.RuntimeDefinedParameterDictionary"
    $Dict.Add("Name", $Parameter) > $null
    return $Dict
}

Begin {
    $Name = $PSBoundParameters['Name']
}

End {
    $ErrorActionPreference = 'Stop'

    $psReleaseBranch = 'master'
    $psReleaseFork = 'PowerShell'
    $location = Join-Path -Path $PSScriptRoot -ChildPath 'PSRelease'
    if(Test-Path $location)
    {
        Remove-Item -Path $location -Recurse -Force
    }

    $gitBinFullPath = (Get-Command -Name git).Source
    if (-not $gitBinFullPath)
    {
        throw "Git is required to proceed. Install from 'https://git-scm.com/download/win'"
    }

    Write-Verbose "cloning -b $psReleaseBranch --quiet https://github.com/$psReleaseFork/PSRelease.git" -verbose
    & $gitBinFullPath clone -b $psReleaseBranch --quiet https://github.com/$psReleaseFork/PSRelease.git $location

    Push-Location -Path $PWD.Path
    try{
        Set-Location $location
        & $gitBinFullPath  submodule update --init --recursive --quiet
    }
    finally
    {
        Pop-Location
    }

    $unresolvedRepoRoot = Join-Path -Path $PSScriptRoot '../..'
    $resolvedRepoRoot = (Resolve-Path -Path $unresolvedRepoRoot).ProviderPath

    try 
    {
        Write-Verbose "Starting build at $resolvedRepoRoot  ..." -Verbose
        Import-Module "$location/vstsBuild" -Force
        Import-Module "$location/dockerBasedBuild" -Force
        Clear-VstsTaskState

        Invoke-Build -RepoPath $resolvedRepoRoot  -BuildJsonPath './tools/releaseBuild/build.json' -Name $Name -Parameters $PSBoundParameters
    }
    catch
    {
        Write-VstsError -Error $_
    }
    finally{
        Write-VstsTaskState
        exit 0
    }
}
