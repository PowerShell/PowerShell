// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// PSTuple is a helper class used to create Tuple from an input array.
    /// </summary>
    internal static class PSTuple
    {
        /// <summary>
        /// ArrayToTuple is a helper method used to create a tuple for the supplied input array.
        /// </summary>
        /// <typeparam name="T">The first generic type parameter.</typeparam>
        /// <param name="inputObjects">Input objects used to create a tuple.</param>
        /// <returns>Tuple object.</returns>
        internal static object ArrayToTuple<T>(IList<T> inputObjects)
        {
            return ArrayToTuple(inputObjects, 0);
        }

        /// <summary>
        /// ArrayToTuple is a helper method used to create a tuple for the supplied input array.
        /// </summary>
        /// <typeparam name="T">The first generic type parameter.</typeparam>
        /// <param name="inputObjects">Input objects used to create a tuple.</param>
        /// <param name="startIndex">Start index of the array from which the objects have to considered for the tuple creation.</param>
        /// <returns>Tuple object.</returns>
        private static object ArrayToTuple<T>(IList<T> inputObjects, int startIndex)
        {
            Diagnostics.Assert(inputObjects != null, "inputObjects is null");
            Diagnostics.Assert(inputObjects.Count > 0, "inputObjects is empty");

            switch (inputObjects.Count - startIndex)
            {
                case 0:
                    return null;
                case 1:
                    return Tuple.Create(inputObjects[startIndex]);
                case 2:
                    return Tuple.Create(inputObjects[startIndex], inputObjects[startIndex + 1]);
                case 3:
                    return Tuple.Create(inputObjects[startIndex], inputObjects[startIndex + 1], inputObjects[startIndex + 2]);
                case 4:
                    return Tuple.Create(inputObjects[startIndex], inputObjects[startIndex + 1], inputObjects[startIndex + 2], inputObjects[startIndex + 3]);
                case 5:
                    return Tuple.Create(
                        inputObjects[startIndex],
                        inputObjects[startIndex + 1],
                        inputObjects[startIndex + 2],
                        inputObjects[startIndex + 3],
                        inputObjects[startIndex + 4]);
                case 6:
                    return Tuple.Create(
                        inputObjects[startIndex],
                        inputObjects[startIndex + 1],
                        inputObjects[startIndex + 2],
                        inputObjects[startIndex + 3],
                        inputObjects[startIndex + 4],
                        inputObjects[startIndex + 5]);
                case 7:
                    return Tuple.Create(
                        inputObjects[startIndex],
                        inputObjects[startIndex + 1],
                        inputObjects[startIndex + 2],
                        inputObjects[startIndex + 3],
                        inputObjects[startIndex + 4],
                        inputObjects[startIndex + 5],
                        inputObjects[startIndex + 6]);
                case 8:
                    return Tuple.Create(
                        inputObjects[startIndex],
                        inputObjects[startIndex + 1],
                        inputObjects[startIndex + 2],
                        inputObjects[startIndex + 3],
                        inputObjects[startIndex + 4],
                        inputObjects[startIndex + 5],
                        inputObjects[startIndex + 6],
                        inputObjects[startIndex + 7]);
                default:
                    return Tuple.Create(
                        inputObjects[startIndex],
                        inputObjects[startIndex + 1],
                        inputObjects[startIndex + 2],
                        inputObjects[startIndex + 3],
                        inputObjects[startIndex + 4],
                        inputObjects[startIndex + 5],
                        inputObjects[startIndex + 6],
                        ArrayToTuple(inputObjects, startIndex + 7));
            }
        }
    }

    /// <summary>
    /// Emitted by Group-Object when the NoElement option is true.
    /// </summary>
    public sealed class GroupInfoNoElement : GroupInfo
    {
        internal GroupInfoNoElement(OrderByPropertyEntry groupValue) : base(groupValue)
        {
        }

        internal override void Add(PSObject groupValue)
        {
            Count++;
        }
    }

    /// <summary>
    /// Emitted by Group-Object.
    /// </summary>
    [DebuggerDisplay("{Name} ({Count})")]
    public class GroupInfo
    {
        internal GroupInfo(OrderByPropertyEntry groupValue)
        {
            Group = new Collection<PSObject>();
            this.Add(groupValue.inputObject);
            GroupValue = groupValue;
            Name = BuildName(groupValue.orderValues);
        }

        internal virtual void Add(PSObject groupValue)
        {
            Group.Add(groupValue);
            Count++;
        }

        private static string BuildName(List<ObjectCommandPropertyValue> propValues)
        {
            StringBuilder sb = new();
            foreach (ObjectCommandPropertyValue propValue in propValues)
            {
                var propValuePropertyValue = propValue?.PropertyValue;
                if (propValuePropertyValue != null)
                {
                    if (propValuePropertyValue is ICollection propertyValueItems)
                    {
                        sb.Append('{');
                        var length = sb.Length;

                        foreach (object item in propertyValueItems)
                        {
                            sb.AppendFormat(CultureInfo.CurrentCulture, $"{item}, ");
                        }

                        sb = sb.Length > length ? sb.Remove(sb.Length - 2, 2) : sb;
                        sb.Append("}, ");
                    }
                    else
                    {
                        sb.AppendFormat(CultureInfo.CurrentCulture, $"{propValuePropertyValue}, ");
                    }
                }
            }

            return sb.Length >= 2 ? sb.Remove(sb.Length - 2, 2).ToString() : string.Empty;
        }

        /// <summary>
        /// Gets the values of the group.
        /// </summary>
        public ArrayList Values
        {
            get
            {
                ArrayList values = new();
                foreach (ObjectCommandPropertyValue propValue in GroupValue.orderValues)
                {
                    values.Add(propValue.PropertyValue);
                }

                return values;
            }
        }

        /// <summary>
        /// Gets the number of objects in the group.
        /// </summary>
        public int Count { get; internal set; }

        /// <summary>
        /// Gets the list of objects in this group.
        /// </summary>
        public Collection<PSObject> Group { get; }

        /// <summary>
        /// Gets the name of the group.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the OrderByPropertyEntry used to build this group object.
        /// </summary>
        internal OrderByPropertyEntry GroupValue { get; }
    }

    /// <summary>
    /// Group-Object implementation.
    /// </summary>
    [Cmdlet(VerbsData.Group, "Object", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096619", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(Hashtable), typeof(GroupInfo))]
    public class GroupObjectCommand : ObjectBase
    {
        #region tracer

        /// <summary>
        /// An instance of the PSTraceSource class used for trace output.
        /// </summary>
        [TraceSource("GroupObjectCommand", "Class that has group base implementation")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("GroupObjectCommand", "Class that has group base implementation");

        #endregion tracer

        #region Command Line Switches

        /// <summary>
        /// Gets or sets the NoElement parameter indicating of the groups should be flattened.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter NoElement { get; set; }

        /// <summary>
        /// Gets or sets the AsHashTable parameter.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "HashTable")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "HashTable")]
        [Alias("AHT")]
        public SwitchParameter AsHashTable { get; set; }

        /// <summary>
        /// Gets or sets the AsString parameter.
        /// </summary>
        [Parameter(ParameterSetName = "HashTable")]
        public SwitchParameter AsString { get; set; }

        private readonly List<GroupInfo> _groups = new();
        private readonly OrderByProperty _orderByProperty = new();
        private readonly Dictionary<object, GroupInfo> _tupleToGroupInfoMappingDictionary = new();
        private readonly List<OrderByPropertyEntry> _entriesToOrder = new();
        private OrderByPropertyComparer _orderByPropertyComparer;
        private bool _hasProcessedFirstInputObject;
        private bool _hasDifferentValueTypes;
        private Type[] _propertyTypesCandidate;

        #endregion

        #region utils

        /// <summary>
        /// Utility function called by Group-Object to create Groups.
        /// </summary>
        /// <param name="currentObjectEntry">Input object that needs to be grouped.</param>
        /// <param name="noElement">True if we are not accumulating objects.</param>
        /// <param name="groups">List containing Groups.</param>
        /// <param name="groupInfoDictionary">Dictionary used to keep track of the groups with hash of the property values being the key.</param>
        /// <param name="orderByPropertyComparer">The Comparer to be used while comparing to check if new group has to be created.</param>
        private static void DoGrouping(
            OrderByPropertyEntry currentObjectEntry,
            bool noElement,
            List<GroupInfo> groups,
            Dictionary<object, GroupInfo> groupInfoDictionary,
            OrderByPropertyComparer orderByPropertyComparer)
        {
            var currentObjectOrderValues = currentObjectEntry.orderValues;
            if (currentObjectOrderValues != null && currentObjectOrderValues.Count > 0)
            {
                object currentTupleObject = PSTuple.ArrayToTuple(currentObjectOrderValues);

                if (groupInfoDictionary.TryGetValue(currentTupleObject, out var currentGroupInfo))
                {
                    // add this inputObject to an existing group
                    currentGroupInfo.Add(currentObjectEntry.inputObject);
                }
                else
                {
                    bool isCurrentItemGrouped = false;

                    for (int groupsIndex = 0; groupsIndex < groups.Count; groupsIndex++)
                    {
                        // Check if the current input object can be converted to one of the already known types
                        // by looking up in the type to GroupInfo mapping.
                        if (orderByPropertyComparer.Compare(groups[groupsIndex].GroupValue, currentObjectEntry) == 0)
                        {
                            groups[groupsIndex].Add(currentObjectEntry.inputObject);
                            isCurrentItemGrouped = true;
                            break;
                        }
                    }

                    if (!isCurrentItemGrouped)
                    {
                        // create a new group
                        s_tracer.WriteLine("Create a new group: {0}", currentObjectOrderValues);
                        GroupInfo newObjGrp = noElement ? new GroupInfoNoElement(currentObjectEntry) : new GroupInfo(currentObjectEntry);
                        groups.Add(newObjGrp);

                        groupInfoDictionary.Add(currentTupleObject, newObjGrp);
                    }
                }
            }
        }

        /// <summary>
        /// Utility function called by Group-Object to create Groups.
        /// </summary>
        /// <param name="currentObjectEntry">Input object that needs to be grouped.</param>
        /// <param name="noElement">True if we are not accumulating objects.</param>
        /// <param name="groups">List containing Groups.</param>
        /// <param name="groupInfoDictionary">Dictionary used to keep track of the groups with hash of the property values being the key.</param>
        /// <param name="orderByPropertyComparer">The Comparer to be used while comparing to check if new group has to be created.</param>
        private static void DoOrderedGrouping(
            OrderByPropertyEntry currentObjectEntry,
            bool noElement,
            List<GroupInfo> groups,
            Dictionary<object, GroupInfo> groupInfoDictionary,
            OrderByPropertyComparer orderByPropertyComparer)
        {
            var currentObjectOrderValues = currentObjectEntry.orderValues;
            if (currentObjectOrderValues != null && currentObjectOrderValues.Count > 0)
            {
                object currentTupleObject = PSTuple.ArrayToTuple(currentObjectOrderValues);

                if (groupInfoDictionary.TryGetValue(currentTupleObject, out var currentGroupInfo))
                {
                    // add this inputObject to an existing group
                    currentGroupInfo.Add(currentObjectEntry.inputObject);
                }
                else
                {
                    bool isCurrentItemGrouped = false;

                    if (groups.Count > 0)
                    {
                        var lastGroup = groups[groups.Count - 1];

                        // Check if the current input object can be converted to one of the already known types
                        // by looking up in the type to GroupInfo mapping.
                        if (orderByPropertyComparer.Compare(lastGroup.GroupValue, currentObjectEntry) == 0)
                        {
                            lastGroup.Add(currentObjectEntry.inputObject);
                            isCurrentItemGrouped = true;
                        }
                    }

                    if (!isCurrentItemGrouped)
                    {
                        // create a new group
                        s_tracer.WriteLine("Create a new group: {0}", currentObjectOrderValues);
                        GroupInfo newObjGrp = noElement
                            ? new GroupInfoNoElement(currentObjectEntry)
                            : new GroupInfo(currentObjectEntry);

                        groups.Add(newObjGrp);

                        groupInfoDictionary.Add(currentTupleObject, newObjGrp);
                    }
                }
            }
        }

        private void WriteNonTerminatingError(Exception exception, string resourceIdAndErrorId, ErrorCategory category)
        {
            Exception ex = new(StringUtil.Format(resourceIdAndErrorId), exception);
            WriteError(new ErrorRecord(ex, resourceIdAndErrorId, category, null));
        }

        #endregion utils

        /// <summary>
        /// Process every input object to group them.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (InputObject != null && InputObject != AutomationNull.Value)
            {
                OrderByPropertyEntry currentEntry;

                if (!_hasProcessedFirstInputObject)
                {
                    Property ??= OrderByProperty.GetDefaultKeyPropertySet(InputObject);

                    _orderByProperty.ProcessExpressionParameter(this, Property);

                    if (AsString && !AsHashTable)
                    {
                        ArgumentException ex = new(UtilityCommonStrings.GroupObjectWithHashTable);
                        ErrorRecord er = new(ex, "ArgumentException", ErrorCategory.InvalidArgument, AsString);
                        ThrowTerminatingError(er);
                    }

                    if (AsHashTable && !AsString && (Property != null && (Property.Length > 1 || _orderByProperty.MshParameterList.Count > 1)))
                    {
                        ArgumentException ex = new(UtilityCommonStrings.GroupObjectSingleProperty);
                        ErrorRecord er = new(ex, "ArgumentException", ErrorCategory.InvalidArgument, Property);
                        ThrowTerminatingError(er);
                    }

                    currentEntry = _orderByProperty.CreateOrderByPropertyEntry(this, InputObject, CaseSensitive, _cultureInfo);
                    bool[] ascending = new bool[currentEntry.orderValues.Count];
                    for (int index = 0; index < currentEntry.orderValues.Count; index++)
                    {
                        ascending[index] = true;
                    }

                    _orderByPropertyComparer = new OrderByPropertyComparer(ascending, _cultureInfo, CaseSensitive);

                    _hasProcessedFirstInputObject = true;
                }
                else
                {
                    currentEntry = _orderByProperty.CreateOrderByPropertyEntry(this, InputObject, CaseSensitive, _cultureInfo);
                }

                _entriesToOrder.Add(currentEntry);

                var currentEntryOrderValues = currentEntry.orderValues;
                if (!_hasDifferentValueTypes)
                {
                    UpdateOrderPropertyTypeInfo(currentEntryOrderValues);
                }
            }
        }

        private void UpdateOrderPropertyTypeInfo(List<ObjectCommandPropertyValue> currentEntryOrderValues)
        {
            if (_propertyTypesCandidate == null)
            {
                _propertyTypesCandidate = currentEntryOrderValues.Select(static c => PSObject.Base(c.PropertyValue)?.GetType()).ToArray();
                return;
            }

            if (_propertyTypesCandidate.Length != currentEntryOrderValues.Count)
            {
                _hasDifferentValueTypes = true;
                return;
            }

            // check all the types we group on.
            // if we find more than one set of types, _hasDifferentValueTypes is set to true,
            // and we are forced to take a slower code path when we group our objects
            for (int i = 0; i < _propertyTypesCandidate.Length; i++)
            {
                var candidateType = _propertyTypesCandidate[i];
                var propertyType = PSObject.Base(currentEntryOrderValues[i].PropertyValue)?.GetType();
                if (propertyType == null)
                {
                    // we ignore properties without values. We can always compare against null.
                    continue;
                }

                // if we haven't gotten a type for a property yet, update it when we do get a value
                if (propertyType != candidateType)
                {
                    if (candidateType == null)
                    {
                        _propertyTypesCandidate[i] = propertyType;
                    }
                    else
                    {
                        _hasDifferentValueTypes = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Completes the processing of the gathered group objects.
        /// </summary>
        protected override void EndProcessing()
        {
            if (!_hasDifferentValueTypes)
            {
                // using OrderBy to get stable sort.
                // fast path when we only have the same object types to group
                foreach (var entry in _entriesToOrder.Order(_orderByPropertyComparer))
                {
                    DoOrderedGrouping(entry, NoElement, _groups, _tupleToGroupInfoMappingDictionary, _orderByPropertyComparer);
                    if (Stopping)
                    {
                        return;
                    }
                }
            }
            else
            {
                foreach (var entry in _entriesToOrder)
                {
                    DoGrouping(entry, NoElement, _groups, _tupleToGroupInfoMappingDictionary, _orderByPropertyComparer);
                    if (Stopping)
                    {
                        return;
                    }
                }
            }

            s_tracer.WriteLine(_groups.Count);
            if (_groups.Count > 0)
            {
                if (AsHashTable.IsPresent)
                {
                    StringComparer comparer = CaseSensitive.IsPresent
                        ? StringComparer.CurrentCulture
                        : StringComparer.CurrentCultureIgnoreCase;
                    var hashtable = new Hashtable(comparer);
                    try
                    {
                        if (AsString)
                        {
                            foreach (GroupInfo grp in _groups)
                            {
                                hashtable.Add(grp.Name, grp.Group);
                            }
                        }
                        else
                        {
                            foreach (GroupInfo grp in _groups)
                            {
                                hashtable.Add(PSObject.Base(grp.Values[0]), grp.Group);
                            }
                        }
                    }
                    catch (ArgumentException e)
                    {
                        WriteNonTerminatingError(e, UtilityCommonStrings.InvalidOperation, ErrorCategory.InvalidArgument);
                        return;
                    }

                    WriteObject(hashtable);
                }
                else
                {
                    WriteObject(_groups, true);
                }
            }
        }
    }
}
