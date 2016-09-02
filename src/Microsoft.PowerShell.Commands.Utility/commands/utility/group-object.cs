/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
        /// <param name="inputObjects">Input objects used to create a tuple.</param>
        /// <returns>Tuple object.</returns>
        internal static object ArrayToTuple(object[] inputObjects)
        {
            Diagnostics.Assert(inputObjects != null, "inputObjects is null");
            Diagnostics.Assert(inputObjects.Length > 0, "inputObjects is empty");

            return ArrayToTuple(inputObjects, 0);
        }

        /// <summary>
        /// ArrayToTuple is a helper method used to create a tuple for the supplied input array.
        /// </summary>
        /// <param name="inputObjects">Input objects used to create a tuple</param>
        /// <param name="startIndex">Start index of the array from which the objects have to considered for the tuple creation.</param>
        /// <returns>Tuple object.</returns>
        internal static object ArrayToTuple(object[] inputObjects, int startIndex)
        {
            Diagnostics.Assert(inputObjects != null, "inputObjects is null");
            Diagnostics.Assert(inputObjects.Length > 0, "inputObjects is empty");

            switch (inputObjects.Length - startIndex)
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
                    return Tuple.Create(inputObjects[startIndex], inputObjects[startIndex + 1], inputObjects[startIndex + 2], inputObjects[startIndex + 3], inputObjects[startIndex + 4]);
                case 6:
                    return Tuple.Create(inputObjects[startIndex], inputObjects[startIndex + 1], inputObjects[startIndex + 2], inputObjects[startIndex + 3], inputObjects[startIndex + 4],
                        inputObjects[startIndex + 5]);
                case 7:
                    return Tuple.Create(inputObjects[startIndex], inputObjects[startIndex + 1], inputObjects[startIndex + 2], inputObjects[startIndex + 3], inputObjects[startIndex + 4],
                        inputObjects[startIndex + 5], inputObjects[startIndex + 6]);
                case 8:
                    return Tuple.Create(inputObjects[startIndex], inputObjects[startIndex + 1], inputObjects[startIndex + 2], inputObjects[startIndex + 3], inputObjects[startIndex + 4],
                        inputObjects[startIndex + 5], inputObjects[startIndex + 6], inputObjects[startIndex + 7]);
                default:
                    return Tuple.Create(inputObjects[startIndex], inputObjects[startIndex + 1], inputObjects[startIndex + 2], inputObjects[startIndex + 3], inputObjects[startIndex + 4],
                        inputObjects[startIndex + 5], inputObjects[startIndex + 6], ArrayToTuple(inputObjects, startIndex + 7));
            }
        }
    }

    /// <summary>
    /// Emitted by Group-Object when the NoElement option is true
    /// </summary>
    public sealed class GroupInfoNoElement : GroupInfo
    {
        internal GroupInfoNoElement(OrderByPropertyEntry groupValue)
            : base(groupValue)
        {
        }

        internal override void Add(PSObject groupValue)
        {
            Count++;
        }
    }

    /// <summary>
    /// Emitted by Group-Object
    /// </summary>
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
            StringBuilder sb = new StringBuilder();
            foreach (ObjectCommandPropertyValue propValue in propValues)
            {
                if (propValue != null && propValue.PropertyValue != null)
                {
                    var propertyValueItems = propValue.PropertyValue as ICollection;
                    if (propertyValueItems != null)
                    {
                        sb.Append("{");
                        var length = sb.Length;

                        foreach (object item in propertyValueItems)
                        {
                            sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}, ", item.ToString()));
                        }

                        sb = sb.Length > length ? sb.Remove(sb.Length - 2, 2) : sb;
                        sb.Append("}, ");
                    }
                    else
                    {
                        sb.Append(string.Format(CultureInfo.InvariantCulture, "{0}, ", propValue.PropertyValue.ToString()));
                    }
                }
            }
            return sb.Length >= 2 ? sb.Remove(sb.Length - 2, 2).ToString() : string.Empty;
        }


        /// <summary>
        /// 
        /// Values of the group
        ///
        /// </summary>
        public ArrayList Values
        {
            get
            {
                ArrayList values = new ArrayList();
                foreach (ObjectCommandPropertyValue propValue in GroupValue.orderValues)
                {
                    values.Add(propValue.PropertyValue);
                }
                return values;
            }
        }

        /// <summary>
        ///
        /// Number of objects in the group
        ///
        /// </summary>
        public int Count { get; internal set; }

        /// <summary>
        ///
        /// The list of objects in this group
        ///
        /// </summary>
        public Collection<PSObject> Group { get; } = null;

        /// <summary>
        ///
        /// The name of the group
        ///
        /// </summary>
        public string Name { get; } = null;

        /// <summary>
        ///
        /// The OrderByPropertyEntry used to build this group object
        ///
        /// </summary>
        internal OrderByPropertyEntry GroupValue { get; } = null;
    }

    /// <summary>
    ///
    /// Group-Object implementation
    ///
    /// </summary>
    [Cmdlet("Group", "Object", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113338", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(Hashtable), typeof(GroupInfo))]
    public class GroupObjectCommand : ObjectBase
    {
        #region tracer

        /// <summary>
        /// An instance of the PSTraceSource class used for trace output
        /// </summary>
        [TraceSourceAttribute(
             "GroupObjectCommand",
             "Class that has group base implementation")]
        private static PSTraceSource s_tracer =
            PSTraceSource.GetTracer("GroupObjectCommand",
             "Class that has group base implementation");

        #endregion tracer

        #region Command Line Switches

        /// <summary>
        /// 
        /// Flatten the groups
        /// 
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter NoElement
        {
            get { return _noElement; }
            set { _noElement = value; }
        }
        private bool _noElement;
        /// <summary>
        /// the AsHashTable parameter
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "HashTable")]
        [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "HashTable")]
        [Alias("AHT")]
        public SwitchParameter AsHashTable { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = "HashTable")]
        public SwitchParameter AsString { get; set; }

        private List<GroupInfo> _groups = new List<GroupInfo>();
        private OrderByProperty _orderByProperty = new OrderByProperty();
        private bool _hasProcessedFirstInputObject;
        private Dictionary<object, GroupInfo> _tupleToGroupInfoMappingDictionary = new Dictionary<object, GroupInfo>();
        private OrderByPropertyComparer _orderByPropertyComparer = null;

        #endregion

        #region utils

        /// <summary>
        /// Utility function called by Group-Object to create Groups.
        /// </summary>
        /// <param name="currentObjectEntry">Input object that needs to be grouped.</param>
        /// <param name="noElement">true if we are not accumulating objects</param>
        /// <param name="groups">List containing Groups.</param>
        /// <param name="groupInfoDictionary">Dictionary used to keep track of the groups with hash of the property values being the key.</param>
        /// <param name="orderByPropertyComparer">The Comparer to be used while comparing to check if new group has to be created.</param>
        internal static void DoGrouping(OrderByPropertyEntry currentObjectEntry, bool noElement, List<GroupInfo> groups, Dictionary<object, GroupInfo> groupInfoDictionary,
            OrderByPropertyComparer orderByPropertyComparer)
        {
            if (currentObjectEntry != null && currentObjectEntry.orderValues != null && currentObjectEntry.orderValues.Count > 0)
            {
                object currentTupleObject = PSTuple.ArrayToTuple(currentObjectEntry.orderValues.ToArray());

                GroupInfo currentGroupInfo = null;
                if (groupInfoDictionary.TryGetValue(currentTupleObject, out currentGroupInfo))
                {
                    if (currentGroupInfo != null)
                    {
                        //add this inputObject to an existing group
                        currentGroupInfo.Add(currentObjectEntry.inputObject);
                    }
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
                        s_tracer.WriteLine("Create a new group: {0}", currentObjectEntry.orderValues);
                        GroupInfo newObjGrp = noElement ? new GroupInfoNoElement(currentObjectEntry) : new GroupInfo(currentObjectEntry);
                        groups.Add(newObjGrp);

                        groupInfoDictionary.Add(currentTupleObject, newObjGrp);
                    }
                }
            }
        }
        private void WriteNonTerminatingError(Exception exception, string resourceIdAndErrorId,
      ErrorCategory category)
        {
            Exception ex = new Exception(StringUtil.Format(resourceIdAndErrorId), exception);
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
                OrderByPropertyEntry currentEntry = null;

                if (!_hasProcessedFirstInputObject)
                {
                    if (Property == null)
                    {
                        Property = OrderByProperty.GetDefaultKeyPropertySet(InputObject);
                    }
                    _orderByProperty.ProcessExpressionParameter(this, Property);

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

                DoGrouping(currentEntry, this.NoElement, _groups, _tupleToGroupInfoMappingDictionary, _orderByPropertyComparer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void EndProcessing()
        {
            s_tracer.WriteLine(_groups.Count);
            if (_groups.Count > 0)
            {
                if (AsHashTable)
                {
                    Hashtable _table = CollectionsUtil.CreateCaseInsensitiveHashtable();
                    try
                    {
                        foreach (GroupInfo _grp in _groups)
                        {
                            if (AsString)
                            {
                                _table.Add(_grp.Name, _grp.Group);
                            }
                            else
                            {
                                if (_grp.Values.Count == 1)
                                {
                                    _table.Add(_grp.Values[0], _grp.Group);
                                }
                                else
                                {
                                    ArgumentException ex = new ArgumentException(UtilityCommonStrings.GroupObjectSingleProperty);
                                    ErrorRecord er = new ErrorRecord(ex, "ArgumentException", ErrorCategory.InvalidArgument, Property);
                                    ThrowTerminatingError(er);
                                }
                            }
                        }
                    }
                    catch (ArgumentException e)
                    {
                        WriteNonTerminatingError(e, UtilityCommonStrings.InvalidOperation, ErrorCategory.InvalidArgument);
                        return;
                    }
                    WriteObject(_table);
                }
                else
                {
                    if (AsString)
                    {
                        ArgumentException ex = new ArgumentException(UtilityCommonStrings.GroupObjectWithHashTable);
                        ErrorRecord er = new ErrorRecord(ex, "ArgumentException", ErrorCategory.InvalidArgument, AsString);
                        ThrowTerminatingError(er);
                    }
                    else
                    {
                        WriteObject(_groups, true);
                    }
                }
            }
        }
    }
}

