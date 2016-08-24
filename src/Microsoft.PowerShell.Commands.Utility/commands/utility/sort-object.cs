/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// 
    /// </summary>
    [Cmdlet("Sort", "Object", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113403", RemotingCapability = RemotingCapability.None)]
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
            orderByProperty.OrderMatrix.Sort(orderByProperty.Comparer);

            if (_unique)
            {
                RemoveDuplicates(orderByProperty);
            }

            // write to output stream
            foreach (OrderByPropertyEntry x in orderByProperty.OrderMatrix)
            {
                WriteObject(x.inputObject);
            }
        }
    }
}

