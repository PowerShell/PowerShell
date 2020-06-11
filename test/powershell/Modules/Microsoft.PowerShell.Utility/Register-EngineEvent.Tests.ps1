# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Register-EngineEvent" -Tags "CI" {

    Context "Check return type of Register-EngineEvent" {
	It "Should return System.Management.Automation.PSEventJob as return type of Register-EngineEvent" {
	    Register-EngineEvent -SourceIdentifier PesterTestRegister -Action {Write-Output registerengineevent} | Should -BeOfType System.Management.Automation.PSEventJob
	    Unregister-Event -SourceIdentifier PesterTestRegister
	}
    }
}
