# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Validate start of console host" -Tag CI {
    BeforeAll {
        $expectedAssemblies = @(
            'Microsoft.ApplicationInsights.dll'
            'Microsoft.Management.Infrastructure.dll'
            'Microsoft.PowerShell.Commands.Management.dll'
            'Microsoft.PowerShell.Commands.Utility.dll'
            'Microsoft.PowerShell.ConsoleHost.dll'
            'Microsoft.PowerShell.MarkdownRender.dll'
            'Microsoft.PowerShell.Security.dll'
            'Microsoft.Win32.Primitives.dll'
            'Microsoft.Win32.Registry.dll'
            'netstandard.dll'
            'Newtonsoft.Json.dll'
            'pwsh.dll'
            'System.Buffers.dll'
            'System.Collections.Concurrent.dll'
            'System.Collections.dll'
            'System.Collections.Immutable.dll'
            'System.Collections.NonGeneric.dll'
            'System.Collections.Specialized.dll'
            'System.ComponentModel.dll'
            'System.ComponentModel.Primitives.dll'
            'System.ComponentModel.TypeConverter.dll'
            'System.Console.dll'
            'System.Data.Common.dll'
            'System.Diagnostics.Debug.dll'
            'System.Diagnostics.DiagnosticSource.dll'
            'System.Diagnostics.FileVersionInfo.dll'
            'System.Diagnostics.Process.dll'
            'System.Diagnostics.StackTrace.dll'
            'System.Diagnostics.Tools.dll'
            'System.Diagnostics.TraceSource.dll'
            'System.Diagnostics.Tracing.dll'
            'System.DirectoryServices.dll'
            'System.Globalization.dll'
            'System.IO.FileSystem.AccessControl.dll'
            'System.IO.FileSystem.dll'
            'System.IO.FileSystem.DriveInfo.dll'
            'System.IO.Pipes.dll'
            'System.Linq.dll'
            'System.Linq.Expressions.dll'
            'System.Management.Automation.dll'
            'System.Memory.dll'
            'System.Net.Http.dll'
            'System.Net.Mail.dll'
            'System.Net.NetworkInformation.dll'
            'System.Net.Primitives.dll'
            'System.ObjectModel.dll'
            'System.Private.CoreLib.dll'
            'System.Private.Uri.dll'
            'System.Private.Xml.dll'
            'System.Private.Xml.Linq.dll'
            'System.Reflection.dll'
            'System.Reflection.Emit.dll'
            'System.Reflection.Emit.ILGeneration.dll'
            'System.Reflection.Emit.Lightweight.dll'
            'System.Reflection.Extensions.dll'
            'System.Reflection.Primitives.dll'
            'System.Resources.ResourceManager.dll'
            'System.Runtime.dll'
            'System.Runtime.Extensions.dll'
            'System.Runtime.InteropServices.dll'
            'System.Runtime.InteropServices.RuntimeInformation.dll'
            'System.Runtime.Loader.dll'
            'System.Runtime.Numerics.dll'
            'System.Runtime.Serialization.Formatters.dll'
            'System.Runtime.Serialization.Primitives.dll'
            'System.Security.AccessControl.dll'
            'System.Security.Claims.dll'
            'System.Security.Cryptography.Algorithms.dll'
            'System.Security.Cryptography.Encoding.dll'
            'System.Security.Cryptography.Primitives.dll'
            'System.Security.Cryptography.X509Certificates.dll'
            'System.Security.Principal.dll'
            'System.Security.Principal.Windows.dll'
            'System.Text.Encoding.CodePages.dll'
            'System.Text.Encoding.Extensions.dll'
            'System.Text.RegularExpressions.dll'
            'System.Threading.dll'
            'System.Threading.AccessControl.dll'
            'System.Threading.Tasks.dll'
            'System.Threading.Tasks.Parallel.dll'
            'System.Threading.Thread.dll'
            'System.Threading.ThreadPool.dll'
            'System.Threading.Timer.dll'
            'System.Xml.ReaderWriter.dll'
            'System.Xml.XDocument.dll'
        )

        if ($IsWindows) {
            $expectedAssemblies += @(
                'Microsoft.Management.Infrastructure.CimCmdlets.dll'
                'Microsoft.PowerShell.CoreCLR.Eventing.dll'
                'System.DirectoryServices.dll'
                'System.Management.dll'
                'System.Reflection.Metadata.dll'
                'System.Security.Permissions.dll'
                'System.Threading.Overlapped.dll'
            )
        }
        else {
            $expectedAssemblies += @(
                'System.IO.MemoryMappedFiles.dll'
                'System.Net.Sockets.dll'
                'System.Reflection.Metadata.dll'            )
        }

        $loadedAssemblies = pwsh -noprofile -command '([System.AppDomain]::CurrentDomain.GetAssemblies()).manifestmodule | ? { $_.Name -notlike ""<*>"" } | % { $_.Name }'
    }

    It "No new assemblies are loaded" {
        foreach ($assembly in $loadedAssemblies) {
            $expectedAssemblies | Should -Contain $assembly
        }
    }
}
