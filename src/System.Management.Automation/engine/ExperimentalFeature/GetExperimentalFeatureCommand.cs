// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Implements Get-ExperimentalFeature cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ExperimentalFeature", HelpUri = "https://go.microsoft.com/fwlink/?linkid=2007351")]
    public class GetExperimentalFeatureCommand : PSCmdlet
    {
        /// <summary>
        /// Get and set the feature names.
        /// </summary>
        [Parameter(ValueFromPipeline = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string[] Name { get; set; }

        /// <summary>
        /// Get and set the switch flag to search module paths to find all available experimental features.
        /// </summary>
        [Parameter]
        public SwitchParameter ListAvailable { get; set; }

        /// <summary>
        /// ProcessRecord method of this cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            const WildcardOptions wildcardOptions = WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant;
            IEnumerable<WildcardPattern> namePatterns = SessionStateUtilities.CreateWildcardsFromStrings(Name, wildcardOptions);

            if (ListAvailable)
            {
                foreach (ExperimentalFeature feature in GetAvailableExperimentalFeatures(namePatterns).OrderBy(GetSortingString))
                {
                    WriteObject(feature);
                }
            }
            else if (ExperimentalFeature.EnabledExperimentalFeatureNames.Count > 0)
            {
                foreach (ExperimentalFeature feature in GetEnabledExperimentalFeatures(namePatterns).OrderBy(GetSortingString))
                {
                    WriteObject(feature);
                }
            }
        }

        /// <summary>
        /// Construct the string for sorting experimental feature records.
        /// </summary>
        /// <remarks>
        /// Engine features come before module features.
        /// Within engine features and module features, features are ordered by name.
        /// </remarks>
        private static (int, string) GetSortingString(ExperimentalFeature feature)
        {
            return ExperimentalFeature.EngineSource.Equals(feature.Source, StringComparison.OrdinalIgnoreCase)
                        ? (0, feature.Name)
                        : (1, feature.Name);
        }

        /// <summary>
        /// Get enabled experimental features based on the specified name patterns.
        /// </summary>
        private IEnumerable<ExperimentalFeature> GetEnabledExperimentalFeatures(IEnumerable<WildcardPattern> namePatterns)
        {
            var moduleFeatures = new List<string>();
            var moduleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string featureName in ExperimentalFeature.EnabledExperimentalFeatureNames)
            {
                // Only process the feature names that matches any name patterns.
                if (SessionStateUtilities.MatchesAnyWildcardPattern(featureName, namePatterns, defaultValue: true))
                {
                    if (ExperimentalFeature.EngineExperimentalFeatureMap.TryGetValue(featureName, out ExperimentalFeature feature))
                    {
                        yield return feature;
                    }
                    else
                    {
                        moduleFeatures.Add(featureName);
                        int lastDotIndex = featureName.LastIndexOf('.');
                        moduleNames.Add(featureName.Substring(0, lastDotIndex));
                    }
                }
            }

            if (moduleFeatures.Count > 0)
            {
                var featuresFromGivenModules = new Dictionary<string, ExperimentalFeature>(StringComparer.OrdinalIgnoreCase);
                foreach (string moduleFile in GetValidModuleFiles(moduleNames))
                {
                    foreach (var feature in ModuleIntrinsics.GetExperimentalFeature(moduleFile))
                    {
                        featuresFromGivenModules.TryAdd(feature.Name, feature);
                    }
                }

                foreach (string featureName in moduleFeatures)
                {
                    if (featuresFromGivenModules.TryGetValue(featureName, out ExperimentalFeature feature))
                    {
                        yield return feature;
                    }
                    else
                    {
                        yield return new ExperimentalFeature(featureName, description: null, source: null, isEnabled: true);
                    }
                }
            }
        }

        /// <summary>
        /// Get available experimental features based on the specified name patterns.
        /// </summary>
        private IEnumerable<ExperimentalFeature> GetAvailableExperimentalFeatures(IEnumerable<WildcardPattern> namePatterns)
        {
            foreach (ExperimentalFeature feature in ExperimentalFeature.EngineExperimentalFeatures)
            {
                if (SessionStateUtilities.MatchesAnyWildcardPattern(feature.Name, namePatterns, defaultValue: true))
                {
                    yield return feature;
                }
            }

            foreach (string moduleFile in GetValidModuleFiles(moduleNamesToFind: null))
            {
                ExperimentalFeature[] features = ModuleIntrinsics.GetExperimentalFeature(moduleFile);
                foreach (var feature in features)
                {
                    if (SessionStateUtilities.MatchesAnyWildcardPattern(feature.Name, namePatterns, defaultValue: true))
                    {
                        yield return feature;
                    }
                }
            }
        }

        /// <summary>
        /// Get valid module files from module paths.
        /// </summary>
        private IEnumerable<string> GetValidModuleFiles(HashSet<string> moduleNamesToFind)
        {
            var modulePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in ModuleIntrinsics.GetModulePath(includeSystemModulePath: false, Context))
            {
                string uniquePath = path.TrimEnd(Utils.Separators.Directory);
                if (!modulePaths.Add(uniquePath)) { continue; }

                foreach (string moduleFile in ModuleUtils.GetDefaultAvailableModuleFiles(uniquePath))
                {
                    // We only care about module manifest files because that's where experimental features are declared.
                    if (!moduleFile.EndsWith(StringLiterals.PowerShellDataFileExtension, StringComparison.OrdinalIgnoreCase)) { continue; }

                    if (moduleNamesToFind != null)
                    {
                        string currentModuleName = ModuleIntrinsics.GetModuleName(moduleFile);
                        if (!moduleNamesToFind.Contains(currentModuleName)) { continue; }
                    }

                    yield return moduleFile;
                }
            }
        }
    }
}
