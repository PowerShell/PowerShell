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
    /// </summary>
    public class ArgumentToVersionTransformationAttribute : ArgumentTransformationAttribute
    {
        private static readonly IReadOnlyDictionary<string, Version> _empty = new Dictionary<string, Version>();

        private readonly IReadOnlyDictionary<string, Version> _map;

        /// <summary>
        /// Initializes a new instance of the ArgumentToVersionTransformationAttribute class with empty version map.
        /// </summary>
        public ArgumentToVersionTransformationAttribute() : this(_empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ArgumentToVersionTransformationAttribute class.
        /// </summary>
        /// <param name="map">Version mapping.</param>
        public ArgumentToVersionTransformationAttribute(IReadOnlyDictionary<string, Version> map)
        {
            _map = map ?? _empty;
        }

        /// <inheritdoc/>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            object version = PSObject.Base(inputData);

            if (version is string versionStr)
            {
                if (TryMap(versionStr, out var fromMap))
                {
                    return fromMap;
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

        private bool TryMap(string versionName, [NotNullWhen(true)] out Version? version)
        {
            return _map.TryGetValue(versionName, out version);
        }
    }
}
