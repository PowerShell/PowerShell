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
        /// Moves unique entries to the front of the list.
        /// </summary>
        private void MoveUniqueEntriesToFront(List<OrderByPropertyEntry> sortedData, OrderByPropertyComparer comparer, out int uniqueCount)
        {
            // If we have sorted data then we know we have at least one unique item
            uniqueCount = sortedData.Count > 0 ? 1 : 0;
            // Move the first of each unique entry to the front of the list
            for (int uniqueItemIndex = 0, nextUniqueItemIndex = 1; uniqueItemIndex < sortedData.Count && uniqueCount != Top; uniqueItemIndex++, nextUniqueItemIndex++)
            {
                // Identify the index of the next unique item
                while (nextUniqueItemIndex < sortedData.Count && comparer.Compare(sortedData[uniqueItemIndex], sortedData[nextUniqueItemIndex]) == 0)
                {
                    nextUniqueItemIndex++;
                }

                // If there are no more unique items, break
                if (nextUniqueItemIndex == sortedData.Count)
                {
                    break;
                }

                // Move the next unique item forward and increment the unique item counter
                sortedData[uniqueItemIndex + 1] = sortedData[nextUniqueItemIndex];
                uniqueCount++;
            }
        }

        /// <summary>
        /// Sort unsorted OrderByPropertyEntry data using an indexed min-/max-heap sort
        /// </summary>
        private List<OrderByPropertyEntry> Heapify(List<OrderByPropertyEntry> dataToSort, OrderByPropertyComparer orderByPropertyComparer, out int heapCount)
        {
            // Instantiate the Heapify comparer, which takes index into account for sort stability
            var comparer = new IndexedOrderByPropertyComparer(orderByPropertyComparer);

            // Identify how many items will be in the heap and the current number of items
            int heapCapacity = Top > 0 ? Top : Bottom;
            heapCount = 0;

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
            for (int dataIndex = 0; dataIndex < dataToSort.Count; dataIndex++)
            {
                // Min-heap: if the heap is full and the root item is larger than the entry, discard the entry
                // Max-heap: if the heap is full and the root item is smaller than the entry, discard the entry
                if (heapCount == heapCapacity && comparer.Compare(dataToSort[0], dataToSort[dataIndex]) == comparator)
                {
                    continue;
                }

                // Add the current item to the heap and bubble it up into the correct position
                int childIndex = dataIndex;
                while (childIndex > 0)
                {
                    int parentIndex = ((childIndex > (heapCapacity - 1) ? heapCapacity : childIndex) - 1) >> 1;

                    // Min-heap: if the child item is larger than its parent, break
                    // Max-heap: if the child item is smaller than its parent, break
                    if (comparer.Compare(dataToSort[childIndex], dataToSort[parentIndex]) == comparator)
                    {
                        break;
                    }

                    var temp = dataToSort[parentIndex];
                    dataToSort[parentIndex] = dataToSort[childIndex];
                    dataToSort[childIndex] = temp;

                    childIndex = parentIndex;
                }
                heapCount++;

                // If the heap size is too large, remove the root and rearrange the heap
                if (heapCount > heapCapacity)
                {
                    // Move the last item to the root and reset the heap count (this effectively removes the last item)
                    dataToSort[0] = dataToSort[dataIndex];
                    heapCount = heapCapacity;

                    // Bubble the root item down into the correct position
                    int parentIndex = 0;
                    int parentItemCount = heapCapacity >> 1;
                    while (parentIndex < parentItemCount)
                    {
                        // Min-heap: use the smaller of the two children in the comparison
                        // Max-heap: use the larger of the two children in the comparison
                        int leftChildIndex = (parentIndex << 1) + 1;
                        int rightChildIndex = leftChildIndex + 1;
                        childIndex = rightChildIndex == heapCapacity || comparer.Compare(dataToSort[leftChildIndex], dataToSort[rightChildIndex]) != comparator
                            ? leftChildIndex
                            : rightChildIndex;

                        // Min-heap: if the smallest child is larger than or equal to its parent, break
                        // Max-heap: if the largest child is smaller than or equal to its parent, break
                        int childComparisonResult = comparer.Compare(dataToSort[childIndex], dataToSort[parentIndex]);
                        if (childComparisonResult == 0 || childComparisonResult == comparator)
                        {
                            break;
                        }

                        var temp = dataToSort[childIndex];
                        dataToSort[childIndex] = dataToSort[parentIndex];
                        dataToSort[parentIndex] = temp;

                        parentIndex = childIndex;
                    }
                }
            }

            dataToSort.Sort(0, heapCount, comparer);

            return dataToSort;
        }

        /// <summary>
        /// Outputs a subset of the sorted data
        /// </summary>
        private void WritePartial(List<OrderByPropertyEntry> sortedData, int startIndex, int endIndex)
        {
            for (int index = startIndex; index <= endIndex; index++)
            {
                WriteObject(sortedData[index].inputObject);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void EndProcessing()
        {
            OrderByProperty orderByProperty = new OrderByProperty(
                this, InputObjects, Property, !Descending, ConvertedCulture, CaseSensitive);

            var dataToProcess = orderByProperty.OrderMatrix;
            var comparer = orderByProperty.Comparer;
            if (comparer == null || dataToProcess == null || dataToProcess.Count == 0)
            {
                return;
            }

            // Track the range of sorted data in the data that we are processing
            int outputStartIndex = 0;
            int outputEndIndex = dataToProcess.Count - 1;

            // If -Unique was used, if -Top & -Bottom were not used, or if -Top or -Bottom would return all objects, sort
            // all objects, remove duplicates if necessary, and identify the list of items to return
            if (_unique || (Top == 0 && Bottom == 0) || Top >= dataToProcess.Count || Bottom >= dataToProcess.Count)
            {
                // Note: It may be worth compariing List.Sort with SortedSet.Sort when handling unique records in
                // case SortedSet.Sort is faster (SortedSet was not an option in earlier versions of PowerShell).
                dataToProcess.Sort(comparer);
                int dataToProcessCount = dataToProcess.Count;

                if (_unique)
                {
                    // Move unique entries to the front of the list (this is significantly faster than deleting them)
                    int uniqueCount = 0;
                    MoveUniqueEntriesToFront(dataToProcess, comparer, out uniqueCount);
                    dataToProcessCount = uniqueCount;
                    outputEndIndex = uniqueCount - 1;
                }

                if (Top > 0 && Top < dataToProcessCount)
                {
                    // Write the top N list items to the output stream
                    outputEndIndex = Top - 1;
                }
                else if (Bottom > 0 && Bottom < dataToProcessCount)
                {
                    // Write the bottom N list items to the output stream
                    outputStartIndex = outputEndIndex - Bottom + 1;
                }
            }
            // Otherwise, use an indexed min-/max-heap to perform an in-place sort of all objects
            else
            {
                int heapCount = 0;
                var indexedMinMaxHeap = Heapify(dataToProcess, comparer, out heapCount);
                outputEndIndex = heapCount - 1;
            }

            // Write out the portion of the processed data that was requested
            WritePartial(dataToProcess, outputStartIndex, outputEndIndex);
        }
    }
}
