//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("Microsoft.PackageManagement")]
[assembly: AssemblyDescription("PackageManagement Core")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("PackageManagement")]
[assembly: AssemblyCopyright("Copyright Microsoft © 2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Normally, we wouldn't actually permit access to the internals of the API
// but we're sharing code with the other tightly-coupled providers
// Third-party providers shouldn't use (or want) to be tightly-coupled

[assembly: InternalsVisibleTo("Microsoft.PowerShell.PackageManagement")]
[assembly: InternalsVisibleTo("Microsoft.PackageManagement.Test")]
[assembly: InternalsVisibleTo("Microsoft.PackageManagement.MetaProvider.PowerShell")]
[assembly: InternalsVisibleTo("Microsoft.PackageManagement.CoreProviders")]
[assembly: InternalsVisibleTo("Microsoft.PackageManagement.ArchiverProviders")]
[assembly: InternalsVisibleTo("Microsoft.PackageManagement.MsiProvider")]
[assembly: InternalsVisibleTo("Microsoft.PackageManagement.MsuProvider")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("2335ef65-8af7-4923-8d8b-a9e6943e4ff9")]
