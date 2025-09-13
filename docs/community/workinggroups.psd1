@{
    Members = @(
        @{
            Name          = 'James Truher'
            GitHub        = 'JamesWTruher'
            Type          = 'Microsoft Employee'
            WorkingGroups = @(
                'Cmdlets and Modules'
                'Developer Experience'
                'Engine'
                'Interactive UX'
                'Language'
            )
        }
        @{
            Name          = 'Steve Lee'
            GitHub        = 'SteveL-MSFT'
            Type          = 'Microsoft Employee'
            WorkingGroups = @(
                'Cmdlets and Modules'
                'DSC'
                'Security'
            )
        }
        @{
            Name          = 'Jeff Hicks'
            GitHub        = 'jdhitsolutions'
            Type          = 'Community Contributor'
            WorkingGroups = @('Cmdlets and Modules')
        }
        @{
            Name          = 'Tobias Weltner'
            GitHub        = 'TobiasPSP'
            Type          = 'Community Contributor'
            WorkingGroups = @('Cmdlets and Modules')
        }
        @{
            Name          = 'Ryan Yates'
            GitHub        = 'kilasuit'
            Type          = 'Community Contributor'
            WorkingGroups = @(
                'Cmdlets and Modules'
                'Engine'
                'Interactive UX'
            )
        }
        @{
            Name          = 'Travis Plunk'
            GitHub        = 'TravisEz13'
            Type          = 'Microsoft Employee'
            WorkingGroups = @(
                'DSC'
                'Remoting'
                'Security'
            )
        }
        @{
            Name          = 'Jason Helmick'
            GitHub        = 'theJasonHelmick'
            Type          = 'Microsoft Employee'
            WorkingGroups = @(
                'DSC'
                'Interactive UX'
            )
        }
        @{
            Name          = 'Andrey Nemenya'
            GitHub        = 'anmenaga'
            Type          = 'Microsoft Employee'
            WorkingGroups = @(
                'DSC'
                'Remoting'
            )
        }
        @{
            Name          = 'Gael Colas'
            GitHub        = 'gaelcolas'
            Type          = 'Community Contributor'
            WorkingGroups = @('DSC')
        }
        @{
            Name          = 'Michael Lombardi'
            GitHub        = 'michaeltlombardi'
            Type          = 'Microsoft Employee'
            WorkingGroups = @(
                'DSC'
                'Developer Experience'
            )
        }
        @{
            Name          = 'Aditya Patwardhan'
            GitHub        = 'adityapatwardhan'
            Type          = 'Microsoft Employee'
            WorkingGroups = @(
                'Developer Experience'
                'Interactive UX'
            )
        }
        @{
            Name          = 'SeeminglyScience'
            GitHub        = 'SeeminglyScience'
            Type          = 'Microsoft Employee'
            WorkingGroups = @(
                'Developer Experience'
                'Engine'
                'Interactive UX'
                'Language'
            )
        }
        @{
            Name          = 'Bergmeister'
            GitHub        = 'bergmeister'
            Type          = 'Community Contributor'
            WorkingGroups = @('Developer Experience')
        }
        @{
            Name          = 'Dongbo Wang'
            GitHub        = 'daxian-dbw'
            Type          = 'Microsoft Employee'
            WorkingGroups = @(
                'Engine'
                'Interactive UX'
                'Language'
            )
        }
        @{
            Name          = 'Keith Hill'
            GitHub        = 'rkeithhill'
            Type          = 'Community Contributor'
            WorkingGroups = @('Engine')
        }
        @{
            Name          = 'Vexx32'
            GitHub        = 'vexx32'
            Type          = 'Community Contributor'
            WorkingGroups = @('Engine')
        }
        @{
            Name          = 'IISResetMe'
            GitHub        = 'IISResetMe'
            Type          = 'Community Contributor'
            WorkingGroups = @('Engine')
        }
        @{
            Name          = 'PowerCode'
            GitHub        = 'powercode'
            Type          = 'Community Contributor'
            WorkingGroups = @('Engine')
        }
        @{
            Name          = 'Sean Wheeler'
            GitHub        = 'sdwheeler'
            Type          = 'Microsoft Employee'
            WorkingGroups = @('Interactive UX')
        }
        @{
            Name          = 'Friedrich Weinmann'
            GitHub        = 'FriedrichWeinmann'
            Type          = 'Microsoft Employee'
            WorkingGroups = @('Interactive UX')
        }
        @{
            Name          = 'Steven Bucher'
            GitHub        = 'StevenBucher98'
            Type          = 'Microsoft Employee'
            WorkingGroups = @('Interactive UX')
        }
        @{
            Name          = 'Sydney Smith'
            GitHub        = 'SydneySmithReal'
            Type          = 'Microsoft Employee'
            WorkingGroups = @('Security')
        }
        @{
            Name          = 'Anam Navi'
            GitHub        = 'anamnavi'
            Type          = 'Microsoft Employee'
            WorkingGroups = @('Security')
        }
    )

    WorkingGroups = @(
        @{
            Name             = 'Cmdlets and Modules'
            Description      = 'Focuses on core/inbox modules in the PowerShell/PowerShell repo, including proposing new cmdlets/parameters, improvements, bugfixes, and breaking changes.'
            Members          = @(
                'JamesWTruher'
                'SteveL-MSFT'
                'jdhitsolutions'
                'TobiasPSP'
                'kilasuit'
            )
            HistoricMembers  = @()
            Responsibilities = @(
                'Core/inbox modules in PowerShell/PowerShell repo'
                'Propose new cmdlets/parameters'
                'Improvements and bugfixes to existing cmdlets/parameters'
                'Breaking changes'
            )
        }
        @{
            Name             = 'DSC'
            Description      = 'The Desired State Configuration (DSC) WG manages all facets of DSC in PowerShell 7, including language features (like the Configuration keyword) and the PSDesiredStateConfiguration module. Today, DSC is integrated into the PowerShell language, and we need to manage it as such.'
            Members          = @(
                'TravisEz13'
                'theJasonHelmick'
                'anmenaga'
                'gaelcolas'
                'michaeltlombardi'
                'SteveL-MSFT'
            )
            HistoricMembers  = @()
            Responsibilities = @(
                'Manage all facets of DSC in PowerShell 7'
                'Oversee language features like the Configuration keyword'
                'Maintain the PSDesiredStateConfiguration module'
            )
            Repositories     = @()
            TaskTrackingProjects = @()
        }
        @{
            Name             = 'Developer Experience'
            Description      = 'The PowerShell developer experience includes the development of modules (in C#, PowerShell script, etc.), as well as the experience of hosting PowerShell and its APIs in other applications and language runtimes. Special consideration should be given to topics like backwards compatibility with Windows PowerShell (e.g. with PowerShell Standard) and integration with related developer tools (e.g. .NET CLI or the PowerShell extension for VS Code).'
            Members          = @(
                'JamesWTruher'
                'adityapatwardhan'
                'michaeltlombardi'
                'SeeminglyScience'
                'bergmeister'
            )
            HistoricMembers  = @()
            Responsibilities = @(
                'Development of modules in C#, PowerShell script, etc.'
                'Hosting PowerShell and its APIs in other applications and runtimes'
                'Backwards compatibility with Windows PowerShell'
                'Integration with developer tools (e.g. .NET CLI, VS Code extension)'
            )
            Repositories     = @()
            TaskTrackingProjects = @()
        }
        @{
            Name             = 'Engine'
            Description      = 'The Engine WG focuses on the implementation and maintenance of core PowerShell engine code, including the language parser, command and parameter binders, module and provider systems, performance, componentization, and AssemblyLoadContext. Not responsible for the definition of the PowerShell language.'
            Members          = @(
                'daxian-dbw'
                'JamesWTruher'
                'rkeithhill'
                'vexx32'
                'SeeminglyScience'
                'IISResetMe'
                'powercode'
                'kilasuit'
            )
            HistoricMembers  = @()
            Responsibilities = @(
                'Implementation and maintenance of core PowerShell engine code'
                'Language parser, command and parameter binders'
                'Module and provider systems'
                'Performance, componentization, AssemblyLoadContext'
            )
            Repositories     = @()
            TaskTrackingProjects = @()
        }
        @{
            Name             = 'Interactive UX'
            Description      = 'Focuses on the interactive user experience in PowerShell, including the console, help system, tab completion/IntelliSense, markdown rendering, PSReadLine, and debugging.'
            Members          = @(
                'theJasonHelmick'
                'daxian-dbw'
                'adityapatwardhan'
                'JamesWTruher'
                'SeeminglyScience'
                'sdwheeler'
                'kilasuit'
                'FriedrichWeinmann'
                'StevenBucher98'
            )
            HistoricMembers  = @()
            Responsibilities = @(
                'Console user experience'
                'Help system'
                'Tab completion / IntelliSense'
                'Markdown rendering'
                'PSReadLine'
                'Debugging'
            )
        }
        @{
            Name             = 'Language'
            Description      = 'Deals with the abstract definition of the PowerShell language itself, working closely with the PowerShell Committee.'
            Members          = @(
                'JamesWTruher'
                'daxian-dbw'
                'SeeminglyScience'
            )
            HistoricMembers  = @()
            Responsibilities = @(
                'Definition of the PowerShell language'
                'Work with the PowerShell Committee on language decisions'
            )
            Repositories     = @()
            TaskTrackingProjects = @()
        }
        @{
            Name             = 'Remoting'
            Description      = 'Focuses on the PowerShell Remoting Protocol (PSRP), protocols implemented under PSRP (e.g. WinRM, SSH), and the PowerShell job system.'
            Members          = @(
                'anmenaga'
                'TravisEz13'
            )
            HistoricMembers  = @()
            Responsibilities = @(
                'PowerShell Remoting Protocol (PSRP)'
                'Protocols under PSRP (WinRM, SSH)'
                'Other remoting protocols'
                'PowerShell job system'
            )
        }
        @{
            Name             = 'Security'
            Description      = 'Handles issues and pull requests with security implications, providing expertise, concerns, and guidance.'
            Members          = @(
                'TravisEz13'
                'SydneySmithReal'
                'anamnavi'
                'SteveL-MSFT'
            )
            HistoricMembers  = @()
            Responsibilities = @(
                'Review issues and PRs with security implications'
                'Provide security expertise and guidance'
            )
            Repositories     = @()
            TaskTrackingProjects = @()
        }
    )
}
