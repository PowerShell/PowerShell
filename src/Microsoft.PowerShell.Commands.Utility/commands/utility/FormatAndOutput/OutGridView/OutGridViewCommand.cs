// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Enum for SelectionMode parameter.
    /// </summary>
    public enum OutputModeOption
    {
        /// <summary>
        /// None is the default and it means OK and Cancel will not be present
        /// and no objects will be written to the pipeline.
        /// The selectionMode of the actual list will still be multiple.
        /// </summary>
        None,
        /// <summary>
        /// Allow selection of one single item to be written to the pipeline.
        /// </summary>
        Single,
        /// <summary>
        ///Allow select of multiple items to be written to the pipeline.
        /// </summary>
        Multiple
    }

    /// <summary>
    /// Implementation for the Out-GridView command.
    /// </summary>
    [Cmdlet(VerbsData.Out, "GridView", DefaultParameterSetName = "PassThru", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2109378")]
    public class OutGridViewCommand : PSCmdlet, IDisposable
    {
        #region Properties

        private const string DataNotQualifiedForGridView = "DataNotQualifiedForGridView";
        private const string RemotingNotSupported = "RemotingNotSupported";

        private TypeInfoDataBase _typeInfoDataBase;
        private PSPropertyExpressionFactory _expressionFactory;
        private OutWindowProxy _windowProxy;
        private GridHeader _gridHeader;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OutGridViewCommand"/> class.
        /// </summary>
        public OutGridViewCommand()
        {
        }

        #endregion Constructors

        #region Input Parameters

        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; } = AutomationNull.Value;

        /// <summary>
        /// Gets/sets the title of the Out-GridView window.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Title { get; set; }

        /// <summary>
        /// Get or sets a value indicating whether the cmdlet should wait for the window to be closed.
        /// </summary>
        [Parameter(ParameterSetName = "Wait")]
        public SwitchParameter Wait { get; set; }

        /// <summary>
        /// Get or sets a value indicating whether the selected items should be written to the pipeline
        /// and if it should be possible to select multiple or single list items.
        /// </summary>
        [Parameter(ParameterSetName = "OutputMode")]
        public OutputModeOption OutputMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the selected items should be written to the pipeline.
        /// Setting this to true is the same as setting the OutputMode to Multiple.
        /// </summary>
        [Parameter(ParameterSetName = "PassThru")]
        public SwitchParameter PassThru
        {
            get { return OutputMode == OutputModeOption.Multiple ? new SwitchParameter(true) : new SwitchParameter(false); }

            set { this.OutputMode = value.IsPresent ? OutputModeOption.Multiple : OutputModeOption.None; }
        }

        #endregion Input Parameters

        #region Public Methods

        /// <summary>
        /// Provides a one-time, pre-processing functionality for the cmdlet.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Set up the ExpressionFactory
            _expressionFactory = new PSPropertyExpressionFactory();

            // If the value of the Title parameter is valid, use it as a window's title.
            if (this.Title != null)
            {
                _windowProxy = new OutWindowProxy(this.Title, OutputMode, this);
            }
            else
            {
                // Using the command line as a title.
                _windowProxy = new OutWindowProxy(this.MyInvocation.Line, OutputMode, this);
            }

            // Load the Type info database.
            _typeInfoDataBase = this.Context.FormatDBManager.GetTypeInfoDataBase();
        }

        /// <summary>
        /// Blocks depending on the wait and selected.
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();

            if (_windowProxy == null)
            {
                return;
            }

            // If -Wait is used or outputMode is not None we have to wait for the window to be closed
            // The pipeline will be blocked while we don't return
            if (this.Wait || this.OutputMode != OutputModeOption.None)
            {
                _windowProxy.BlockUntilClosed();
            }

            // Output selected items to pipeline.
            List<PSObject> selectedItems = _windowProxy.GetSelectedItems();
            if (this.OutputMode != OutputModeOption.None && selectedItems != null)
            {
                foreach (PSObject selectedItem in selectedItems)
                {
                    if (selectedItem == null)
                    {
                        continue;
                    }

                    PSPropertyInfo originalObjectProperty = selectedItem.Properties[OutWindowProxy.OriginalObjectPropertyName];
                    if (originalObjectProperty == null)
                    {
                        return;
                    }

                    this.WriteObject(originalObjectProperty.Value, false);
                }
            }
        }

        /// <summary>
        /// Provides a record-by-record processing functionality for the cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (InputObject == null || InputObject == AutomationNull.Value)
            {
                return;
            }

            if (InputObject.BaseObject is IDictionary dictionary)
            {
                // Dictionaries should be enumerated through because the pipeline does not enumerate through them.
                foreach (DictionaryEntry entry in dictionary)
                {
                    ProcessObject(PSObjectHelper.AsPSObject(entry));
                }
            }
            else
            {
                ProcessObject(InputObject);
            }
        }

        /// <summary>
        /// StopProcessing is called close the window when Ctrl+C in the command prompt.
        /// </summary>
        protected override void StopProcessing()
        {
            if (this.Wait || this.OutputMode != OutputModeOption.None)
            {
                _windowProxy.CloseWindow();
            }
        }

        /// <summary>
        /// Converts the provided PSObject to a string preserving PowerShell formatting.
        /// </summary>
        /// <param name="liveObject">PSObject to be converted to a string.</param>
        internal string ConvertToString(PSObject liveObject)
        {
            StringFormatError formatErrorObject = new();
            string smartToString = PSObjectHelper.SmartToString(liveObject,
                                                                _expressionFactory,
                                                                InnerFormatShapeCommand.FormatEnumerationLimit(),
                                                                formatErrorObject);
            if (formatErrorObject.exception != null)
            {
                // There was a formatting error that should be sent to the console.
                this.WriteError(
                    new ErrorRecord(
                        formatErrorObject.exception,
                        "ErrorFormattingType",
                        ErrorCategory.InvalidResult,
                        liveObject)
                    );
            }

            return smartToString;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Execute formatting on a single object.
        /// </summary>
        /// <param name="input">Object to process.</param>
        private void ProcessObject(PSObject input)
        {
            // Make sure the OGV window is not closed.
            if (_windowProxy.IsWindowClosed())
            {
                LocalPipeline pipeline = (LocalPipeline)this.Context.CurrentRunspace.GetCurrentlyRunningPipeline();

                if (pipeline != null && !pipeline.IsStopping)
                {
                    // Stop the pipeline cleanly.
                    pipeline.StopAsync();
                }

                return;
            }

            object baseObject = input.BaseObject;

            // Throw a terminating error for types that are not supported.
            if (baseObject is ScriptBlock ||
                baseObject is SwitchParameter ||
                baseObject is PSReference ||
                baseObject is FormatInfoData ||
                baseObject is PSObject)
            {
                ErrorRecord error = new(
                    new FormatException(StringUtil.Format(FormatAndOut_out_gridview.DataNotQualifiedForGridView)),
                    DataNotQualifiedForGridView,
                    ErrorCategory.InvalidType,
                    null);

                this.ThrowTerminatingError(error);
            }

            if (_gridHeader == null)
            {
                // Columns have not been added yet; Start the main window and add columns.
                _windowProxy.ShowWindow();
                _gridHeader = GridHeader.ConstructGridHeader(input, this);
            }
            else
            {
                _gridHeader.ProcessInputObject(input);
            }

            // Some thread synchronization needed.
            Exception exception = _windowProxy.GetLastException();
            if (exception != null)
            {
                ErrorRecord error = new(
                    exception,
                    "ManagementListInvocationException",
                    ErrorCategory.OperationStopped,
                    null);

                this.ThrowTerminatingError(error);
            }
        }

        #endregion Private Methods

        internal abstract class GridHeader
        {
            protected OutGridViewCommand parentCmd;

            internal GridHeader(OutGridViewCommand parentCmd)
            {
                this.parentCmd = parentCmd;
            }

            internal static GridHeader ConstructGridHeader(PSObject input, OutGridViewCommand parentCmd)
            {
                if (DefaultScalarTypes.IsTypeInList(input.TypeNames) ||
                    !OutOfBandFormatViewManager.HasNonRemotingProperties(input))
                {
                    return new ScalarTypeHeader(parentCmd, input);
                }

                return new NonscalarTypeHeader(parentCmd, input);
            }

            internal abstract void ProcessInputObject(PSObject input);
        }

        internal sealed class ScalarTypeHeader : GridHeader
        {
            private readonly Type _originalScalarType;

            internal ScalarTypeHeader(OutGridViewCommand parentCmd, PSObject input) : base(parentCmd)
            {
                _originalScalarType = input.BaseObject.GetType();

                // On scalar types the type name is used as a column name.
                this.parentCmd._windowProxy.AddColumnsAndItem(input);
            }

            internal override void ProcessInputObject(PSObject input)
            {
                if (!_originalScalarType.Equals(input.BaseObject.GetType()))
                {
                    parentCmd._gridHeader = new HeteroTypeHeader(base.parentCmd, input);
                }
                else
                {
                    // Columns are already added; Add the input PSObject as an item to the underlying Management List.
                    base.parentCmd._windowProxy.AddItem(input);
                }
            }
        }

        internal sealed class NonscalarTypeHeader : GridHeader
        {
            private readonly AppliesTo _appliesTo = null;

            internal NonscalarTypeHeader(OutGridViewCommand parentCmd, PSObject input) : base(parentCmd)
            {
                // Prepare a table view.
                TableView tableView = new();
                tableView.Initialize(parentCmd._expressionFactory, parentCmd._typeInfoDataBase);

                // Request a view definition from the type database.
                ViewDefinition viewDefinition = DisplayDataQuery.GetViewByShapeAndType(parentCmd._expressionFactory, parentCmd._typeInfoDataBase, FormatShape.Table, input.TypeNames, null);
                if (viewDefinition != null)
                {
                    // Create a header using a view definition provided by the types database.
                    parentCmd._windowProxy.AddColumnsAndItem(input, tableView, (TableControlBody)viewDefinition.mainControl);

                    // Remember all type names and type groups the current view applies to.
                    _appliesTo = viewDefinition.appliesTo;
                }
                else
                {
                    // Create a header using only the input object's properties.
                    parentCmd._windowProxy.AddColumnsAndItem(input, tableView);
                    _appliesTo = new AppliesTo();

                    // Add all type names except for Object and MarshalByRefObject types because they are too generic.
                    // Leave the Object type name if it is the only type name.
                    int index = 0;
                    foreach (string typeName in input.TypeNames)
                    {
                        if (index > 0 && (typeName.Equals(typeof(object).FullName, StringComparison.OrdinalIgnoreCase) ||
                            typeName.Equals(typeof(MarshalByRefObject).FullName, StringComparison.OrdinalIgnoreCase)))
                        {
                            break;
                        }

                        _appliesTo.AddAppliesToType(typeName);
                        index++;
                    }
                }
            }

            internal override void ProcessInputObject(PSObject input)
            {
                // Find out if the input has matching types in the this.appliesTo collection.
                foreach (TypeOrGroupReference typeOrGroupRef in _appliesTo.referenceList)
                {
                    if (typeOrGroupRef is TypeReference)
                    {
                        // Add deserialization prefix.
                        string deserializedTypeName = typeOrGroupRef.name;
                        Deserializer.AddDeserializationPrefix(ref deserializedTypeName);

                        for (int i = 0; i < input.TypeNames.Count; i++)
                        {
                            if (typeOrGroupRef.name.Equals(input.TypeNames[i], StringComparison.OrdinalIgnoreCase)
                                || deserializedTypeName.Equals(input.TypeNames[i], StringComparison.OrdinalIgnoreCase))
                            {
                                // Current view supports the input's Type;
                                // Add the input PSObject as an item to the underlying Management List.
                                base.parentCmd._windowProxy.AddItem(input);
                                return;
                            }
                        }
                    }
                    else
                    {
                        // Find out if the input's Type belongs to the current TypeGroup.
                        // TypeGroupReference has only a group's name, so use the database to get through all actual TypeGroup's.
                        List<TypeGroupDefinition> typeGroupList = base.parentCmd._typeInfoDataBase.typeGroupSection.typeGroupDefinitionList;
                        foreach (TypeGroupDefinition typeGroup in typeGroupList)
                        {
                            if (typeGroup.name.Equals(typeOrGroupRef.name, StringComparison.OrdinalIgnoreCase))
                            {
                                // A matching TypeGroup is found in the database.
                                // Find out if the input's Type belongs to this TypeGroup.
                                foreach (TypeReference typeRef in typeGroup.typeReferenceList)
                                {
                                    // Add deserialization prefix.
                                    string deserializedTypeName = typeRef.name;
                                    Deserializer.AddDeserializationPrefix(ref deserializedTypeName);

                                    if (input.TypeNames.Count > 0
                                        && (typeRef.name.Equals(input.TypeNames[0], StringComparison.OrdinalIgnoreCase)
                                            || deserializedTypeName.Equals(input.TypeNames[0], StringComparison.OrdinalIgnoreCase)))
                                    {
                                        // Current view supports the input's Type;
                                        // Add the input PSObject as an item to the underlying Management List.
                                        base.parentCmd._windowProxy.AddItem(input);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                // The input's Type is not supported by the current view;
                // Switch to the Hetero Type view.
                parentCmd._gridHeader = new HeteroTypeHeader(base.parentCmd, input);
            }
        }

        internal sealed class HeteroTypeHeader : GridHeader
        {
            internal HeteroTypeHeader(OutGridViewCommand parentCmd, PSObject input) : base(parentCmd)
            {
                // Clear all existed columns and add Type and Value columns.
                this.parentCmd._windowProxy.AddHeteroViewColumnsAndItem(input);
            }

            internal override void ProcessInputObject(PSObject input)
            {
                this.parentCmd._windowProxy.AddHeteroViewItem(input);
            }
        }

        /// <summary>
        /// Implements IDisposable logic.
        /// </summary>
        /// <param name="isDisposing">True if being called from Dispose.</param>
        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (_windowProxy != null)
                {
                    _windowProxy.Dispose();
                    _windowProxy = null;
                }
            }
        }

        /// <summary>
        /// Dispose method in IDisposable.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
