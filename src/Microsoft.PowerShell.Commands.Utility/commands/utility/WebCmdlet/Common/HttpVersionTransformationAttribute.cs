// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    internal sealed class HttpVersionTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            object version = PSObject.Base(inputData);

            if (version is string versionStr && versionStr.Contains('.'))
            {
                // If the string contains a '.', let the Version constructor handle the conversion.
                return inputData;
            }

            if (version is double versionDouble)
            {
                // 1.0 converts to string "1" so we preserve .0 with "0.0" format
                return versionDouble.ToString("0.0", CultureInfo.InvariantCulture);
            }

            if (LanguagePrimitives.TryConvertTo(version, out int majorVersion))
            {
                return new Version(majorVersion, 0);
            }

            return inputData;
        }
    }
}
