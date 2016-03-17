using Xunit;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Linux.Host;

namespace PSTests
{
    public class SessionStateTests
    {
        [Fact]
        public void TestDrives()
        {
			PowerShellAssemblyLoadContextInitializer.SetPowerShellAssemblyLoadContext(AppContext.BaseDirectory);
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
			PSHost hostInterface =  new DefaultHost(currentCulture,currentCulture);
			RunspaceConfiguration runspaceConfiguration =  RunspaceConfiguration.Create();
			InitialSessionState iss = InitialSessionState.CreateDefault2();
			AutomationEngine engine = new AutomationEngine(hostInterface, runspaceConfiguration, iss);
			ExecutionContext executionContext = new ExecutionContext(engine, hostInterface, iss);
            SessionStateInternal sessionState = new SessionStateInternal(executionContext);
            Collection<PSDriveInfo> drives = sessionState.Drives(null);
			Assert.True(drives.Count>0);
        }

    }
}
