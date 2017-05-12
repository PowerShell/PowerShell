@{

# Version number of this module.
ModuleVersion = '1.0.0'

# ID used to uniquely identify this module
GUID = 'e148b26c-0594-4963-99e5-419d4ff302e2'

# Author of this module
Author = 'Steve Lee'

# Company or vendor of this module
CompanyName = 'Microsoft'

# Copyright statement for this module
Copyright = '(c) Microsoft. All rights reserved.'

# Description of the functionality provided by this module
Description = 'Creates a new HTTP Listener for testing purposes'

# Modules to import as nested modules of the module specified in RootModule/ModuleToProcess
RootModule = 'HttpListener.psm1'

# Functions to export from this module
FunctionsToExport = @('Start-HttpListener','Stop-HttpListener')

}

