// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using Microsoft.Management.Infrastructure.Options;

namespace Microsoft.PowerShell.Cmdletization.Cim
{
    internal class CimCmdletDefinitionContext
    {
        internal CimCmdletDefinitionContext(
            string cmdletizationClassName,
            string cmdletizationClassVersion,
            Version cmdletizationModuleVersion,
            bool supportsShouldProcess,
            IDictionary<string, string> privateData)
        {
            this.CmdletizationClassName = cmdletizationClassName;
            this.CmdletizationClassVersion = cmdletizationClassVersion;
            this.CmdletizationModuleVersion = cmdletizationModuleVersion;
            this.SupportsShouldProcess = supportsShouldProcess;
            _privateData = privateData;
        }

        public string CmdletizationClassName { get; private set; }

        public string CmdletizationClassVersion { get; private set; }

        public Version CmdletizationModuleVersion { get; private set; }

        public bool SupportsShouldProcess { get; private set; }

        private readonly IDictionary<string, string> _privateData;

        private const string QueryLanguageKey = "QueryDialect";
        private bool? _useEnumerateInstancesInsteadOfWql;
        public bool UseEnumerateInstancesInsteadOfWql
        {
            get
            {
                if (!_useEnumerateInstancesInsteadOfWql.HasValue)
                {
                    bool newValue = false;
                    string queryLanguage;
                    if (_privateData != null &&
                        _privateData.TryGetValue(QueryLanguageKey, out queryLanguage) &&
                        queryLanguage.Equals("None", StringComparison.OrdinalIgnoreCase))
                    {
                        newValue = true;
                    }

                    _useEnumerateInstancesInsteadOfWql = newValue;
                }

                return _useEnumerateInstancesInsteadOfWql.Value;
            }
        }

        private const int FallbackDefaultThrottleLimit = 15;
        /* PS> dir 'WSMan:\localhost\Plugin\WMI Provider\Quotas' | ft -auto

               WSManConfig: Microsoft.WSMan.Management\WSMan::localhost\Plugin\WMI Provider\Quotas

            Name                           Value   Type
            ----                           -----   ----
            MaxConcurrentUsers             100     System.String
            MaxConcurrentOperationsPerUser 15      System.String
            MaxConcurrentOperations        1500    System.String
        */

        public int DefaultThrottleLimit
        {
            get
            {
                string defaultThrottleLimitString;
                if (!_privateData.TryGetValue("DefaultThrottleLimit", out defaultThrottleLimitString))
                {
                    return FallbackDefaultThrottleLimit;
                }

                int defaultThrottleLimitInteger;
                if (!LanguagePrimitives.TryConvertTo(defaultThrottleLimitString, CultureInfo.InvariantCulture, out defaultThrottleLimitInteger))
                {
                    return FallbackDefaultThrottleLimit;
                }

                return defaultThrottleLimitInteger;
            }
        }

        public bool ExposeCimNamespaceParameter
        {
            get { return _privateData.ContainsKey("CimNamespaceParameter"); }
        }

        public bool ClientSideWriteVerbose
        {
            get { return _privateData.ContainsKey("ClientSideWriteVerbose"); }
        }

        public bool ClientSideShouldProcess
        {
            get
            {
                return _privateData.ContainsKey("ClientSideShouldProcess");
            }
        }

        private Uri _resourceUri;
        private bool _resourceUriHasBeenCalculated;
        public Uri ResourceUri
        {
            get
            {
                if (!_resourceUriHasBeenCalculated)
                {
                    string newResourceUriString;
                    Uri newResourceUri;
                    if (_privateData != null &&
                        _privateData.TryGetValue("ResourceUri", out newResourceUriString) &&
                        Uri.TryCreate(newResourceUriString, UriKind.RelativeOrAbsolute, out newResourceUri))
                    {
                        _resourceUri = newResourceUri;
                    }

                    _resourceUriHasBeenCalculated = true;
                }

                return _resourceUri;
            }
        }

        public bool SkipTestConnection
        {
            get { return _privateData.ContainsKey("SkipTestConnection"); }
        }

        private CimOperationFlags? _schemaConformanceLevel;
        public CimOperationFlags SchemaConformanceLevel
        {
            get
            {
                if (!_schemaConformanceLevel.HasValue)
                {
                    CimOperationFlags newSchemaConformanceLevel = 0;

                    string schemaConformanceFromCdxml;
                    if (_privateData != null &&
                        _privateData.TryGetValue("TypeInformation", out schemaConformanceFromCdxml))
                    {
                        if (schemaConformanceFromCdxml.Equals("Basic", StringComparison.OrdinalIgnoreCase))
                        {
                            newSchemaConformanceLevel = CimOperationFlags.BasicTypeInformation;
                        }
                        else if (schemaConformanceFromCdxml.Equals("Full", StringComparison.OrdinalIgnoreCase))
                        {
                            newSchemaConformanceLevel = CimOperationFlags.FullTypeInformation;
                        }
                        else if (schemaConformanceFromCdxml.Equals("None", StringComparison.OrdinalIgnoreCase))
                        {
                            newSchemaConformanceLevel = (CimOperationFlags)0x0400; // this magic number should be changed to a named constant, once MI Client .NET API changes for schema support are completed
                        }
                        else if (schemaConformanceFromCdxml.Equals("Standard", StringComparison.OrdinalIgnoreCase))
                        {
                            newSchemaConformanceLevel = (CimOperationFlags)0x0800; // this magic number should be changed to a named constant, once MI Client .NET API changes for schema support are completed
                        }
                    }

                    _schemaConformanceLevel = newSchemaConformanceLevel;
                }

                return _schemaConformanceLevel.Value;
            }
        }
    }
}
