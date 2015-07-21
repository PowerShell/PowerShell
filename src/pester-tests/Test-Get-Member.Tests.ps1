 
 
 

Describe "Test-Get-Member" {
    It "Should be able to be called on string objects, ints, arrays, etc" {
        $a = 1 #test numbers
        $b = 1.3
        $c = $false #test bools
        $d = @(1,3) # test arrays
        $e = "anoeduntodeu" #test strings
        $f = 'asntoheusth' #test strings
        
        Get-Member -InputObject $a | Should Not BeNullOrEmpty
        Get-Member -InputObject $b | Should Not BeNullOrEmpty
        Get-Member -InputObject $c | Should Not BeNullOrEmpty
        Get-Member -InputObject $d | Should Not BeNullOrEmpty
        Get-Member -InputObject $e | Should Not BeNullOrEmpty
        Get-Member -InputObject $f | Should Not BeNullOrEmpty
    }

    It "Should be able to extract a field from string objects, ints, arrays, etc" {
        $a = 1 #test numbers
        $b = 1.3
        $c = $false #test bools
        $d = @(1,3) # test arrays
        $e = "anoeduntodeu" #test strings
        $f = 'asntoheusth' #test strings

        $a.GetType().Name | Should Be 'Int32'
        $b.GetType().Name | Should Be 'Double'
        $c.GetType().Name | Should Be 'Boolean'
        $d.GetType().Name | Should Be 'Object[]'
        $e.GetType().Name | Should Be 'String'
        $f.GetType().Name | Should Be 'String'
    }

    It "Should be able to be called on a newly created PSObject" {
        $o = New-Object psobject
        # this creates a dependency on the Add-Member cmdlet.
        Add-Member -InputObject $o -MemberType NoteProperty -Name proppy -Value "superVal"

        Get-Member -InputObject $o | Should Not BeNullOrEmpty
    }
}
