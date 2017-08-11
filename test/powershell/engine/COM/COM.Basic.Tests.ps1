
try {
    $defaultParamValues = $PSdefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:skip"] = !$IsWindows

    Describe 'Basic COM Tests' -Tags "CI" {
        It "Should enumerate ShellWindows" {
            $shell = New-Object -ComObject "Shell.Application"
            $windows = $shell.Windows()

            ## $windows is a collection of all of the open windows that belong to the Shell, and it should be enumerated.
            ##  - If there are any open shell windows, then $element will be the first window from the enumeration;
            ##  - If there is no open shell window ($windows is an empty collection), then $element will be $null.
            ## So in either case, $element should not be the same as $windows
            $element = $windows | Select-Object -First 1
            $element | Should Not Be $windows
        }

        It "Should enumerate drives" {
            $fileSystem = New-Object -ComObject scripting.filesystemobject
            $drives = $fileSystem.Drives

            ## $drives is a read-only collection of all available drives, and it should be enumerated.
            $drives | Measure-Command | ForEach-Object Count | Should Be $drives.Count
            ## $element should be the first drive from the enumeration. It shouldn't be the same as $drives,
            ## but it should be the same as '$drives.Item($element.DriveLetter)'
            $element = $drives | Select-Object -First 1
            $element | Should Not Be $drives
            $element | Should Be $drives.Item($element.DriveLetter)
        }
    }

} finally {
    $global:PSdefaultParameterValues = $defaultParamValues
}
