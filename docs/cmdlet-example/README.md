# Building a C# Cmdlet

This demonstrates how to build your own C# cmdlet for PowerShell Core with Visual Studio.
We will use free Visual Studio Community 2017 that can be downloaded from [https://www.visualstudio.com/downloads](https://www.visualstudio.com/downloads)

1. When installing Visual Studio 2017 make sure that '.NET Core cross-platform development' is selected under 'Other Toolsets':
![1](https://user-images.githubusercontent.com/11860095/31471799-7ca192f4-ae9f-11e7-9731-03fb24ba949c.png)

2. Create new C# project `SendGreeting` of type `Class Library (.NET Core)`
![2](https://user-images.githubusercontent.com/11860095/31471800-7cba3c28-ae9f-11e7-9d05-48fcddb8af85.png)

3. Now we need to setup PowerShell Core reference assemblies.
In `Solution Explorer` right click on project `Dependencies` and select 'Manage NuGet Packages...'
In the top-right corner of the package manager click on the small `Settings` sprocket icon that is to the right from `Package source` dropdown.
By default, there will be only `nuget.org` package source in `Available package sources` list.
Add another package source with name `powershell-core` and source `https://powershell.myget.org/F/powershell-core/api/v3/index.json`
![3](https://user-images.githubusercontent.com/11860095/31471801-7cd0d186-ae9f-11e7-9a87-2c9326d7f446.png)
In the package manager select new `powershell-core` in `Package source` dropdown, select 'Browse' tab, type in `System.Management.Automation` in the search and select `Include prerelease`.
It should find `System.Management.Automation` package, select it and it will show package details; install it using `Install` button.
![4](https://user-images.githubusercontent.com/11860095/31471802-7ce85bf8-ae9f-11e7-97c8-afa09b64aced.png)

4. Add the code of a cmdlet:
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
![5](https://user-images.githubusercontent.com/11860095/31471803-7d014afa-ae9f-11e7-92e5-ca9dc3b9e25c.png)

5. Build solution (F6); The `Output` window will print the location of generated cmdlet DLL:
![6](https://user-images.githubusercontent.com/11860095/31471804-7d1a66f2-ae9f-11e7-93c2-df72fb43da81.png)

6. Start PowerShell Core, run `Import-Module` on DLL path from previous step and run cmdlet:
![7](https://user-images.githubusercontent.com/11860095/31471805-7d326784-ae9f-11e7-8752-9839c7538abc.png)
