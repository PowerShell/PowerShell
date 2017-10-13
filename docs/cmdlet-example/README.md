# Building a C# Cmdlet

This demonstrates how to build your own C# cmdlet for PowerShell Core with Visual Studio.

We will use free [Visual Studio Community 2017](https://www.visualstudio.com/downloads).

1. When installing Visual Studio 2017 select `.NET Core cross-platform development` under `Other Toolsets`
![Step1](./Images/Step1.png)

2. Create new C# project `SendGreeting` of type `Class Library (.NET Core)`
![Step2](./Images/Step2.png)

3. Now we need to setup PowerShell Core reference assemblies.
In `Solution Explorer` right click on project `Dependencies` and select `Manage NuGet Packages...`
In the top-right corner of the package manager click on the small `Settings` sprocket icon that is to the right from `Package source` dropdown.
By default, there will be only `nuget.org` package source in `Available package sources` list.
Add another package source with name `powershell-core` and source `https://powershell.myget.org/F/powershell-core/api/v3/index.json`
![Step3](./Images/Step3.png)  
In the package manager select new `powershell-core` in `Package source` dropdown, select `Browse` tab, type in `System.Management.Automation` in the search and select `Include prerelease`.
It should find `System.Management.Automation` package, select it and it will show package details; install it using `Install` button.
![Step4](./Images/Step4.png)

4. Add the code of cmdlet:  
```CSharp
using System.Management.Automation;  // PowerShell namespace.

namespace SendGreeting
{
    // Declare the class as a cmdlet and specify and 
    // appropriate verb and noun for the cmdlet name.
    [Cmdlet(VerbsCommunications.Send, "Greeting")]
    public class SendGreetingCommand : Cmdlet
    {
        // Declare the parameters for the cmdlet.
        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        // Overide the ProcessRecord method to process
        // the supplied user name and write out a 
        // greeting to the user by calling the WriteObject
        // method.
        protected override void ProcessRecord()
        {
            WriteObject("Hello " + Name + "!");
        }
    }
}
```  
At this point everything should look like this:  
![Step5](./Images/Step5.png)  

5. Build solution (F6); The `Output` window will print the location of generated cmdlet DLL:
![Step6](./Images/Step6.png)

6. Start PowerShell Core, run `Import-Module` on DLL path from previous step and run cmdlet:
![Step7](./Images/Step7.png)  
You can also run the same cmdlet on Linux and other systems that PowerShell Core supports:
![Step8](./Images/Step8.png)
