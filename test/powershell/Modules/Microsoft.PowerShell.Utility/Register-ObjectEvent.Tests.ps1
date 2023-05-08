# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Register-ObjectEvent" -Tags "CI" {

    BeforeEach {
	$pesterobject = (New-Object System.Collections.ObjectModel.ObservableCollection[object])
    }

    AfterEach {
	Unregister-Event -SourceIdentifier PesterTestRegister -ErrorAction SilentlyContinue
    }

    Context "Check return type of Register-ObjectEvent" {

	It "Should return System.Management.Automation.PSEventSubscriber as return type of New-Event with the registered sourceidentifier" {
	    Register-ObjectEvent -InputObject $pesterobject -EventName CollectionChanged -SourceIdentifier PesterTestRegister
	    Get-EventSubscriber -SourceIdentifier PesterTestRegister | Should -BeOfType System.Management.Automation.PSEventSubscriber
	}
    }

    Context "Check Register-ObjectEvent can validly register events"{
	It "Should return source identifier of PesterTimer " {
	    Register-ObjectEvent -InputObject $pesterobject -EventName CollectionChanged -SourceIdentifier PesterTestRegister
	    (Get-EventSubscriber -SourceIdentifier PesterTestRegister).SourceIdentifier | Should -BeExactly "PesterTestRegister"
	}

	It "Should return an integer greater than 0 for the SubscriptionId" {
	    Register-ObjectEvent -InputObject $pesterobject -EventName CollectionChanged -SourceIdentifier PesterTestRegister
	    (Get-EventSubscriber -SourceIdentifier PesterTestRegister).SubscriptionId | Should -BeGreaterThan 0

	}
    }
}
