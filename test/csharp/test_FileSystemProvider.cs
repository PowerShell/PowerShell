using Xunit;
using System;
using System.Collections.Generic;
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

namespace PSTests
{
    [Collection("AssemblyLoadContext")]
    public class FileSystemProviderTests: IDisposable
    {
		private string testPath;
		private string testContent;
		public FileSystemProviderTests()
		{
			testPath = Path.Combine(Path.GetTempPath(),"test");
			testContent = "test content!";
			if(File.Exists(testPath)) File.Delete(testPath);
			File.AppendAllText(testPath,testContent);
		}
		void IDisposable.Dispose()
		{
			File.Delete(testPath);
		}
		
		private ExecutionContext GetExecutionContext()
		{
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
            PSHost hostInterface =  new DefaultHost(currentCulture,currentCulture);
            RunspaceConfiguration runspaceConfiguration =  RunspaceConfiguration.Create();
            InitialSessionState iss = InitialSessionState.CreateDefault2();
            AutomationEngine engine = new AutomationEngine(hostInterface, runspaceConfiguration, iss);
            ExecutionContext executionContext = new ExecutionContext(engine, hostInterface, iss);
			return executionContext;
		}
		private ProviderInfo GetProvider()
        {
            ExecutionContext executionContext = GetExecutionContext();
            SessionStateInternal sessionState = new SessionStateInternal(executionContext);
			
			SessionStateProviderEntry providerEntry = new SessionStateProviderEntry("FileSystem",typeof(FileSystemProvider), null);
			sessionState.AddSessionStateEntry(providerEntry);
            ProviderInfo matchingProvider = sessionState.ProviderList.ToList()[0];

            return matchingProvider;
        }
		
        [Fact]
        public void TestCreateJunctionFails()
        {
            Assert.False(InternalSymbolicLinkLinkCodeMethods.CreateJunction("",""));
        }
		
		[Fact]
        public void TestGetHelpMaml()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			Assert.Equal(fileSystemProvider.GetHelpMaml(String.Empty,String.Empty),String.Empty);
			Assert.Equal(fileSystemProvider.GetHelpMaml("helpItemName",String.Empty),String.Empty);
			Assert.Equal(fileSystemProvider.GetHelpMaml(String.Empty,"path"),String.Empty);
        }
		
		[Fact]
        public void TestMode()
        {
			Assert.Equal(FileSystemProvider.Mode(null),String.Empty);
			FileSystemInfo directoryObject = new DirectoryInfo(@"/");
			FileSystemInfo fileObject = new FileInfo(@"/etc/hosts");
			FileSystemInfo executableObject = new FileInfo(@"/bin/echo");
			Assert.Equal(FileSystemProvider.Mode(PSObject.AsPSObject(directoryObject)).Replace("r","-"),"d-----");
			Assert.Equal(FileSystemProvider.Mode(PSObject.AsPSObject(fileObject)).Replace("r","-"),"------");
			Assert.Equal(FileSystemProvider.Mode(PSObject.AsPSObject(executableObject)).Replace("r","-"),"------");
        }
		
		[Fact]
        public void TestGetProperty()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			PSObject pso=new PSObject();
			pso.AddOrSetProperty("IsReadOnly",false);
			fileSystemProvider.SetProperty(testPath, pso);
			fileSystemProvider.GetProperty(testPath, new Collection<string>(){"IsReadOnly"});
			FileInfo fileSystemObject1 = new FileInfo(testPath);
			PSObject psobject1=PSObject.AsPSObject(fileSystemObject1);
			foreach(PSPropertyInfo property in psobject1.Properties)
			{
				if(property.Name == "IsReadOnly")
				{
					Assert.Equal(property.Value,false);
				}
			}
        }
		
		[Fact]
        public void TestSetProperty()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			fileSystemProvider.GetProperty(testPath, new Collection<string>(){"Name"});
			FileInfo fileSystemObject1 = new FileInfo(testPath);
			PSObject psobject1=PSObject.AsPSObject(fileSystemObject1);
			foreach(PSPropertyInfo property in psobject1.Properties)
			{
				if(property.Name == "Name")
				{
					Assert.Equal(property.Value,"test");
				}
			}
        }
		
		[Fact]
        public void TestClearProperty()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			fileSystemProvider.ClearProperty(testPath, new Collection<string>(){"Attributes"});
        }
		
		[Fact]
        public void TestGetContentReader()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			
			IContentReader contentReader = fileSystemProvider.GetContentReader(testPath);
			Assert.Equal(contentReader.Read(1)[0],testContent);
			contentReader.Close();
        }
		
		[Fact]
        public void TestGetContentWriter()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			
			IContentWriter contentWriter = fileSystemProvider.GetContentWriter(testPath);
			contentWriter.Write(new List<string>(){"contentWriterTestContent"});
			contentWriter.Close();
			Assert.Equal(File.ReadAllText(testPath), testContent+@"contentWriterTestContent"+ System.Environment.NewLine);
        }
		
		[Fact]
        public void TestClearContent()
        {
			FileSystemProvider fileSystemProvider = new FileSystemProvider();
			ProviderInfo providerInfoToSet = GetProvider();
			fileSystemProvider.SetProviderInformation(providerInfoToSet);
			fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
			fileSystemProvider.ClearContent(testPath);
			Assert.Empty(File.ReadAllText(testPath));
        }
    }
}
