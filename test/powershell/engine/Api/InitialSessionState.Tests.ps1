Describe "InitialSessionState" -Tags "DRT" {

    # MSFT:5885218
    It -pending 'allows to use ImportPSModulesFromPath with version-based module from C#' {
        Add-Type -OutputAssembly TestDrive:\app.exe -OutputType ConsoleApplication -reference mscorlib,System.Management.Automation -TypeDefinition @'
using System;
using System.Management.Automation.Runspaces;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.ImportPSModulesFromPath(args[0]);
            Console.WriteLine(initialSessionState.Modules.Count.ToString());
        }
    }
}
'@
        new-item -type directory TestDrive:\root\AAA\1.2.3
        New-ModuleManifest -Path TestDrive:\root\AAA\1.2.3\AAA.psd1 -ModuleVersion 1.2.3
        $output = TestDrive:\app.exe ((ls TestDrive:\root\).FullName)
        $output | Should Be 1
    }   

    It 'can use function with AddCommand' {
        function get-foo
        {
            'foo!'
        }

        $functionInfo = get-command -Name get-foo -CommandType Function

        $ssfe = [System.Management.Automation.Runspaces.SessionStateFunctionEntry]::new(
            $functionInfo.Name,
            $functionInfo.Definition,
            $functionInfo.Options,
            $functionInfo.HelpFile
        )

        $ss = [initialsessionstate]::Create()
        $ss.Commands.Add($ssfe)

        $ps = [powershell]::Create($ss)
        $ps.AddCommand($functionInfo) > $null
        $ps.Invoke() | Should Be 'foo!'
    }

    It 'can use native command (Application) with AddCommand' {
        if ( $IsWindows ) {
        $cmdInfo = get-command -Name ping -CommandType Application
        }
        else {
            $cmdInfo = get-command -Name ifconfig -CommandType Application
        }
        $cmdInfo | Should Not Be $null
        $ps = [powershell]::Create()
        $ps.AddCommand($cmdInfo) > $null
        $ps.Invoke() | Should Not Be $null
    }

    It 'can use cmdlet with AddCommand' {
        $psInfo = get-command -Name Get-Process -CommandType Cmdlet
        $ps = [powershell]::Create()
        $ps.AddCommand($psInfo) > $null
        $ps.Invoke() | Should Not Be $null
    }

    It 'can use alias with AddCommand' {
        if ( !( get-alias ps -ea silentlycontinue )) {
            set-alias -Name ps -Value get-process -scope local
        }
        $psInfo = get-command -Name ps -CommandType Alias
        $ps = [powershell]::Create()
        $ps.AddCommand($psInfo) > $null
        $ps.Invoke() | Should Not Be $null
    }
}
