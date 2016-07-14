Describe "Validation for Information stream in PowerShell" -Tags "CI" {
    it "Supports Write-Information cmdlet and InformationVariable" {
        $process = Get-Process -id $pid
        Write-Information $process -Tags "Tag1","Tag2" -InformationVariable infoVar
        
        $infoVar.MessageData.Id | Should be $pid
        $infoVar.Tags[0] | Should be Tag1
        $infoVar.Tags[1] | Should be Tag2
    }
    
    it "Verifies InformationAction" {
        $output = Write-Information Test -Tags "Tag1","Tag2" *>&1 -InformationAction Ignore
        $output | Should be $null
    }
    
    it "Verifies InformationPreference" {
        $informationPreference = "Ignore"
        $output = Write-Information Test -Tags "Tag1","Tag2" *>&1 -InformationAction Ignore
        $output | Should be $null
    }

    it "Verifies Information stream redirection" {
        $process = Get-Process -id $pid
        $result = Write-Information $process -Tags "Tag1","Tag2"  6>&1
    
        $result.MessageData.Id | Should be $pid
        $result.Tags[0] | Should be Tag1
        $result.Tags[1] | Should be Tag2
    }

    it "Verifies Information stream on PowerShell object" {
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript('$process = Get-Process -id $pid; $result = Write-Information $process -Tags "Tag1","Tag2"').Invoke()
        
        $result = $ps.Streams.Information[0]
    
        $result.MessageData.Id | Should be $pid
        $result.Tags[0] | Should be Tag1
        $result.Tags[1] | Should be Tag2
    }

    it -pending "Verifies Information stream on jobs" {
        $j = Start-Job { Write-Information "FromJob" -Tags "Tag1","Tag2" }
        Wait-Job $j
        
        $result = $j.ChildJobs[0].Information[0]
    
        $result.MessageData | Should be "FromJob"
        $result.Tags[0] | Should be Tag1
        $result.Tags[1] | Should be Tag2
    }

    it -pending "Verifies Information stream on workflow" {
        ## Test regular invocation
        # NOTE THE FOLLOWING LINE MUST BE UNCOMMENTED WHEN WORKFLOW IS SUPPORTED ON LINUX
        # workflow foo { Write-Information Bar }
        foo -InformationVariable bar
        $bar.MessageData | Should be "Bar"

        ## Test job invocation
        $j = foo -asjob
        Wait-Job $j
        $j.ChildJobs[0].Information.MessageData | Should be "Bar"

        ## Ensure that InformationAction works on workflow
        $result = foo -informationaction ignore *>&1
        $result | Should be $null
    }
    
    it -pending "Verifies InformationVariable in workflow compilation" {
        # NOTE THE FOLLOWING LINE MUST BE UNCOMMENTED WHEN WORKFLOW IS SUPPORTED ON LINUX
        # workflow IVWorkflowTest { Write-Information Test -InformationVariable Test; $test }
        $result = IVWorkflowTest
        $result.MessageData | Should be "Test"   
    }
    
    it -pending "Verifies Information stream works over remoting" {
        icm localhost { Write-Information Test } -InformationVariable Test
        $test.MessageData | Should be "Test"
    }
    
    it -pending "Verifies client-side stream redirection works over remoting" {

        $result = icm localhost { Write-Information Test 6>&1 }
        $result.MessageData | Should be "Test"
    }

    it "Verifies PowerShell-calling-PowerShell" {

        $result = powershell -noprofile { $pid; Write-Information Bar } *>&1
        $processPid = $result[0]
        $result[1].ProcessId | Should be $processPid
    }
    
    it "Verifies that host output can be redirected" {
        function spammer { [CmdletBinding()] param() Write-Host "Some host output" }
        spammer -InformationVariable spam
        $spam.MessageData.Message | Should be "Some host output"
    }
}
