using Xunit;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;
using Microsoft.PowerShell.Commands;

namespace PSTests
{
    [Collection("AssemblyLoadContext")]
    public static class FileSystemProviderTests
    {
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
			//FileSystemProvider fileSystemProvider = new FileSystemProvider();
			//fileSystemProvider.GetProperty(@"/", new Collection<string>(){"Attributes"});
        }
		
		[Fact]
        public static void TestSetProperty()
        {
			//FileSystemProvider fileSystemProvider = new FileSystemProvider();
			//fileSystemProvider.SetProperty(@"/root", PSObject.AsPSObject(propertyToSet));
        }
		
		[Fact]
        public static void TestClearProperty()
        {
			// FileSystemProvider fileSystemProvider = new FileSystemProvider();
			// fileSystemProvider.ClearProperty(@"/test", new Collection<string>(){"Attributes"});
        }
		
		[Fact]
        public static void TestGetContentReader()
        {
			// FileSystemProvider fileSystemProvider = new FileSystemProvider();
			// IContentReader contentReader = fileSystemProvider.GetContentReader(@"/test");
			// Assert.NotNull(contentReader);
        }
		
		[Fact]
        public static void TestGetContentWriter()
        {
			// FileSystemProvider fileSystemProvider = new FileSystemProvider();
			// IContentWriter contentWriter = fileSystemProvider.GetContentWriter(@"/test");
			// Assert.NotNull(contentWriter);
        }
		
		[Fact]
        public static void TestClearContent()
        {
			//FileSystemProvider fileSystemProvider = new FileSystemProvider();
			//fileSystemProvider.ClearContent(@"/test");
        }
    }
}
