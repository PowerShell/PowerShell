// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reporting
{
    public class Test
    {
        public IList<string> Categories { get; set; } = new List<string>();

        public string Name { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();

        public IList<Counter> Counters { get; set; } = new List<Counter>();

        public void AddCounter(Counter counter)
        {
            if (counter.DefaultCounter && Counters.Any(c => c.DefaultCounter))
            {
                throw new Exception($"Duplicate default counter, name: ${counter.Name}");
            }

            if (Counters.Any(c => c.Name.Equals(counter.Name)))
            {
                throw new Exception($"Duplicate counter name, name: ${counter.Name}");
            }

            Counters.Add(counter);
        }

        public void AddCounter(IEnumerable<Counter> counters)
        {
            foreach (var counter in counters)
                AddCounter(counter);
        }
    }
}
