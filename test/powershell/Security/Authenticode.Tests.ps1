Describe "Tests for authenticode cmdlets" -Tags "CI" {

    It "Verifies that we can retrieve catalog signatures from OS Binaries" -skip:(!$IsWindows) {
    
        ## Only supported on Win8+
        if([Version] (Get-WmiObject Win32_OperatingSystem).Version -lt ([Version] "6.2"))
        {
            return
        }
        
        $signature = Get-Command wmic | Get-Item | Get-AuthenticodeSignature
        $signature.IsOSBinary | Should be $true
    }
}
