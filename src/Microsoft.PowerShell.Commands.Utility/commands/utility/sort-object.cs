/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// 
    /// </summary>
    [Cmdlet("Sort",
            "Object",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113403",
            DefaultParameterSetName="Default",
            RemotingCapability = RemotingCapability.None)]
    public sealed class SortObjectCommand : OrderObjectBase
    {
        #region Command Line Switches
        /// <summary>
        /// This param specifies if sort order is ascending. 
        /// </summary>
        [Parameter]
        public SwitchParameter Descending
        {
            get { return DescendingOrder; }
            set { DescendingOrder = value; }
        }
        /// <summary>
        /// This param specifies if only unique objects are filtered.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Unique
        {
            get { return _unique; }
            set { _unique = value; }
        }
        private bool _unique;
        #endregion

        /// <summary>
        /// This param specifies you only want the top N items returned.
        /// </summary>
        [Parameter(ParameterSetName="Default")]
        [ValidateRange(1,int.MaxValue)]
        public int Top { get; set; } = 0;

        /// <summary>
        /// This param specifies you only want the bottom N items returned.
        /// </summary>
        [Parameter(ParameterSetName="Bottom", Mandatory=true)]
        [ValidateRange(1,int.MaxValue)]
        public int Bottom { get; set; } = 0;

        /// <summary>
        /// Remove duplicates.
        /// </summary>
        private static void RemoveDuplicates(OrderByProperty orderByProperty)
        {
            int current = 0, lookAhead;
            OrderByPropertyEntry currentObj = orderByProperty.OrderMatrix[current];
            while (current + 1 < orderByProperty.OrderMatrix.Count)
            {
                lookAhead = current + 1;
                OrderByPropertyEntry lookAheadObj = orderByProperty.OrderMatrix[lookAhead];

                if (orderByProperty.Comparer.Compare(currentObj, lookAheadObj) == 0)
                {
                    orderByProperty.OrderMatrix.RemoveAt(lookAhead);
                }
                else
                {
                    current = lookAhead;
                    currentObj = lookAheadObj;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void EndProcessing()
        {
            OrderByProperty orderByProperty = new OrderByProperty(
                this, InputObjects, Property, !Descending, ConvertedCulture, CaseSensitive);
            if (orderByProperty.Comparer == null || orderByProperty.OrderMatrix == null || orderByProperty.OrderMatrix.Count == 0)
            {
                return;
            }

            // If -Unique was used, if -Top & -Bottom were not used, or of -Top or -Bottom would return all objects, sort
            // all objects, remove duplicates if necessary, and identify the list of items to return
            if (_unique || (Top == 0 && Bottom == 0) || Top >= orderByProperty.OrderMatrix.Count || Bottom >= orderByProperty.OrderMatrix.Count)
            {
                orderByProperty.OrderMatrix.Sort(orderByProperty.Comparer);

                if (_unique)
                {
                    RemoveDuplicates(orderByProperty);
                }

                var listToOutput = orderByProperty.OrderMatrix;

                if (Top > 0 && Top < listToOutput.Count)
                {
                    // There may be a faster algorithm to sort while eliminating duplicates and retaining only
                    // the top N records; for now though, this gets the job done
                    listToOutput = listToOutput.GetRange(0, Top);
                }
                else if (Bottom > 0 && Bottom < listToOutput.Count)
                {
                    int lastItemIndex = listToOutput.Count - 1;
                    listToOutput = listToOutput.GetRange(lastItemIndex - Bottom, lastItemIndex);
                }

                // write to output stream
                foreach (var x in listToOutput)
                {
                    WriteObject(x.inputObject);
                }
            }
            // Otherwise, use an indexed min-/max-heap to sort all objects
            else
            {
                // The heap entry comparer will sort the items in the heap
                var comparer = new IndexedMinMaxHeapComparer(orderByProperty.Comparer);

                // Identify how many items will be in the heap
                int indexedMinMaxHeapSize = Top > 0 ? Top : Bottom;

                // A small list will store the data for the heap 
                var indexedMinMaxHeap = new List<IndexedMinMaxHeapEntry>(indexedMinMaxHeapSize + 1);

                // Identify the comparator (the value all comparisons will be made against based on whether we're
                // doing a Top N or Bottom N sort)
                // Note: All comparison results in the loop below are performed related to the value of the
                // comparator. OrderByPropertyComparer.Compare will return -1 to indicate that the lhs is smaller
                // if an ascending sort is being executed, or -1 to indicate that the lhs is larger if a descending
                // sort is being executed. The comparator will be -1 if we're executing a Top N sort, or 1 if we're
                // executing a Bottom N sort. These two pairs of states allow us to perform the proper comparison
                // regardless of whether we're executing an ascending or descending Top N or Bottom N sort. This
                // allows us to build a min-heap or max-heap for each of these sorts with the exact same logic.
                // Min-heap: used for faster processing of a top N descending sort and a bottom N ascending sort 
                // Max-heap: used for faster processing of a top N ascending sort and a bottom N descending sort 
                int comparator = Top > 0 ? -1 : 1;

                // Tracking the index is necessary so that unsortable items can be output at the end, in the order
                // in which they were received.
                int entryIndex = 0;
                foreach (var orderMatrixEntry in orderByProperty.OrderMatrix)
                {
                    // Create a new indexed entry
                    var entry = new IndexedMinMaxHeapEntry(orderMatrixEntry, entryIndex++); 

                    // Min-heap: if the heap is full and the root item is larger than the entry, discard the entry
                    // Max-heap: if the heap is full and the root item is smaller than the entry, discard the entry
                    if (indexedMinMaxHeap.Count == indexedMinMaxHeapSize && comparer.Compare(indexedMinMaxHeap[0], entry) == comparator)
                    {
                        continue;
                    }

                    indexedMinMaxHeap.Add(entry);
                    int childIndex = indexedMinMaxHeap.Count - 1;
                    while (childIndex > 0)
                    {
                        int parentIndex = (childIndex - 1) >> 1;

                        // Min-heap: if the child item is larger than its parent, break
                        // Max-heap: if the child item is smaller than its parent, break
                        if (comparer.Compare(indexedMinMaxHeap[childIndex], indexedMinMaxHeap[parentIndex]) == comparator)
                        {
                            break;
                        }

                        var temp = indexedMinMaxHeap[parentIndex];
                        indexedMinMaxHeap[parentIndex] = indexedMinMaxHeap[childIndex];
                        indexedMinMaxHeap[childIndex] = temp;

                        childIndex = parentIndex;
                    }

                    // If the heap size is too large, remove the root and rearrange the heap
                    if (indexedMinMaxHeap.Count > indexedMinMaxHeapSize)
                    {
                        // Move the last item added to the root 
                        indexedMinMaxHeap[0] = indexedMinMaxHeap[indexedMinMaxHeapSize];
                        indexedMinMaxHeap.RemoveAt(indexedMinMaxHeapSize);

                        // Bubble the root item down into the correct position
                        int parentIndex = 0;
                        int parentItemCount = indexedMinMaxHeap.Count >> 1;
                        while (parentIndex < parentItemCount)
                        {
                            // Min-heap: use the smaller of the two children in the comparison
                            // Max-heap: use the larger of the two children in the comparison
                            int leftChildIndex = (parentIndex << 1) + 1;
                            int rightChildIndex = leftChildIndex + 1;
                            childIndex = rightChildIndex == indexedMinMaxHeap.Count || comparer.Compare(indexedMinMaxHeap[leftChildIndex], indexedMinMaxHeap[rightChildIndex]) != comparator
                                ? leftChildIndex
                                : rightChildIndex;

                            // Min-heap: if the smallest child is larger than or equal to its parent, break
                            // Max-heap: if the largest child is smaller than or equal to its parent, break
                            int childComparisonResult = comparer.Compare(indexedMinMaxHeap[childIndex], indexedMinMaxHeap[parentIndex]); 
                            if (childComparisonResult == 0 || childComparisonResult == comparator)
                            {
                                break;
                            }

                            var temp = indexedMinMaxHeap[childIndex];
                            indexedMinMaxHeap[childIndex] = indexedMinMaxHeap[parentIndex];
                            indexedMinMaxHeap[parentIndex] = temp;

                            parentIndex = childIndex;
                        }
                    }
                }

                indexedMinMaxHeap.Sort(comparer);

                foreach (var entry in indexedMinMaxHeap)
                {
                    WriteObject(entry.Entry.inputObject);
                }
            }
        }
    }

    /// <summary>
    /// This is a single entry in the min-/max-heap used for top N/bottom N sorts
    /// </summary>
    public sealed class IndexedMinMaxHeapEntry
    {
        internal IndexedMinMaxHeapEntry(OrderByPropertyEntry orderByPropertyEntry, int index)
        {
            Entry = orderByPropertyEntry;
            Index = index;
            foreach (var value in Entry.orderValues)
            {
                if (value.IsExistingProperty)
                {
                    Sortable = true;
                    break;
                }
            }
        }
        
        internal OrderByPropertyEntry Entry { get; } = null;
        internal int Index { get; } = -1;
        internal bool Sortable { get; } = false;
    }

    internal class IndexedMinMaxHeapComparer : IComparer<IndexedMinMaxHeapEntry>
    {
        internal IndexedMinMaxHeapComparer(OrderByPropertyComparer orderByPropertyComparer)
        {
            _orderByPropertyComparer = orderByPropertyComparer;
        }

        public int Compare(IndexedMinMaxHeapEntry lhs, IndexedMinMaxHeapEntry rhs)
        {
            // Push non-sortable items to the end, regardless of the type of sort being used
            if (lhs.Sortable != rhs.Sortable)
            {
                return lhs.Sortable.CompareTo(rhs.Sortable) * -1;
            }
            int result = _orderByPropertyComparer.Compare(lhs.Entry, rhs.Entry);
            // When items are identical according to the internal comparison, sort by index
            // to preserve the original order
            if (result == 0)
            {
                return lhs.Index.CompareTo(rhs.Index);
            }
            // Otherwise, return the internal comparison sort results
            return result;
        }

        OrderByPropertyComparer _orderByPropertyComparer = null;
    }    
}