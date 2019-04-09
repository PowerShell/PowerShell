// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// </summary>
    [Cmdlet("Sort",
            "Object",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113403",
            DefaultParameterSetName = "Default",
            RemotingCapability = RemotingCapability.None)]
    public sealed class SortObjectCommand : OrderObjectBase
    {
        #region Command Line Switches

        /// <summary>
        /// Gets or sets a value indicating whether a stable sort is required.
        /// </summary>
        /// <value></value>
        /// <remarks>
        /// Items that are duplicates according to the sort algorithm will appear
        /// in the same relative order in a stable sort.
        /// </remarks>
        [Parameter(ParameterSetName = "Default")]
        public SwitchParameter Stable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sort order is descending.
        /// </summary>
        [Parameter]
        public SwitchParameter Descending
        {
            get { return DescendingOrder; }

            set { DescendingOrder = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the sort filters out any duplicate objects.
        /// </summary>
        /// <value></value>
        [Parameter]
        public SwitchParameter Unique { get; set; }

        #endregion

        /// <summary>
        /// Gets or sets the number of items to return in a Top N sort.
        /// </summary>
        [Parameter(ParameterSetName = "Top", Mandatory = true)]
        [ValidateRange(1, int.MaxValue)]
        public int Top { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of items to return in a Bottom N sort.
        /// </summary>
        [Parameter(ParameterSetName = "Bottom", Mandatory = true)]
        [ValidateRange(1, int.MaxValue)]
        public int Bottom { get; set; } = 0;

        /// <summary>
        /// Moves unique entries to the front of the list.
        /// </summary>
        private int MoveUniqueEntriesToFront(List<OrderByPropertyEntry> sortedData, OrderByPropertyComparer comparer)
        {
            // If we have sorted data then we know we have at least one unique item
            int uniqueCount = sortedData.Count > 0 ? 1 : 0;

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

            return uniqueCount;
        }

        /// <summary>
        /// Sort unsorted OrderByPropertyEntry data using a full sort.
        /// </summary>
        private int FullSort(List<OrderByPropertyEntry> dataToSort, OrderByPropertyComparer comparer)
        {
            // Track how many items in the list are sorted
            int sortedItemCount = dataToSort.Count;

            // Future: It may be worth comparing List.Sort with SortedSet when handling unique
            // records in case SortedSet is faster (SortedSet was not an option in earlier
            // versions of PowerShell).
            dataToSort.Sort(comparer);

            if (Unique)
            {
                // Move unique entries to the front of the list (this is significantly faster
                // than removing them)
                sortedItemCount = MoveUniqueEntriesToFront(dataToSort, comparer);
            }

            return sortedItemCount;
        }

        /// <summary>
        /// Sort unsorted OrderByPropertyEntry data using an indexed min-/max-heap sort.
        /// </summary>
        private int Heapify(List<OrderByPropertyEntry> dataToSort, OrderByPropertyComparer orderByPropertyComparer)
        {
            // Instantiate the Heapify comparer, which takes index into account for sort stability
            var comparer = new IndexedOrderByPropertyComparer(orderByPropertyComparer);

            // Identify how many items will be in the heap and the current number of items
            int heapCount = 0;
            int heapCapacity = Stable ? int.MaxValue
                                      : Top > 0 ? Top : Bottom;

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

            // For unique sorts, use a sorted set to avoid adding unique items to the heap
            SortedSet<OrderByPropertyEntry> uniqueSet = Unique ? new SortedSet<OrderByPropertyEntry>(orderByPropertyComparer) : null;

            // Tracking the index is necessary so that unsortable items can be output at the end, in the order
            // in which they were received.
            for (int dataIndex = 0, discardedDuplicates = 0; dataIndex < dataToSort.Count - discardedDuplicates; dataIndex++)
            {
                // Min-heap: if the heap is full and the root item is larger than the entry, discard the entry
                // Max-heap: if the heap is full and the root item is smaller than the entry, discard the entry
                if (heapCount == heapCapacity && comparer.Compare(dataToSort[0], dataToSort[dataIndex]) == comparator)
                {
                    continue;
                }

                // If we're doing a unique sort and the entry is not unique, discard the duplicate entry
                if (Unique && !uniqueSet.Add(dataToSort[dataIndex]))
                {
                    discardedDuplicates++;
                    if (dataIndex != dataToSort.Count - discardedDuplicates)
                    {
                        // When discarding duplicates, replace them with an item at the end of the list and
                        // adjust our counter so that we check the item we just swapped in next
                        dataToSort[dataIndex] = dataToSort[dataToSort.Count - discardedDuplicates];
                        dataIndex--;
                    }

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

            return heapCount;
        }

        /// <summary>
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

            // Track the number of items that will be output from the data once it is sorted
            int sortedItemCount = dataToProcess.Count;

            // If -Stable, -Top & -Bottom were not used, invoke an in-place full sort
            if (!Stable && Top == 0 && Bottom == 0)
            {
                sortedItemCount = FullSort(dataToProcess, comparer);
            }
            // Otherwise, use an indexed min-/max-heap to perform an in-place heap sort (heap
            // sorts are inheritantly stable, meaning they will preserve the respective order
            // of duplicate objects as they are sorted on the heap)
            else
            {
                sortedItemCount = Heapify(dataToProcess, comparer);
            }

            // Write out the portion of the processed data that was sorted
            for (int index = 0; index < sortedItemCount; index++)
            {
                WriteObject(dataToProcess[index].inputObject);
            }
        }
    }
}
