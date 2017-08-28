
try {
    $defaultParamValues = $PSdefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:skip"] = ![System.Management.Automation.Platform]::IsWindowsDesktop

    Describe 'Basic COM Tests' -Tags "CI" {
        BeforeAll {
            $null = New-Item -Path $TESTDRIVE/file1 -ItemType File
            $null = New-Item -Path $TESTDRIVE/file2 -ItemType File
            $null = New-Item -Path $TESTDRIVE/file3 -ItemType File
        }

        It "Should enumerate files from a folder" {
            $shell = New-Object -ComObject "Shell.Application"
            $folder = $shell.Namespace("$TESTDRIVE")
            $items = $folder.Items()

            ## $items is a collection of all items belong to the folder, and it should be enumerated.
            $items | Measure-Object | ForEach-Object Count | Should Be $items.Count
        }

        It "Should enumerate IEnumVariant interface object without exception" {
            $shell = New-Object -ComObject "Shell.Application"
            $folder = $shell.Namespace("$TESTDRIVE")
            $items = $folder.Items()

            ## $enumVariant is an IEnumVariant interface of all items belong to the folder, and it should be enumerated.
            $enumVariant = $items._NewEnum()
            $enumVariant | Measure-Object | ForEach-Object Count | Should Be $items.Count
        }

        It "Should enumerate drives" {
            $fileSystem = New-Object -ComObject scripting.filesystemobject
            $drives = $fileSystem.Drives

            ## $drives is a read-only collection of all available drives, and it should be enumerated.
            $drives | Measure-Object | ForEach-Object Count | Should Be $drives.Count
            ## $element should be the first drive from the enumeration. It shouldn't be the same as $drives,
            ## but it should be the same as '$drives.Item($element.DriveLetter)'
            $element = $drives | Select-Object -First 1
            [System.Object]::ReferenceEquals($element, $drives) | Should Be $false
            $element | Should Be $drives.Item($element.DriveLetter)
        }
    }

} finally {
    $global:PSdefaultParameterValues = $defaultParamValues
}
