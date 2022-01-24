// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation
{
    /// <summary>
    /// To make it easier to specify a version, we add some conversions that wouldn't happen otherwise:
    ///   * A simple integer, i.e. 2;
    ///   * A string without a dot, i.e. "2".
    ///   * the string "off" sets the version to 0.0 (unset)
    ///   * the string "latest" sets to the current version
    /// </summary>
    internal sealed class ArgumentToPSVersionTransformationAttribute : ArgumentToVersionTransformationAttribute
    {
        protected override bool TryConvertFromString(string versionString, [NotNullWhen(true)] out Version? version)
        {
            if (string.Equals("off", versionString, StringComparison.OrdinalIgnoreCase))
            {
                version = new Version(0, 0);
                return true;
            }

            if (string.Equals("latest", versionString, StringComparison.OrdinalIgnoreCase))
            {
                version = PSVersionInfo.PSVersion;
                return true;
            }

            return base.TryConvertFromString(versionString, out version);
        }
    }

    /// <summary>
    /// To make it easier to specify a version, we add some conversions that wouldn't happen otherwise:
    ///   * A simple integer, i.e. 2;
    ///   * A string without a dot, i.e. "2".
    /// </summary>
    internal class ArgumentToVersionTransformationAttribute : ArgumentTransformationAttribute
    {
        /// <inheritdoc/>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            object version = PSObject.Base(inputData);

            if (version is string versionStr)
            {
                if (TryConvertFromString(versionStr, out var convertedVersion))
                {
                    return convertedVersion;
                }

                if (versionStr.Contains('.'))
                {
                    // If the string contains a '.', let the Version constructor handle the conversion.
                    return inputData;
                }
            }

            if (version is double)
            {
                // The conversion to int below is wrong, but the usual conversions will turn
                // the double into a string, so just return the original object.
                return inputData;
            }

            if (LanguagePrimitives.TryConvertTo<int>(version, out var majorVersion))
            {
                return new Version(majorVersion, 0);
            }

            return inputData;
        }

        protected virtual bool TryConvertFromString(string versionString, [NotNullWhen(true)] out Version? version)
        {
            version = null;
            return false;
        }
    }
}
