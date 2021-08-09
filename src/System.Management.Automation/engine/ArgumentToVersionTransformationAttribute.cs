// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Management.Automation
{
    /// <summary>
    /// To make it easier to specify a version, we add some conversions that wouldn't happen otherwise:
    ///   * A simple integer, i.e. 2
    ///   * A string without a dot, i.e. "2"
    /// </summary>
    public class ArgumentToVersionTransformationAttribute : ArgumentTransformationAttribute
    {
        ///<inheritdoc/>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            object version = PSObject.Base(inputData);

            if (version is string versionStr && TryString(versionStr, inputData, out var fromStr))
            {
                return fromStr;
            }

            if (version is double versionDbl && TryDouble(versionDbl, inputData, out var fromDbl))
            {
                return fromDbl;
            }

            if (LanguagePrimitives.TryConvertTo<int>(version, out var majorVersion))
            {
                return new Version(majorVersion, 0);
            }

            return inputData;
        }

        /// <summary>
        /// Try to convert string input
        /// </summary>
        /// <param name="versionString"><see cref="string"/> input data</param>
        /// <param name="inputData">Original value</param>
        /// <param name="version">Parsed <see cref="Version"/></param>
        /// <returns>true if conversion succeeds</returns>
        protected virtual bool TryString(string versionString, object inputData, [NotNullWhen(true)] out object? version)
        {
            if (versionString.Contains('.'))
            {
                version = inputData;
                return true;
            } 

            version = null;
            return false;
        }

        /// <summary>
        /// Try to convert double input
        /// </summary>
        /// <param name="versionDouble"><see cref="double"/> intput data</param>
        /// <param name="inputData">Original value</param>
        /// <param name="version">Parsed <see cref="Version"/></param>
        /// <returns>true if conversion succeeds</returns>
        protected virtual bool TryDouble(double versionDouble, object inputData, [NotNullWhen(true)] out object? version)
        {
            // just return as is
            version = inputData;
            return true;
        }
    }
}
