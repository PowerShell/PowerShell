using Xunit;
using System;
using System.Management.Automation;
using Microsoft.PowerShell.CoreConsoleHost;

// This collection fixture initializes Core PowerShell's AssemblyLoadContext once and only
// once. Attempting to initialize in a class level fixture will cause multiple
// initializations, resulting in test failure due to "Binding model is already locked for
// the AppDomain and cannot be reset".
namespace PSTests
{
    public class AssemblyLoadContextFixture
    {
        public AssemblyLoadContextFixture()
        {
            // Initialize the Core PowerShell AssemblyLoadContext
            PowerShellAssemblyLoadContextInitializer.SetPowerShellAssemblyLoadContext(AppContext.BaseDirectory);
        }
    }

    [CollectionDefinition("AssemblyLoadContext")]
    public class AssemblyLoadContextCollection : ICollectionFixture<AssemblyLoadContextFixture>
    {
        // nothing to do but satisfy the interface
    }
}
