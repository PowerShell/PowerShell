#
# Module manifest for module 'IsolatedModule'
#

@{
    ModuleVersion = '0.0.1'
    GUID = '20d4742b-b17d-4ce8-b8da-29b25433cd18'
    Author = 'Microsoft Corporation'

    RootModule = 'Test.Isolated.Root.dll'
    NestedModules = @('Test.Isolated.Init.dll', 'Test.Isolated.Nested.dll')
    FunctionsToExport = @()
    CmdletsToExport = @('Test-NestedCommand', 'Test-RootCommand')
    VariablesToExport = '*'
    AliasesToExport = @()
}
