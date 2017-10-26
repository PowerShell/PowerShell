//
//    Copyright (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.PowerShell.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using Microsoft.PowerShell.Commands.Internal.Format;

    internal class ExpressionColumnInfo : ColumnInfo
    {
        private MshExpression _expression;

        internal ExpressionColumnInfo(string staleObjectPropertyName, string displayName, MshExpression expression)
            : base(staleObjectPropertyName, displayName)
        {
            _expression = expression;
        }

        internal override Object GetValue(PSObject liveObject)
        {
            List<MshExpressionResult> resList = _expression.GetValues(liveObject);

            if (resList.Count == 0)
            {
                return null;
            }

            // Only first element is used.
            MshExpressionResult result = resList[0];
            if (result.Exception != null)
            {
                return null;
            }

            object objectResult = result.Result;
            return objectResult == null ? String.Empty : ColumnInfo.LimitString(objectResult.ToString());
        }
    }
}