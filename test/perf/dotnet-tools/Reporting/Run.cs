// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Reporting
{
    public class Run
    {
        public bool Hidden { get; set; }

        public string CorrelationId { get; set; }

        public string PerfRepoHash { get; set; }

        public string Name { get; set; }

        public string Queue { get; set; }
        public IDictionary<string, string> Configurations { get; set; } = new Dictionary<string, string>();
    }
}
