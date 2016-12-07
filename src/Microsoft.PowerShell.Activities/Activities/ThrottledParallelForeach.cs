//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Activities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Markup;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Implements the equivalent of the ParallelForeach activity, but supports throttling
    /// as well. Taken from the Workflow SDK: http://www.microsoft.com/en-us/download/details.aspx?id=21459
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ContentProperty("Body")]
    public sealed class ThrottledParallelForEach<T> : NativeActivity
    {
        Variable<bool> hasCompleted;
        Variable<IEnumerator<T>> valueEnumerator;
        CompletionCallback onBodyComplete;

        /// <summary>
        /// Creates a new instance of the ThrottledParallelForeach activity
        /// </summary>
        public ThrottledParallelForEach()
            : base()
        {
        }

        /// <summary>
        /// Gets or sets the actions to be invoked in parallel
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        public ActivityAction<T> Body { get; set; }

        /// <summary>
        /// Gets or sets the number of activities that may be scheduled simultaneously
        /// </summary>
        public InArgument<int> ThrottleLimit { get; set; }

        /// <summary>
        /// Gets or sets the values to be iterated over
        /// </summary>
        [RequiredArgument]
        [DefaultValue(null)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<IEnumerable<T>> Values { get; set; }

        /// <summary>
        /// Store implementation variables
        /// </summary>
        /// <param name="metadata"></param>
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            // add the arguments to the argument collection
            metadata.AddArgument(new RuntimeArgument("Values", typeof(IEnumerable<T>), ArgumentDirection.In, true));
            metadata.AddArgument(new RuntimeArgument("ThrottleLimit", typeof(int), ArgumentDirection.In));

            // initialize the hasCompleted and valueEnumerator and add it to the list of private variables
            this.hasCompleted = new Variable<bool>();
            metadata.AddImplementationVariable(this.hasCompleted);

            this.valueEnumerator = new Variable<IEnumerator<T>>();
            metadata.AddImplementationVariable(this.valueEnumerator);

            // add the body to the delegates collection
            metadata.AddDelegate(this.Body);
        }

        /// <summary>
        /// Invoke the activity's actions
        /// </summary>
        /// <param name="context"></param>
        protected override void Execute(NativeActivityContext context)
        {
            // get the list of value to iterate through
            IEnumerable<T> values = this.Values.Get(context);
            if (values == null)
            {
                throw new ApplicationException("ParallelForEach requires a non null Values collection");
            }

            // get the enumerator
            this.valueEnumerator.Set(context, values.GetEnumerator());

            // initialize the values for creating the execution window (max and runningCount)
            int max = this.ThrottleLimit.Get(context);
            if (max < 1) max = int.MaxValue;
            int runningCount = 0;

            // initialize the value of the completion variable
            this.hasCompleted.Set(context, false);

            // cache the completion callback
            onBodyComplete = new CompletionCallback(OnBodyComplete);

            // iterate while there are items available and we didn't exceed the throttle factor
            while (runningCount < max && valueEnumerator.Get(context).MoveNext())
            {
                // increase the running instances counter
                runningCount++;

                if (this.Body != null)
                {
                    context.ScheduleAction(this.Body, valueEnumerator.Get(context).Current, onBodyComplete);
                }
            }
        }

        void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            if (!this.hasCompleted.Get(context))
            {
                // get the next child and schedule it!
                IEnumerator<T> enumerator = this.valueEnumerator.Get(context);
                if (this.valueEnumerator.Get(context).MoveNext())
                {
                    context.ScheduleAction(this.Body, this.valueEnumerator.Get(context).Current, onBodyComplete);
                }
            }
        }
    }
}