
@{

# Version number of this module.
ModuleVersion = '2.0'

# ID used to uniquely identify this module
GUID = '0eae34da-99dd-4608-8d28-c614fe7b0841'

# Author of this module
Author = 'manikb'

# Company or vendor of this module
CompanyName = 'Unknown'

# Copyright statement for this module
Copyright = '(c) 2015 manikb. All rights reserved.'

# Description of the functionality provided by this module
Description = 'ModuleWithDependencies2 module'

# Modules that must be imported into the global environment prior to importing this module
RequiredModules = @('NestedRequiredModule1')

# Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
NestedModules = @('NestedRequiredModule1') 

# Functions to export from this module
FunctionsToExport = @()

# Cmdlets to export from this module
CmdletsToExport = @()

# Variables to export from this module
VariablesToExport = @()

# Aliases to export from this module
AliasesToExport = @()

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        Tags = 'Tag1', 'Tag2', 'Tag-ModuleWithDependencies2-2.0'

        # A URL to the license for this module.
        LicenseUri = 'http://modulewithdependencies2.com/license'

        # A URL to the main website for this project.
        ProjectUri = 'http://modulewithdependencies2.com/'

        # A URL to an icon representing this module.
        IconUri = 'http://modulewithdependencies2.com/icon'

        # ReleaseNotes of this module
        ReleaseNotes = 'ModuleWithDependencies2 release notes'

    } # End of PSData hashtable

} # End of PrivateData hashtable

}
