# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Native Windows tilde expansion tests' -tags "CI" {
    BeforeAll {
        $originalDefaultParams = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues["it:skip"] = -Not $IsWindows
        $EnabledExperimentalFeatures.Contains('PSNativeWindowsTildeExpansion') | Should -BeTrue
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParams
    }

    # Test ~ expansion
    It 'Tilde should be replaced by the filesystem provider home directory' {
        cmd /c echo ~ | Should -BeExactly ($ExecutionContext.SessionState.Provider.Get("FileSystem").Home)
    }
    # Test ~ expansion with a path fragment (e.g. ~/foo)
    It '~/foo should be replaced by the <filesystem provider home directory>/foo' {
        cmd /c echo ~/foo | Should -BeExactly "$($ExecutionContext.SessionState.Provider.Get("FileSystem").Home)/foo"
        cmd /c echo ~\foo | Should -BeExactly "$($ExecutionContext.SessionState.Provider.Get("FileSystem").Home)\foo"
    }
	It '~ should not be replaced when quoted' {
		cmd /c echo '~' | Should -BeExactly '~'
		cmd /c echo "~" | Should -BeExactly '~'
		cmd /c echo '~/foo' | Should -BeExactly '~/foo'
		cmd /c echo "~/foo" | Should -BeExactly '~/foo'
        cmd /c echo '~\foo' | Should -BeExactly '~\foo'
		cmd /c echo "~\foo" | Should -BeExactly '~\foo'
	}
    It '~ should be expanded with implicit quotes' {
        cmd /c echo ~\a` b | Should -BeExactly "$($ExecutionContext.SessionState.Provider.Get("FileSystem").Home)\a b"
    }
}
