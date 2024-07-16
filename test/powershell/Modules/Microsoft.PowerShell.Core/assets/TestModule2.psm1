# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
class TestModule2Type {
    [string]$prop1
    [string]$prop2
    [string]$prop3
    [string]$prop4
    [string]$prop5
    [string]$prop6
}

function Get-MyFormatsAndTypesFilePath {
    $Host.runspace.InitialSessionState.Types.FileName | ?{$_ -match "TestModule2"}
    $Host.runspace.InitialSessionState.Formats.FileName | ?{$_ -match "TestModule2"}
}

function get-mytype {
    $module = [TestModule2Type]::new()
    $module.prop1 = "p1"
    $module.prop2 = "p2"
    $module.prop3 = "p3"
    $module.prop4 = "p4"
    $module.prop5 = "p5"
    $module
}
