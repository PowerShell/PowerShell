#if !CORECLR
//
//    Copyright (C) Microsoft.  All rights reserved.
//
namespace Microsoft.PowerShell.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Reflection;
    using System.Management.Automation;
    using System.Management.Automation.Internal;
    using System.Management.Automation.Runspaces;

    using Microsoft.PowerShell.Commands.Internal.Format;
	
    internal class OutWindowProxy: IDisposable
    {
        private const string OutGridViewWindowClassName = "Microsoft.Management.UI.Internal.OutGridViewWindow";
        private const string OriginalTypePropertyName = "OriginalType";
        internal const string OriginalObjectPropertyName = "OutGridViewOriginalObject";
        private const string ToStringValuePropertyName = "ToStringValue";
        private const string IndexPropertyName = "IndexValue";
        private int index;

        
        /// <summary> Columns definition of the underlying Management List</summary>
        private HeaderInfo headerInfo;

        private bool IsWindowStarted;

        private string title;
		
        private OutputModeOption outputMode;
		
        private AutoResetEvent closedEvent;

        private OutGridViewCommand parentCmdlet;

        private GraphicalHostReflectionWrapper graphicalHostReflectionWrapper;

        /// <summary>
        /// Initializes a new instance of the OutWindowProxy class.
        /// </summary>
        internal OutWindowProxy(string title, OutputModeOption outPutMode, OutGridViewCommand parentCmdlet)
        {
            this.title = title;
            this.outputMode = outPutMode;
            this.parentCmdlet = parentCmdlet;

            this.graphicalHostReflectionWrapper = GraphicalHostReflectionWrapper.GetGraphicalHostReflectionWrapper(parentCmdlet, OutWindowProxy.OutGridViewWindowClassName);
        }

        /// <summary>
        /// Adds columns to the output window.
        /// </summary>
        /// <param name="propertyNames">An array of property names to add.</param>
        /// <param name="displayNames">An array of display names to add.</param>
        /// <param name="types">An array of types to add.</param>
        internal void AddColumns(string[] propertyNames, string[] displayNames, Type[] types)
        {
            if (null == propertyNames)
            {
                throw new ArgumentNullException("propertyNames");
            }
            if (null == displayNames)
            {
                throw new ArgumentNullException("displayNames");
            }
            if (null == types)
            {
                throw new ArgumentNullException("types");
            }

            try
            {
                this.graphicalHostReflectionWrapper.CallMethod("AddColumns", propertyNames, displayNames, types);
            }
            catch (TargetInvocationException ex)
            {
                // Verify if this is an error loading the System.Core dll.
                FileNotFoundException fileNotFoundEx = ex.InnerException as FileNotFoundException;
                if(fileNotFoundEx != null && fileNotFoundEx.FileName.Contains("System.Core"))
                {
                    this.parentCmdlet.ThrowTerminatingError(
                        new ErrorRecord(new InvalidOperationException(
                                StringUtil.Format(FormatAndOut_out_gridview.RestartPowerShell,
                                parentCmdlet.CommandInfo.Name), ex.InnerException), 
                            "ErrorLoadingAssembly",
                            ErrorCategory.ObjectNotFound,
                            null));
                }
                else
                {
                    // Let PowerShell take care of this problem.
                    throw;
                }
            }
        }

        // Types that are not defined in the database and are not scalar.
        internal void AddColumnsAndItem(PSObject liveObject, TableView tableView)
        {
            // Create a header using only the input object's properties.
            this.headerInfo = tableView.GenerateHeaderInfo(liveObject, parentCmdlet);

            AddColumnsAndItemEnd(liveObject);
        }

        // Database defined types.
        internal void AddColumnsAndItem(PSObject liveObject, TableView tableView, TableControlBody tableBody)
        {
            this.headerInfo = tableView.GenerateHeaderInfo(liveObject, tableBody, parentCmdlet);

            AddColumnsAndItemEnd(liveObject);
        }

        // Scalar types.
        internal void AddColumnsAndItem(PSObject liveObject)
        {
            this.headerInfo = new HeaderInfo();

            // On scalar types the type name is used as a column name.
            headerInfo.AddColumn(new ScalarTypeColumnInfo(liveObject.BaseObject.GetType()));
            AddColumnsAndItemEnd(liveObject);
        }

        private void AddColumnsAndItemEnd(PSObject liveObject)
        {
            // Add columns to the underlying Management list and as a byproduct get a stale PSObject.
            PSObject staleObject = headerInfo.AddColumnsToWindow(this, liveObject);

            // Add 3 extra properties, so that the stale PSObject has meaningful info in the Hetero-type header view.
            AddExtraProperties(staleObject, liveObject);
            
            // Add the stale PSObject to the underlying Management list.
            this.graphicalHostReflectionWrapper.CallMethod("AddItem", staleObject);
        }

        // Hetero types.
        internal void AddHeteroViewColumnsAndItem(PSObject liveObject)
        {
            this.headerInfo = new HeaderInfo();

            headerInfo.AddColumn(new IndexColumnInfo(OutWindowProxy.IndexPropertyName,
                StringUtil.Format(FormatAndOut_out_gridview.IndexColumnName), this.index));
            headerInfo.AddColumn(new ToStringColumnInfo(OutWindowProxy.ToStringValuePropertyName,
                StringUtil.Format(FormatAndOut_out_gridview.ValueColumnName), this.parentCmdlet));
            headerInfo.AddColumn(new TypeNameColumnInfo(OutWindowProxy.OriginalTypePropertyName,
                StringUtil.Format(FormatAndOut_out_gridview.TypeColumnName)));
            
            // Add columns to the underlying Management list and as a byproduct get a stale PSObject.
            PSObject staleObject = headerInfo.AddColumnsToWindow(this, liveObject);
            
            // Add the stale PSObject to the underlying Management list.
            this.graphicalHostReflectionWrapper.CallMethod("AddItem", staleObject);
        }

        private void AddExtraProperties(PSObject staleObject, PSObject liveObject)
        {
            // Add 3 extra properties, so that the stale PSObject has meaningful info in the Hetero-type header view.
            staleObject.Properties.Add(new PSNoteProperty(OutWindowProxy.IndexPropertyName, index++));
            staleObject.Properties.Add(new PSNoteProperty(OutWindowProxy.OriginalTypePropertyName, liveObject.BaseObject.GetType().FullName));
            staleObject.Properties.Add(new PSNoteProperty(OutWindowProxy.OriginalObjectPropertyName, liveObject));

            // Convert the LivePSObject to a string preserving PowerShell formatting.
            staleObject.Properties.Add(new PSNoteProperty(OutWindowProxy.ToStringValuePropertyName,
                                                          this.parentCmdlet.ConvertToString(liveObject)));
        }

        /// <summary>
        /// Adds an item to the out window.
        /// </summary>
        /// <param name="livePSObject">
        /// The item to add.
        /// </param>
        internal void AddItem(PSObject livePSObject)
        {
            if (null == livePSObject)
            {
                throw new ArgumentNullException("livePSObject");
            }

            if(this.headerInfo == null)
            {
                throw new InvalidOperationException();
            }

            PSObject stalePSObject = this.headerInfo.CreateStalePSObject(livePSObject);

            // Add 3 extra properties, so that the stale PSObject has meaningful info in the Hetero-type header view.
            AddExtraProperties(stalePSObject, livePSObject);

            this.graphicalHostReflectionWrapper.CallMethod("AddItem", stalePSObject);
        }

        /// <summary>
        /// Adds an item to the out window.
        /// </summary>
        /// <param name="livePSObject">
        /// The item to add.
        /// </param>
        internal void AddHeteroViewItem(PSObject livePSObject)
        {
            if (null == livePSObject)
            {
                throw new ArgumentNullException("livePSObject");
            }

            if(this.headerInfo == null)
            {
                throw new InvalidOperationException();
            }

            PSObject stalePSObject = this.headerInfo.CreateStalePSObject(livePSObject);
            this.graphicalHostReflectionWrapper.CallMethod("AddItem", stalePSObject);
        }

        /// <summary>
        /// Shows the out window if it has not already been displayed.
        /// </summary>
        internal void ShowWindow()
        {
            if (!this.IsWindowStarted)
            {
                this.closedEvent = new AutoResetEvent(false);
                this.graphicalHostReflectionWrapper.CallMethod("StartWindow", this.title, this.outputMode.ToString(), this.closedEvent);
                this.IsWindowStarted = true;
            }
        }

        internal void BlockUntillClosed()
        {
            if (this.closedEvent != null)
            {
                this.closedEvent.WaitOne();
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
                if (this.closedEvent != null)
                {
                    this.closedEvent.Dispose();
                    this.closedEvent = null;
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
        /// Close the window if it has already been displayed.
        /// </summary>
        internal void CloseWindow()
        {
            if (this.IsWindowStarted)
            {
                this.graphicalHostReflectionWrapper.CallMethod("CloseWindow");
                this.IsWindowStarted = false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the out window is closed.
        /// </summary>
        /// <returns>
        /// True if the out window is closed, false otherwise.
        /// </returns>
        internal bool IsWindowClosed()
        {
            return (bool)this.graphicalHostReflectionWrapper.CallMethod("GetWindowClosedStatus");
        }

        /// <summary>Returns any exception that has been thrown by previous method calls.</summary>
        /// <returns>The thrown and caught exception. It returns null if no exceptions were thrown by any previous method calls.</returns>
        internal Exception GetLastException()
        {
            return (Exception)this.graphicalHostReflectionWrapper.CallMethod("GetLastException");
        }
		
        /// <summary>
        /// Return the selected item of the OutGridView.
        /// </summary>
        /// <returns>
        /// The selected item
        /// </returns>
        internal List<PSObject> GetSelectedItems()
        {
            return (List<PSObject>)this.graphicalHostReflectionWrapper.CallMethod("SelectedItems");
        }
    }
}

#endif
