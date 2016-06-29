# Check if History string on the PowerShell class can be publicly set.
Describe -tags 'Innerloop', 'DRT' "bug530585" {
    BeforeAll {
        $s = new-pssession
        $ps = [powershell]::Create()
        $null = $ps.AddCommand("Get-Process")
        $ps.Runspace = $s.Runspace
        $settings=New-Object System.Management.Automation.PSInvocationSettings
        $settings.AddToHistory=$true
        $sampleHistoryString = "This is a sample history string"
    }
    AfterAll {
        $s | Remove-PSSession
    }

    It "update History correctly" {
        $results = $ps.Invoke($null,$settings)
        $history = invoke-command $s { Get-History }
        $history.ToString() |Should be "Get-Process"
    }

    It "Set the History String" {
        $history = invoke-command $s { Clear-History }
        $ps.HistoryString = $sampleHistoryString
        $results = $ps.Invoke($null,$settings)
        $history = invoke-command $s { Get-History }
        $history[1].ToString() | should be $sampleHistoryString
    }

    It "retain the updated History String" {
        $results = $ps.Invoke($null,$settings)
        $history = invoke-command $s { Get-History }
        $history[3].ToString() | should be $sampleHistoryString
    }

}

# Tests Restricted Language Mode check is applied when API is used.
Describe -tags 'Innerloop', 'DRT' "bug536735" {
    BeforeAll {
        # create a dummy script file with some function
        $func = 'function test-bug536735 { }'
        $testfile = join-path $TestDrive (([io.path]::getrandomfilename())+".ps1")
        $func | out-file $testfile

        $rs = [runspacefactory]::CreateRunspace()
        $rs.open()
        $rs.SessionStateProxy.LanguageMode = "restrictedlanguage"
    }

	It "Running powershell.invoke() with external script as command should fail in restricted language mode" {
        # run the script file as command with global scope
        $ps = [powershell]::create().addcommand("$testfile", $false)
        $ps.runspace = $rs
        { $ps.invoke() } | Should throw
        $ps.InvocationStateInfo.State  | Should be "failed"
	}

	It "Get-Command should fail to find script in language mode" {
        # make sure the function is not dot-sourced
        $ps = [powershell]::create().addcommand("get-command").AddArgument("test-bug536735")
        $ps.runspace = $rs
        $op = $ps.invoke()
		$op.count | Should Be 0
	}
}

# Switch parameters are not listed in CommandInfo.ParameterSets[i].Parameters
Describe -tags 'Innerloop', 'DRT' "bug594908-switches-vs-parametersets" {
    BeforeAll {
        function foo { param([switch]$x, $y) "x = $x; y = $y" }
        $c = Get-Command foo -Type function
    }
	It "switch parameter is present in FunctionInfo.ParameterSets[i].Parameters" {
        @($c.Parametersets[0].Parameters | ?{ $_.name -eq 'x'}).Count | should be 1
	}
	It "non-switch parameter is present in FunctionInfo.ParameterSets[i].Parameters" {
		@($c.ParameterSets[0].Parameters | ?{ $_.Name -eq 'y' }).Count | should be 1
	}
}

# If UserName is supplied to Get-Credential Cmdlet, then check if the exception is thrown
# if the length of the user name is greater than maximum allowed ( currently 513 ).
Describe -tags 'Innerloop', 'DRT' "bug607794" {
    AfterAll {
        if ( $currentValue )
        {
            set-itemproperty $key $name $currentValue
        }
    }
    BeforeAll {
        $key = "hklm:\SOFTWARE\Microsoft\PowerShell\1\ShellIds"
        $name = "ConsolePrompting"
        $currentValue = Get-itemproperty $key -Name $name -ea silentlycontinue
        if ( $currentValue )
        {
            set-itemproperty $key $name False
        }
    }
	It "Failed to identify invalid UserName" {
        $result = PowerShell -command { 
            try { 
                Get-Credential -Message Foo -UserName ('a' * 514) -ea stop 
            } catch { 
                $_.fullyqualifiederrorid 
            } 
        }
        $result | Should be "CouldNotPromptForCredential,Microsoft.PowerShell.Commands.GetCredentialCommand"
    }
}

# Format/Type files should not use set-strictmode. This test
# makes sure that error formatting works with set-strictmode -version 2
# The ErrorRecord exception does not have a PSMessageDetails property but should not raise an
# error when running with Set-StrictMode -version 2.0
Describe -tags 'Innerloop', 'DRT' "bug615684" {
    BeforeAll {
        Set-StrictMode -Version 2
    }
    AfterAll {
        Set-StrictMode -off
    }

	It "Formatting an ErrorRecord should not cause an error" {
        $errorStr = new-object system.management.automation.errorrecord 42, "NotReallyAnError", 0, $Null | Out-String
        $errorStr | should not BeNullOrEmpty
	}

	It "Formatting a DateTime should not cause an error" {
        $dateStr = [DateTime]::Now | Out-String
        $dateStr | Should not BeNullOrEmpty
	}

	It "AuditToString on a System.Security.AccessControl.ObjectSecurity should not cause an error" {
        $auditStr = (get-acl .).AuditToString | Out-String
        $auditStr | Should not BeNullOrEmpty
	}
}

# Win8:665097 - Tab completion is broken over psremoting
# Test the new CompletionInput APIs and the new TabExpansion2 function
Describe -tags 'Innerloop', 'DRT' "bug665097-NewTabCompletionAPI" {
    BeforeAll {
        $script = "Get-Command"
        $tuple = [System.Management.Automation.CommandCompletion]::MapStringInputToParsedInput($script, $script.Length)
        $ps = [PowerShell]::Create()
    }
    AfterAll {
        $ps.Dispose()
    }

    ## 1. TabExpansion2 -- Ast input
	It "The first match using TabExpansion2 and Tuple should be 'Get-Command'" {
        $result = TabExpansion2 $tuple.Item1 $tuple.Item2 $tuple.Item3 $null
		$result.CompletionMatches[0].ListItemText | should be "Get-Command"
	}

    ## 2. TabExpansion2 -- String input
	It "The first match using TabExpansion2 and `$script should be 'Get-Command'" {
        $result = TabExpansion2 $script $script.Length $null
		$result.CompletionMatches[0].ListItemText |should be "Get-Command"
	}

    ## 3. API tests
	It "The first match using API and `$script should be 'Get-Command'" {
        $result = [System.Management.Automation.CommandCompletion]::CompleteInput($script, $script.Length, $null, $ps)
		$result.CompletionMatches[0].ListItemText | should be "Get-Command"
	}

	It "The first match using API should be 'Get-Command'" {
        $result = [System.Management.Automation.CommandCompletion]::CompleteInput($tuple.Item1, $tuple.Item2, $tuple.Item3, $null, $ps)
		$result.CompletionMatches[0].ListItemText | should be "Get-Command"
	}

}

# If a COM object implements both IEnumerable IEnumerator, then
# IEnumerable is checked prior to IEnumerator.
# This is in sync with Powershell V2.
Describe -tags 'Innerloop', 'DRT' "bug744151" {
    BeforeAll {
        $skip = $false
        try
        {
            $networkListManager = [Activator]::CreateInstance([Type]::GetTypeFromCLSID([Guid]"{DCB00C01-570F-4A9B-8D69-199FDBA5723B}"));
            $connections = $networkListManager.GetNetworkConnections()
        }
        catch
        {
            $skip = $true
            return
        }
        $firstCount = $secondCount = 0
        if ( $connections )
        {
            $connections | %{ $firstCount++ }
        }
        if ( $firstCount -ne 0 )
        {
            $connections | %{ $secondCount++ }
        }
        if ( $firstCount -eq 0 )
        {
            $skip = $true
        }
    }
    It -skip:$skip "preserve the IEnumerable interface of the COM object." {
        $firstCount | Should be $secondCount
    }
}

# Verify that serialization works on IEnumerables that don't support Reset() method.
Describe -tags 'Innerloop', 'DRT' "bug948569" {
    BeforeAll {
        $skip = $false
        if ( -not (get-command Add-Type))
        {
            $skip = $true
            return
        }
        if ( -not ("MyTest.Bug948569" -as "type" ))
        {
            $skip = try {
                Add-Type -erroraction stop -TypeDefinition 'namespace MyTest {
                        public class Bug948569 {
                            public System.Collections.Generic.IEnumerable<int> MyProperty {
                                get { yield return 1; yield return 2; yield return 3; }
                            }
                        }
                    }'
                $false
            }
            catch
            {
                $true
                # no reason to create the object or continue any of this
                return
            }
        }
        $myClassInstance = new-object Mytest.Bug948569
        $filename = "TestDrive:\{0}.clixml" -f ([io.path]::getrandomfilename())
        $myClassInstance | Export-CliXml $fileName -Depth 3
        $deserializedInstance = Import-CliXml $fileName
    }

    It -skip:$skip "The array returned from MyProperty was expected." {
		$left = @($myClassInstance.MyProperty)
		$right = @(1,2,3)
		$left.count | Should Be $right.count
		for($i=0; $i -lt $left.count; $i++) {
			$left[$i] | Should Be $right[$i]
		}
    }

	It -skip:$skip "Serialization of IEnumerable with no reset method is not working properly" {
		$left = @($deserializedInstance.MyProperty)
		$right = @($myClassInstance.MyProperty)
		$left.count | Should Be $right.count
		for($i=0; $i -lt $left.count; $i++) {
			$left[$i] | Should Be $right[$i]
		}
	}
}

Describe -tags 'Innerloop', 'P1' "OutputRedirectTests" {
    BeforeAll {
        $CurrentVerbosePreference = $VerbosePreference 
        $CurrentDebugPreference = $DebugPreference
        $VerbosePreference = "continue"
        $DebugPreference = "continue"
    }
    AfterAll {
        $VerbosePreference = $CurrentVerbosePreference 
        $DebugPreference = $CurrentDebugPreference
    }

	It "Write-Error can be redirected to output variable." {
        $results = Write-Error "Test Error" 2>&1
        $results | SHould Not BeNullOrEmpty
	}
    It "Write-Error can be redirected to a file" {
        $filename = "TestDrive:\ErrorRedirect"
        Write-Error "Test Error" 2> $filename
        $filename |Should Contain "Test Error"
    }

    ## Output stream merging.
	It "Write Error redirected to output variable." {
        $results = Write-Error "Test Error" 2>&1
		$results | Should Not BeNullOrEmpty
	}

	It "Script: Write Error redirected to output variable." {
        $results = $(Write-Error "Test Error") 2>&1
		$results | Should Not BeNullOrEmpty
	}

	It "Write Warning redirected to output variable." {
        $results = Write-Warning "Test Warning" 3>&1
		$results | Should Not BeNullOrEmpty
    }

	It "Script: Write Warning redirected to output variable." {
        $results = $(Write-Warning "Test Warning") 3>&1
		$results | Should Not BeNullOrEmpty
	}

	It "Write Verbose redirected to output variable." {
        $results = Write-Verbose "Test Verbose" 4>&1
		$results | Should Not BeNullOrEmpty
	}

	It "Script: Write Verbose redirected to output variable." {
        $results = $(Write-Verbose "Test Verbose") 4>&1
		$results | Should Not BeNullOrEmpty
	}

	It "Write Debug redirected to output variable." {
        $results = Write-Debug "Test Debug" 5>&1
        $results.Message | Should be "Test Debug"
	}

	It "Script: Write Debug redirected to output variable." {
        $results = $(Write-Debug "Test Debug") 5>&1
        $results.Message | Should be "Test Debug"
	}

    ## Error file redirect variations.
	It "Write Error redirected to file." {
        $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
        Write-Error "Test Error" 2>&1 1>$filename
		$filename |should contain 'Test Error'
	}

	It "Write Error redirected to file." {
        $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
        Write-Error "Test Error" 2>$filename
        $filename | should contain "Test Error"
	}

	It "Script: Write Error redirected to file." {
        $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
        $(Write-Error "Test Error") 2>$filename
        $filename | should contain "Test Error"
	}

	It "Write Warning not redirected to file." {
        $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
        Write-Warning "Test Warning" 3>$filename
        $filename | Should contain "Test Warning"
	}

	It "Script: Write Warning not redirected to file." {
        $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
        $(Write-Warning "Test Warning") 3>$filename
        $filename | Should contain "Test Warning"
	}

    ## Verbose file redirect variations.
	It "Write Warning not redirected to file." {
        $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
        Write-Verbose "Test Verbose" 4>$filename
        $filename | Should contain "Test Verbose"
	}

	It "Script: Write Warning not redirected to file." {
        $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
        $(Write-Verbose "Test Verbose") 4>$filename
        $filename | Should contain "Test Verbose"
	}

    ## Debug file redirect variations.
	It "Write Debug not redirected to file." {
        $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
        Write-Debug "Test Debug" 5>$filename
        $filename | Should contain "Test Debug"
	}

	It "Script: Write Debug not redirected to file." {
        $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
        $(Write-Debug "Test Debug") 5>$filename
        $filename | Should contain "Test Debug"
	}

    Context "Redirection of All Streams to variable" {
        BeforeAll {
            $results = $(
                Write-Error "Test Error"
                Write-Warning "Test Warning"
                Write-Verbose "Test Verbose"
                Write-Debug "Test Debug"
            ) *>&1
        }
        It "Script: Write Error redirection as all to success output" {
            $results[0] | should match 'Test Error'
        }
        It "Script: Write Warning redirection as all to success output" {
            $results[1] | should match 'Test Warning'
        }
        It "Script: Write Verbose redirection as all to success output" {
            $results[2] | should match 'Test Verbose'
        }
        It "Script: Write Debug redirection as all to success output" {
            $results[3] | should match 'Test Debug'
        }
        It "Script: Write Warning redirected to output variable." {
            $results = $(Write-Warning "Test Warning") *>&1
            $results |should match 'Test Warning'
        }
    }

    Context "Redirection of All Streams to file" {
        BeforeAll {
            $filename = "TestDrive:\redirection.txt"
            $(
                Write-Error "Test Error"
                Write-Warning "Test Warning"
                Write-Verbose "Test Verbose"
                Write-Debug "Test Debug"
            ) *>$filename
        }
        It "Script: Write-Error redirect all should redirect to file." {
            $filename | Should Contain "Test Error"
        }
        It "Script: Write-Warning redirect all should redirect to file." {
            $filename | Should Contain "Test Warning"
        }
        It "Script: Write-Verbose redirect all should redirect to file." {
            $filename | Should Contain "Test Verbose"
        }
        It "Script: Write-Debug redirect all should redirect to file." {
            $filename | Should Contain "Test Debug"
        }
    }

    Context "Scriptblock redirection is correct" {
        It "ScriptBlock: Write-Error should be redirected to file." {
            $filename = "TestDrive:\redirection2.txt"
            . {Write-Error "Write Error"} 2>$filename
            $filename | should contain "Write Error"
        }
        It "ScriptBlock: Write-Warning should be redirected to file." {
            $filename = "TestDrive:\redirection3.txt"
            . {Write-Warning "Write Warning"} 3>$filename
            $filename | should contain "Write Warning"
        }
        It "ScriptBlock: Write-Verbose should be redirected to file." {
            $filename = "TestDrive:\redirection4.txt"
            . {Write-Verbose "Write Verbose"} 4>$filename
            $filename | should contain "Write Verbose"
        }
        It "ScriptBlock: Write-Debug should be redirected to file." {
            $filename = "TestDrive:\redirection5.txt"
            . {Write-Debug "Write Debug"} 5>$filename
            $filename | should contain "Write Debug"
        }
        It "ScriptBlock: Multiple Redirections are correct" {
            $filename = "TestDrive:\redirection43.txt"
            $results = . {write-warning "write warning"; . {write-verbose verbose} 4>$null} 3>$filename
            $filename | should contain "Write Warning"
        }

        It "ScriptBlock: Write-Error should be redirected to a variable." {
            $value = . {Write-Error "Write Error"} 2>&1
            $value | should match "Write Error"
        }
        It "ScriptBlock: Write-Warning should be redirected to a variable." {
            $value = . {Write-Warning "Write Warning"} 3>&1
            $value | should match "Write Warning"
        }
        It "ScriptBlock: Write-Verbose should be redirected to a variable." {
            $value = . {Write-Verbose "Write Verbose"} 4>&1
            $value | should match "Write Verbose"
        }
        It "ScriptBlock: Write-Debug should be redirected to a variable." {
            $value = . {Write-Debug "Write Debug"} 5>&1
            $value | should match "Write Debug"
        }
    }


    Context "Function Redirection is correct" {
        BeforeAll {
            function WarningVerboseDebug
            {
                write-warning "Write Warning"
                write-verbose "Write Verbose"
                write-debug "Write Debug"
            }
            function OutputWarning 
            {
                Write-Output "Write Output"
                Write-Warning "Write Warning"
            }
            function WarningOutput
            {
                Write-Warning "Write Warning"
                Write-Output "Write Output"
            }
            function OutputWarning-WarningOutput
            {
                Write-Warning "Write Warning"
                Write-Output "Write Output"
                OutputWarning
            }
        }
        It "Function: Debug output should be directed to output." {
            $filename = "TestDrive:\{0}" -f ([io.path]::GetRandomFileName())
            $results = WarningVerboseDebug 3>$null 4>$filename 5>&1
            $results.Message | Should Match "Write Debug"
            $filename | Should Contain "Write Verbose"
        }
        It "Warning should be redirected to variable." {
            $warning = .{$out = OutputWarning } 3>&1
            $warning | should match "Write Warning"
        }

        It "Both warnings should be assigned to warning variable." {
            $warning = .{$null = OutputWarning; $null = WarningOutput} 3>&1
            $warning.count |should be 2
        }
        It "Only first warning should be assigned to warning variable." {
            $warning = .{$out = OutputWarning; .{$out1 = WarningOutput 3>&1}} 3>&1
            $warning |should match 'Write Warning'
            $out1[0] |should match 'Write Warning'
        }
        It "Both warnings should be assigned to warning variable." {
            $warning = .{$out = OutputWarning-WarningOutput} 3>&1
            $warning.count |should be 2
        }
    }


    Context "Redirection to Null" {
        It "There should be no output assignment." {
            $a = (write-output "output") 1>$null
            $a | Should BeNullOrEmpty
        }

        It "There should be no output assignment." {
            $a = (write-output "output") *>$null
            $a | Should BeNullOrEmpty
        }

        It "Output should be assigned to variable." {
            $a = (write-output "output") *>&1
            $a | should be "output"
        }

        It "Output should still be assigned to variable when error is redirected." {
            $a = (write-output "output") 2>$null
            $a | should be "output"
        }

        It "Output should still be assigned to variable when warning is redirected." {
            $a = (write-output "output") 3>$null
            $a | should be "output"
        }

        It "Output should still be assigned to variable when verbose is redirected." {
            $a = (write-output "output") 4>$null
            $a | should be "output"
        }

        It "Output should still be assigned to variable when debug is redirected." {
            $a = (write-output "output") 5>$null
            $a | should be "output"
        }
    }

}

# Tests "Paging Support" feature
Describe -tags 'Innerloop', 'DRT' "PagingSupport" {
    BeforeAll {
        function Get-PageOfNumbers
        {
            [CmdletBinding(SupportsPaging = $true)]
            param( [double] $TotalCountAccuracy = 1.0)

            It "PSCmdlet.PagingParameters is expected to be always non-null" {
                $PSCmdlet.PagingParameters | Should Not BeNullOrEmpty
            }

            $FirstNumber = [Math]::Min($PSCmdlet.PagingParameters.Skip, 100)
            $LastNumber = [Math]::Min($PSCmdlet.PagingParameters.First + $FirstNumber - 1, 100)

            if ($PSCmdlet.PagingParameters.IncludeTotalCount)
            {
                $TotalCount = $PSCmdlet.PagingParameters.NewTotalCount(100, $TotalCountAccuracy)
                Write-Output $TotalCount
            }

            $FirstNumber .. $LastNumber | %{ New-Object PSObject -Prop @{
                Number = $_
                Skip = $PSCmdlet.PagingParameters.Skip
                First = $PSCmdlet.PagingParameters.First
                IncludeTotalCount = $PSCmdlet.PagingParameters.IncludeTotalCount
                } 
            } | Write-Output
        }

        function Get-UnpagedNumbers
        {
            [CmdletBinding()]
            param()

                It "PSCmdlet.PagingParameters should always be null" {
                $PSCmdlet.PagingParameters | Should BeNullOrEmpty
            }
        }

        # data for tests
        $pagingFunction = Get-Command Get-PageOfNumbers
        $pagingMetadata = New-Object System.Management.Automation.CommandMetadata $pagingFunction
        $nonPagingFunction = Get-Command Get-UnpagedNumbers
        $nonPagingMetadata = New-Object System.Management.Automation.CommandMetadata $nonPagingFunction
    }

    # Test metadata and presence of parameters
    It "SupportsPaging is properly reflected in CommandMetadata" {
        $pagingMetadata.SupportsPaging | Should Be $true
    }

    Context "IncludeTotalCount Parameter is correct" {
        BeforeAll {
            $IncludeTotalCountParameter = $pagingFunction.Parameters['IncludeTotalCount']
        }
        It "IncludeTotalCount parameter is present" {
            $IncludeTotalCountParameter | Should Not BeNullOrEmpty
        }
        It "IncludeTotalCount parameter has the right type" {
            $IncludeTotalCountParameter.ParameterType | Should Be ([switch])
        }
		It "IncludeTotalCount parameter is not mandatory" {
            $IncludeTotalCountParameter.Attributes.Mandatory | Should Be $false
        }
        It "IncludeTotalCount parameter doesn't have aliases" {
            $IncludeTotalCountParameter.Aliases.Count | Should Be 0
        }
		It "IncludeTotalCount parameter is not positional" {
            ($IncludeTotalCountParameter.Attributes.Position -lt 0) | Should Be $true
        }
		It "IncludeTotalCount parameter is not VFP" {
            $IncludeTotalCountParameter.Attributes.ValueFromPipeline | Should Be $false
        }
		It "IncludeTotalCount parameter is not VFPBPN" {
            $IncludeTotalCountParameter.Attributes.ValueFromPipelineByPropertyName | Should Be $false
        }
		It "IncludeTotalCount parameter is not VFRA" {
            $IncludeTotalCountParameter.Attributes.ValueFromRemainingArguments | Should Be $false
        }
	}

    Context "Skip Parameter is correct" {
        BeforeAll {
            $SkipParameter = $pagingFunction.Parameters['Skip']
        }
        It "SkipParameter parameter is present" {
            $SkipParameter | Should Not BeNullOrEmpty
        }
        It "SkipParameter parameter has the right type" {
            $SkipParameter.ParameterType | Should Be ([uint64])
        }
		It "SkipParameter parameter is not mandatory" {
            $SkipParameter.Attributes.Mandatory | Should Be $false
        }
        It "SkipParameter parameter doesn't have aliases" {
            $SkipParameter.Aliases.Count | Should Be 0
        }
		It "SkipParameter parameter is not positional" {
            ($SkipParameter.Attributes.Position -lt 0) | Should Be $true
        }
		It "SkipParameter parameter is not VFP" {
            $SkipParameter.Attributes.ValueFromPipeline | Should Be $false
        }
		It "SkipParameter parameter is not VFPBPN" {
            $SkipParameter.Attributes.ValueFromPipelineByPropertyName | Should Be $false
        }
		It "SkipParameter parameter is not VFRA" {
            $SkipParameter.Attributes.ValueFromRemainingArguments | Should Be $false
        }
	}

    Context "First Parameter is correct" {
        BeforeAll {
            $FirstParameter = $pagingFunction.Parameters['First']
        }
        It "FirstParameter parameter is present" {
            $FirstParameter | Should Not BeNullOrEmpty
        }
        It "FirstParameter parameter has the right type" {
            $FirstParameter.ParameterType | Should Be ([uint64])
        }
		It "FirstParameter parameter is not mandatory" {
            $FirstParameter.Attributes.Mandatory | Should Be $false
        }
        It "FirstParameter parameter doesn't have aliases" {
            $FirstParameter.Aliases.Count | Should Be 0
        }
		It "FirstParameter parameter is not positional" {
            ($FirstParameter.Attributes.Position -lt 0) | Should Be $true
        }
		It "FirstParameter parameter is not VFP" {
            $FirstParameter.Attributes.ValueFromPipeline | Should Be $false
        }
		It "FirstParameter parameter is not VFPBPN" {
            $FirstParameter.Attributes.ValueFromPipelineByPropertyName | Should Be $false
        }
		It "FirstParameter parameter is not VFRA" {
            $FirstParameter.Attributes.ValueFromRemainingArguments | Should Be $false
        }
	}

    Context "SupportsPaging should be missing when cmdlet attribute is not present" {
        It "SupportsPaging is properly reflected in CommandMetadata" {
            $nonPagingMetadata.SupportsPaging | Should Be $false
        }

        It "IncludeTotalCount parameter is not present" {
            $p = $nonpagingfunction.Parameters['IncludeTotalCount']
            $p | Should BeNullOrEmpty
        }

        It "Skip parameter is not present" {
            $p = $nonpagingfunction.Parameters['Skip']
            $p | Should BeNullOrEmpty
        }

        It "First parameter is not present" {
            $p = $nonpagingfunction.Parameters['First']
            $p | Should BeNullOrEmpty
        }
    }

    # Test propagation of parameters
    Context "Paging works correctly" {
        BeforeAll {
            $n1 = Get-PageOfNumbers | select-object -last 1
            $n2 = Get-PageOfNumbers -IncludeTotalCount | select-object -last 1
            $n3 = Get-PageOfNumbers -skip 5 | select-object -last 1
            $n4 = Get-PageOfNumbers -First 7 | Select-Object -Last 1
            $n5 = Get-PageOfNumbers -IncludeTotalCount -TotalCountAccuracy 1.0
            $n6 = Get-PageOfNumbers -IncludeTotalCount -TotalCountAccuracy 0.5
            $n7 = Get-PageOfNumbers -IncludeTotalCount -TotalCountAccuracy 0.0

            $actualDisplayOfItems = $n5 | Select-Object -Skip 1 | Out-String
            $actualDisplayOfTotalCount = $n5 | Select-Object -First 1 | Out-String
            $actualDisplayOfEverything = $n5 | Out-String

            $n6ActualDisplayOfItems = $n6 | Select-Object -Skip 1 | Out-String
            $n6ActualDisplayOfTotalCount = $n6 | Select-Object -First 1 | Out-String
            $n6ActualDisplayOfEverything = $n6 | Out-String

            $n7actualDisplayOfItems = $n7 | Select-Object -Skip 1 | Out-String
            $n7actualDisplayOfTotalCount = $n7 | Select-Object -First 1 | Out-String
            $n7actualDisplayOfEverything = $n7 | Out-String

            $skip = $true
            if ($PSUICulture -eq 'en-US')
            {
                $skip = $false
            }
        }

        It "Default IncludeTotalCount is correct" {
            $n1.IncludeTotalCount.IsPresent | Should Be $false
        }
        It "Default Skip is correct" {
            $n1.Skip | Should Be 0
        }
        It "Default First is correct" {
            $n1.First | Should Be ([uint64]::MaxValue)
        }
        It "IncludeTotalCount is passed correctly" {
            $n2.IncludeTotalCount.IsPresent | Should Be $true
        }
        It "Default Skip is correct" {
            $n2.Skip | Should Be 0
        }
        It "Default First is correct" {
            $n2.First | Should Be ([uint64]::MaxValue)
        }

        It "Default IncludeTotalCount is correct" {
            $n3.IncludeTotalCount.IsPresent | Should Be $false
        }
        It "Skip is passed correctly" {
            $n3.Skip | Should Be 5
        }
        It "Default First is correct" {
            $n3.First | Should Be ([uint64]::MaxValue)
        }
        It "Default IncludeTotalCount is correct" {
            $n4.IncludeTotalCount.IsPresent | Should Be $false
        }
        It "Default Skip is correct" {
            $n4.Skip | Should Be 0
        }
        It "First is passed correctly" {
            $n4.First | Should Be 7
        }

        It "First object returned is a total count" {
            $n5[0].pstypenames[0] | Should Be "System.UInt64"
        }
        It "Second object returned is an actual object" {
            $n5[1].pstypenames[0] | Should Match "System.Management.Automation.PSCustomObject"
        }
        It "Last object returned is an actual object" {
            $n5[$n5.Count - 1].pstypenames[0] | Should Match "System.Management.Automation.PSCustomObject"
        }

        It "Display of total count includes the number 100" {
            $actualDisplayOfTotalCount |should match "100"
        }

        # localized test
		It -skip:$skip "Test of localizable display content" {
            $actualDisplayOfTotalCount |should match "Total count"
        }

        It "Display of everything is a concatenation of separate totalcount+items displays" {
            $expectedDisplayOfEverything = @($actualDisplayOfTotalCount, $actualDisplayOfItems) -join ""
            $actualDisplayOfEverything | Should Be $expectedDisplayOfEverything
        }

        It "First object returned is a total count" {
            $n6[0].pstypenames[0] | Should Be "System.UInt64"
        }
        It "Second object returned is an actual object" {
            $n6[1].pstypenames[0] | Should Match "System.Management.Automation.PSCustomObject"
        }
        It "Last object returned is an actual object" {
            $n6[$n6.Count - 1].pstypenames[0] | Should Match "System.Management.Automation.PSCustomObject"
        }
        It "Display of total count includes the number 100" {
            $n6ActualDisplayOfTotalCount | should match "100"
        }
        # localized test
        It -skip:$skip "Test of localizable display content (1)" {
            $n6actualDisplayOfTotalCount | should match "Total count"
        }
        # localized test
        It -skip:$skip "Test of localizable display content (2)" {
            $n6actualDisplayOfTotalCount | should match "estimated"
        }
        It "Display of everything is a concatenation of separate totalcount+items displays" {
            $expectedDisplayOfEverything = @($n6actualDisplayOfTotalCount, $n6actualDisplayOfItems) -join ""
            $n6actualDisplayOfEverything | Should Be $expectedDisplayOfEverything
        }

        It "First object returned is a total count" {
            $n7[0].pstypenames[0] | Should Be "System.UInt64"
        }
        It "Second object returned is an actual object" {
            $n7[1].pstypenames[0] | Should Match "System.Management.Automation.PSCustomObject"
        }
        It "Last object returned is an actual object" {
            $n7[$n7.Count - 1].pstypenames[0] | Should Match "System.Management.Automation.PSCustomObject"
        }
        It "Display of total count does not include the number 100" {
            "$n7ActualDisplayOfTotalCount "|should not match "100"
        }
        It -skip:$skip "Test of localizable display content" {
            "$n7ActualDisplayOfTotalCount" |should match "unknown"
        }
        It "DIsplay of everything is a concatenation of separate totalcount+items displays" {
            $expectedDisplayOfEverything = @($n7ActualDisplayOfTotalCount, $n7ActualDisplayOfItems) -join ""
            $n7ActualDisplayOfEverything | Should Be $expectedDisplayOfEverything
        }
    }
}

# Exit should occur cleanly with the LastExitCode showing no error.
Describe -tags 'Innerloop', 'P1' "PowerShellCrashOnExit" {
    It "PowerShell should not crash during exit. (Bug 68814)" {
        ## Start a new powershell process and start two local sessions and two jobs, then exit.
        powershell -noprofile -command {
            ## Remove current AppDomain unhandled exception handler so that any 
            ## unhandled excpetion will propagate and set the shell LastExitCode.
            $psUE = [AppDomain].GetEvent("UnhandledException")
            $d = [delegate]::CreateDelegate(
                [UnhandledExceptionEventHandler],
                [psobject].Assembly.GetType("System.Management.Automation.WindowsErrorReporting"),
                "CurrentDomain_UnhandledException"
                )
            $psUE.RemoveEventHandler([AppDomain]::CurrentDomain, $d)

            ## Begin test.
            new-pssession | out-null
            new-pssession | out-null
            1..2 | % { start-job { start-sleep -s 300 } } | out-null
        }
        $LASTEXITCODE |should be 0
    }
}

# Verifies that all objects in the pipeline are processed when the -OutBuffer parameter is used
# (regression test for Win7:452714)
Describe -tags 'Innerloop', 'DRT' "ScriptProcessorTests" {
    foreach($buffersize in 0..5)
    {
        It "outBuffersize set to '$buffersize' emits proper number of objects in foreach-object process block" {
            $output = write-output (1..4) -Outbuffer $buffersize | & { process { $_ } }
            $output.Length | Should Be 4
        }
        It "outBuffersize set to '$buffersize' emits proper number of objects in foreach-object end block" {
            $output = write-output (1..4) -Outbuffer $buffersize | & { $input }
            $output.Length | Should Be 4
        }
    }
}

Describe -tags 'Innerloop', 'DRT' "SessionStatePublicConstructorTest" {
    BeforeAll {
        $sessionState = new-object System.Management.Automation.SessionState
        $providerInfo = $null;
        $path = $sessionState.Path.GetResolvedProviderPathFromPSPath("HKLM:\SOFTWARE\Microsoft", [ref] $providerInfo)
        $skip = $true
        if ( get-psprovider -ea silentlycontinue registry )
        {
            $skip = $false
        }
    }
    
	It -skip:$skip "output of GetResolvedProviderPathFromPSPath should not be null." {
		$path | Should Not BeNullOrEmpty
	}
	It -skip:$skip "Provider should be registry when resolving HKLM:\SOFTWARE\Microsoft path" {
		$providerInfo.Name | Should Be "Registry"
	}
}

# Verifies parameter binding by pipeline object
Describe -tags 'Innerloop', 'DRT' "Test-MultiPipelineParameterBinding" {
    BeforeAll {
        function Test-Binding-Three-Parameters
        {
            [CmdletBinding(DefaultParameterSetName = "FromFile")]
            param(
            [Parameter(Mandatory = $true, ParameterSetName = "FromDirectory", ValueFromPipeline = $true)]
            [System.IO.DirectoryInfo] $directory,

            [Parameter(Mandatory = $true, ParameterSetName = "FromFile", ValueFromPipeline = $true)]
            [System.IO.FileInfo] $file,

            [Parameter(Mandatory = $true, ParameterSetName = "FromProcess", ValueFromPipeline = $true)]
            [System.Diagnostics.Process] $process,

            [Parameter(Mandatory = $true, ParameterSetName = "ID")]
            [Guid] $ID
            )

            process
            {
                if($file) { "FILE: $file" }
                if($directory) { "DIRECTORY: $directory" }
                if($process) { "PROCESS: $process" }
                if($ID) { "ID: $ID" }
            }
        }
        $result = (Get-Location | Get-Item),(Get-Process -Id $pid),@(Get-Location | Get-ChildItem | ? { -not $_.PsIsContainer })[0]
        $output1 = $result | Test-Binding-Three-Parameters

        function Test-ParameterDisambiguationByPropertyName
        {
            param(
                [Parameter(Mandatory = $true, ParameterSetName="Foo1", ValueFromPipelineByPropertyName = $true)]
                [int] $process = 0,
                [Parameter(Mandatory = $true, ParameterSetName="Foo2", ValueFromPipelineByPropertyName = $true)]
                [int] $directory = 0
            )
            begin { }
            process { "Process was: $process, Directory was: $directory" }
            end { }
        }
        $output2 = [pscustomobject]@{ Process = 5 },[pscustomobject]@{ Directory = 6 } |
                Test-ParameterDisambiguationByPropertyName

    }

    ## Pass in file, directory, process.
	It "Should have bound directory" {
		$output1[0] |should match "DIRECTORY"
	}
	It "Should have bound process" {
		$output1[1] |should match "PROCESS"
	}
	It "Should have bound file" {
		$output1[2] |should match "FILE"
	}

    ## Pass in invalid object. Should fail to bind to default
	It "Should have attempted to bind to default parameter set" {
        try 
        {
            [Guid]::NewGuid() | Test-Binding-Three-Parameters -ErrorAction stop
            Throw "OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "InputObjectNotBound,Test-Binding-Three-Parameters" 
        }
	}

    ## Pass in pipeline and named. Should error out.
	It "Should not be able to bind to ID" {
        try 
        {
            (Get-Process -Id $pid) | Test-Binding-Three-Parameters -ID ([Guid]::NewGuid()) -ErrorAction Stop
            Throw "OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be "InputObjectNotBound,Test-Binding-Three-Parameters" 
        }
	}

	It "Should have bound process" {
        $output2[0] | Should Be "Process was: 5, Directory was: 0" 
	}
	It "Should have bound directory" {
        $output2[1] | Should Be "Process was: 0, Directory was: 6" 
	}


}
# Tests PowerShell batch execution in both local and remote sessions
Describe -tags 'Innerloop', 'P1' "TestBatchExecution" {
    Context "Local Execution" {
        BeforeEach {
            $localRunspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $localRunspace.Open()
            $ps = [System.Management.Automation.PowerShell]::Create()
            $ps.Runspace = $localRunspace
        }
        AfterEach {
            $ps.Dispose()
            $localRunspace.Dispose()
        }
        It "Should have two results and the first should be 'idle'" {
            $result = $ps.AddCommand("Get-Process").AddArgument("idle").AddStatement().AddCommand("Get-Date").Invoke()
            $result.count | should be 2
            $result[0].processname | should be "idle"
        }

        It "AddStatement without a prior AddCommand should work" {
            $result = $ps.AddStatement().AddCommand("Get-Date").Invoke()
            $result.Count | should be 1
        }

        It "Async execution is correct" {
            $async = $ps.AddCommand("Get-Process").AddArgument("idle").AddStatement().AddCommand("Get-Date").BeginInvoke()
            $result = $ps.EndInvoke($async)
            $result.Count | Should Be 2
            $result[0].ProcessName | Should Be "Idle"
        }
        It "Async execution using stop is correct" {
            $async = $ps.AddCommand("Start-Sleep").AddArgument("10000").AddStatement().AddCommand("Get-Date").BeginInvoke()
            $ps.Stop()

            try
            {
                $result = $ps.EndInvoke($async)
            }
            catch
            {
                # don't check to see whether this throws or not
                # the error (if one exists) is non-deterministic
                # it may not throw at all (if the timing is right)
                ; 
            }
            # All we really care about is that the state is "Stopped"
            $ps.InvocationStateInfo.State | Should Be "Stopped"
        }
        It "Should handle errors in async correctly" {
            $result = $ps.AddScript("get-Command BogusCommand").AddStatement().AddCommand("get-date").BeginInvoke()
            $output = $ps.EndInvoke($result)
            $output.Count | Should Be 1
            $ps.Streams.Error.Count | Should Be 1
        }
    }

    Context "Remote Execution" {
        BeforeEach {
            $connectionInfo = New-Object System.Management.Automation.Runspaces.WSManConnectionInfo
            $remoteRunspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace($connectionInfo)
            $remoteRunspace.Open()
            $ps = [System.Management.Automation.PowerShell]::Create()
            $ps.Runspace = $remoteRunspace
        }
        AfterEach {
            $ps.Dispose()
            $remoteRunspace.Dispose()
        }
        It "Should execute 2 statements and the result should be correct" {
            $result = $ps.AddCommand("Get-Process").AddArgument("idle").AddStatement().AddCommand("Get-Date").Invoke()
            $result.Count | Should Be 2
            $result[0].ProcessName | Should Be "Idle"
        }
        It "Should execute 2 statements async and the result should be correct" {
            $ps.Commands.Clear()
            $async = $ps.AddCommand("Get-Process").AddArgument("idle").AddStatement().AddCommand("Get-Date").BeginInvoke()
            $result = $ps.EndInvoke($async)
            $result.Count | Should Be 2
            $result[0].ProcessName | Should Be "Idle"
        }
        It "Should stop async operations correctly" {
            $ps.Commands.Clear()
            $async = $ps.AddCommand("Start-Sleep").AddArgument("10000").AddStatement().AddCommand("Get-Date").BeginInvoke()
            $ps.Stop()

            # Ignore remote wrapper exception and check final PowerShell state.
            # A wrapped stop exception may or may not be thrown depending on which PSRP message
            # is received first.  If it is the "invocation state changed" message then it will include
            # the exception.  But if it is the "command complete" message then it will not include
            # any stop related message.
            try
            { 
                $results = $ps.EndInvoke($async)
            } catch { }
            $ps.InvocationStateInfo.State | Should Be "Stopped"
        }
    }
    Context "Execution in default runspace" {
        BeforeAll {
            $ps = [System.Management.Automation.PowerShell]::Create()
            [void]$ps.Commands.AddCommand("Get-Process").AddArgument("idle").AddStatement().AddCommand("Get-Date")
            $result1 = $ps.Invoke()
            $ps.Commands.Clear()
            [void]$ps.Commands.AddCommand("Get-Process").AddArgument("idle")
            $result2 = $ps.Invoke()
        }
        It "Expecting 2 results" {
            $result1.Count | Should Be 2
        }
        It "Expecting the idle process" {
            $result1[0].ProcessName | Should Be "Idle"
        }
        It "Expecting 1 result" {
            $result2.Count | Should Be 1
        }
        It "Expecting the idle process" {
            $result2[0].ProcessName | Should Be "Idle"
        }
    }
}

Describe "PSDefaultParameters settings are effective" -Tags "P1", "RI" {
    BeforeAll {
        $currentSettings = $PSDefaultParameterValues
        $PSDefaultParameterValues["Disabled"] = $true
        $expected = get-process -id $PID
    }
    AfterAll {
        $PSdefaultParameterValues = $currentSettings
    }
    Context "Proper isolation of PSDefaultParameterValues" {
        BeforeAll {
            Set-Content 'Function PSDefaultParamTest { $PSDefaultParameterValues["Get-Process:id"] = $pid ; get-process }' -Path "TestDrive:\PSDefaultParameterTest.psm1"
        }
        It "After module load, defaultparameters should be set" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = PSDefaultParamTest
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.ProcessId | Should be $expected.ProcessId
        }
        # we should have more processes here
        It "module load does not change global setting" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = get-process
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.Count -gt 1 | should be $true
        }
        It "After module unload setting changes behavior" {
            $ps = get-process
            @($ps).Count -gt 1 | Should be $true
        }
    }

    Context "PSDefaultParameters settings should be isolated in the module and work in a case insensitive manner" {
        BeforeAll {
            Set-Content 'Function PSDefaultParamTest { $PSDefaultParameterValues["gET-pROCESS:ID"] = $pid ; get-process }' -Path "TestDrive:\PSDefaultParameterTest.psm1"
        }
        It "After module load, defaultparameters should be set" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = PSDefaultParamTest
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.ProcessId | Should be $expected.ProcessId
        }
        # we should have more processes here
        It "module load does not change global setting" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = get-process
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.Count -gt 1 | should be $true
        }
        It "After module unload setting changes behavior" {
            $ps = get-process
            @($ps).Count -gt 1 | Should be $true
        }
    }

    Context "PSDefaultParameters settings should be isolated in the module and work in a whitespace insensitive manner" {
        BeforeAll {
            Set-Content 'Function PSDefaultParamTest { $PSDefaultParameterValues[" gET-pROCESS : ID "] = $pid ; get-process }' -Path "TestDrive:\PSDefaultParameterTest.psm1"
        }
        It "After module load, defaultparameters should be set" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = PSDefaultParamTest
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.ProcessId | Should be $expected.ProcessId
        }
        # we should have more processes here
        It "module load does not change global setting" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = get-process
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.Count -gt 1 | should be $true
        }
        It "After module unload setting changes behavior" {
            $ps = get-process
            @($ps).Count -gt 1 | Should be $true
        }
    }

    Context "PSDefaultParameters settings should not be affected by how much whitespace is in the setting" {
        BeforeAll {
            Set-Content 'Function PSDefaultParamTest { $PSDefaultParameterValues["   gET-pROCESS : ID    "] = $pid ; get-process }' -Path "TestDrive:\PSDefaultParameterTest.psm1"
        }
        It "After module load, defaultparameters should be set" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = PSDefaultParamTest
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.ProcessId | Should be $expected.ProcessId
        }
        # we should have more processes here
        It "module load does not change global setting" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = get-process
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.Count -gt 1 | should be $true
        }
        It "After module unload setting changes behavior" {
            $ps = get-process
            @($ps).Count -gt 1 | Should be $true
        }
    }

    Context "PSDefaultParameters settings should not be affected if the settings have quotes" {
        BeforeAll {
            Set-Content 'Function PSDefaultParamTest { $PSDefaultParameterValues["gET-pROCESS":"ID"] = $pid ; get-process }' -Path "TestDrive:\PSDefaultParameterTest.psm1"
        }
        It "After module load, defaultparameters should be set" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = PSDefaultParamTest
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.ProcessId | Should be $expected.ProcessId
        }
        # we should have more processes here
        It "module load does not change global setting" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = get-process
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.Count -gt 1 | should be $true
        }
        It "After module unload setting changes behavior" {
            $ps = get-process
            @($ps).Count -gt 1 | Should be $true
        }
    }

    Context "PSDefaultParameters settings should not be affected if the settings have tabs" {
        BeforeAll {
            Set-Content 'Function PSDefaultParamTest { $PSDefaultParameterValues["gET-pROCESS":"`tID"] = $pid ; get-process }' -Path "TestDrive:\PSDefaultParameterTest.psm1"
        }
        It "After module load, defaultparameters should be set" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = PSDefaultParamTest
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.ProcessId | Should be $expected.ProcessId
        }
        # we should have more processes here
        It "module load does not change global setting" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = get-process
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.Count -gt 1 | should be $true
        }
        It "After module unload setting changes behavior" {
            $ps = get-process
            @($ps).Count -gt 1 | Should be $true
        }
    }

    Context "PSDefaultParameters settings should not be affected if the settings have tabs" {
        BeforeAll {
            Set-Content 'Function PSDefaultParamTest { $PSDefaultParameterValues["gET-pROC*":"I*D"] = $pid ; get-process }' -Path "TestDrive:\PSDefaultParameterTest.psm1"
        }
        It "After module load, defaultparameters should be set" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = PSDefaultParamTest
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.ProcessId | Should be $expected.ProcessId
        }
        # we should have more processes here
        It "module load does not change global setting" {
            import-module TestDrive:\PSDefaultParameterTest.psm1
            $ps = get-process
            remove-module PSDefaultPArameterTest.psm1 -ea silentlycontinue
            $ps.Count -gt 1 | should be $true
        }
        It "After module unload setting changes behavior" {
            $ps = get-process
            @($ps).Count -gt 1 | Should be $true
        }
    }

}

Describe "PSDefaultPArameterValues settings are respected" -Tags "P1", "RI" {
    BeforeAll {
        $currentSettings = $PSDefaultParameterValues
        $PSDefaultParameterValues["Disabled"] = $true
    }
    AfterAll {
        $PSDefaultParameterValues = $currentSettings 
    }
    Context "When value is an array" {
        BeforeEach {
            $PSdefaultParameterValues["Disabled"] = $false
        }
        AfterEach {
            $PSDefaultParameterValues.Clear()
        }
        BeforeAll {
            $expected = Get-Alias icm,gi,gc -exclude gi,gc
        }
        It "ignores whitespace-1" {
            $PSDefaultPArameterValues["Get-alias:exclude"] = "gi","gc"
            $observed = get-alias icm,gi,ci
            $observed.Definition |should be $expected.Definition
        }
        It "ignores whitespace-2" {
            $PSDefaultPArameterValues[" Get-alias  : exclude "] = @("gi","gc")
            $observed = get-alias icm,gi,ci
            $observed.Definition |should be $expected.Definition
        }
        It "ignores whitespace-3" {
            $PSDefaultPArameterValues[" Get-alias  : `texclude "] = @("gi","gc")
            $observed = get-alias icm,gi,ci
            $observed.Definition |should be $expected.Definition
        }
        It "ignores whitespace and quotes" {
            $PSDefaultPArameterValues[" Get-alias  :' exclude '"] = @("gi","gc")
            $observed = get-alias icm,gi,ci
            $observed.Definition |should be $expected.Definition
        }
    }

    Context "Binding works when value is a string" {
        BeforeEach {
            $PSdefaultParameterValues["Disabled"] = $false
        }
        AfterEach {
            $PSDefaultParameterValues.Clear()
        }
        It "setting date default output format" {
            $fmt = "%Y / %m / %d / %A / %Z"
            $PSDefaultPArameterValues["Get-date:uformat"] = $fmt
            $observed = get-date
            $expected = get-date -uformat $fmt
            $observed.Definition |should be $expected.Definition
        }
    }

    Context "Binding works when value is a scriptblock" {
        BeforeEach {
            $PSdefaultParameterValues["Disabled"] = $false
        }
        AfterEach {
            $PSDefaultParameterValues.Clear()
        }
        It "default invocation of hostname" {
            $sb = { hostname }
            $PSDefaultPArameterValues["invoke-command:scriptblock"] = { $sb }
            $observed = invoke-command
            $expected = invoke-command $sb
            $observed.Definition |should be $expected.Definition
        }
    }

    Context "Binding works with the wildcards in the '*'.'*' key format" {
        BeforeEach {
            $PSdefaultParameterValues["Disabled"] = $false
        }
        AfterEach {
            $PSDefaultParameterValues.Clear()
        }
        It "default invocation of hostname" {
            $PSDefaultPArameterValues = @{ "get-random:mini*" = 10; "`"get-random`":`"maxi*`"" = 11 }
            $observed = get-random
            $expected = get-random -maximum 11 -minimum 10
            $observed |should be $expected
        }
    }

    Context "Binding works appropriately with dynamic parameters" {
        BeforeAll {
            New-Item -type file TestDrive:\AAABBB | out-null
        }
        BeforeEach {
            $PSdefaultParameterValues["Disabled"] = $false
        }
        AfterEach {
            $PSDefaultParameterValues.Clear()
        }
        It "Dynamic parameter is correct" {
            $PSDefaultParameterValues["Get-Childitem:Filter"] = "AA*"
            $observed = get-childitem TestDrive:\
            $expected = get-childitem TestDrive:\ -filter "AA*"
            $observed.Name | Should be $expected.Name
        }
        It "Dynamic and non-dynamic parameters are correct" {
            $PSDefaultParameterValues = @{ "Get-ChildItem:path" = "TestDrive:\"; "Get-ChildItem:Filter" = "AA*" }
            $observed = get-childitem 
            $expected = get-childitem TestDrive:\ -filter "AA*"
            $observed.Name | Should be $expected.Name
        }
    }
    Context "Additional Tests" {
        BeforeAll {
            New-Item -type File TESTDRIVE:\AAAZZ | out-null
            New-Item -type File TESTDRIVE:\BBBZZ | out-null
        }
        BeforeEach {
            $PSdefaultParameterValues["Disabled"] = $false
        }
        AfterEach {
            $PSDefaultParameterValues.Clear()
        }
        
        It "Binding works before mandatory parameter checking" {
            $PSDefaultParameterValues = @{ "Get-Random:Count" = 1 ; "Get-Random:maximum" = 1 }
            $expected = Get-Random -Maximum 1
            $observed = Get-Random
            $observed | Should be $expected
        }
        It "binding happens after pipeline input binding" {
            $PSDefaultParameterValues = @{ "Get-Random:Count" = 1 ; "Get-Random:maximum" = 1 }
            $expected = "String" | Get-Random -Count 1
            $observed = "String" | Get-Random
            $observed | Should be $expected
        }

        It "binding happens before mandatory checking" {
            function Get-Param1
            {
                [CmdletBinding(DefaultParameterSetName="Set2")]
                param(
                    [Parameter(Mandatory=$true,ParameterSetName="Set1",ValueFromPipeline=$true)][string]$StringParam, 
                    [Parameter(Mandatory=$true,ParameterSetName="Set2")][int]$IntParam
                    )
                if($PSCmdlet.ParameterSetName -eq "Set1") { Write-Output $StringParam }
                else { Write-Output $IntParam }
            }
            $PSDefaultParameterValues = @{ "Get-Param1:stringparam" = "string"; "Get-param1:intparam" = 2 }
            $expected = Get-Param1 -intparam 2
            $observed = Get-Param1
            $observed | Should be $expected
        }
            

        It "binding happens after pipeline binding" {
            function Get-Param2
            {
                [CmdletBinding(DefaultParameterSetName="Set2")]
                param(
                    [Parameter(Mandatory=$true,ParameterSetName="Set1",ValueFromPipeline=$true)][string]$StringParam, 
                    [Parameter(ParameterSetName="Set1")][string]$SecondParam, 
                    [Parameter(Mandatory=$true,ParameterSetName="Set2")][int]$IntParam
                    )
                if($PSCmdlet.ParameterSetName -eq "Set1") { Write-Output $StringParam $SecondParam }
                else { Write-Output $IntParam }
            }
            $expected = "string" | Get-Param2 -SecondParam PIPELINE
            $PSDefaultParameterValues = @{ "Get-Param2:SecondParam" = "PIPELINE"; "Get-param2:intparam" = 3 }
            $observed = "string" | Get-Param2
            $observed | Should be $expected
        }

        It "binding state is passed to the ScriptBlock: BoundParameters" {
            $expected = Get-Process -id $pid -module
            $PSDefaultParameterValues["get-process:module"] = { param($state) if($state.BoundParameters.Contains("id")) {$true} }
            $observed = Get-Process -id $pid
            $observed.Count | Should be $expected.Count
        } 

        It "binding state is passed to the ScriptBlock: BoundPositionalParameters" {
            $expected = get-childitem TestDrive:\ -filter AA*
            $PSDefaultParameterValues["get-childitem:filter"] = { param($state) if($state.BoundPositionalParameters.Contains("path")) {"AA*"} else {"ZZ*"} }
            $observed = Get-childitem TestDrive:\
            $observed.Name | Should be $expected.Name
        }
        It "default parameter binding works if the user explicitly picks another parameter set" {
            $expected = Get-Process -Name System
            $PSDefaultParameterValues= @{ "get-process:id" = $pid }
            $observed = Get-Process -Name System
            $observed.Name | Should be $expected.Name
        }

        It "default binding supports cmdlet alias" {
            $expected = Get-Process -id $PID
            $PSDefaultPArameterValues["gps:id"] = $PID
            $observed = Get-process
            $observed.ProcessId | Should be $expected.ProcessId
        }
    
        It "supports value from remaining argument binding" {
            $expected = ForEach-Object {"process1"} {"process2"} {"process3"} -Begin {"Begin"}
            $PSDefaultParameterValues["foreach-object:begin"] = {{"Begin"}}
            $observed = ForEach-Object {"process1"} {"process2"} {"process3"}   
            $expectedString = $expected -join ":"      
            $observedString = $observed -join ":"
            $observedString | Should be $expectedString
        }

        It "default binding affects the positional binding" {
            $expected = "string" | ForEach-Object Length
            $PSDefaultParameterValues= @{ "foreach-object:begin" = {{"Begin"}}}
            $observed = "string" | ForEach-Object Length
            $observed | Should be $expected
        }

        It "Throws the proper error if the argument cannot be converted" {
            try { 
                $PSDefaultParameterValues["ForEach-Object:Begin"] = {{"Begin"}}
                ForEach-Object {"Process1"} {"Process2"} @{a = 1} 
                throw "Execution OK"
            } catch { 
                $_.FullyQualifiedErrorId | Should be "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.ForEachObjectCommand"
            }
        }

        It "Throws the proper error when no positional parameter is found" {
            Function Get-Param3
            {
                [CmdletBinding(DefaultParameterSetName="Set1")]
                param( 
                [Parameter(Mandatory=$true,ParameterSetName="Set1")][string]$StringParam, 
                [Parameter(Mandatory=$true,ParameterSetName="Set2")][int]$IntParam
                )
            }
            try { 
                $PSDefaultParameterValues["Get-Param3:StringParam"] = "string"
                Get-Param3 argument
                throw "Execution OK"
            } catch { 
                $_.FullyQualifiedErrorId | Should be "PositionalParameterNotFound,Get-Param3"
            }
        }

        It "Throws the proper error when the parameter set cannot be determined" {
            try { 
                $PSDefaultParameterValues["Invoke-Command:ComputerName"] = "Any"
                Invoke-Command
                throw "Execution OK"
            } catch { 
                $_.FullyQualifiedErrorId | Should be "AmbiguousParameterSet,Microsoft.PowerShell.Commands.InvokeCommandCommand"
            }
        }

    }

}

# Tests Restricted Language Mode check is applied when API is used.
Describe -tags 'Innerloop', 'DRT' "TestHadErrorsProperty" {
    BeforeAll {
        $s = New-PSSession
        $remoteRunspace = $s.Runspace
        $localRunspace = [runspacefactory]::CreateRunspace()
        $localRunspace.Open()
    }
    AfterAll {
        Remove-PsSession $s
        $localRunspace.Dispose()
    }

    foreach ($r in $localRunspace,$remoteRunspace)
    {
        $case = if ($r -eq $localRunspace) { "Local case" } else { "Remote case" }
        $p = [powershell]::Create().AddScript({write-output 'no error'});
        $p.Runspace = $r
        $result = $p.Invoke()
        It "${case}: Invoke() should have returned 'no error'" {
            $result |should be 'no error'
        }
        It "${case}: no error case - HadErrors should be false" {
            $p.HadErrors | Should be $false
        }

        $p = [powershell]::Create().AddScript({write-output 'wrote error'; write-error 'an error'});
        $p.Runspace = $r
        $result = $p.Invoke()
        It "${case}: Invoke() should have returned 'wrote error'" {
            $result |should be 'wrote error'
        }
        It "${case}: wrote error case - HadErrors should be true" {
            $p.HadErrors | Should Be $true
        }

        $p = [powershell]::Create().AddScript({write-output 'exception';  1/$null});
        $p.Runspace = $r
        $result = $p.Invoke()
        It "${case}: Invoke() should have returned 'exception'" {
            $result |should be 'exception'
        }
        if ($case -notmatch "Local case")
        {
            It "${case}: exception case - HadErrors should be true" {
                $p.HadErrors | Should Be $true
            }
        }

        $p = [powershell]::Create().AddScript({write-output 'caught exception' ; try { 1/$null } catch { } });
        $p.Runspace = $r
        $result = $p.Invoke()
        It "${case}: Invoke() should have returned 'caught exception'" {
            $result |should be 'caught exception'
        }
        It "${case}: caught exception case - HadErrors should be false" {
            $p.HadErrors | Should Be $false
        }

        $p = [powershell]::Create().AddScript({write-output 'redirected error' ; write-error foo > $null; 'hi'});
        $p.Runspace = $r
        $result = $p.Invoke()
        It "${case}: Invoke() should have returned 'redirected error'" {
            $result[0] |should be 'redirected error'
        }
        It "${case}: redirected error case - HadErrors should be true" {
            $p.HadErrors | Should Be $true
        }
    }
}

# Includes tests related to Win7:527516 Regression:  Can't redirect native stderr to $null in PowerShell 2.0
Describe -tags 'Innerloop', 'DRT' "TestNativeCommandRedirection" {
    BeforeAll {
        $script = "${TestDrive}\script.cmd"
        "@echo off","echo Hello","Remove-Item TESTDRIVE:\doesnotexist" | Set-content $script
    }
    Context ".Cmd redirection should redirect stderr, if script does not" {
        BeforeAll {
            & { cmd /c $script } 2> TestDrive:\unexpectedRedirection
        }
        It "There should be stderr redirection" {
            "TestDrive:\unexpectedRedirection" | Should contain "remove-item"
        }
    }
    Context ".Cmd redirection should redirect stdout" {
        BeforeAll {
            & { cmd /c $script > "$TestDrive\stdout" } 2> TestDrive:\unexpectedRedirection
        }
        It "Stdout has proper contents" {
            "${TESTDRIVE}\stdout" | should contain hello
        }
        It "Stderr has proper contents" {
            "TestDrive:\unexpectedRedirection" | Should contain "remove-item"
        }
    }
    Context ".Cmd redirection should redirect stderr" {
        BeforeAll {
            & { cmd /c $script 2> "$TestDrive\stderr" } 2> TestDrive:\unexpectedRedirection
        }
        It "Stderr has proper contents" {
            "${TESTDRIVE}\stderr" | should contain "remove-item"
        }
        It "no extra redirection should be present" {
            (Get-Item "TestDrive:\unexpectedRedirection").Length | Should be 0
        }
    }
    Context ".Cmd redirection should redirect stderr and stdout" {
        BeforeAll {
            & { cmd /c $script > "$TestDrive\stdout" 2> "$TestDrive\stderr" } 2> TestDrive:\unexpectedRedirection
        }
        It "Stdout has proper contents" {
            "${TESTDRIVE}\stdout" | should contain "hello"
        }
        It "Stderr has proper contents" {
            "${TESTDRIVE}\stderr" | should contain "remove-item"
        }
        It "no extra redirection should be present" {
            (Get-Item "TestDrive:\unexpectedRedirection").Length | Should be 0
        }
    }
    Context ".Cmd redirection should redirect stderr and stdout to a single file" {
        BeforeAll {
            & { cmd /c $script > "$TestDrive\stdout" 2>&1 } 2> TestDrive:\unexpectedRedirection
        }
        It "file has proper stdout contents" {
            "${TESTDRIVE}\stdout" | should contain "hello"
        }
        It "file  has proper stderr contents" {
            "${TESTDRIVE}\stdout" | should contain "remove-item"
        }
        It "no extra redirection should be present" {
            (Get-Item "TestDrive:\unexpectedRedirection").Length | Should be 0
        }
    }
}

# Verifies parameter binding handles non-resettable enumerators
Describe -tags 'Innerloop', 'DRT' "TestNonResettableEnumeratorBinding" {
	It "Should not have generated parameter binding error" {
        $result = "blah" | powershell.exe -noprofile { try { write-warning -message $input 3>$null } catch { "Caught" } }
		$result |should not be "Caught"
	}
}


# Verifies that redirection is detected properly when you've defined an Out-Default function
Describe -tags 'Innerloop', 'DRT' "TestOutDefaultRedirection" {
    BeforeAll {
        $outputExe = "${TestDrive}\RedirectionTester.exe"
    }

    # this context is for isolation to be sure that the executable that is created may be removed
    Context "Custom Out-Default function does not impact redirection" {
        BeforeAll {
            $skip = $false
            if ( -not (get-command Add-Type -ea silentlycontinue ))
            {
                $Skip = $true
                return
            }
            Add-Type -OutputAssembly $outputExe -typedefinition '
            public class RedirectionTester {
                public static int Main(string[] args) {
                    try { bool visible = System.Console.CursorVisible; }
                    catch(System.IO.IOException) { return 1; }
                return 0;
                }
            }'

            $outDefault = @'
            function GLOBAL:Out-Default
            {
                [CmdletBinding()]
                param( [Parameter(ValueFromPipeline=$true)] [System.Management.Automation.PSObject] ${InputObject})
                begin {
                    try {
                        $cachedOutput = New-Object System.Collections.ArrayList
                        $wrappedCmd = $ExecutionContext.InvokeCommand.GetCommand('Out-Default', [System.Management.Automation.CommandTypes]::Cmdlet)
                        $scriptCmd = {& $wrappedCmd @PSBoundParameters }
                        $steppablePipeline = $scriptCmd.GetSteppablePipeline()
                        $steppablePipeline.Begin($PSCmdlet)
                    } catch {
                        throw
                    }
                }
                process {
                    try {
                        [void] ($cachedOutput.Add($_))
                        while($cachedOutput.Count -gt 500) { $cachedOutput.RemoveAt(0) }
                        $steppablePipeline.Process($_)
                    } catch {
                        throw
                    }
                }
                end {
                    try {
                        $GLOBAL:ll = $cachedOutput | % { $_ }
                        $steppablePipeline.End()
                    } catch {
                        throw
                    }
                }
            }
'@

            $powerShell = [PowerShell]::Create()
            [void]$powerShell.AddScript($outDefault)
            [void]$powerShell.Invoke()
            [void]$powerShell.Commands.Clear()
        }

        # there is a timing issue with regard to removing TESTDRIVE
        # be sure that you remove the executable before TESTDRIVE
        # is deleted
        AfterAll {
            if ( $powershell ) { $powershell.Dispose() }
            remove-item variable:powershell
            $loop = 0
            while ( test-path $outputExe )
            {
                remove-item $outputExe -force -ea silentlycontinue 
                start-sleep 1
                $loop++
                if ( $loop -ge 10 ) {break}
            }
        }

        It -skip:$skip "Should not have been redirected" {
            [void]$powerShell.AddScript("& '$outputExe'")
            [void]$powerShell.AddCommand('Out-Default')
            $result = $powerShell.Invoke()
            [void]$powerShell.Commands.Clear()
            [void]$powerShell.AddScript('$lastExitCode')
            $lastExitCode = $powerShell.Invoke()
            "$lastExitCode" | Should be "0"
        }

        It -skip:$skip "Should have been redirected" {
            [void]$powerShell.Commands.Clear()
            [void]$powerShell.AddScript("`$foo = & '$outputExe'")
            [void]$powerShell.AddCommand('Out-Default')
            [void]$powerShell.Invoke()
            [void]$powerShell.Commands.Clear()
            [void]$powerShell.AddScript('$lastExitCode')
            $lastExitCode = $powerShell.Invoke()
            "$lastExitCode" | should be "1" 
        }
    }
}

# Verifies that parameter binder is aware of PSTypeName attribute
Describe -tags 'Innerloop', 'DRT' "TestParameterBindingForEtsType" {
    BeforeAll {
        $skip = $false
        if ( -not (get-command add-type -ea silentlycontinue ) )
        {
            $skip = $true
            return
        }
        if ( -not (get-command get-wmiobject -ea silentlycontinue ) )
        {
            $skip = $true
            return
        }
        $wmiProcess = Get-WmiObject -Query "SELECT * FROM Win32_Process WHERE ProcessId = $pid"
        $wmiService = Get-WmiObject -Query "SELECT * FROM Win32_Service WHERE Name = 'Netlogon'"
        $wmiOperatingSystem = Get-WmiObject Win32_OperatingSystem

        # we will leave this in TEMPDIR (rather than TESTDRIVE) as we cannot rid ourselves of loaded assemblies
        # This test will leak files on the filesystem
        $tempdll = "$([io.path]::GetTempFileName())-testParameterBindingForEtsType-$(Get-Random).dll"
        $cmdletDefinition = @"
            namespace MyNamespace
            {
                using System;
                using System.Management;
                using System.Management.Automation;

                [Cmdlet("Invoke", "TestCmdlet")]
                public class InvokeTestCmdletCommand : PSCmdlet
                {
                    [PSTypeName(@"System.Management.ManagementObject#root\cimv2\Win32_Process")]
                    [Parameter(ParameterSetName = "WmiProcess", Position = 0, ValueFromPipeline = true)]
                    public ManagementBaseObject WmiProcess { get; set; }

                    [PSTypeName(@"System.Management.ManagementObject#root\cimv2\Win32_Service")]
                    [Parameter(ParameterSetName = "WmiService", Position = 0, ValueFromPipeline = true)]
                    public ManagementBaseObject WmiService { get; set; }

                    protected override void ProcessRecord() {
                        if (this.ParameterSetName.Equals("WmiProcess")) {
                            this.WriteObject("WmiProcess: " + this.WmiProcess.Properties["Name"].Value.ToString());
                        }
                        else if (this.ParameterSetName.Equals("WmiService")) {
                            this.WriteObject("WmiService: " + this.WmiService.Properties["Name"].Value.ToString());
                        }
                        else {
                            this.WriteObject("No parameters have been bound");
                        }
                    }
                }
            }
"@
        # try to not continually recreate this type
        # if you need to make changes to the class in iterative test runs,
        # you will need to start a new PowerShell process
        $cmdletType = "MyNamespace.InvokeTestCmdletCommand" -as "type"
        if ( $cmdletType )
        {
            $tempdll = $cmdletType.Assembly.Location 
        }
        else
        {
            add-type -outputassembly $tempdll -referencedassemblies System.Management -typedefinition $cmdletDefinition
        }
        import-module $tempdll

        function scriptCmdlet {
            param(
                [Parameter(ValueFromPipeline = $true, Position = 0, ParameterSetName = "WmiProcess")]
                [System.Management.Automation.PSTypeName("System.Management.ManagementObject#root\cimv2\Win32_Process")]
                [wmi] $wmiProcess,

                [Parameter(ValueFromPipeline = $true, Position = 0, ParameterSetName = "WmiService")]
                [System.Management.Automation.PSTypeNameAttribute("System.Management.ManagementObject#root\cimv2\Win32_Service")]
                [wmi] $wmiService
            )
            if ($PSCmdlet.ParameterSetName -eq "WmiProcess") {
                "WmiProcess: $($wmiProcess.Name)"
            } elseif ($PSCmdlet.ParameterSetName -eq "WmiService") {
                "WmiService: $($wmiService.Name)"
            } else {
                "No parameters have been bound"
            }
        }

        function regularFunction {
            param( [PSTypeName("System.Management.ManagementObject#root\cimv2\Win32_Process")] [wmi] $wmiProcess)
            if ($wmiProcess) {
                "WmiProcess: $($wmiProcess.Name)"
            } else {
                "No parameters have been bound"
            }
        }

        $scriptCmdletInfo = Get-Command scriptCmdlet
        $commandMetadata = New-Object System.Management.Automation.CommandMetadata $scriptCmdletInfo
        $proxyBody = [System.Management.Automation.ProxyCommand]::Create($commandMetadata)
        $function:scriptCmdletProxy = $proxyBody

        $currentProcessName = $wmiProcess.Name
    }

	It -skip:$skip "scriptCmdlet wmiProcess succeeds" {
		$(scriptCmdlet $wmiProcess) | Should Be "WmiProcess: $currentProcessName"
	}
	It -skip:$skip "scriptCmdlet wmiProcess succeeds" {
		$(scriptCmdlet $wmiService) | Should Be "WmiService: Netlogon"
	}
	It -skip:$skip "wmiProcess | scriptCmdlet succeeds" {
		$($wmiProcess | scriptCmdlet) | Should Be "WmiProcess: $currentProcessName"
	}
	It -skip:$skip "wmiProcess | scriptCmdlet succeeds" {
		$($wmiService | scriptCmdlet) | Should Be "WmiService: Netlogon"
	}

    It -skip:$skip "explicit parameter binding errors correctly on bad type" {
        try {
            scriptCmdlet -wmiService $wmiProcess
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "MismatchedPSTypeName,scriptCmdlet"
        }
    }
    It -skip:$skip "positional parameter binding errors correctly on bad type" {
        try {
            scriptCmdlet $wmiOperatingSystem
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "MismatchedPSTypeName,scriptCmdlet"
        }
    }
    It -skip:$skip "pipeline parameter binding errors correctly on bad type" {
        try {
            $wmiOperatingSystem | scriptCmdlet -ea stop | out-null
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "AmbiguousParameterSet,scriptCmdlet"
        }
    }

	It -skip:$skip "scriptCmdletProxy wmiProcess succeeds" {
		$(scriptCmdletProxy $wmiProcess) | Should Be "WmiProcess: $currentProcessName"
	}
	It -skip:$skip "scriptCmdletProxy wmiProcess succeeds" {
		$(scriptCmdletProxy $wmiService) | Should Be "WmiService: Netlogon"
	}
	It -skip:$skip "wmiProcess | scriptCmdletProxy succeeds" {
		$($wmiProcess | scriptCmdletProxy) | Should Be "WmiProcess: $currentProcessName"
	}
	It -skip:$skip "wmiProcess | scriptCmdletProxy succeeds" {
		$($wmiService | scriptCmdletProxy) | Should Be "WmiService: Netlogon"
	}

    It -skip:$skip "explicit parameter binding errors correctly with proxy on bad type" {
        try {
            scriptCmdletProxy -wmiService $wmiProcess
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "MismatchedPSTypeName,scriptCmdletPRoxy"
        }
    }
    It -skip:$skip "positional parameter binding errors correctly with proxy on bad type" {
        try {
            scriptCmdletProxy $wmiOperatingSystem
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "MismatchedPSTypeName,scriptCmdletPRoxy"
        }
    }
    It -skip:$skip "pipeline parameter binding errors correctly with proxy on bad type" {
        try {
            $wmiOperatingSystem | scriptCmdletProxy -ea | out-null
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "MissingArgument,scriptCmdletPRoxy"
        }
    }

	It -skip:$skip "regularFunction wmiProcess succeeds" {
		$(regularFunction $wmiProcess) | Should Be "WmiProcess: $currentProcessName"
	}

    It -skip:$skip "pipeline parameter binding errors correctly with function on bad type" {
        try {
            regularFunction $wmiOperatingSystem
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "MismatchedPSTypeName,regularFunction"
        }
    }

	It -skip:$skip "Invoke-TestCmdlet wmiProcess succeeds" {
		$(Invoke-TestCmdlet $wmiProcess) | Should Be "WmiProcess: $currentProcessName"
	}
	It -skip:$skip "Invoke-TestCmdlet wmiProcess succeeds" {
		$(Invoke-TestCmdlet $wmiService) | Should Be "WmiService: Netlogon"
	}
	It -skip:$skip "wmiProcess | Invoke-TestCmdlet succeeds" {
		$($wmiProcess | Invoke-TestCmdlet) | Should Be "WmiProcess: $currentProcessName"
	}
	It -skip:$skip "wmiProcess | Invoke-TestCmdlet succeeds" {
		$($wmiService | Invoke-TestCmdlet) | Should Be "WmiService: Netlogon"
	}

    It -skip:$skip "explicit parameter binding errors correctly with compiled cmdlet on bad type" {
        try {
            Invoke-TestCmdlet -wmiService $wmiProcess
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "MismatchedPSTypeName,MyNamespace.InvokeTestCmdletCommand"
        }
    }
    It -skip:$skip "positional parameter binding errors correctly with compiled cmdlet on bad type" {
        try {
            Invoke-TestCmdlet $wmiOperatingSystem
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "MismatchedPSTypeName,MyNamespace.InvokeTestCmdletCommand"
        }
    }
    It -skip:$skip "pipeline parameter binding errors correctly with compiled cmdlet on bad type" {
        try {
            $wmiOperatingSystem | Invoke-TestCmdlet -ea Stop| out-null
            throw "Execution OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "AmbiguousParameterSet,MyNamespace.InvokeTestCmdletCommand"
        }
    }

}

# Test the public CmdletInfo constructor and using it with the PowerShell API
# Test error scenarios
Describe -tags 'Innerloop', 'DRT' "TestPowerShellAPIwithCmdletInfo" {
	It "Expected fully-qualified error id to be 'ArgumentNull'" {
        try 
        {
            # test name is null
            new-object system.management.automation.cmdletinfo $null,([int])
            Throw "OK"
        } catch {
            $_.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should Be "ArgumentNull" 
        }
    }

	It "Expected fully-qualified error id to be 'ArgumentNull'" {
        try
        {
            # test type is null
            new-object system.management.automation.cmdletinfo "foo-bar",$null
            Throw "OK"
        } catch
        {
            $_.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should Be "ArgumentNull" 
        }
    }

	It "Expected fully-qualified error id to be 'InvalidOperation'" {
        try
        {
            # Test type is invalid
            new-object system.management.automation.cmdletinfo "foo-bar",([int])
            Throw "OK"
        } catch
        {
            $_.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should Be "InvalidOperation" 
        }
    }

    Context "Create and validate a CmdletInfo using the implementing type for Get-Date" {
        BeforeAll {
            $o = @(new-object system.management.automation.cmdletinfo "foo-bar", ([Microsoft.PowerShell.Commands.GetDateCommand]))
        }
        It "Expected one CommandInfo object to be returned" {
            $o.Count | should be 1 
        }
        It "Expected cmdlet verb to be 'foo'" {
            $o[0].Verb | should be "foo"
        }
        It "Expected cmdlet noun to be 'bar'" {
            $o[0].Noun | should be "bar"
        }
        It "Expected cmdlet type to be 'Microsoft.PowerShell.Commands.GetDateCommand'" {
            ($o[0].ImplementingType) | Should Be ([Microsoft.PowerShell.Commands.GetDateCommand]) 
        }
        It "Should be possible to create a command" {
            $result = @([powershell]::create().AddCommand($o[0]).Invoke())
            $result.Count | should be 1
            $result[0].GetType() | should be ([datetime])
        }
    }

    Context "Execute a CmdletInfo Correctly" {
        BeforeAll {
            $gp = @( new-object system.management.automation.cmdletinfo "my-getprocess", ([Microsoft.PowerShell.Commands.GetProcessCommand]))[0]
            $result = @([powershell]::create().AddCommand($gp).AddParameter("id", $pid).Invoke())
        }
        It "Execution should return a single object" {
            $result.Count |should be 1
        }
        It "Execution should return the proper type" {
            $result[0].GetType() | Should be ([System.Diagnostics.Process])
        }
        It "Execution should return the correct object" {
            $result[0].ID | Should be $pid
        }
    }

}
# Verifies that you get an appropriate error message if you try to access the events
# queue after disposing the runspace.
Describe -tags 'Innerloop', 'DRT' "TestRunspaceCloseEvents" {
    BeforeAll {
        $skip = $false
        if ( -not (get-command add-type -ea silentlycontinue ) )
        {
            $skip = $true
            return
        }
        $member = 'public static string TestRunspace() {
            try {
                Runspace rs = RunspaceFactory.CreateRunspace();
                rs.Open();
                rs.Close();
                Object foo = rs.Events;
            }
            catch(Exception e) { return e.GetType().ToString(); }
            return "No Exception";
        }'

        $AddTypeArgs = @{
            Name = "RunspaceTester"
            Namespace = "TestRunspaceCrash"
            MemberDefinition = $member
            Using = "System.Management.Automation.Runspaces"
            PassThru = $true
            }
        $type = "TestRunspaceCrash.RunspaceTester" -as "type"
        if ( ! $type )
        {
            $type = Add-Type @AddTypeArgs
        }
    }
	It -skip:$skip "Should not throw a null reference exception." {
        $result = $type::TestRunspace() 
        $result | Should Be "No Exception" 
	}
}

# Verifies that error redirection to success output works corrrectly with
# the steppable pipeline.
Describe -tags 'Innerloop', 'DRT' "TestStepPipeErrorRedirect" {
    BeforeAll {
        $t = (Get-Command Write-Error).ImplementingType
        $m = New-Object System.Management.Automation.CommandMetadata($t)
        $p = [System.Management.Automation.ProxyCommand]::Create($m)
        ${function:Write-ProxiedError} = $p
        $x = Write-ProxiedError foo 2>&1
    }

	It "Steppable pipeline error should be redirected to success output." {
        $x | should not BeNullOrEmpty
	}
}

# Verifies that the steppable pipeline will collect output from
# all statements executed in the pipeline.
Describe -tags 'Innerloop', 'DRT' "TestStepPipelineOutput" {
	It "Should output at least four items." {
        ## Create filter with four statements
        filter a {$_*2; $_*4; 5*5; gps -id $pid}
        ## Create and run steppable pipeline
        $sp = {a}.GetSteppablePipeline()
        $sp.Begin($true)
        $out = $sp.Process("powershell")
        $sp.End()
		$out.Count |should be 4
	}
}

# Win8: 110913 Parameter binding fails for named arguments when positional binding works in functions and scripts.
Describe -tags 'Innerloop', 'DRT' "win8_110913" {
    BeforeAll {
        $stringListFromArray = [System.Collections.Generic.List[string]] @("abc", "edf")
        $stringListFromScalar = [System.Collections.Generic.List[string]] "abc"
    }
	It '$stringListFromArray should contains two items' {
		$stringListFromArray.Count | Should Be 2
	}
	It '$stringListFromArray[0] should be "abc"' {
		$stringListFromArray[0] | Should Be "abc"
	}
	It '$stringListFromArray[1] should be "edf"' {
		$stringListFromArray[1] | Should Be "edf"
	}

	It '$stringListFromScalar should contains one items' {
		$stringListFromScalar.Count | Should Be 1
	}
	It '$stringListFromScalar[0] should be "abc"' {
		$stringListFromScalar[0] | Should Be "abc"
	}

    try{
        [System.Collections.Generic.List[int]] @("abc", 123)
        throw "Could Convert 'abc' to an int"
    }
    catch
    {
        It "Items in array are not all ints, conversion should fail" {
            $_.FullyQualifiedErrorId | Should Be "ConvertToFinalInvalidCastException"
        }
    }
}

# Win8: 169492 RunspacePool can report that there are negative runspaces available.
Describe -tags 'Innerloop', 'P1' "win8_169492" {
	It "GetAvailableRunspaces should always return greater than or equal to 0" {
        $rs = [system.management.automation.runspaces.runspacefactory]::CreateRunspacePool(1, 3)
        $rs.Open();
        $rs.GetAvailableRunspaces()

        $script = '1..10 | foreach { start-sleep -sec 1 }'
        foreach($r in 0..2) {
            $psh = [powershell]::Create();
            $psh.RunspacePool = $rs;
            $null = $psh.AddScript($script).BeginInvoke();
        }

        $rs.SetMaxRunspaces(1)
        $availableRunspaces = $rs.GetAvailableRunspaces()
        $rs.dispose()

		($availableRunspaces -ge 0) | Should Be $true
	}
}

# Win8: 169518 When using cleanup intervals less than around 20seconds, RunspacePool
Describe -tags 'Innerloop', 'P1' "win8_169518" {
    BeforeAll {
        remove-item $env:Temp\engineexit.txt -ea SilentlyContinue
        #Create a runspace pool with min 1, max 3
        $rs = [system.management.automation.runspaces.runspacefactory]::CreateRunspacePool(1, 3)
        # Set the cleanup to 30 seconds
        $rs.CleanupInterval= new-timespan -sec 30

        $rs.Open()
        $async = @()
        $shells = @()
        $script = '$null = Register-EngineEvent -source PowerShell.Exiting -action { "Exiting" >> $env:Temp\engineexit.txt }; 1..10 | foreach { start-sleep -sec 1 }'
        foreach($r in 0..2) { 
            $psh = [powershell]::Create()
            $shells += $psh
            $psh.RunspacePool = $rs
            $null = $psh.AddScript($script)
            $async += $psh.BeginInvoke()
        }
    }
    AfterAll {
        $rs.Dispose()
    }
	It "Expected 0 avaliable runspaces" {
		$rs.GetAvailableRunspaces() | Should Be 0
	}

    # Wait until they all finish.
    #Now it should show 3 available runspaces since they all finished executing.
	It "Expected 3 avaliable runspaces since all the commands are completed now." {
        foreach($i in 0..2) { $shells[$i].EndInvoke($async[$i]) }
        start-sleep -sec 10
		$rs.GetAvailableRunspaces() | Should Be 3
	}

    # After 30 seconds, 2 of the runspaces should be cleaned up because they are not in use and we should see a file called engineexit.txt with messages saying that they exited.
	It "There should be 2 lines written to engineexit.txt" {
        start-sleep -sec 40
        $countExiting = (Get-content $env:temp\engineexit.txt | measure-object).Count
		$countExiting | Should Be 2
	}

}

# Win8: 178063 Flow-control-exceptions in scripts executed via PowerShell API, will affect the script that is calling into the PowerShell API
Describe -tags 'Innerloop', 'P1' "win8_178063" {
	It "There should one pipelinestopped exception" {
        $ps = [powershell]::create().addscript("start-sleep -sec 100")
        $ar = $ps.BeginInvoke()
        start-sleep -mil 2000
        try {
            $ps.Stop()
            $ps.EndInvoke($ar)
        }
        catch {
            $_.FullyQualifiedErrorId | Should be PipelineStoppedException
        }
	}
}

# 182409: PowerShell 3.0 should run in STA mode by default
Describe -tags 'Innerloop', 'DRT' "win8_182409" {
	It "Default apartment state of PowerShell should be STA" {
        $expected = [System.Threading.ApartmentState]::STA.tostring().trim() 
        $actual = powershell -noprofile -command '[system.threading.thread]::currentthread.ApartmentState.tostring().trim()'
        $actual | Should Be $expected
	}
}

# Win8: 228176 PSTypeName attribute doesn't prevent binding like a type declaration would.
Describe -tags 'Innerloop', 'DRT' "win8_228176" {
    BeforeAll {
        function foo_withPSTypeName
        {
            param(
                [PSTypeName("type1")] [Parameter(ValueFromPipeline=$true)] $x, 
                [PSTypeName("type2")] [Parameter(ValueFromPipeline=$true)] $y
            )
            process {
                $result = new-object psobject
                $result = $result | add-member noteproperty -name "x" -value $x -passthru
                $result = $result | add-member noteproperty -name "y" -value $y -passthru
                $result
            }
        }

        $x = new-object uri http://www.microsoft.com
        $x.pstypenames.insert(0, "type1")
        $op = $x | foo_withPSTypeName
    }

	It "PSTypeName based parameter binding did not work" {
        $op.x | Should be $x
	}
	It "PSTypeName based parameter binding should not bind parameter y" {
        $op.y | should BeNullOrEmpty
		# (!$op.y) | Should Be $true
	}
}

# Win8: 246310 Regression [win7]: powershell no longer sets  exitcode to 1  if the command throws a terminating error
Describe -tags 'Innerloop', 'DRT' "win8_246310" {
	It '$global:? should be false when there is a terminating error' {
        try {
            new-object blah 
        }
        catch { }
        finally {
            $global:? | Should Be $false
        }
	}

	It '$? should be false when there is a terminating error' {
        powershell -command "new-object blah" 2>$null
		$? | Should Be $false
	}
}

#       Win8: 277635, 569622 - Preserve additional parameter sets when it's necessary.
## (1) Scenario 1:
## Valid parameter sets when it comes to the mandatory checking: A, B
## Mandatory parameters in A, B:
## Set      Non-PipelineableMandatory-InSet         Pipelineable-Mandatory-InSet       Common-Non-PipelineableMandatory       Common-PipelineableMandatory
## A        N/A                                     N/A                                N/A                                    AllParam (of type DateTime)
## B        N/A                                     ParamB (of type TimeSpan)          N/A                                    AllParam (of type DateTime)
## Piped-in object:
## Get-DateTime

## Originally, the mandatory checking will resolve the parameter set to be B, which will fail in the pipeline binding later.
## After the change, the parameter set A in the scenario 1 and the set A, Default in the scenario 2 will be preserved, and the pipeline binding later will succeed.
Describe -tags 'Innerloop', 'DRT' "win8_277635_MandatoryCheckingFix" {
	It "Should bound to the parameter set 'computer'" {
        Function Test-Win8Bug277635
        {
            [CmdletBinding()]
            param(
            [Parameter(Mandatory=$true, ValueFromPipeline=$true)] [System.DateTime] $Date,
            [Parameter(ParameterSetName="computer")] [Parameter(ParameterSetName="session")] $ComputerName,
            [Parameter(ParameterSetName="session", Mandatory=$true, ValueFromPipeline=$true)] [System.TimeSpan] $TimeSpan
            )

            Process { Write-Output $PsCmdlet.ParameterSetName }
        }
        $result = Get-Date | Test-Win8Bug277635
        $result | Should be "computer"
	}

## (2) Scenario 2:
## Valid parameter sets when it comes to the mandatory checking: A, B, default
## Mandatory parameters in A, B, Default:
## Set      Non-PipelineableMandatory-InSet         Pipelineable-Mandatory-InSet       Common-Non-PipelineableMandatory       Common-PipelineableMandatory
## A        N/A                                     N/A                                N/A                                    AllParam (of type DateTime)
## B        N/A                                     ParamB (of type TimeSpan)          N/A                                    AllParam (of type DateTime)
## C        N/A                                     N/A                                N/A                                    AllParam (of type DateTime)
## Default  N/A                                     N/A                                N/A                                    AllParam (of type DateTime)

## Originally, the mandatory checking will resolve the parameter set to be B, which will fail in the pipeline binding later.
## After the change, the parameter set A in the scenario 1 and the set A, Default in the scenario 2 will be preserved, and the pipeline binding later will succeed.
	It "Should bound to the parameter set 'computer'" {
        Function Test-Win8Bug277635Again
        {
            [CmdletBinding(DefaultParameterSetName="computer")]
            param(
            [Parameter(ParameterSetName="new")] $NewName,
            [Parameter(Mandatory=$true, ValueFromPipeline=$true)] [System.DateTime] $Date,
            [Parameter(ParameterSetName="computer")] [Parameter(ParameterSetName="session")] $ComputerName,
            [Parameter(ParameterSetName="session", Mandatory=$true, ValueFromPipeline=$true)] [System.TimeSpan] $TimeSpan
            )

            Process { Write-Output $PsCmdlet.ParameterSetName }
        }
        $result = Get-Date | Test-Win8Bug277635Again
        $result | Should be "computer"
	}


    # Since we try to latch on the "session" set, we should prioritize the binding for it, 
    # which will succeed with type coerce
	It "Should bound to the parameter set 'session'" {
        Function Test-Win8Bug277635-NoBreakingChange
        {
            [CmdletBinding()]
            param(
            [Parameter(Mandatory=$true, ValueFromPipeline=$true)] [System.DateTime] $Date,
            [Parameter(ParameterSetName="computer")] [Parameter(ParameterSetName="session")] $ComputerName,
            [Parameter(ParameterSetName="session", Mandatory=$true, ValueFromPipeline=$true)] [string] $TimeSpan,
            [Parameter(ParameterSetName="network", ValueFromPipeline=$true)] [System.DateTime] $NetAddress
            )
            Process { Write-Output $PsCmdlet.ParameterSetName }
        }
        $result = Get-Date | Test-Win8Bug277635-NoBreakingChange
		$result | Should be "session"
	}


    ## We prioritize the binding for the set "session" but it will fail. The set "network" should be bound successfully.
	It "Should bound to the parameter set 'network'" {
        Function Test-Win8Bug277635-BoundToNetwork
        {
            [CmdletBinding()]
            param(
                [Parameter(Mandatory=$true, ValueFromPipeline=$true)] [System.DateTime] $Date,
                [Parameter(ParameterSetName="computer")] [Parameter(ParameterSetName="session")] $ComputerName,
                [Parameter(ParameterSetName="session", Mandatory=$true, ValueFromPipeline=$true)] [System.TimeSpan] $TimeSpan,
                [Parameter(ParameterSetName="network", ValueFromPipeline=$true)] [System.DateTime] $NetAddress
            )
            Process { Write-Output $PsCmdlet.ParameterSetName }
        }

        $result = Get-Date | Test-Win8Bug277635-BoundToNetwork
		$result | Should be "network"
	}


    ## We prioritize the binding for "network" and it will succeed with type coerce
	It "Should bound to the parameter set 'network'" {
        Function Test-Win8Bug569622-NoBreakingChange
        {
            [CmdletBinding(DefaultParameterSetName="server")]
            param(
            [Parameter(ParameterSetName="network", Mandatory=$true, ValueFromPipeline=$true)] [string] $network,
            [Parameter(ParameterSetName="computer", ValueFromPipelineByPropertyName=$true)] [Parameter(ParameterSetName="session", ValueFromPipelineByPropertyName=$true)] [Parameter(ParameterSetName="server", ValueFromPipelineByPropertyName=$true)] [string[]] $ComputerName,
            [Parameter(ParameterSetName="computer", Mandatory=$true)] [switch] $DisableComputer,
            [Parameter(Mandatory=$true, ValueFromPipeline=$true)] [DateTime] $Date
            )

            Process { Write-Output $PsCmdlet.ParameterSetName }
        }

        $result = Get-Date | Test-Win8Bug569622-NoBreakingChange
		$result | Should be "network"
	}


    ## We prioritize the binding for "network", but it will fail. The default "server" set will be chosen.
	It "Should bound to the parameter set 'server'" {
        Function Test-Win8Bug569622-BoundToServer
        {
            [CmdletBinding(DefaultParameterSetName="server")]
            param(
            [Parameter(ParameterSetName="network", Mandatory=$true, ValueFromPipeline=$true)] [TimeSpan] $network,
            [Parameter(ParameterSetName="computer", ValueFromPipelineByPropertyName=$true)] [Parameter(ParameterSetName="session", ValueFromPipelineByPropertyName=$true)] [Parameter(ParameterSetName="server", ValueFromPipelineByPropertyName=$true)] [string[]] $ComputerName,
            [Parameter(ParameterSetName="computer", Mandatory=$true)] [switch] $DisableComputer,
            [Parameter(Mandatory=$true, ValueFromPipeline=$true)] [DateTime] $Date
            )

            Process { Write-Output $PsCmdlet.ParameterSetName }
        }

        $result = Get-Date | Test-Win8Bug569622-BoundToServer
		$result | Should Be "server"
	}


    ## We prioritize the binding for "network", but it will fail. The "session" will be bound.
	It "Should bound to the parameter set 'session'" {
        Function Test-Win8Bug569622-BoundToSession
        {
            [CmdletBinding()]
            param(
            [Parameter(ParameterSetName="network", Mandatory=$true, ValueFromPipeline=$true)] [TimeSpan] $network,
            [Parameter(ParameterSetName="computer", ValueFromPipelineByPropertyName=$true)] [Parameter(ParameterSetName="session", ValueFromPipelineByPropertyName=$true)] [string[]] $ComputerName,
            [Parameter(ParameterSetName="computer", Mandatory=$true)] [switch] $DisableComputer, 
            [Parameter(ParameterSetName="session", ValueFromPipeline=$true)] [DateTime] $Date
            )

            Process { Write-Output $PsCmdlet.ParameterSetName }
        }

        $result = Get-Date | Test-Win8Bug569622-BoundToSession
		$result | Should Be "session"
	}


    ## We prioritize the binding for "network", but it will fail. The "session" will be resolved as the working parameter set as it contains no mandatory parameters.
	It "Should bound to the parameter set 'session'" {
        Function Test-Win8Bug569622-BoundToSession-Again
        {
            [CmdletBinding()]
            param(
            [Parameter(ParameterSetName="network", Mandatory=$true, ValueFromPipeline=$true)] [TimeSpan] $network, 
            [Parameter(ParameterSetName="computer", ValueFromPipelineByPropertyName=$true)] [Parameter(ParameterSetName="session", ValueFromPipelineByPropertyName=$true)] [string[]] $ComputerName, 
            [Parameter(ParameterSetName="computer", Mandatory=$true)] [switch] $DisableComputer, 
            [Parameter(ValueFromPipeline=$true)] [DateTime] $Date
            )

            Process { Write-Output $PsCmdlet.ParameterSetName }
        }

        $result = Get-Date | Test-Win8Bug569622-BoundToSession-Again
		$result | Should Be "session"
	}


    ## We prioritize the binding for "network", but it will fail. The default set "server" will be chosen.
	It "Should bound to the parameter set 'server'" {
        Function Test-Win8Bug569622-BoundToServer-Again
        {
            [CmdletBinding(DefaultParameterSetName="server")]
            param(
            [Parameter(ParameterSetName="network", Mandatory=$true, ValueFromPipeline=$true)] [TimeSpan] $network, 
            [Parameter(ParameterSetName="computer", ValueFromPipelineByPropertyName=$true)] [Parameter(ParameterSetName="session", ValueFromPipelineByPropertyName=$true)] [Parameter(ParameterSetName="server", ValueFromPipelineByPropertyName=$true)] [string[]] $ComputerName,
            [Parameter(ParameterSetName="computer", Mandatory=$true)] [switch] $DisableComputer, 
            [Parameter(ValueFromPipeline=$true)] [DateTime] $Date
            )

            Process { Write-Output $PsCmdlet.ParameterSetName }
        }

        $result = Get-Date | Test-Win8Bug569622-BoundToServer-Again
		$result | Should Be "server"
	}
}

# Win8: 329209 - Module auto-loading eats first error when run through the pipeline APIs
Describe -tags 'Innerloop', 'DRT' "win8_329209" {
	It "There should be 2 error messages written out" {
        $ps = [PowerShell]::Create()
        $ps.AddScript('$a = 1,2; $a | powershell.exe -noprofile -command {$input|foreach {write-error foo}}').Invoke()
		$ps.Streams.Error.Count | Should Be 2
	}
}

# Win8: 393501 - FunctionInfo should have VERB and NOUN properties
# WinBlue: 3243 - The Noun and Verb properties of a FunctionInfo object returned by Get-Command are null when a function implements dynamic parameters
Describe -tags 'Innerloop', 'DRT' "win8_392501_FunctionInfo_Verb_Noun" {
	It "FunctionInfo should have VERB and NOUN properties" {
        function Get-Win8_393501 { "In Get-Win8_393501" }
        $func = Get-Command -Name Get-Win8_393501
        $func.Verb | Should be "Get"
        $func.Noun | Should be "Win8_393501"
	}

    ## 3243 - The Noun and Verb properties of a FunctionInfo object returned by Get-Command are null when a function implements dynamic parameters
## When a function contains dynamic parameter, a copy of the FunctionInfo instance in the FunctionTable will be returned so that the dynamic parameters 
## are merged with the static parameters in the copy instance instead of the original instance. The bug happens because the copy constructor of the FunctionInfo 
## doesn't copy the noun and verb fields in addition to copying all of the other fields.
	It "FunctionInfo with dynamic parameters should have VERB and NOUN properties set" {
        function Get-WinBlue_3243 {
            [CmdletBinding()]
            Param ( [Parameter(Mandatory=$True)] [ValidateSet("SMTPAuth", "SMTP", "Outlook", "Service")] [string]$Send)

            DynamicParam {
                if ($Send -eq "SMTP") {
                    $attr = new-object System.Management.Automation.ParameterAttribute
                    $attr.Mandatory = $true
                    $validateNNE = new-object System.Management.Automation.ValidateNotNullOrEmptyAttribute

                    $attributes = new-object -Type System.Collections.ObjectModel.Collection``1[System.Attribute]
                    [void]$attributes.Add($attr)
                    [void]$attributes.Add($validateNNE)

                    $param = new-object System.Management.Automation.RuntimeDefinedParameter("ConfigFile", [String], $attributes)
                    $dic = new-object System.Management.Automation.RuntimeDefinedParameterDictionary
                    [void]$dic.Add("ConfigFile", $param)
                    return $dic
                }
            }
            process
            {
                Write-Host "Send: $Send" -Fore Green
                Write-Host "ConfigFile: $($PSBoundParameters['ConfigFile'])" -Fore Green
            }
        }

        $func = Get-Command Get-WinBlue_3243
        $func.Verb | Should be Get
        $func.Noun | Should be WinBlue_3243
	}
}

# Win8: 399659 Default $MaximumHistoryCount too low
Describe -tags 'Innerloop', 'P1' "win8_399659" {
	It '$MaximumHistoryCount should be 4096.' {
		(powershell.exe -noprofile -command { $MaximumHistoryCount }) | Should Be 4096
	}

}

# Win8: 456634 Parameter binder should not call the setter of a parameter multiple times
Describe -tags 'Innerloop', 'P1' "win8_456634_BinderCallSetterTwice" {
    BeforeAll {
        $skip = $false
        if ( -not (get-command add-type -ea silentlycontinue ) )
        {
            $skip = $true
            return
        }

        $filepath = "${TestDrive}\win8_456634.dll"
        $classText = @'
            using System;
            using System.Management.Automation;
            namespace Win8Bug456634 {
                [Cmdlet(VerbsCommon.Set, "Win8Bug456634", DefaultParameterSetName = DefaultSet)]
                public class ParameterBinderInvokeSetterTest : PSCmdlet {
                    private const string DefaultSet = "default";
                    private const string ComputerSet = "computer";
                    private const string SessionSet = "session";
                    [Parameter(Position = 0, Mandatory = true, ParameterSetName = DefaultSet)]
                    public string DefaultProperty { get; set; }
                    private int _count;
                    private DateTime _cimInstance;
                    [Parameter(Position = 0, ParameterSetName = ComputerSet)]
                    [Parameter(Position = 0, ParameterSetName = SessionSet)]
                    public DateTime CimInstance
                    {
                        get { return _cimInstance; }
                        set { _count++; _cimInstance = value; }
                    }
                    [Parameter(Position = 1, ParameterSetName = ComputerSet)]
                    public string Computer { get; set; }
                    protected override void ProcessRecord()
                    {
                        WriteObject(_count);
                    }
                }
            }
'@
        $myType = "Win8Bug456634.ParameterBinderInvokeSetterTest" -as "Type"
        if ( ! $myType ) {
            Add-Type -TypeDefinition $classText -OutputAssembly $filePath -ErrorAction SilentlyContinue
        }
        else {
            $filePath = $myType.Assembly.Location 
        }
    }

    Context "Isolate test in new process for cleanup" {
        BeforeAll {
            $result = powershell -noprofile -c "import-module -force ${filePath};Set-Win8Bug456634 ([datetime]::Now) 'Computer'"
        }

        It 'Parameter binder should call the setter the right number of times' {
            $result | should be 1
        }
    }
}

# Win8: 504444 Recent fix to TabCompletion Infrastructure causes crash on ISE and 
# might cause crash on other scenarios
## The CSharp repro code. Compile it to be a console application and run the executable.
Describe -tags 'Innerloop', 'P1' "win8_504444_WaitAllOnSTAThread" {
        BeforeAll {
            $skip = $false
            if ( -not (get-command add-type -ea silentlycontinue ) )
            {
                $skip = $true
                return
            }
            $csharp = @'
            using System;
            using System.Threading;
            using System.Management.Automation;
            using System.Management.Automation.Runspaces;

            namespace ISECrashRepro {
                class Program {
                    [STAThread]
                    static void Main(string[] args) {
                        PowerShell powershell = PowerShell.Create();
                        Runspace runspace = RunspaceFactory.CreateRunspace();
                        runspace.Open();
                        powershell.Runspace = runspace;

                        // We register a script block action for the OnIdle event, and the script block 
                        // itself creates a nested pipeline.
                        // So there will be two running pipelines: pulse pipeline, nested pipeline created 
                        // by the pulse pipeline
                        ScriptBlock scriptblock = ScriptBlock.Create("$ps = [PowerShell]::Create([System.Management.Automation.RunspaceMode]::CurrentRunspace); $ps.AddScript('sleep -second 10'); $ps.Invoke()");
                        powershell.AddCommand("Register-EngineEvent")
                        .AddParameter("SourceIdentifier", PSEngineEvent.OnIdle)
                        .AddParameter("Action", scriptblock)
                        .AddParameter("MaxTriggerCount", 1);
                        powershell.Invoke();

                        Thread.Sleep(4000);
                        // The current thread is a STA thread, if we are still using WaitAll for multiple 
                        // pipelines on a STA thread, this call will throw an exception
                        runspace.SessionStateProxy.PSVariable.Set("variable1", "variable");
                        Console.WriteLine("Execution OK");
                        }
                    }
            }
'@

            $executableFile = "TestDrive:\win8_504444_WaitAllOnSTAThread.exe"
            Add-Type -TypeDefinition $csharp -OutputType ConsoleApplication -OutputAssembly $executableFile

            ## If we are still using WaitAll for multiple pipelines on a STA thread, this call will 
            # throw a NotSupportedException "WaitAll for multiple handles on a STA thread is not supported."
            $result = & $executableFile 2>&1
        }

        AfterAll {
            if ( test-path $executableFile )
            {
                Remove-Item $executableFile -ea silentlycontinue -force
            }
        }

        It -skip:$skip "Should not cause tabcompletion to fail" {
            [string]$result | Should Be "Execution OK"
        }


}

# Win8: 619951 [Exchange PS3 Integration] "set-ThrottlingPolicy def* -PowerShellMaxDestructiveCmdlets $null" is failed in PS3 which works well in PS2
# ScriptBlockToPowerShellWithNullParameterValue
Describe -tags 'Innerloop', 'DRT' "win8_619951_ScriptBlockToPowerShellWithNullParameterValue" {
    BeforeAll {
        $s = New-PSSession
    }
    AfterAll {
        Remove-PSSession $s
    }
	It "should output the result_1" {
        Invoke-Command -Session $s -ScriptBlock { function bar { param($parameter) "Parameter is $parameter" } }
        $params = @{InputObject = $null}
        $result = Invoke-Command -Session $s -ArgumentList $params -ScriptBlock { param($cc) bar @cc }
		"Parameter is " | Should Be $result
	}
}

# Win8:789598 - Importing certain modules breaks the help system
# The method GetMergedCommandParameterMetdata should ignore the ParameterBindingException when the Arguments list is empty
# The function Test-Bug789598 has no default parameter set so running the command "Test-Bug789598"
# will cause a parameter binding exception about "cannot resolve parameter set". The GetMergedCommandParameterMetdata
# method should ignore this parameter binding exception when the Arguments is empty.
Describe -tags 'Innerloop', 'DRT' "win8_789598_IgnoreParameterBindingExceptionWhenArgumentsIsEmpty" {
	It "The output of get-help should not be null." {
        Function Test-Bug789598
        {
            [CmdletBinding()]
            param(
            [Parameter(ParameterSetName = "ParamSet1")][string]$parameter1,
            [Parameter(ParameterSetName = "ParamSet2")][string]$parameter2,
            [Parameter(ParameterSetName = "ParamSet3")][string]$parameter3
            )
            Process { Write-Output $PSCmdlet.ParameterSetName }

            DynamicParam {
                $paramDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
                return $paramDic
            }
        }

        $helpOutput = Get-Help Test-Bug789598
        $helpOutput | Should Not BeNullOrEmpty
	}
}

# Win8 965086 - PowerShell Storage Module - cmdlets have no verbs/nouns via Get-Command
Describe -tags 'Innerloop', 'DRT' "win8_965086" {
    BeforeAll {
        # Create an advanced function with dynamic parameters.
        function Have-DynamicParameter {
            [CmdletBinding()]
            Param (
                [Parameter(Mandatory=$True)]
                [ValidateSet("SMTPAuth", "SMTP", "Outlook", "Service")]
                [string]$Send
            )
            DynamicParam
            {
                if ($Send -eq "SMTP")
                {
                $attr = new-object System.Management.Automation.ParameterAttribute
                $attr.Mandatory = $true
                $validateNNE = new-object System.Management.Automation.ValidateNotNullOrEmptyAttribute

                $attributes = new-object -Type System.Collections.ObjectModel.Collection``1[System.Attribute]
                [void]$attributes.Add($attr)
                [void]$attributes.Add($validateNNE)

                $param = new-object System.Management.Automation.RuntimeDefinedParameter("ConfigFile", [String], $attributes)

                $dic = new-object System.Management.Automation.RuntimeDefinedParameterDictionary
                [void]$dic.Add("ConfigFile", $param)

                return $dic
                }
            }
            process
            {
                Write-Host "Send: $Send" -Fore Green
                Write-Host "ConfigFile: $($PSBoundParameters['ConfigFile'])" -Fore Green
            }
        }

        $cmd = Get-Command Have-DynamicParameter
        $verb = $cmd.Verb
        $noun = $cmd.Noun
    }
	It "The Verb returned by get-command for the advanced function with dynamic parameters is correct" {
		$verb | Should Be "Have"
	}
	It "The Noun returned by get-command for the advanced function with dynamic parameters is correct" {
		$noun | Should Be "DynamicParameter"
	}

}

# WinBlue: 17593 - Passing a variable containing an object that derives from PSObject to Get-Member causes a hang.
# The problem is because of TypeRestrictions on the Enumerable Binder. The type restrictions are wrongly checking
# for "PSObject'.
# The test mimicks calling get-member on an object derived from PSObject and checks the call succeed (without hang)
Describe -tags 'Innerloop', 'P1' "WinBlue_17593" {
    BeforeAll {
        $skip = $false
        if ( -not (get-command add-type -ea silentlycontinue))
        {
            $skip = $true
            return
        }
        $Definition = '
            using System.Management.Automation;
            public class MyDerivedPsObject : PSObject {
                public MyDerivedPsObject() { }
            }'
        $type = "MyDerivedPsObject" -as "type"
        if ( ! ( $type )) 
        {
            add-type -warningaction silentlycontinue $Definition
        }
    }
    It -skip:$skip "Get-Member should not hang on a object derived from PSObject" {
        $x = new-object MyDerivedPsObject
        # This should not hang.
        $members = $x | Get-Member
        $members | should not BeNullOrEmpty
    }

}

# WinBlue: 19691 - Specifying credential type as advance function parameter produces unexpected result
# For a script function/cmdlet, we need to bind the default parameter values, and if a parameter has "ArgumentTransformationAttribute" defined for itself,
# then the transformation logic will be applied during the binding. For this specific bug, the default value for the -Credential parameter is null,
# so the transformation logic of the CredentialAttribute prompts to ask for credential input. Therefore, even though the -Credential parameter is not a
# mandatory parameter, it behaves like one.
# The parameter "Credential" has an attribute "CredentialAttribute", when binding the default parameter values for
# script function/cmdlet, if the default value is a powershell default value and it's $null, then we skip the ArgumentTranformationAttribute processing
Describe -tags 'Innerloop', 'DRT' "WinBlue_19691_ArgumentTransformationAttributeForDefaultParameterValueBinding" {
	It "The CredentialAttribute is skipped, so Test-Credential should return true" {
        function Test-Credential {
            [cmdletbinding()]
            param( [System.Management.Automation.CredentialAttribute()] $Credential )
            $Credential -eq $null
        }
        $result = Test-Credential
		$result | Should Be $true
	}
}

# WinBlue: 2032 - Cmdlet that throws CmdletProviderInvocationException causes NullRef in ParserUtils.UpdateExceptionErrorRecordPosition
# WinBlue: 84485 - Regression: Creating a PSSnapinException and getting its ErrorRecord property crashes PowerShell.
Describe -tags 'Innerloop', 'DRT' "WinBlue_2032" {
    BeforeAll {
        $CmdletProviderInvocationException = New-Object System.Management.Automation.CmdletProviderInvocationException "CmdletInvocationException"
        $PsSnapinException = New-Object System.Management.Automation.Runspaces.PSSnapInException "PSSnapinException"
        $ProviderInvocationException = New-Object System.Management.Automation.ProviderInvocationException "ProviderInvocationException"

        Function Test-WinBlue2032-1 {
            Process { throw $CmdletProviderInvocationException }
        }
        Function Test-WinBlue2032-2 {
            Process { throw $PsSnapinException }
        }
        Function Test-WinBlue2032-3 {
            Process { throw $ProviderInvocationException }
        }
    }

    It "The FullyQualifiedErrorId should be 'CmdletInvocationException'" {
        try {
            Test-WinBlue2032-1
            Throw "OK"
        } catch {
            $_.FullyQualifiedErrorId |should be "CmdletInvocationException"
        }
    }

	It "The FullyQualifiedErrorId should be 'PSSnapInException'" {
        try {
            Test-WinBlue2032-2
            Throw "OK"
        } catch {
            $_.FullyQualifiedErrorId | Should be "PSSnapInException"
        }
    }

    It "The FullyQualifiedErrorId should be 'ProviderInvocationException'" {
        try {
            Test-WinBlue2032-3
            Throw "OK"
        } catch {
            $_.FullyQualifiedErrorId | SHould be "ProviderInvocationException"
        }
    }

    ## Test for WinBlue:84485
	It "PSSnapinException should have an error record with it" {
		$PsSnapinException.ErrorRecord |Should Not BeNullOrEmpty
	}
	It "PSSnapinException should have message" {
		$PsSnapinException.Message |Should Not BeNullOrEmpty
	}


}

#       WinBlue:209579 - powershell.exe -file should set the errorlevel variable if the .ps1 file cannot be executed due to execution policy
#       We check AuthorizationManager.ShouldRun() for the script file before actually running it. So if the execution policy prevents it from running, we skip the file and set the ExitCode to be 1.
Describe -tags 'Innerloop', 'P1' "WinBlue_209579" {
    BeforeAll {
        # we have to use ${TESTDRIVE} because TESTDRIVE: isn't available in the new process
        $baseTestFile = "${TESTDRIVE}\Base.ps1"
        $helpTestFile = "${TESTDRIVE}\Help.ps1"
        
        Set-Content -Path $helpTestFile -Value "'Hello'" -Force
        "Set-ExecutionPolicy AllSigned -Scope Process -Force;",
        '$ErrorActionPreference = "Stop"', $helpTestFile,
        "exit 120" | Set-Content -Path $baseTestFile -Force 
    }

    ## Setup the test file content
	It "The ExitCode should be set to be 1 when the execution policy prevents the script file to run" {
        powershell.exe -noprofile -executionpolicy AllSigned -file $baseTestFile 2>&1 | Out-Null
		$LASTEXITCODE | Should Be 1
	}

	It "The ExitCode should be 1, since the PSSecurityException happens during the execution of the -file script" {
        powershell.exe -noprofile -executionpolicy Unrestricted -file $baseTestFile 2>&1 | Out-Null
		$LASTEXITCODE | Should Be 1
	}
}

# WinBlue: 39058 - Use of remoting proxies causes a hang in PowerShell V3
# Pretty much any use of a remoting proxy will cause a hang in PowerShell V3.
# The test mimicks calling a proxy and ensures the call succeed (without hang)
Describe -tags 'Innerloop', 'P1' "WinBlue_39058" {
    BeforeAll {
        $skip = $false
        if ( -not (get-command add-type -ea silentlycontinue ) )
        {
            $skip = $true
            return
        }
        $Definition = @"
            using System;
            using System.Runtime.Remoting;
            using System.Runtime.Remoting.Proxies;
            using System.Runtime.Remoting.Messaging;
            namespace ProxyTest {
                public class MyProxy<T> : RealProxy where T : class {
                    public MyProxy() : base(typeof(T)) {}
                    public override IMessage Invoke(IMessage imsg) {
                        if (imsg is IMethodCallMessage) {
                            IMethodCallMessage call = imsg as IMethodCallMessage;
                            if (call.MethodName.Equals("GetType")) {
                                return new ReturnMessage(typeof(T), null, 0, null, call);
                            }
                            else if (call.MethodName.Equals("HelloWorld")) {
                                // Console.WriteLine(call.Args[0]); 
                                return new ReturnMessage("It Works", null, 0, null, call);
                            }
                        }
                        return null;
                    }
                }
                public interface IMyService {
                    object HelloWorld(string message);
                }
                public class Test {
                    public static IMyService GetService() {
                        MyProxy<IMyService> proxy = new MyProxy<IMyService>();
                        return (IMyService)proxy.GetTransparentProxy();
                    }
                }
            }
"@

        $type = 'ProxyTest.MyProxy``1' -as "type"
        if ( ! $type )
        {
            $type = add-type -ReferencedAssemblies Microsoft.CSharp $Definition
        }

    }

	It -skip:$skip "Remote proxy method invocation failed." {
        $svc = [ProxyTest.Test]::GetService()
        $result = $svc.HelloWorld("it works")  
		$result | Should Be "It Works"
	}

}

# WinBlue:397730 - Powershell ISE hangs occasionally during intellisense autocomplete
# This bug actually contain two issues --
#   1. Tab completion doesn't deal with member completion in ISE well enough.
#     Take the following script as an example,
#       $xml = New-Object Xml; $xml.$xml.Save("C:\data.xml")
#     If auto completion is triggered when the cursor is right after the first '$xml.', '$xml.$xml' would be used as the target
#     for member completion incorrectly.
#
#   2. Dynamic member access may hang.
#     PowerShell will hang with the script
#       $xml = New-Object Xml;
#       $xml.$xml
#     This is because we didn't call PSObject.Base() on the member expression in the generated binding restriction. This causes
#     the string comparison in the binding restriction always fail, and thus the execution endlessly falls back to the Bind method
#     of PSGetDynamicMemberBinder.
Describe -tags 'Innerloop', 'DRT' "WinBlue_397730_DynamicMemberAccess" {
        It 'The expression $xml.$xml should return $null' {
            $xml = New-Object Xml
            $result =  $xml.$xml
            $result | Should Be $null
        }

        It "The first completion result should be 'Attributes'" {
            if ( test-path variable:xml ) { remove-item variable:xml -force -ea silentlycontinue }
            # under some circumstances, $xml may already exist, get rid of it
            $inputScript = '$xml=New-Object Xml;$xml.$xml.Save("C:\data.xml")'
            $result = TabExpansion2 -inputScript $inputScript -cursorColumn 25
            $result.CompletionMatches[0].CompletionText | Should Be "Attributes"
        }

        ## Case of $null
        It "Result should be a collection - the 'null' case with where" {
            $result = $null.Where{"I didn't run"}
            $result.GetType().FullName | Should Match 'System.Collections.ObjectModel.Collection`1\[\[System.Management.Automation.PSObject'
            $result.Count | Should Be 0
        }

        It "Result should be a collection - the 'null' case with where" {
            $result = $null.ForEach{"I didn't run"}
            $result.GetType().FullName | Should Match 'System.Collections.ObjectModel.Collection`1\[\[System.Management.Automation.PSObject'
            $result.Count | Should Be 0
        }

        It "Wrong method signature for 'Where' should cause 'MethodCountCouldNotFindBest' error - the 'null' case" {
            try { 
                $null.Where() 
                Throw "OK"
            } catch { 
                $_.FullyQualifiedErrorId | Should Be "MethodCountCouldNotFindBest"
            }
        }

        It "Wrong method signature for 'ForEach' should cause 'MethodCountCouldNotFindBest' error - the 'null' case" {
            try
            {
                $null.ForEach()
                Throw "OK"
            } catch {
                $_.FullyQualifiedErrorId | Should Be "MethodCountCouldNotFindBest"
            }
        }

    ## TEST 4 -- Where/ForEach operators on AutomationNull
    Context "Where/Foreach operators on AutomationNull" {
        BeforeAll {
            function noreturn {}
            $namePattern = 'System.Collections.ObjectModel.Collection`1\[\[System.Management.Automation.PSObject'
            $methodError = "MethodCountCouldNotFindBest"
        }

        It "Result should be a collection - the 'AutomationNull' case" {
            $result = (noreturn).Where{"I didn't run"}
            $result.GetType().FullName | Should Match $namePattern
            $result.Count | Should be 0
        }

        It "Result should be a collection - the 'AutomationNull' case" {
            $result = (noreturn).ForEach{"I didn't run"}
            $result.GetType().FullName | Should Match $namePattern
            $result.Count | Should be 0
        }

        It "Wrong method signature for 'Where' should cause 'MethodCountCouldNotFindBest' error - the 'AutomationNull' case" {
            try {
                (noreturn).Where()
                Throw "OK"
            } catch {
                $_.FullyQualifiedErrorId | Should Be $methodError
            }
        }

        It "Wrong method signature for 'ForEach' should cause 'MethodCountCouldNotFindBest' error - the 'AutomationNull' case" {
            try {
                (noreturn).ForEach()
                Trhow "OK"
            } catch { 
                $_.FullyQualifiedErrorId | Should Be $methodError
            }
        }
    }
}

#       WinBlue:445735 - On local variable type resolution failure, type constraint is ignored
#       For a local variable, if its type constraint cannot be resolved, powershell will just use the default type 'Object'. But the local variable
#       analysis happens at compile time, and it's possible the type will be loaded at runtime, and then it becomes resolvable. The fix is to make the
#       variable a PSVariable and force the dynamic lookup if the type constraint of the variable cannot be resolved.
Describe -tags 'Innerloop', 'DRT' "WinBlue_445735_UnresolvableTypeConstraintForLocalVariables" {
    BeforeAll {
        function WinBlue445735_ParamTest
        {
            param( [YYY] $p1) 
            "Throw"
        }
        function WinBlue445735_LocalVariableTest
        {
            [XXX] $v1 = Get-ChildItem | select -First 1
            $v1.FullName
        }
    }


    It "The type constraint for parameter p1 should be unresolvable" {
        try {
            WinBlue445735_ParamTest
            throw "Execution OK"
        }
        catch {
            $_.FullyQualifiedErrorId | Should Be "TypeNotFound"
            $_.Exception.Message |Should Match "\[YYY\]"
        }
    }

    It "The type constraint for local variable v1 should be unresolvable" {
        try
        {
            WinBlue445735_LocalVariableTest
        } catch {
            $_.FullyQualifiedErrorId | Should Be "TypeNotFound"
            $_.Exception.Message |should match "\[XXX\]"
        }
    }

    # TEST #2 - If unresolvable type at compile time becomes resolvable at runtime,
    #           then the type constraint should be honored.
    Context "unresolvable type at compile time becomes resolvable at runtime" {
        BeforeAll {
            $skip = $false
            if ($env:PROCESSOR_ARCHITECTURE -eq "ARM")
            {
                $skip = $true
            }
            else 
            {
                function WinBlue445735_NewType {
                    $text = '
                    namespace WinBlue445735 {
                    using System;
                    public class WinBlue445735Type {
                        public WinBlue445735Type(int number) { _number = number; }
                        public int Number { get { return _number; } }
                        private int _number;
                        }
                    }'
                if ( -not ("WinBlue445735.WinBlue445735Type" -as "Type"))
                {
                    Add-Type -TypeDefinition $text
                }
                [WinBlue445735.WinBlue445735Type] $v1 = 1;
                $v1.GetType().FullName
            }
            }
        }
        It -skip:$skip "The type name should be 'WinBlue445735.WinBlue445735Type'" {
            $result = WinBlue445735_NewType
            $result | Should Be "WinBlue445735.WinBlue445735Type"
        }
    }
}

# WinBlue:540698 - Pipeline.Invoke() doesn't collect output of native commands when it's running a script with error redirection.
# This bug happens because when using the Pipeline API, if error redirection happens in the outer scope of a native command, the native
# command will be incorrectly treated as running standalone.
Describe -tags 'Innerloop', 'P1' "WinBlue_540698_NativeCommandRedirection" {
    BeforeAll {
        $rs = [runspacefactory]::CreateRunspace([initialsessionstate]::CreateDefault2())
        $rs.Open()
        $pipeline = $rs.CreatePipeline()
        $pipeline.Commands.AddScript("& { ipconfig.exe } 2>&1")
        $result1 = $pipeline.Invoke()
        $pipeline = $rs.CreatePipeline()
        $pipeline.Commands.AddScript("& { net use WinBlue540698NotExist } 2>&1")
        $result2 = $pipeline.Invoke()
        $rs.Close()
    }

    # make this a single string, because multiple strings might have an empty line
	It "The output from ipconfig.exe should be captured and assigned to a variable" {
		"$result1" | Should not BeNullOrEmpty
	}

	It "The first item should be an error record" {
		$result2[0].GetType() | Should Be ([System.Management.Automation.ErrorRecord])
	}

    It "The FullyQualifiedErrorId is correct" {
        $result2[0].FullyQualifiedErrorId | Should be "NativeCommandError"
    }

    Context "using the PowerShell API captures error stream correctly" {
        BeforeAll {
            $ps = [powershell]::Create([initialsessionstate]::CreateDefault2())
            $null = $ps.AddScript("net use WinBlue540698NotExist")
            $ps.Invoke()
            $skip = $false
        }
        AfterAll {
            $ps.Dispose()
        }
        It "The first item should be an error record" {
            $ps.Streams.Error[0].GetType() | Should Be ([System.Management.Automation.ErrorRecord])
        }
        It "The FullyQualifiedErrorId is correct" {
            $ps.Streams.Error[0].FullyQualifiedErrorId | Should be "NativeCommandError"
        }
    }
}

#      WinBlue: 5911 - [Connect Critical] Remove-Item -recurse 'does not work properly.' and 'is faulty'
Describe -tags 'Innerloop', 'DRT' "WinBlue_5911" {
    Context "Remove-Item -Recurse with Include" {
        BeforeAll {
            new-item -itemtype directory $TestDrive\Test | Out-Null
            new-item -itemtype directory $TestDrive\Test\1 | Out-Null
            new-item -itemtype directory $TestDrive\Test\2 | Out-Null
            New-Item -type file -Path $TestDrive\Test\1\foo.txt | Out-Null
            New-Item -type file -Path $TestDrive\Test\2\foo.txt | Out-Null
            $OutputAndErrors = Remove-Item $TestDrive\Test -Recurse -Include *.txt 2>&1
        }
        It "There should be no errors" {
            $OutputAndErrors | should BeNullOrEmpty
        }
        It "There should be 2 directories with the correct name left" {
            $items = Get-ChildItem $TestDrive\Test -Recurse
            $Items.Count | should be 2
            $Items[0].Name | Should be 1
            $Items[1].Name | Should be 2
        }
    }

    Context "TEST - Remove-Item -Recurse With Include" {
        BeforeAll {
            new-item -itemtype directory $TestDrive\Test\1 | Out-Null
            new-item -itemtype directory $TestDrive\Test\2 | Out-Null
            new-item -itemtype directory $TestDrive\Test\1\11 | Out-Null
            new-item -itemtype directory $TestDrive\Test\1\12 | Out-Null
            New-Item -type file -path $TestDrive\Test\1\11\foo.txt | Out-Null
            New-Item -type file -path $TestDrive\Test\1\11\foo.xml | Out-Null
            New-Item -type file -path $TestDrive\Test\1\12\foo.txt | Out-Null
            New-Item -type file -path $TestDrive\Test\1\12\foo.xml | Out-Null
            $OutputAndErrors = Remove-Item $TestDrive\Test -Recurse -Include *.txt 2>&1
            $xmlItems = Get-ChildItem $TestDrive\Test -Include *.xml -Recurse -ErrorAction SilentlyContinue
            $xmlOutputAndErrors = Remove-Item $TestDrive\Test -Recurse -Include *.xml 2>&1
            $finalOutput = Get-ChildItem $TestDrive\Test -Recurse -ErrorAction SilentlyContinue
        }
                

        It "There should be no errors" {
            $OUtputAndErrors | Should BeNullOrEmpty
        }
        It " The 2 xml files should not be deleted" {
            $xmlItems.Count | Should be 2
        }
        It "There should be no errors removing XML files" {
            $xmlOUtputAndErrors | Should BeNullOrEmpty
        }
        It "There should be four directories" {
            $finalOutput.Count | should be 4
        }
        It "Directory '1' still exists" {
            $finalOutput[0].Name | Should be 1
        }
        It "Directory '1' still exists" {
            $finalOutput[1].Name | Should be 2
        }
        It "Directory '1' still exists" {
            $finalOutput[2].Name | Should be 11
        }
        It "Directory '1' still exists" {
            $finalOutput[3].Name | Should be 12
        }
	}
    Context "Remove-Item -Recurse (Registry Provider)" {
        BeforeAll {
            $skip = $true
            if ( get-psprovider -ea silentlycontinue registry )
            {
                $skip = $false
            }    
            push-location
            set-location HKCU:
            new-item -itemtype directory TestKey  | Out-Null
            new-item -itemtype directory TestKey\1  | Out-Null
            new-item -itemtype directory TestKey\2  | Out-Null
            New-Item -path TestKey\1 -name abc -value "abc" | Out-Null
            New-Item -path TestKey\2 -name abc -value "abc" | Out-Null
            New-Item -path TestKey\1 -name def -value "def" | Out-Null
            New-Item -path TestKey\2 -name def -value "def" | Out-Null
            $InitialKeys = Get-Childitem TestKey -Recurse
            $FirstKeyRemoval = Remove-Item -Path TestKey -Recurse -Include *ab* 2>&1
            $DeKeys = Get-ChildItem TestKey -Recurse -Include *de* -ErrorAction SilentlyContinue
            $DeKeyRemoval = Remove-Item TestKey -Recurse -Include *De* 2>&1
            $RemainingKeys = Get-ChildItem TestKey
        }
        AfterAll {
            if ( Test-Path HKCU:\TestKey )
            {
                remove-Item -force -recurse HKCU:\TestKey
            }
            Pop-Location
        }
        It -skip:$skip "The setup is correct" {
            $InitialKeys.Count |  should be 6
        }
        It -skip:$skip "No errors should be present when removing Keys" {
            $FirstKeyRemoval | Should BeNullOrEmpty
        }
        It -skip:$skip "The 2 '*de*' keys should not be deleted" {
            $DeKeys.Count | should be 2
        }
        It -skip:$skip "Removing 'DE*' keys should not produce errors" {
            $DeKeyRemoval | Should BeNullOrEmpty
        }
        It -skip:$skip "The parent keys should not be deleted" {
            $RemainingKeys.Count |Should Be 2
        }
        It -skip:$skip "The parent keys should not be deleted since Include has been specified" {
            $RemainingKeys[0].Name | should be 'HKEY_CURRENT_USER\TestKey\1'
            $RemainingKeys[1].Name | Should Be 'HKEY_CURRENT_USER\TestKey\2'
        }
        It -skip:$skip "a PathNotFound error should be generated when running Get-ChildItem on 'TestKey'" {
            Remove-Item TestKey -Recurse
            try
            {
                Get-ChildItem TestKey -ErrorAction Stop
                Throw "OK"
            }
            catch {
                $_.FullyQualifiedErrorId | should be 'PathNotFound,Microsoft.PowerShell.Commands.GetChildItemCommand'
            }
        }
    }
}

Describe -Tags 'DRT' "CommandInfo.Parameters race condition" {
    try
    {
        # Conditions necessry to trigger the race condition:
        #
        # * Get a CommandInfo instance that implements dynamic parameters from a runspace
        # * Asynchronously run something in that runspace that changes the EngineSessionState
        # * Simultaneously, access the Parameters property on the command info instance from that runspace.

        $ps = [PowerShell]::Create()
        $commandInfo = $ps.AddCommand("Get-Command").AddArgument("Get-ChildItem").AddArgument("cert:").Invoke()
        $ps.Commands.Clear()

        $asyncResult = $ps.AddScript(@'
    $iterations = 0
    $m = New-Module { $testVar = 11 }
    while (11 -eq (& $m {$testVar}))
    {
        $iterations++
        if ($iterations -eq 5000) { return "passed" }
    }
    "Failed after $iterations iterations"
'@).BeginInvoke()

        It "Check command info dynamic parameter" {
            $commandInfo.Parameters['CodeSigningCert'] | Should Not Be $null
        }

        It "Try to trigger race" {
            while (!$asyncResult.IsCompleted)
            {
                # CommandInfo.Parameters is not cached, so repeated access of
                # the property could trigger the (former) race condition.
                $null = $commandInfo.Parameters
            }
        }

        It "Make sure the async block finished w/ no errors" {
            $ps.EndInvoke($asyncResult) | Should Be "passed"
        }
    }
    finally
    {
        $ps.Dispose()
    }
}
