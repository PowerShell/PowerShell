// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// </summary>
    [Cmdlet(VerbsData.Compare, "Object", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113286",
        RemotingCapability = RemotingCapability.None)]
    public sealed class CompareObjectCommand : ObjectCmdletBase
    {
        #region Parameters
        /// <summary>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [AllowEmptyCollection]
        public PSObject[] ReferenceObject { get; set; }

        /// <summary>
        /// </summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyCollection]
        public PSObject[] DifferenceObject { get; set; }

        /// <summary>
        /// </summary>
        [Parameter]
        [ValidateRange(0, Int32.MaxValue)]
        public int SyncWindow { get; set; } = Int32.MaxValue;

        /// <summary>
        /// </summary>
        /// <value></value>
        [Parameter]
        public object[] Property { get; set; }

        /* not implemented
        /// <summary>
        /// </summary>
        [Parameter]
        public SwitchParameter IgnoreWhiteSpace
        {
            get { return _ignoreWhiteSpace; }

            set { _ignoreWhiteSpace = value; }
        }

        private bool _ignoreWhiteSpace = false;
        */

        /// <summary>
        /// </summary>
        [Parameter]
        public SwitchParameter ExcludeDifferent
        {
            get { return _excludeDifferent; }

            set { _excludeDifferent = value; }
        }

        private bool _excludeDifferent /*=false*/;

        /// <summary>
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeEqual
        {
            get { return _includeEqual; }

            set
            {
                _isIncludeEqualSpecified = true;
                _includeEqual = value;
            }
        }

        private bool _includeEqual /* = false */;
        private bool _isIncludeEqualSpecified /* = false */;

        /// <summary>
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get { return _passThru; }

            set { _passThru = value; }
        }

        private bool _passThru /* = false */;
        #endregion Parameters

        #region Internal
        private List<OrderByPropertyEntry> _referenceEntries;
        private List<OrderByPropertyEntry> _referenceEntryBacklog
            = new List<OrderByPropertyEntry>();
        private List<OrderByPropertyEntry> _differenceEntryBacklog
            = new List<OrderByPropertyEntry>();
        private OrderByProperty _orderByProperty = null;
        private OrderByPropertyComparer _comparer = null;

        private int _referenceObjectIndex /* = 0 */;

        // These are programmatic strings, not subject to INTL
        private const string SideIndicatorPropertyName = "SideIndicator";
        private const string SideIndicatorMatch = "==";
        private const string SideIndicatorReference = "<=";
        private const string SideIndicatorDifference = "=>";
        private const string InputObjectPropertyName = "InputObject";

        /// <summary>
        /// The following is the matching algorithm:
        /// Retrieve the incoming object (differenceEntry) if any
        /// Retrieve the next reference object (referenceEntry) if any
        /// If differenceEntry matches referenceEntry
        ///   Emit referenceEntry as a match
        ///   Return
        /// If differenceEntry matches any entry in referenceEntryBacklog
        ///   Emit the backlog entry as a match
        ///   Remove the backlog entry from referenceEntryBacklog
        ///   Clear differenceEntry
        /// If referenceEntry (if any) matches any entry in differenceEntryBacklog
        ///   Emit referenceEntry as a match
        ///   Remove the backlog entry from differenceEntryBacklog
        ///   Clear referenceEntry
        /// If differenceEntry is still present
        ///   If SyncWindow is 0
        ///     Emit differenceEntry as unmatched
        ///   Else
        ///     While there is no space in differenceEntryBacklog
        ///       Emit oldest entry in differenceEntryBacklog as unmatched
        ///       Remove oldest entry from differenceEntryBacklog
        ///     Add differenceEntry to differenceEntryBacklog
        /// If referenceEntry is still present
        ///   If SyncWindow is 0
        ///     Emit referenceEntry as unmatched
        ///   Else
        ///     While there is no space in referenceEntryBacklog
        ///       Emit oldest entry in referenceEntryBacklog as unmatched
        ///       Remove oldest entry from referenceEntryBacklog
        ///     Add referenceEntry to referenceEntryBacklog.
        /// </summary>
        /// <param name="differenceEntry"></param>
        private void Process(OrderByPropertyEntry differenceEntry)
        {
            Diagnostics.Assert(_referenceEntries != null, "null referenceEntries");

            // Retrieve the next reference object (referenceEntry) if any
            OrderByPropertyEntry referenceEntry = null;
            if (_referenceObjectIndex < _referenceEntries.Count)
            {
                referenceEntry = _referenceEntries[_referenceObjectIndex++];
            }

            // If differenceEntry matches referenceEntry
            //   Emit referenceEntry as a match
            //   Return
            // 2005/07/19 Switched order of referenceEntry and differenceEntry
            //   so that we cast differenceEntry to the type of referenceEntry.
            if (referenceEntry != null && differenceEntry != null &&
                0 == _comparer.Compare(referenceEntry, differenceEntry))
            {
                EmitMatch(referenceEntry);
                return;
            }

            // If differenceEntry matches any entry in referenceEntryBacklog
            //   Emit the backlog entry as a match
            //   Remove the backlog entry from referenceEntryBacklog
            //   Clear differenceEntry
            OrderByPropertyEntry matchingEntry =
                MatchAndRemove(differenceEntry, _referenceEntryBacklog);
            if (matchingEntry != null)
            {
                EmitMatch(matchingEntry);
                differenceEntry = null;
            }

            // If referenceEntry (if any) matches any entry in differenceEntryBacklog
            //   Emit referenceEntry as a match
            //   Remove the backlog entry from differenceEntryBacklog
            //   Clear referenceEntry
            matchingEntry =
                MatchAndRemove(referenceEntry, _differenceEntryBacklog);
            if (matchingEntry != null)
            {
                EmitMatch(referenceEntry);
                referenceEntry = null;
            }

            // If differenceEntry is still present
            //   If SyncWindow is 0
            //     Emit differenceEntry as unmatched
            //   Else
            //     While there is no space in differenceEntryBacklog
            //       Emit oldest entry in differenceEntryBacklog as unmatched
            //       Remove oldest entry from differenceEntryBacklog
            //     Add differenceEntry to differenceEntryBacklog
            if (differenceEntry != null)
            {
                if (0 < SyncWindow)
                {
                    while (_differenceEntryBacklog.Count >= SyncWindow)
                    {
                        EmitDifferenceOnly(_differenceEntryBacklog[0]);
                        _differenceEntryBacklog.RemoveAt(0);
                    }

                    _differenceEntryBacklog.Add(differenceEntry);
                }
                else
                {
                    EmitDifferenceOnly(differenceEntry);
                }
            }

            // If referenceEntry is still present
            //   If SyncWindow is 0
            //     Emit referenceEntry as unmatched
            //   Else
            //     While there is no space in referenceEntryBacklog
            //       Emit oldest entry in referenceEntryBacklog as unmatched
            //       Remove oldest entry from referenceEntryBacklog
            //     Add referenceEntry to referenceEntryBacklog
            if (referenceEntry != null)
            {
                if (0 < SyncWindow)
                {
                    while (_referenceEntryBacklog.Count >= SyncWindow)
                    {
                        EmitReferenceOnly(_referenceEntryBacklog[0]);
                        _referenceEntryBacklog.RemoveAt(0);
                    }

                    _referenceEntryBacklog.Add(referenceEntry);
                }
                else
                {
                    EmitReferenceOnly(referenceEntry);
                }
            }
        }

        private void InitComparer()
        {
            if (_comparer != null)
                return;

            List<PSObject> referenceObjectList = new List<PSObject>(ReferenceObject);
            _orderByProperty = new OrderByProperty(
                this, referenceObjectList, Property, true, _cultureInfo, CaseSensitive);
            Diagnostics.Assert(_orderByProperty.Comparer != null, "no comparer");
            Diagnostics.Assert(
                _orderByProperty.OrderMatrix != null &&
                _orderByProperty.OrderMatrix.Count == ReferenceObject.Length,
                "no OrderMatrix");
            if (_orderByProperty.Comparer == null || _orderByProperty.OrderMatrix == null || _orderByProperty.OrderMatrix.Count == 0)
            {
                return;
            }

            _comparer = _orderByProperty.Comparer;
            _referenceEntries = _orderByProperty.OrderMatrix;
        }

        private OrderByPropertyEntry MatchAndRemove(
            OrderByPropertyEntry match,
            List<OrderByPropertyEntry> list)
        {
            if (match == null || list == null)
                return null;
            Diagnostics.Assert(_comparer != null, "null comparer");
            for (int i = 0; i < list.Count; i++)
            {
                OrderByPropertyEntry listEntry = list[i];
                Diagnostics.Assert(listEntry != null, "null listEntry " + i);
                if (0 == _comparer.Compare(match, listEntry))
                {
                    list.RemoveAt(i);
                    return listEntry;
                }
            }

            return null;
        }

        #region Emit
        private void EmitMatch(OrderByPropertyEntry entry)
        {
            if (_includeEqual)
                Emit(entry, SideIndicatorMatch);
        }

        private void EmitDifferenceOnly(OrderByPropertyEntry entry)
        {
            if (!ExcludeDifferent)
                Emit(entry, SideIndicatorDifference);
        }

        private void EmitReferenceOnly(OrderByPropertyEntry entry)
        {
            if (!ExcludeDifferent)
                Emit(entry, SideIndicatorReference);
        }

        private void Emit(OrderByPropertyEntry entry, string sideIndicator)
        {
            Diagnostics.Assert(entry != null, "null entry");

            PSObject mshobj;
            if (PassThru)
            {
                mshobj = PSObject.AsPSObject(entry.inputObject);
            }
            else
            {
                mshobj = new PSObject();
                if (Property == null || 0 == Property.Length)
                {
                    PSNoteProperty inputNote = new PSNoteProperty(
                        InputObjectPropertyName, entry.inputObject);
                    mshobj.Properties.Add(inputNote);
                }
                else
                {
                    List<MshParameter> mshParameterList = _orderByProperty.MshParameterList;
                    Diagnostics.Assert(mshParameterList != null, "null mshParameterList");
                    Diagnostics.Assert(mshParameterList.Count == Property.Length, "mshParameterList.Count " + mshParameterList.Count);

                    for (int i = 0; i < Property.Length; i++)
                    {
                        // 2005/07/05 This is the closest we can come to
                        // the string typed by the user
                        MshParameter mshParameter = mshParameterList[i];
                        Diagnostics.Assert(mshParameter != null, "null mshParameter");
                        Hashtable hash = mshParameter.hash;
                        Diagnostics.Assert(hash != null, "null hash");
                        object prop = hash[FormatParameterDefinitionKeys.ExpressionEntryKey];
                        Diagnostics.Assert(prop != null, "null prop");
                        string propName = prop.ToString();
                        PSNoteProperty propertyNote = new PSNoteProperty(
                            propName,
                            entry.orderValues[i].PropertyValue);
                        try
                        {
                            mshobj.Properties.Add(propertyNote);
                        }
                        catch (ExtendedTypeSystemException)
                        {
                            // this is probably a duplicate add
                        }
                    }
                }
            }

            mshobj.Properties.Remove(SideIndicatorPropertyName);
            PSNoteProperty sideNote = new PSNoteProperty(
                SideIndicatorPropertyName, sideIndicator);
            mshobj.Properties.Add(sideNote);
            WriteObject(mshobj);
        }
        #endregion Emit
        #endregion Internal

        #region Overrides

        /// <summary>
        /// If the parameter 'ExcludeDifferent' is present, then we need to turn on the
        /// 'IncludeEqual' switch unless it's turned off by the user specifically.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (ExcludeDifferent)
            {
                if (_isIncludeEqualSpecified == false)
                {
                    return;
                }

                if (_isIncludeEqualSpecified && !_includeEqual)
                {
                    return;
                }

                _includeEqual = true;
            }
        }

        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ReferenceObject == null || ReferenceObject.Length == 0)
            {
                HandleDifferenceObjectOnly();
                return;
            }
            else if (DifferenceObject == null || DifferenceObject.Length == 0)
            {
                HandleReferenceObjectOnly();
                return;
            }

            if (_comparer == null && 0 < DifferenceObject.Length)
            {
                InitComparer();
            }

            List<PSObject> differenceList = new List<PSObject>(DifferenceObject);
            List<OrderByPropertyEntry> differenceEntries =
                OrderByProperty.CreateOrderMatrix(
                this, differenceList, _orderByProperty.MshParameterList);

            foreach (OrderByPropertyEntry incomingEntry in differenceEntries)
            {
                Process(incomingEntry);
            }
        }

        /// <summary>
        /// </summary>
        protected override void EndProcessing()
        {
            // Clear remaining reference objects if there are more
            // reference objects than difference objects
            if (_referenceEntries != null)
            {
                while (_referenceObjectIndex < _referenceEntries.Count)
                {
                    Process(null);
                }
            }

            // emit all remaining backlogged objects
            foreach (OrderByPropertyEntry differenceEntry in _differenceEntryBacklog)
            {
                EmitDifferenceOnly(differenceEntry);
            }

            _differenceEntryBacklog.Clear();
            foreach (OrderByPropertyEntry referenceEntry in _referenceEntryBacklog)
            {
                EmitReferenceOnly(referenceEntry);
            }

            _referenceEntryBacklog.Clear();
        }
        #endregion Overrides

        private void HandleDifferenceObjectOnly()
        {
            if (DifferenceObject == null || DifferenceObject.Length == 0)
            {
                return;
            }

            List<PSObject> differenceList = new List<PSObject>(DifferenceObject);
            _orderByProperty = new OrderByProperty(
                this, differenceList, Property, true, _cultureInfo, CaseSensitive);
            List<OrderByPropertyEntry> differenceEntries =
                OrderByProperty.CreateOrderMatrix(
                this, differenceList, _orderByProperty.MshParameterList);

            foreach (OrderByPropertyEntry entry in differenceEntries)
            {
                EmitDifferenceOnly(entry);
            }
        }

        private void HandleReferenceObjectOnly()
        {
            if (ReferenceObject == null || ReferenceObject.Length == 0)
            {
                return;
            }

            InitComparer();
            Process(null);
        }
    }
}
