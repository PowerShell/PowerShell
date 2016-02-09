param( $config = "debug" ) 

try {
    pushd $PSScriptRoot

    # get a new build number
    $number=(([int](get-content "$PSScriptRoot\.number"))+1)
    set-content "$PSScriptRoot\.number" -Value $number
    $ENV:DOTNET_BUILD_VERSION = "beta-$number"
    
    # get the projects in the src folder
    $items = (dir src\*\project.json).Directory.Name

    # nuke any existing outputs before we start.
    $output = "$PSScriptRoot\packages"
    $intdir = "$PSScriptRoot\intermediate"
    
    rmdir -recurse -force -ea 0  $output,$intdir
    
    $shh = mkdir $output,$intdir -ea 0

    $items | %{ 
        pushd "src\$_"
        # dotnet restore $_
        dotnet pack --configuration $config --output "$PSScriptRoot\packages" --temp-output "$PSScriptRoot\intermediate" --version-suffix "beta-$number" 
        popd
    }

    
} finally {
    popd   
}
