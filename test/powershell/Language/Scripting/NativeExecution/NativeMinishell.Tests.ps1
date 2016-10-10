# Minishell is a powershell concept.
# It's primare use-case is when somebody executes a scriptblock in the new powershell process.
# The objects are automatically marshelled back to the parent session, so users can avoid custom
# serialization to pass objects between two processes.

Describe 'minishell for native executables' -Tag 'CI' {
	
    BeforeAll {
        $powershell = Join-Path -Path $PsHome -ChildPath "powershell"
    }

    It 'gets a hashtable object from minishell' {
        $output = & powershell { @{'a' = 'b'} }
        ($output | measure).Count | Should Be 1
        ($output.GetType().Name) | Should Be 'Hashtable'
        $output['a'] | Should Be 'b'
    }
}
