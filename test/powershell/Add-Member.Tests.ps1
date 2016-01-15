Describe "Add-Member" {

    It "should be able to see a newly added member of an object" {
        $o = New-Object psobject
        Add-Member -InputObject $o -MemberType NoteProperty -Name proppy -Value "superVal"

        $o.proppy | Should Not BeNullOrEmpty
        $o.proppy | Should Be "superVal"
    }

    It "Should be able to add a member to an object that already has a member in it" {
        $o = New-Object psobject
        Add-Member -InputObject $o -MemberType NoteProperty -Name proppy -Value "superVal"
        Add-Member -InputObject $o -MemberType NoteProperty -Name AnotherMember -Value "AnotherValue"

        $o.AnotherMember | Should Not BeNullOrEmpty
        $o.AnotherMember | Should Be "AnotherValue"
    }
}
