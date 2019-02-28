// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.PowerShell.Commands.Diagnostics.Common
{
    internal static class CommonUtilities
    {
        public static ResourceManager GetResourceManager()
        {
            // this naming pattern is dictated by the dotnet cli
            return new ResourceManager("Microsoft.PowerShell.Commands.Diagnostics.resources.GetEventResources", typeof(CommonUtilities).GetTypeInfo().Assembly);
        }
    }
}

