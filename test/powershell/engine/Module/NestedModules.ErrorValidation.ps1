

Describe "Validate Negative Scenarios for a module manifest with nested modules." -Tags "InnerLoop", "RI" {

    $testDirectory = Join-Path "TestDrive:" "Modules"
    $modulePath = Join-Path $testDirectory "TestModule"

    function GetImportModuleError 
    {
        param ([string]$path)
        
        $result = $null
        try 
        {
            Import-Module $path -Force -ea Stop
            throw "CodeExecuted"
        }
        catch
        {
            $result = $_
        }
        return $result
    }

    function CreateTestModule 
    {
        param (
            [string]$testCaseName,
            [string]$nestedModule
        )

        # Create module directory
        if (Test-Path $modulePath)
        {
            Remove-Item $modulePath -Recurse -Force -ea SilentlyContinue
        }
        New-Item -Path $modulePath -Force -ItemType Directory | Out-Null

        # Create module manifest
        $manifestName = "TestModule.psd1"
        $manifestPath = Join-Path $modulePath $manifestName
        New-ModuleManifest -Path $manifestPath -NestedModules $nestedModule -ModuleVersion '1.0.0.0'
        
        # Generate the NestedModule
        $nestedModulePath = Join-Path $modulePath $nestedModule

        if ($testCaseName -ne "ModuleFileNotFound")
        { 
            "some content" | Out-File -FilePath $nestedModulePath -Force
        }

        # return the path to the module manifest.
        return (get-item $manifestPath).FullName
    }

    function Test-ModulesError
    {
        param (
            [string]$description,
            [string]$testCaseName,
            [string]$expectedFullyQualifiedErrorId,
            [string]$nestedModule,
            [string[]]$expectedKeywordsInErrorMessage
        )

        It $description {

            $moduleManifestPath = CreateTestModule -testCaseName $testCaseName -nestedModule $nestedModule
            $result = GetImportModuleError -path $moduleManifestPath

            $result.FullyQualifiedErrorId | Should Be $expectedFullyQualifiedErrorId
            foreach ($keyword in $expectedKeywordsInErrorMessage)
            {
                $result.Exception | Should Match $keyword
            }
        }
    }

    $testScenarios = @(
        @{
            Description = "An invalid dll in 'NestedModules' generates System.BadImageFormatException"
            TestCaseName = "BadImageFormatException"
            ExpectedFullyQualifiedErrorId = "System.BadImageFormatException,Microsoft.PowerShell.Commands.ImportModuleCommand"
            NestedModule = "notamodule.dll"
            ExpectedKeywordsInErrorMessage = @("notamodule.dll")
        },
        @{
            Description = "A txt file in 'NestedModules' generates Modules_InvalidModuleExtension Exception"
            TestCaseName = "InvalidModuleExtension"
            ExpectedFullyQualifiedErrorId = "Modules_InvalidModuleExtension,Microsoft.PowerShell.Commands.ImportModuleCommand"
            NestedModule = "notamodule.txt"
            ExpectedKeywordsInErrorMessage = @("notamodule.txt", "NestedModules", ".dll", ".ps1", ".psm1", ".psd1", ".cdxml", ".xaml")
        }
        @{
            Description = "'NestedModules' pointing to a file that does not exist generates Modules_ModuleFileNotFound Exception"
            TestCaseName = "ModuleFileNotFound"
            ExpectedFullyQualifiedErrorId = "Modules_ModuleFileNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand"
            NestedModule = "assemblyThatDoesNotExist.dll"
            ExpectedKeywordsInErrorMessage = @("assemblyThatDoesNotExist.dll", "NestedModules")
        }
    )

    foreach ($testCase in $testScenarios) 
    {
        Test-ModulesError @testCase
    }
}
