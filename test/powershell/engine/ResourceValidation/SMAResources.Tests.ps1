# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
. "$PSScriptRoot/TestRunner.ps1"
$AssemblyName = "System.Management.Automation"

# The 'EventResource.resx' is embedded in S.M.A.dll. However, we are not using its strings from a CSharp
# binding class, but directly using 'ResourceManager.GetString' in 'SysLogProvider.cs' instead.
# So, the CSharp binding class generated from 'ResGen' doesn't get compiled in S.M.A.dll, and hence we
# should exclude this resource from this test.
$excludeList = @("EventResource.resx")

# run the tests
Test-ResourceStrings -AssemblyName $AssemblyName -ExcludeList $excludeList
