// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    internal static class EnumerableExpansionConversion
    {
        internal const string CoreOnlyString = "CoreOnly";
        internal const string EnumOnlyString = "EnumOnly";
        internal const string BothString = "Both";

        internal static bool Convert(string expansionString, out EnumerableExpansion expansion)
        {
            expansion = EnumerableExpansion.EnumOnly;
            if (string.Equals(expansionString, CoreOnlyString, StringComparison.OrdinalIgnoreCase))
            {
                expansion = EnumerableExpansion.CoreOnly;
                return true;
            }

            if (string.Equals(expansionString, EnumOnlyString, StringComparison.OrdinalIgnoreCase))
            {
                expansion = EnumerableExpansion.EnumOnly;
                return true;
            }

            if (string.Equals(expansionString, BothString, StringComparison.OrdinalIgnoreCase))
            {
                expansion = EnumerableExpansion.Both;
                return true;
            }

            return false;
        }
    }
}

