// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    internal class ExpressionColumnInfo : ColumnInfo
    {
        private readonly PSPropertyExpression _expression;

        internal ExpressionColumnInfo(string staleObjectPropertyName, string displayName, PSPropertyExpression expression)
            : base(staleObjectPropertyName, displayName)
        {
            _expression = expression;
        }

        internal override object GetValue(PSObject liveObject)
        {
            List<PSPropertyExpressionResult> resList = _expression.GetValues(liveObject);

            if (resList.Count == 0)
            {
                return null;
            }

            // Only first element is used.
            PSPropertyExpressionResult result = resList[0];
            if (result.Exception != null)
            {
                return null;
            }

            object objectResult = result.Result;
            return objectResult == null ? string.Empty : ColumnInfo.LimitString(objectResult.ToString());
        }
    }
}
