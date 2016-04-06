//
//    Copyright (C) Microsoft.  All rights reserved.
//
namespace Microsoft.PowerShell.Commands
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using System.Management.Automation;
    using System.Management.Automation.Internal;
    using System.Management.Automation.Runspaces;

    using Microsoft.PowerShell.Commands.Internal.Format;
    using Microsoft.Win32;

    /// <summary>
    /// Enum for SelectionMode parameter.
    /// </summary>
    public enum OutputModeOption
    {
        /// <summary>
        /// None is the default and it means OK and Cancel will not be present
        /// and no objects will be written to the pipeline.
        /// The selectionMode of the actual list will still be multiple
        /// </summary>
        None,
        /// <summary>
        /// Allow selection of one sinlge item to be written to the pipeline.
        /// </summary>
        Single,
        /// <summary>
        ///Allow select of multiple items to be written to the pipeline.
        /// </summary>
        Multiple
    }
	
    /// <summary>
    /// Implementation for the Out-GridView command
    /// </summary>
    [Cmdlet("Out", "GridView", DefaultParameterSetName = "PassThru", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113364")]
    public class OutGridViewCommand : PSCmdlet, IDisposable
    {
        #region Properties

        private const string DataNotQualifiedForGridView = "DataNotQualifiedForGridView";
        private const string RemotingNotSupported = "RemotingNotSupported";
        private TypeInfoDataBase typeInfoDataBase;
        private MshExpressionFactory expressionFactory;
        private OutWindowProxy windowProxy;
        private GridHeader gridHeader;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Constructor for OutGridView
        /// </summary>
        public OutGridViewCommand()
        {
        }

        #endregion Contructors

        #region Input Parameters

        /// <summary>
        /// This parameter specifies the current pipeline object 
        /// </summary>
        [Parameter (ValueFromPipeline = true)]
        public PSObject InputObject
        {
            get { return this.inputObject;  }
            set { this.inputObject = value; }
        }
        private PSObject inputObject = AutomationNull.Value;

        /// <summary>
        /// Gets/sets the title of the Out-GridView window.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Title
        {
            get { return title;  }
            set { title = value; }
        }
        internal string title;
        
		/// <summary>
        /// Field used for the Block parameter.
        /// </summary>
        private SwitchParameter wait;

        /// <summary>
        /// Get or sets a value indicating whether the cmdlet should wait for the window to be closed
        /// </summary>
        [Parameter(ParameterSetName = "Wait")]
        public SwitchParameter Wait
        {
            get { return this.wait; }
            set { this.wait = value; }
        }

        /// <summary>
        /// Field used for the OutputMode parameter.
        /// </summary>
        private OutputModeOption outputMode;

        /// <summary>
        /// Get or sets a value indicating whether the selected items should be written to the pipeline
        /// and if it should be possible to select multiple or single list items
        /// </summary>
        [Parameter(ParameterSetName = "OutputMode")]
        public OutputModeOption OutputMode
        {
            set { this.outputMode = value; }
            get { return outputMode; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the selected items should be written to the pipeline
        /// Setting this to true is the same as setting the OutputMode to Multiple
        /// </summary>
        [Parameter(ParameterSetName = "PassThru")]
        public SwitchParameter PassThru
        {
            set { this.OutputMode = value.IsPresent ? OutputModeOption.Multiple : OutputModeOption.None; }
            get { return this.outputMode == OutputModeOption.Multiple ? new SwitchParameter(true) : new SwitchParameter(false); }
        }

        #endregion Input Parameters

        #region Public Methods 

        /// <summary>
        /// Provides a one-time, pre-processing functionality for the cmdlet.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Set up the ExpressionFactory
            this.expressionFactory = new MshExpressionFactory();

            // If the value of the Title parameter is valid, use it as a window's title.
            if(this.title != null)
            {
                this.windowProxy = new OutWindowProxy(this.title, this.outputMode, this);
            }
            else
            {
                // Using the command line as a title.
                this.windowProxy = new OutWindowProxy(this.MyInvocation.Line, this.outputMode, this);
            }

            // Load the Type info database.
            this.typeInfoDataBase = this.Context.FormatDBManager.GetTypeInfoDataBase();
        }

        /// <summary>
        /// Blocks depending on the wait and selected 
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();

            if (this.windowProxy == null)
            {
                return;
            }

            // If -Wait is used or outputMode is not None we have to wait for the window to be closed
            // The pipeline will be blocked while we don't return
            if (this.Wait || this.OutputMode != OutputModeOption.None)
            {
                this.windowProxy.BlockUntillClosed();
            }
			
            // Output selected items to pipeline.
            List<PSObject> selectedItems = this.windowProxy.GetSelectedItems();
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
            if (this.inputObject == null || this.inputObject == AutomationNull.Value)
            {
                return;
            }

            IDictionary dictionary = this.inputObject.BaseObject as IDictionary;
            if(dictionary != null)
            {
                // Dictionaries should be enumerated through because the pipeline does not enumerate through them. 
                foreach(DictionaryEntry entry in dictionary)
                {
                    ProcessObject(PSObjectHelper.AsPSObject(entry));
                }
            }
            else
            {
                ProcessObject(this.inputObject);
            }
        }

        /// <summary>
        /// StopProcessing is called close the window when Ctrl+C in the command prompt.
        /// </summary>
        protected override void StopProcessing()
        {
            if (this.Wait || this.OutputMode != OutputModeOption.None)
            {
                this.windowProxy.CloseWindow();
            }
        }

        /// <summary>
        /// Converts the provided PSObject to a string preserving PowerShell formatting.
        /// </summary>
        /// <param name="liveObject">PSObject to be converted to a string.</param>
        internal string ConvertToString(PSObject liveObject)
        {
            StringFormatError formatErrorObject = new StringFormatError();
            string smartToString = PSObjectHelper.SmartToString(liveObject,
                                                                this.expressionFactory,
                                                                InnerFormatShapeCommand.FormatEnumerationLimit(),
                                                                formatErrorObject);
            if(formatErrorObject.exception != null)
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
        /// Execute formatting on a single object
        /// </summary>
        /// <param name="input">object to process</param>
        private void ProcessObject(PSObject input)
        {
            // Make sure the OGV window is not closed.
            if (this.windowProxy.IsWindowClosed())
            {
                LocalPipeline pipeline = (LocalPipeline)this.Context.CurrentRunspace.GetCurrentlyRunningPipeline();
            
                if (pipeline != null && !pipeline.IsStopping)
                {
                    // Stop the pipeline cleanly.
                    pipeline.StopAsync();
                }
                return;
            }

            Object baseObject = input.BaseObject;

            // Throw a terminating error for types that are not supported.
            if (baseObject is ScriptBlock ||
                baseObject is SwitchParameter ||
                baseObject is PSReference ||
                baseObject is FormatInfoData ||
                baseObject is PSObject)
            {
                ErrorRecord error = new ErrorRecord(
                    new FormatException(StringUtil.Format(FormatAndOut_out_gridview.DataNotQualifiedForGridView)),
                    DataNotQualifiedForGridView,
                    ErrorCategory.InvalidType,
                    null);

                this.ThrowTerminatingError(error);
            }

            if(this.gridHeader == null)
            {
                // Columns have not been added yet; Start the main window and add columns.
                this.windowProxy.ShowWindow();
                this.gridHeader = GridHeader.ConstructGridHeader(input, this);
            }
            else
            {
                this.gridHeader.ProcessInputObject(input);
            }
            
            // Some thread syncronization needed.
            Exception exception = this.windowProxy.GetLastException();
            if(exception != null) 
            {
                ErrorRecord error = new ErrorRecord(
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
                    OutOfBandFormatViewManager.IsPropertyLessObject(input))
                {
                    return new ScalarTypeHeader(parentCmd, input);
                }
                return new NonscalarTypeHeader(parentCmd, input);
            }

            internal abstract void ProcessInputObject(PSObject input);
        }

        internal class ScalarTypeHeader : GridHeader
        {
            private Type originalScalarType;

            internal ScalarTypeHeader(OutGridViewCommand parentCmd, PSObject input) : base(parentCmd)
            {
                this.originalScalarType = input.BaseObject.GetType();

                // On scalar types the type name is used as a column name.
                this.parentCmd.windowProxy.AddColumnsAndItem(input);
            }

            internal override void ProcessInputObject(PSObject input)
            {
                if(!originalScalarType.Equals(input.BaseObject.GetType()))
                {
                    parentCmd.gridHeader = new HeteroTypeHeader(base.parentCmd, input);
                }
                else
                {
                    // Columns are already added; Add the input PSObject as an item to the underlying Management List.
                    base.parentCmd.windowProxy.AddItem(input);
                }
            }
        }

        internal class NonscalarTypeHeader : GridHeader
        {
            private AppliesTo appliesTo = null;

            internal NonscalarTypeHeader(OutGridViewCommand parentCmd, PSObject input) : base(parentCmd)
            {
                // Prepare a table view.
                TableView tableView = new TableView();
                tableView.Initialize(parentCmd.expressionFactory, parentCmd.typeInfoDataBase);

                // Request a view definition from the type database.
                ViewDefinition viewDefinition = DisplayDataQuery.GetViewByShapeAndType(parentCmd.expressionFactory, parentCmd.typeInfoDataBase, FormatShape.Table, input.TypeNames, null);
                if(viewDefinition != null)
                {
                    // Create a header using a view definition provided by the types database.
                    parentCmd.windowProxy.AddColumnsAndItem(input, tableView, (TableControlBody)viewDefinition.mainControl);

                    // Remember all type names and type groups the current view applies to.
                    this.appliesTo = viewDefinition.appliesTo;
                }
                else
                {
                    // Create a header using only the input object's properties.
                    parentCmd.windowProxy.AddColumnsAndItem(input, tableView);
                    this.appliesTo = new AppliesTo();

                    // Add all type names except for Object and MarshalByRefObject types because they are too generic.
                    // Leave the Object type name if it is the only type name.
                    int index = 0;
                    foreach(string typeName in input.TypeNames)
                    {
                        if(index > 0 && (typeName.Equals(typeof(Object).FullName, StringComparison.OrdinalIgnoreCase) ||
                            typeName.Equals(typeof(MarshalByRefObject).FullName, StringComparison.OrdinalIgnoreCase)))
                        {
                            break;
                        }
                        this.appliesTo.AddAppliesToType(typeName);
                        index++;
                    }
                }
            }

            internal override void ProcessInputObject(PSObject input)
            {
                // Find out if the input has matching types in the this.appliesTo collection.
                foreach(TypeOrGroupReference typeOrGroupRef in this.appliesTo.referenceList)
                {
                    if(typeOrGroupRef is TypeReference)
                    {
                        // Add deserialization prefix.
                        string deserializedTypeName = typeOrGroupRef.name;
                        Deserializer.AddDeserializationPrefix(ref deserializedTypeName);

                        for(int i = 0; i < input.TypeNames.Count; i++)
                        {
                            if(typeOrGroupRef.name.Equals(input.TypeNames[i], StringComparison.OrdinalIgnoreCase)
                                || deserializedTypeName.Equals(input.TypeNames[i], StringComparison.OrdinalIgnoreCase))
                            {
                                // Current view supports the input's Type;
                                // Add the input PSObject as an item to the underlying Management List.
                                base.parentCmd.windowProxy.AddItem(input);
                                return;
                            }
                        }
                    }
                    else
                    {
                        // Find out if the input's Type belongs to the current TypeGroup.
                        // TypeGroupReference has only a group's name, so use the database to get through all actual TypeGroup's.
                        List<TypeGroupDefinition> typeGroupList = base.parentCmd.typeInfoDataBase.typeGroupSection.typeGroupDefinitionList;
                        foreach(TypeGroupDefinition typeGroup in typeGroupList)
                        {
                            if(typeGroup.name.Equals(typeOrGroupRef.name, StringComparison.OrdinalIgnoreCase))
                            {
                                // A matching TypeGroup is found in the database.
                                // Find out if the input's Type belongs to this TypeGroup.
                                foreach(TypeReference typeRef in typeGroup.typeReferenceList)
                                {
                                    // Add deserialization prefix.
                                    string deserializedTypeName = typeRef.name;
                                    Deserializer.AddDeserializationPrefix(ref deserializedTypeName);

                                    if(input.TypeNames.Count > 0 
                                        && (typeRef.name.Equals(input.TypeNames[0], StringComparison.OrdinalIgnoreCase)
                                            || deserializedTypeName.Equals(input.TypeNames[0], StringComparison.OrdinalIgnoreCase)))
                                    {
                                        // Current view supports the input's Type;
                                        // Add the input PSObject as an item to the underlying Management List.
                                        base.parentCmd.windowProxy.AddItem(input);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                // The input's Type is not supported by the current view;
                // Switch to the Hetero Type view.
                parentCmd.gridHeader = new HeteroTypeHeader(base.parentCmd, input);
            }
        }

        internal class HeteroTypeHeader : GridHeader
        {
            internal HeteroTypeHeader(OutGridViewCommand parentCmd, PSObject input) : base(parentCmd)
            {
                // Clear all existed columns and add Type and Value columns.
                this.parentCmd.windowProxy.AddHeteroViewColumnsAndItem(input);
            }

            internal override void ProcessInputObject(PSObject input)
            {
                this.parentCmd.windowProxy.AddHeteroViewItem(input);
            }
        }

        /// <summary>
        /// Implements IDisposable logic
        /// </summary>
        /// <param name="isDisposing">true if being called from Dispose</param>
        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (this.windowProxy != null)
                {
                    this.windowProxy.Dispose();
                    this.windowProxy = null;
                }
            }
        }

        /// <summary>
        /// Dispose method in IDisposeable
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~OutGridViewCommand()
        {
            Dispose(false);
        }
    }
}
