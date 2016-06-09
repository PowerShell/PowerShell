using namespace System.Diagnostics

Describe "PowerShell Command Debugging"{
        $powershell = Join-Path -Path $PsHome -ChildPath "powershell"

function NewProcessStartInfo([string]$CommandLine, [switch]$RedirectStdIn)
        {
            return [ProcessStartInfo]@{
                FileName               = $powershell
                Arguments              = $CommandLine
                RedirectStandardInput  = $RedirectStdIn
                RedirectStandardOutput = $true
                RedirectStandardError  = $true
                UseShellExecute        = $false
            }
        }
        
          function RunPowerShell([ProcessStartInfo]$debugfn)
        {
            $process = [Process]::Start($debugfn)
            return $process
        }
        
        
        function EnsureChildHasExited([Process]$process, [int]$WaitTimeInMS = 15000)
        {
            $process.WaitForExit($WaitTimeInMS)

            if (!$process.HasExited)
            {
                $process.HasExited | Should Be $true
                $process.Kill()
            }
        }
        
    It "Should be able to step into debugging"{ 
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

	foreach ($i in 1..2) {
	    $process.StandardOutput.ReadLine()
	}

        $process.StandardInput.Write("foo`n")
	
	foreach ($i in 1..13) {
            $process.StandardOutput.ReadLine()
	}

	$process.StandardInput.Write("s`n")

	foreach ($i in 1..3) {
            $process.StandardOutput.ReadLine() }

	$process.StandardInput.Write("s`n")

	foreach ($i in 1..3) {
            $process.StandardOutput.ReadLine()
	}

	$process.StandardInput.Write("s`n")

	foreach ($i in 1..3) {
            $process.StandardOutput.ReadLine()
	}

        $process.StandardInput.Close() 
	        
        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }   
  
    It "Should be able to continue into debugging"{ 
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

	foreach ($i in 1..2) {
	    $process.StandardOutput.ReadLine()
	}

        $process.StandardInput.Write("foo`n")
	
	foreach ($i in 1..13) {
            $process.StandardOutput.ReadLine()
	}

	$process.StandardInput.Write("c`n")
        $process.StandardOutput.ReadLine() 
        $process.StandardOutput.ReadLine() 

        $process.StandardInput.Close() 
	        
        EnsureChildHasExited $process
        $process.ExitCode | Should Be 0
    }   

    It "Should be able to list help for debugging"{ 
        $debugfn = NewProcessStartInfo "-noprofile ""`$function:foo = { 'bar' }""" -RedirectStdIn
        $process = RunPowerShell $debugfn
        $process.StandardInput.Write("Set-PsBreakpoint -command foo`n")

	foreach ($i in 1..2) {
	    $process.StandardOutput.ReadLine()
	}

        $process.StandardInput.Write("foo`n")
	
	foreach ($i in 1..13) {
            $process.StandardOutput.ReadLine()
	}
	$process.StandardInput.Write("h`n")
	foreach ($i in 1..23) {
            $line = $process.StandardOutput.ReadLine() 
	}

	$process.StandardInput.Write("s`n")

	foreach ($i in 1..3) {
           $process.StandardOutput.ReadLine() 
	}

	$process.StandardInput.Write("s`n")

	foreach ($i in 1..3) {
           $process.StandardOutput.ReadLine()
	}

	$process.StandardInput.Write("s`n")

	foreach ($i in 1..3) {
           $process.StandardOutput.ReadLine()
	}

        $process.StandardInput.Close() 
	        
        EnsureChildHasExited $process
	$line | Should Be  "For instructions about how to customize your debugger prompt, type `"help about_prompt`"."
#        $process.ExitCode | Should Be 0

   

    }   
}



