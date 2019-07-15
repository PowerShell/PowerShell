Describe "verify pwsh" {
    BeforeAll{
        $options = (Get-PSOptions)
        $path = split-path -path $options.Output
        Write-Verbose "Path: '$path'" -Verbose
        $rootPath = split-Path -path $path
        $mount = 'C:\powershell'
        $container = 'mcr.microsoft.com/powershell:nanoserver-1803'
    }

    it "verify version " {
        $version = docker run --rm -v "${rootPath}:${mount}" ${container} "${mount}\publish\pwsh" -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
        $version | Should -match '^8\.'
    }
}
