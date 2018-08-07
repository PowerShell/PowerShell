// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Xml;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Serialization;
using Microsoft.PowerShell;
using Dbg = System.Management.Automation.Diagnostics;
using System.Management.Automation.Remoting;

namespace System.Management.Automation
{
    internal class MISerializer
    {
        internal InternalMISerializer internalSerializer;

        public MISerializer(int depth)
        {
            internalSerializer = new InternalMISerializer(depth); 
        }

        public CimInstance Serialize(object source)
        {
            return internalSerializer.Serialize(source);
        }
    }
}
