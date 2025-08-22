// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Reflection;
using System.Threading;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    internal sealed class OutWindowProxy : IDisposable
    {
        private const string OutGridViewWindowClassName = "Microsoft.Management.UI.Internal.OutGridViewWindow";
        private const string OriginalTypePropertyName = "OriginalType";
        internal const string OriginalObjectPropertyName = "OutGridViewOriginalObject";
        private const string ToStringValuePropertyName = "ToStringValue";
        private const string IndexPropertyName = "IndexValue";

        private int _index;

        /// <summary> Columns definition of the underlying Management List</summary>
        private HeaderInfo _headerInfo;

        private bool _isWindowStarted;

        private readonly string _title;

        private readonly OutputModeOption _outputMode;

        private AutoResetEvent _closedEvent;

        private readonly OutGridViewCommand _parentCmdlet;

        private readonly GraphicalHostReflectionWrapper _graphicalHostReflectionWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutWindowProxy"/> class.
        /// </summary>
        internal OutWindowProxy(string title, OutputModeOption outPutMode, OutGridViewCommand parentCmdlet)
        {
            _title = title;
            _outputMode = outPutMode;
            _parentCmdlet = parentCmdlet;

            _graphicalHostReflectionWrapper = GraphicalHostReflectionWrapper.GetGraphicalHostReflectionWrapper(parentCmdlet, OutWindowProxy.OutGridViewWindowClassName);
        }

        /// <summary>
        /// Adds columns to the output window.
        /// </summary>
        /// <param name="propertyNames">An array of property names to add.</param>
        /// <param name="displayNames">An array of display names to add.</param>
        /// <param name="types">An array of types to add.</param>
        internal void AddColumns(string[] propertyNames, string[] displayNames, Type[] types)
        {
            ArgumentNullException.ThrowIfNull(propertyNames);

            ArgumentNullException.ThrowIfNull(displayNames);

            ArgumentNullException.ThrowIfNull(types);

            try
            {
                _graphicalHostReflectionWrapper.CallMethod("AddColumns", propertyNames, displayNames, types);
            }
            catch (TargetInvocationException ex)
            {
                // Verify if this is an error loading the System.Core dll.
                if (ex.InnerException is FileNotFoundException fileNotFoundEx && fileNotFoundEx.FileName.Contains("System.Core"))
                {
                    _parentCmdlet.ThrowTerminatingError(
                        new ErrorRecord(new InvalidOperationException(
                                StringUtil.Format(FormatAndOut_out_gridview.RestartPowerShell,
                                _parentCmdlet.CommandInfo.Name), ex.InnerException),
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
            _headerInfo = tableView.GenerateHeaderInfo(liveObject, _parentCmdlet);

            AddColumnsAndItemEnd(liveObject);
        }

        // Database defined types.
        internal void AddColumnsAndItem(PSObject liveObject, TableView tableView, TableControlBody tableBody)
        {
            _headerInfo = tableView.GenerateHeaderInfo(liveObject, tableBody, _parentCmdlet);

            AddColumnsAndItemEnd(liveObject);
        }

        // Scalar types.
        internal void AddColumnsAndItem(PSObject liveObject)
        {
            _headerInfo = new HeaderInfo();

            // On scalar types the type name is used as a column name.
            _headerInfo.AddColumn(new ScalarTypeColumnInfo(liveObject.BaseObject.GetType()));
            AddColumnsAndItemEnd(liveObject);
        }

        private void AddColumnsAndItemEnd(PSObject liveObject)
        {
            // Add columns to the underlying Management list and as a byproduct get a stale PSObject.
            PSObject staleObject = _headerInfo.AddColumnsToWindow(this, liveObject);

            // Add 3 extra properties, so that the stale PSObject has meaningful info in the Hetero-type header view.
            AddExtraProperties(staleObject, liveObject);

            // Add the stale PSObject to the underlying Management list.
            _graphicalHostReflectionWrapper.CallMethod("AddItem", staleObject);
        }

        // Hetero types.
        internal void AddHeteroViewColumnsAndItem(PSObject liveObject)
        {
            _headerInfo = new HeaderInfo();

            _headerInfo.AddColumn(new IndexColumnInfo(OutWindowProxy.IndexPropertyName,
                StringUtil.Format(FormatAndOut_out_gridview.IndexColumnName), _index));
            _headerInfo.AddColumn(new ToStringColumnInfo(OutWindowProxy.ToStringValuePropertyName,
                StringUtil.Format(FormatAndOut_out_gridview.ValueColumnName), _parentCmdlet));
            _headerInfo.AddColumn(new TypeNameColumnInfo(OutWindowProxy.OriginalTypePropertyName,
                StringUtil.Format(FormatAndOut_out_gridview.TypeColumnName)));

            // Add columns to the underlying Management list and as a byproduct get a stale PSObject.
            PSObject staleObject = _headerInfo.AddColumnsToWindow(this, liveObject);

            // Add the stale PSObject to the underlying Management list.
            _graphicalHostReflectionWrapper.CallMethod("AddItem", staleObject);
        }

        private void AddExtraProperties(PSObject staleObject, PSObject liveObject)
        {
            // Add 3 extra properties, so that the stale PSObject has meaningful info in the Hetero-type header view.
            staleObject.Properties.Add(new PSNoteProperty(OutWindowProxy.IndexPropertyName, _index++));
            staleObject.Properties.Add(new PSNoteProperty(OutWindowProxy.OriginalTypePropertyName, liveObject.BaseObject.GetType().FullName));
            staleObject.Properties.Add(new PSNoteProperty(OutWindowProxy.OriginalObjectPropertyName, liveObject));

            // Convert the LivePSObject to a string preserving PowerShell formatting.
            staleObject.Properties.Add(new PSNoteProperty(OutWindowProxy.ToStringValuePropertyName,
                                                          _parentCmdlet.ConvertToString(liveObject)));
        }

        /// <summary>
        /// Adds an item to the out window.
        /// </summary>
        /// <param name="livePSObject">
        /// The item to add.
        /// </param>
        internal void AddItem(PSObject livePSObject)
        {
            ArgumentNullException.ThrowIfNull(livePSObject);

            if (_headerInfo == null)
            {
                throw new InvalidOperationException();
            }

            PSObject stalePSObject = _headerInfo.CreateStalePSObject(livePSObject);

            // Add 3 extra properties, so that the stale PSObject has meaningful info in the Hetero-type header view.
            AddExtraProperties(stalePSObject, livePSObject);

            _graphicalHostReflectionWrapper.CallMethod("AddItem", stalePSObject);
        }

        /// <summary>
        /// Adds an item to the out window.
        /// </summary>
        /// <param name="livePSObject">
        /// The item to add.
        /// </param>
        internal void AddHeteroViewItem(PSObject livePSObject)
        {
            ArgumentNullException.ThrowIfNull(livePSObject);

            if (_headerInfo == null)
            {
                throw new InvalidOperationException();
            }

            PSObject stalePSObject = _headerInfo.CreateStalePSObject(livePSObject);
            _graphicalHostReflectionWrapper.CallMethod("AddItem", stalePSObject);
        }

        /// <summary>
        /// Shows the out window if it has not already been displayed.
        /// </summary>
        internal void ShowWindow()
        {
            if (!_isWindowStarted)
            {
                _closedEvent = new AutoResetEvent(false);
                _graphicalHostReflectionWrapper.CallMethod("StartWindow", _title, _outputMode.ToString(), _closedEvent);
                _isWindowStarted = true;
            }
        }

        internal void BlockUntilClosed() => _closedEvent?.WaitOne();

        /// <summary>
        /// Implements IDisposable logic.
        /// </summary>
        /// <param name="isDisposing">True if being called from Dispose.</param>
        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (_closedEvent != null)
                {
                    _closedEvent.Dispose();
                    _closedEvent = null;
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

        /// <summary>
        /// Close the window if it has already been displayed.
        /// </summary>
        internal void CloseWindow()
        {
            if (_isWindowStarted)
            {
                _graphicalHostReflectionWrapper.CallMethod("CloseWindow");
                _isWindowStarted = false;
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
            return (bool)_graphicalHostReflectionWrapper.CallMethod("GetWindowClosedStatus");
        }

        /// <summary>Returns any exception that has been thrown by previous method calls.</summary>
        /// <returns>The thrown and caught exception. It returns null if no exceptions were thrown by any previous method calls.</returns>
        internal Exception GetLastException()
        {
            return (Exception)_graphicalHostReflectionWrapper.CallMethod("GetLastException");
        }

        /// <summary>
        /// Return the selected item of the OutGridView.
        /// </summary>
        /// <returns>
        /// The selected item.
        /// </returns>
        internal List<PSObject> GetSelectedItems()
        {
            return (List<PSObject>)_graphicalHostReflectionWrapper.CallMethod("SelectedItems");
        }
    }
}
