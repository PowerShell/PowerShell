using Xunit;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Management.Automation.Internal.Host;
using System.Management.Automation.Provider;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Linux.Host;

namespace PSTests
{
    [Collection("AssemblyLoadContext")]
    public static class FileSystemProviderTests
    {
		private static ProviderInfo GetProvider()
        {
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
            PSHost hostInterface =  new DefaultHost(currentCulture,currentCulture);
            RunspaceConfiguration runspaceConfiguration =  RunspaceConfiguration.Create();
            InitialSessionState iss = InitialSessionState.CreateDefault2();
			
            AutomationEngine engine = new AutomationEngine(hostInterface, runspaceConfiguration, iss);
            ExecutionContext executionContext = new ExecutionContext(engine, hostInterface, iss);
            SessionStateInternal sessionState = new SessionStateInternal(executionContext);
			
			SessionStateProviderEntry providerEntry = new SessionStateProviderEntry("FileSystem",typeof(FileSystemProvider), null);
			sessionState.AddSessionStateEntry(providerEntry);
            ProviderInfo matchingProvider = sessionState.ProviderList.ToList()[0];

            return matchingProvider;
        }
		
        [Fact]
        public static void TestCreateJunctionFails()
        {
            Assert.False(InternalSymbolicLinkLinkCodeMethods.CreateJunction("",""));
        }
		
		[Fact]
        public static void TestGetHelpMaml()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			Assert.Equal(fileSystemProvider.GetHelpMaml(String.Empty,String.Empty),String.Empty);
			Assert.Equal(fileSystemProvider.GetHelpMaml("helpItemName",String.Empty),String.Empty);
			Assert.Equal(fileSystemProvider.GetHelpMaml(String.Empty,"path"),String.Empty);
        }
		
		[Fact]
        public static void TestMode()
        {
			Assert.Equal(FileSystemProvider.Mode(null),String.Empty);
			FileSystemInfo fileSystemObject = new DirectoryInfo(@"/");
			Assert.NotEqual(FileSystemProvider.Mode(PSObject.AsPSObject(fileSystemObject)),String.Empty);
        }
		
		[Fact(Skip = "Need mock")]
        public static void TestGetProperty()
        {
			string path = @"/filesystemobject1";
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			FileInfo fileSystemObject1 = new FileInfo(path);
			fileSystemProvider.SetProperty(path, PSObject.AsPSObject(fileSystemObject1));
			fileSystemProvider.GetProperty(path, new Collection<string>(){"Attributes"});
        }
		
		[Fact(Skip = "Need mock")]
        public static void TestSetProperty()
        {
			string path = @"/filesystemobject1";
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			FileInfo fileSystemObject1 = new FileInfo(path);
			fileSystemProvider.SetProperty(@"/root", PSObject.AsPSObject(fileSystemObject1));
        }
		
		[Fact(Skip = "Need mock")]
        public static void TestClearProperty()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			fileSystemProvider.ClearProperty(@"/test", new Collection<string>(){"Attributes"});
        }
		
		[Fact(Skip = "Need mock")]
        public static void TestGetContentReader()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			IContentReader contentReader = fileSystemProvider.GetContentReader(@"/test");
			Assert.NotNull(contentReader);
        }
		
		[Fact(Skip = "Need mock")]
        public static void TestGetContentWriter()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			IContentWriter contentWriter = fileSystemProvider.GetContentWriter(@"/test");
			Assert.NotNull(contentWriter);
        }
		
		[Fact(Skip = "Need mock")]
        public static void TestClearContent()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			fileSystemProvider.ClearContent(@"/test");
        }
    }
}
