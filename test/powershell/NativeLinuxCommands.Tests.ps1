Describe "NativeLinuxCommands" {
    It "Should return a type of System.Object for hostname cmdlet" {
    (hostname).GetType().BaseType | Should Be 'System.Object'
	(hostname).GetType().Name | Should Be String
    }

    It "Should have not empty Name flags set for ps object" {
	ps | foreach-object { $_.ProcessName | Should Not BeNullOrEmpty }
    }
	
	It "Should find Application grep" -Skip:$IsWindows {
    (get-command grep).CommandType | Should Be Application
    }

    It "Should pipe to grep and get result" -Skip:$IsWindows {
    "hello world" | grep hello | Should Be "hello world"
    }
	
	It "Should find Application touch" -Skip:$IsWindows {
    (get-command touch).CommandType | Should Be Application
    }
	
	It "Should find Alias ls" -Skip:$IsWindows {
    (get-command ls).CommandType | Should Be Alias
    }
	
	It "Should find Function mkdir" -Skip:$IsWindows {
    (get-command mkdir).CommandType | Should Be Function
    }
}
