// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Language;

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
#if !UNIX
        [ArgumentCompleter(typeof(PropertyTypeArgumentCompleter))]
#endif
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

#if !UNIX
    /// <summary>
    /// Provides argument completion for PropertyType parameter.
    /// </summary>
    public class PropertyTypeArgumentCompleter : IArgumentCompleter
    {
        private static readonly CompletionHelpers.CompletionDisplayInfoMapper RegistryPropertyTypeDisplayInfoMapper = registryPropertyType => registryPropertyType switch
        {
            "String" => (
                ToolTip: TabCompletionStrings.RegistryStringToolTip,
                ListItemText: "String"),
            "ExpandString" => (
                ToolTip: TabCompletionStrings.RegistryExpandStringToolTip,
                ListItemText: "ExpandString"),
            "Binary" => (
                ToolTip: TabCompletionStrings.RegistryBinaryToolTip,
                ListItemText: "Binary"),
            "DWord" => (
                ToolTip: TabCompletionStrings.RegistryDWordToolTip,
                ListItemText: "DWord"),
            "MultiString" => (
                ToolTip: TabCompletionStrings.RegistryMultiStringToolTip,
                ListItemText: "MultiString"),
            "QWord" => (
                ToolTip: TabCompletionStrings.RegistryQWordToolTip,
                ListItemText: "QWord"),
            _ => (
                ToolTip: TabCompletionStrings.RegistryUnknownToolTip,
                ListItemText: "Unknown"),
        };

        private static readonly IReadOnlyList<string> s_RegistryPropertyTypes = new List<string>(capacity: 7)
        {
            "String",
            "ExpandString",
            "Binary",
            "DWord",
            "MultiString",
            "QWord",
            "Unknown"
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
                => IsRegistryProvider(fakeBoundParameters)
                    ? CompletionHelpers.GetMatchingResults(
                        wordToComplete,
                        possibleCompletionValues: s_RegistryPropertyTypes,
                        displayInfoMapper: RegistryPropertyTypeDisplayInfoMapper,
                        resultType: CompletionResultType.ParameterValue)
                    : [];

        /// <summary>
        /// Checks if parameter paths are from Registry provider.
        /// </summary>
        /// <param name="fakeBoundParameters">The fake bound parameters.</param>
        /// <returns>Boolean indicating if paths are from Registry Provider.</returns>
        private static bool IsRegistryProvider(IDictionary fakeBoundParameters)
        {
            Collection<PathInfo> paths;

            if (fakeBoundParameters.Contains("Path"))
            {
                paths = ResolvePath(fakeBoundParameters["Path"], isLiteralPath: false);
            }
            else if (fakeBoundParameters.Contains("LiteralPath"))
            {
                paths = ResolvePath(fakeBoundParameters["LiteralPath"], isLiteralPath: true);
            }
            else
            {
                paths = ResolvePath(@".\", isLiteralPath: false);
            }

            return paths.Count > 0 && paths[0].Provider.NameEquals("Registry");
        }

        /// <summary>
        /// Resolve path or literal path using Resolve-Path.
        /// </summary>
        /// <param name="path">The path to resolve.</param>
        /// <param name="isLiteralPath">Specifies if path is literal path.</param>
        /// <returns>Collection of Pathinfo objects.</returns>
        private static Collection<PathInfo> ResolvePath(object path, bool isLiteralPath)
        {
            using var ps = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace);

            ps.AddCommand("Microsoft.PowerShell.Management\\Resolve-Path");
            ps.AddParameter(isLiteralPath ? "LiteralPath" : "Path", path);

            Collection<PathInfo> output = ps.Invoke<PathInfo>();

            return output;
        }
    }
#endif
}
