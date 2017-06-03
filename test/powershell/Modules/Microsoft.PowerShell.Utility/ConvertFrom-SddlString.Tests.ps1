Describe "ConvertFrom-SddlString" -Tags "CI" {
    It "Should convert without type" -Skip:($IsLinux -Or $IsOSX) {
        $SddlStringInfo = ConvertFrom-SddlString "O:BAG:BAD:(A;CI;CCDCLCSWRPWPRCWD;;;BA)(A;CI;CCDCRP;;;NS)(A;CI;CCDCRP;;;LS)(A;CI;CCDCRP;;;AU)"

        $SddlStringInfo.Owner | Should Be "BUILTIN\Administrators"
        $SddlStringInfo.Group | Should Be "BUILTIN\Administrators"
        $SddlStringInfo.DiscretionaryAcl.Length | Should BeGreaterThan 0
        $SddlStringInfo.SystemAcl.Length | Should Be $null
        $SddlStringInfo.RawDescriptor | Should BeOfType [System.Security.AccessControl.CommonSecurityDescriptor]
    }

    It "Should convert with type" -Skip:($IsLinux -Or $IsOSX) {
        $SddlStringInfo = ConvertFrom-SddlString "D:(A;CI;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)(A;;CCLCSWLOCRRC;;;IU)(A;;CCLCSWLOCRRC;;;SU)S:(AU;SAFA;WDWO;;;BA)" -Type FileSystemRights

        $SddlStringInfo.Owner | Should Be $null
        $SddlStringInfo.Group | Should Be $null
        $SddlStringInfo.DiscretionaryAcl.Length | Should BeGreaterThan 0
        $SddlStringInfo.SystemAcl.Length | Should BeGreaterThan 0
        $SddlStringInfo.RawDescriptor | Should BeOfType [System.Security.AccessControl.CommonSecurityDescriptor]
    }
}
