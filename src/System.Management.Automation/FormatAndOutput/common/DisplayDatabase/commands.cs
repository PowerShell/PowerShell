/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

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
            if (String.Equals(expansionString, CoreOnlyString, StringComparison.OrdinalIgnoreCase))
            {
                expansion = EnumerableExpansion.CoreOnly;
                return true;
            }
            if (String.Equals(expansionString, EnumOnlyString, StringComparison.OrdinalIgnoreCase))
            {
                expansion = EnumerableExpansion.EnumOnly;
                return true;
            }
            if (String.Equals(expansionString, BothString, StringComparison.OrdinalIgnoreCase))
            {
                expansion = EnumerableExpansion.Both;
                return true;
            }
            return false;
        }
    }
}

