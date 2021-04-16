// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Reporting
{
    public class Counter
    {
        public string Name { get; set; }

        public bool TopCounter { get; set; }

        public bool DefaultCounter { get; set; }

        public bool HigherIsBetter { get; set; }

        public string MetricName { get; set; }

        public IList<double> Results { get; set; }
    }
}