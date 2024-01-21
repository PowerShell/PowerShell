// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Management.Automation;
using System.Management.Automation.Language;

using Microsoft.Win32;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to create a new property on an object.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ItemProperty", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096813")]
    public class NewItemPropertyCommand : ItemPropertyCommandBase
    {
        #region Parameters

        /// <summary>
        /// Gets or sets the path parameter to the command.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = "Path", Mandatory = true)]
        public string[] Path
        {
            get
            {
                return paths;
            }

            set
            {
                paths = value;
            }
        }

        /// <summary>
        /// Gets or sets the literal path parameter to the command.
        /// </summary>
        [Parameter(ParameterSetName = "LiteralPath",
                   Mandatory = true, ValueFromPipeline = false, ValueFromPipelineByPropertyName = true)]
        [Alias("PSPath", "LP")]
        public string[] LiteralPath
        {
            get
            {
                return paths;
            }

            set
            {
                base.SuppressWildcardExpansion = true;
                paths = value;
            }
        }

        /// <summary>
        /// The name of the property to create on the item.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        [Alias("PSProperty")]
        public string Name { get; set; }

        /// <summary>
        /// The type of the property to create on the item.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("Type")]
        [ArgumentCompleter(typeof(PropertyTypeArgumentCompleter))]
        public string PropertyType { get; set; }

        /// <summary>
        /// The value of the property to create on the item.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the force property.
        /// </summary>
        /// <remarks>
        /// Gives the provider guidance on how vigorous it should be about performing
        /// the operation. If true, the provider should do everything possible to perform
        /// the operation. If false, the provider should attempt the operation but allow
        /// even simple errors to terminate the operation.
        /// For example, if the user tries to copy a file to a path that already exists and
        /// the destination is read-only, if force is true, the provider should copy over
        /// the existing read-only file. If force is false, the provider should write an error.
        /// </remarks>
        [Parameter]
        public override SwitchParameter Force
        {
            get
            {
                return base.Force;
            }

            set
            {
                base.Force = value;
            }
        }

        /// <summary>
        /// A virtual method for retrieving the dynamic parameters for a cmdlet. Derived cmdlets
        /// that require dynamic parameters should override this method and return the
        /// dynamic parameter object.
        /// </summary>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <returns>
        /// An object representing the dynamic parameters for the cmdlet or null if there
        /// are none.
        /// </returns>
        internal override object GetDynamicParameters(CmdletProviderContext context)
        {
            if (Path != null && Path.Length > 0)
            {
                return InvokeProvider.Property.NewPropertyDynamicParameters(Path[0], Name, PropertyType, Value, context);
            }

            return InvokeProvider.Property.NewPropertyDynamicParameters(".", Name, PropertyType, Value, context);
        }

        #endregion Parameters

        #region parameter data

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Creates the property on the item.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (string path in Path)
            {
                try
                {
                    InvokeProvider.Property.New(path, Name, PropertyType, Value, CmdletProviderContext);
                }
                catch (PSNotSupportedException notSupported)
                {
                    WriteError(
                        new ErrorRecord(
                            notSupported.ErrorRecord,
                            notSupported));
                    continue;
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                    continue;
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                    continue;
                }
                catch (ItemNotFoundException pathNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            pathNotFound.ErrorRecord,
                            pathNotFound));
                    continue;
                }
            }
        }
        #endregion Command code

    }

    /// <summary>
    /// Provides argument completion for PropertyType parameter.
    /// </summary>
    public class PropertyTypeArgumentCompleter : IArgumentCompleter
    {
        private static readonly OrderedDictionary s_RegistryCompletionResults = new()
        {
            { "String", "A normal string." },
            { "ExpandString", "A string that contains unexpanded references to environment variables that are expanded when the value is retrieved." },
            { "Binary", "Binary data in any form." },
            { "DWord", "A 32-bit binary number." },
            { "MultiString", "An array of strings." },
            { "QWord", "A 64-bit binary number." },
            { "Unknown", "An unsupported registry data type." }
        };

        /// <summary>
        /// Returns completion results for PropertyType parameter.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="wordToComplete">The word to complete.</param>
        /// <param name="commandAst">The command AST.</param>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>List of Completion Results.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            // -PropertyType parameter is only supported on Windows
            if (!Platform.IsWindows)
            {
                yield break;
            }

            Collection<PathInfo> paths;

            // Completion: New-ItemProperty -Path <path> -PropertyType <wordToComplete>
            if (fakeBoundParameters.Contains("Path"))
            {
                paths = ResolvePaths(ConvertParameterPathsToArray(fakeBoundParameters["Path"]), isLiteralPath: false);
            }

            // Completion: New-ItemProperty -LiteralPath <path> -PropertyType <wordToComplete>
            else if (fakeBoundParameters.Contains("LiteralPath"))
            {
                paths = ResolvePaths(ConvertParameterPathsToArray(fakeBoundParameters["LiteralPath"]), isLiteralPath: true);
            }

            // Just exit since we need to be sure we are completing for registry provider
            else
            {
                yield break;
            }

            // Perform completion if path is using registry provider
            if (paths.Count > 0 && paths[0].Provider.NameEquals("Registry"))
            {
                var propertyTypePattern = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);

                foreach (DictionaryEntry completionResult in s_RegistryCompletionResults)
                {
                    string completionText = completionResult.Key.ToString();
                    string toolTip = completionResult.Value.ToString();

                    if (propertyTypePattern.IsMatch(completionText))
                    {
                        yield return new CompletionResult(completionText, completionText, CompletionResultType.ParameterValue, toolTip);
                    }
                }
            }
        }

        /// <summary>
        /// Resolve paths or literal paths using Resolve-Path.
        /// </summary>
        /// <param name="paths">The paths to resolve.</param>
        /// <param name="isLiteralPath">Specifies if paths are literal paths.</param>
        /// <returns>Collection of Pathinfo objects.</returns>
        private static Collection<PathInfo> ResolvePaths(string[] paths, bool isLiteralPath)
        {
            using var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);

            ps.AddCommand("Microsoft.PowerShell.Management\\Resolve-Path");
            ps.AddParameter(isLiteralPath ? "LiteralPath" : "Path", paths);

            Collection<PathInfo> resolvedPaths = ps.Invoke<PathInfo>();

            return resolvedPaths;
        }

        /// <summary>
        /// Converts object path to array of paths.
        /// </summary>
        /// <param name="parameterPath">The object parameter path</param>
        /// <returns>Array of path strings.</returns>
        private static string[] ConvertParameterPathsToArray(object parameterPath)
        {
            Type parameterType = parameterPath.GetType();

            if (parameterType == typeof(string))
            {
                return new string[] { parameterPath.ToString() };
            }

            else if (parameterType.IsArray && parameterType.GetElementType() == typeof(object))
            {
                return Array.ConvertAll((object[])parameterPath, path => path.ToString());
            }

            return Array.Empty<string>();
        }
    }
}
