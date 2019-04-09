# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Environment-Variables" -Tags "CI" {

    It "Should have environment variables" {
	Get-Item ENV: | Should -Not -BeNullOrEmpty
    }

    It "Should have a nonempty PATH" {
	$ENV:PATH | Should -Not -BeNullOrEmpty
    }

    It "Should contain /bin in the PATH" {
	if ($IsWindows)
	{
	    $ENV:PATH | Should -Match "C:"
	}
	else
	{
	    $ENV:PATH | Should -Match "/bin"
	}
    }

    It "Should have the correct HOME" {
	if ($IsWindows)
	{
	    # \Windows\System32 is found as $env:HOMEPATH for temporary profiles
	    $expected = "\Users", "\Windows"
	    Split-Path $ENV:HOMEPATH -Parent | Should -BeIn $expected
	}
	else
	{
	    $expected = /bin/bash -c "cd ~ && pwd"
	    $ENV:HOME | Should -Be $expected
	}
    }

    It "Should be able to set the environment variables" {
	$expected = "this is a test environment variable"
	{ $ENV:TESTENVIRONMENTVARIABLE = $expected  } | Should -Not -Throw

	$ENV:TESTENVIRONMENTVARIABLE | Should -Not -BeNullOrEmpty
	$ENV:TESTENVIRONMENTVARIABLE | Should -Be $expected

    }
}
