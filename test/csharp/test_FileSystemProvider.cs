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
		private static ExecutionContext GetExecutionContext()
		{
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
            PSHost hostInterface =  new DefaultHost(currentCulture,currentCulture);
            RunspaceConfiguration runspaceConfiguration =  RunspaceConfiguration.Create();
            InitialSessionState iss = InitialSessionState.CreateDefault2();
            AutomationEngine engine = new AutomationEngine(hostInterface, runspaceConfiguration, iss);
            ExecutionContext executionContext = new ExecutionContext(engine, hostInterface, iss);
			return executionContext;
		}
		private static ProviderInfo GetProvider()
        {
            ExecutionContext executionContext = GetExecutionContext();
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
		
		[Fact]
        public static void TestGetProperty()
        {
			string path = @"/filesystemobject1";
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			FileInfo fileSystemObject1 = new FileInfo(path);
			fileSystemProvider.SetProperty(path, PSObject.AsPSObject(fileSystemObject1));
			fileSystemProvider.GetProperty(path, new Collection<string>(){"Attributes"});
        }
		
		[Fact]
        public static void TestSetProperty()
        {
			string path = @"/filesystemobject1";
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			FileInfo fileSystemObject1 = new FileInfo(path);
			fileSystemProvider.SetProperty(path, PSObject.AsPSObject(fileSystemObject1));
        }
		
		[Fact]
        public static void TestClearProperty()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			fileSystemProvider.ClearProperty(@"/test", new Collection<string>(){"Attributes"});
        }
		
		[Fact]
        public static void TestGetContentReader()
        {
			string path = @"/test";
			if(File.Exists(path)) File.Delete(path);
			File.AppendAllText(path,"test content!");
			
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			
			IContentReader contentReader = fileSystemProvider.GetContentReader(path);
			Assert.NotNull(contentReader);
			File.Delete(path);
        }
		
		[Fact]
        public static void TestGetContentWriter()
        {
			string path = @"/test";
			if(File.Exists(path)) File.Delete(path);
			File.AppendAllText(path,"test content!");
			
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			
			IContentWriter contentWriter = fileSystemProvider.GetContentWriter(path);
			Assert.NotNull(contentWriter);
			File.Delete(path);
        }
		
		[Fact]
        public static void TestClearContent()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			fileSystemProvider.ClearContent(@"/test");
        }
    }
}
