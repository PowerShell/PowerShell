// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;
using Xunit;

namespace PSTests.Parallel
{
    public class FileSystemProviderTests : IDisposable
    {
        private string testPath;
        private string testContent;

        public FileSystemProviderTests()
        {
            testPath = Path.GetTempFileName();
            testContent = "test content!";
            if (File.Exists(testPath))
            {
                File.Delete(testPath);
            }

            File.AppendAllText(testPath, testContent);
        }

        void IDisposable.Dispose()
        {
            File.Delete(testPath);
        }

        private ExecutionContext GetExecutionContext()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            PSHost hostInterface = new DefaultHost(currentCulture, currentCulture);
            InitialSessionState iss = InitialSessionState.CreateDefault2();
            AutomationEngine engine = new AutomationEngine(hostInterface, iss);
            ExecutionContext executionContext = new ExecutionContext(engine, hostInterface, iss);
            return executionContext;
        }

        private ProviderInfo GetProvider()
        {
            ExecutionContext executionContext = GetExecutionContext();
            SessionStateInternal sessionState = new SessionStateInternal(executionContext);

            SessionStateProviderEntry providerEntry = new SessionStateProviderEntry("FileSystem", typeof(FileSystemProvider), null);
            sessionState.AddSessionStateEntry(providerEntry);
            ProviderInfo matchingProvider = sessionState.ProviderList.ToList()[0];

            return matchingProvider;
        }

        [Fact]
        public void TestCreateJunctionFails()
        {
            if (!Platform.IsWindows)
            {
                Assert.False(InternalSymbolicLinkLinkCodeMethods.CreateJunction(string.Empty, string.Empty));
            }
            else
            {
                Assert.Throws<System.ArgumentNullException>(delegate { InternalSymbolicLinkLinkCodeMethods.CreateJunction(string.Empty, string.Empty); });
            }
        }

        [Fact]
        public void TestGetHelpMaml()
        {
            FileSystemProvider fileSystemProvider = new FileSystemProvider();
            Assert.Equal(fileSystemProvider.GetHelpMaml(string.Empty, string.Empty), string.Empty);
            Assert.Equal(fileSystemProvider.GetHelpMaml("helpItemName", string.Empty), string.Empty);
            Assert.Equal(fileSystemProvider.GetHelpMaml(string.Empty, "path"), string.Empty);
        }

        [Fact]
        public void TestMode()
        {
            Assert.Equal(FileSystemProvider.Mode(null), string.Empty);
            FileSystemInfo directoryObject = null;
            FileSystemInfo fileObject = null;
            FileSystemInfo executableObject = null;

            if (!Platform.IsWindows)
            {
                directoryObject = new DirectoryInfo(@"/");
                fileObject = new FileInfo(@"/etc/hosts");
                executableObject = new FileInfo(@"/bin/echo");
            }
            else
            {
                directoryObject = new DirectoryInfo(System.Environment.CurrentDirectory);
                fileObject = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
                executableObject = new FileInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            }

            Assert.Equal("d----", FileSystemProvider.Mode(PSObject.AsPSObject(directoryObject)).Replace("r", "-"));
            Assert.Equal("-----", FileSystemProvider.Mode(PSObject.AsPSObject(fileObject)).Replace("r", "-").Replace("a", "-"));
            Assert.Equal("-----", FileSystemProvider.Mode(PSObject.AsPSObject(executableObject)).Replace("r", "-").Replace("a", "-"));
        }

        [Fact]
        public void TestGetProperty()
        {
            FileSystemProvider fileSystemProvider = new FileSystemProvider();
            ProviderInfo providerInfoToSet = GetProvider();
            fileSystemProvider.SetProviderInformation(providerInfoToSet);
            fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
            PSObject pso = new PSObject();
            pso.AddOrSetProperty("IsReadOnly", false);
            fileSystemProvider.SetProperty(testPath, pso);
            fileSystemProvider.GetProperty(testPath, new Collection<string>(){ "IsReadOnly" });
            FileInfo fileSystemObject1 = new FileInfo(testPath);
            PSObject psobject1 = PSObject.AsPSObject(fileSystemObject1);
            PSPropertyInfo property = psobject1.Properties["IsReadOnly"];
            Assert.False((bool)property.Value);
        }

        [Fact]
        public void TestSetProperty()
        {
            FileSystemProvider fileSystemProvider = new FileSystemProvider();
            ProviderInfo providerInfoToSet = GetProvider();
            fileSystemProvider.SetProviderInformation(providerInfoToSet);
            fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
            fileSystemProvider.GetProperty(testPath, new Collection<string>(){ "Name" });
            FileInfo fileSystemObject1 = new FileInfo(testPath);
            PSObject psobject1 = PSObject.AsPSObject(fileSystemObject1);
            PSPropertyInfo property = psobject1.Properties["FullName"];

            Assert.Equal(testPath, property.Value);
        }

        [Fact]
        public void TestClearProperty()
        {
            FileSystemProvider fileSystemProvider = new FileSystemProvider();
            ProviderInfo providerInfoToSet = GetProvider();
            fileSystemProvider.SetProviderInformation(providerInfoToSet);
            fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());
            fileSystemProvider.ClearProperty(testPath, new Collection<string>(){ "Attributes" });
        }

        [Fact]
        public void TestGetContentReader()
        {
            FileSystemProvider fileSystemProvider = new FileSystemProvider();
            ProviderInfo providerInfoToSet = GetProvider();
            fileSystemProvider.SetProviderInformation(providerInfoToSet);
            fileSystemProvider.Context = new CmdletProviderContext(GetExecutionContext());

            IContentReader contentReader = fileSystemProvider.GetContentReader(testPath);
            Assert.Equal(contentReader.Read(1)[0], testContent);
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
            contentWriter.Write(new List<string>(){ "contentWriterTestContent" });
            contentWriter.Close();
            Assert.Equal(File.ReadAllText(testPath), testContent + @"contentWriterTestContent" + System.Environment.NewLine);
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
