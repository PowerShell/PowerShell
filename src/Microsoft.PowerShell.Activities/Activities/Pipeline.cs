//
//    Copyright (C) Microsoft.  All rights reserved.
//
﻿using System;
using System.Activities;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime;
using System.Activities.Statements;
using System.Management.Automation;
using System.ComponentModel;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// The implementation of pipeline activity.
    /// This similar concept which we have in PowerShell today like Get-Process | Stop-Process.
    /// Pipeline activity will make sure the piped execution of its child activities.
    /// </summary>
#if _NOTARMBUILD_
    [Designer (typeof (PipelineDesigner))]
#endif
    public sealed class Pipeline : PipelineEnabledActivity
    {
        /// <summary>
        /// Tracks the number of current child activity in the collection.
        /// </summary>
        private Variable<int> lastIndexHint;

        private bool inputValidationFailed;
        private bool resultValidationFailed;

        /// <summary>
        /// Maintain intermediate outflow of data from child activity.
        /// </summary>
        private Variable<PSDataCollection<PSObject>> OutputStream;

        /// <summary>
        /// Maintain intermediate inflow of data into child activity.
        /// </summary>
        private Variable<PSDataCollection<PSObject>> InputStream;

        /// <summary>
        /// Get activities.
        /// </summary>
        [RequiredArgument]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needs to support assignment via workflow.")]
        public Collection<PipelineEnabledActivity> Activities { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Pipeline()
            : base()
        {
            this.lastIndexHint = new Variable<int>();
            this.Activities = new Collection<PipelineEnabledActivity>();

            this.inputValidationFailed = false;
            this.resultValidationFailed = false;
        }

        /// <summary>
        /// Validate the required number of activities of pipeline activity.
        /// Setup the cachemetadata with variables and activities.
        /// </summary>
        /// <param name="metadata"></param>
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            int count = 0;

            if (this.Activities != null)
            {
                count = this.Activities.Count;
            }

            if (count == 0)
            {
                metadata.AddValidationError(new ValidationError(ActivityResources.NoChildPipeline, true));
                return;
            }

            //BUGBUG: As written, the following checks cause error in scenarios where they should not. 
            // They are left in for the time being but disabled until we verify that there are no
            // scenarios where we need to check for two variables being assigned.
#if false
            if (Input != null && Input.Expression != null && this.Activities[0].Input != null && this.Activities[0].Input.Expression != null)
            {
                metadata.AddValidationError(new ValidationError(ActivityResources.DuplicateInputDefinedInPipeline, true));
                this.inputValidationFailed = true;
                return;
            }

            if (Result != null && Result.Expression != null && this.Activities[count - 1].Result != null && this.Activities[count - 1].Result.Expression != null)
            {
                metadata.AddValidationError(new ValidationError(ActivityResources.DuplicateResultDefinedInPipeline, true));
                this.resultValidationFailed = true;
                return;
            }
#endif
            // Adding variables into the CacheMetadata of pipeline activity.
            metadata.AddImplementationVariable(this.lastIndexHint);

            // We use a GUID here to make this name hard to guess. It's not a security issue,
            // it just prevents code from accidentally taking a dependency on it.
            this.OutputStream = new Variable<PSDataCollection<PSObject>>(Guid.NewGuid().ToString().Replace("-","_"));
            this.InputStream = new Variable<PSDataCollection<PSObject>>(Guid.NewGuid().ToString().Replace("-","_"));

            metadata.AddVariable(this.OutputStream);
            metadata.AddVariable(this.InputStream);

            bool appendOutput = false;
            if ((this.AppendOutput != null) && (this.AppendOutput.Value))
            {
                appendOutput = true;
            }

            // Adding activities into the CacheMetadata of pipeline activity.
            if (count == 1)
            {

                if (Input != null && Input.Expression != null)
                {
                    this.Activities[0].Input = this.Input;
                }

                if (Result != null && Result.Expression != null)
                {
                    this.Activities[0].Result = this.Result;
                }

                if (appendOutput)
                {
                    this.Activities[0].AppendOutput = true;
                }

                metadata.AddChild(this.Activities[0]);
            }
            else
            {

                if (Input != null && Input.Expression != null)
                {
                    this.Activities[0].Input = this.Input;
                }

                // Connecting child activities with temporary input and out streams.
                this.Activities[0].Result = this.OutputStream;
                metadata.AddChild(this.Activities[0]);

                for (int i = 1; i < (count - 1); i++)
                {
                    this.Activities[i].Input = this.InputStream;
                    this.Activities[i].Result = this.OutputStream;

                    metadata.AddChild(this.Activities[i]);
                }

                if (Result != null && Result.Expression != null)
                {
                    this.Activities[count - 1].Result = this.Result;
                }

                if (appendOutput)
                {
                    this.Activities[count - 1].AppendOutput = true;
                }

                this.Activities[count - 1].Input = this.InputStream;
                metadata.AddChild(this.Activities[count - 1]);
            }
        }

        /// <summary>
        /// Executes the first child activity
        /// </summary>
        /// <param name="executionContext">The execution context of pipeline activity.</param>
        protected override void Execute(NativeActivityContext executionContext)
        {
            int count = 0;

            if (this.Activities != null)
            {
                count = this.Activities.Count;
            }

            if (count == 0)
            {
                throw new ArgumentException(ActivityResources.NoChildPipeline);
            }

            if (this.inputValidationFailed && Input != null && Input.Expression != null && this.Activities[0].Input != null && this.Activities[0].Input.Expression != null)
            {
                throw new ArgumentException(ActivityResources.DuplicateInputDefinedInPipeline);
            }

            if (this.resultValidationFailed && Result != null && Result.Expression != null && this.Activities[count - 1].Result != null && this.Activities[count - 1].Result.Expression != null)
            {
                throw new ArgumentException(ActivityResources.DuplicateResultDefinedInPipeline);
            }

            //Executing the first child activity.
            PipelineEnabledActivity firstChild = this.Activities[0];
            executionContext.ScheduleActivity(firstChild, new CompletionCallback(InternalExecute));
        }

        /// <summary>
        /// Get results from previous activity and schedule the execution of next activity.
        /// </summary>
        /// <param name="executionContext">The execution context of pipeline activity.</param>
        /// <param name="completedInstance">The activity instance of completed child activity.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private void InternalExecute(NativeActivityContext executionContext, ActivityInstance completedInstance)
        {
            int completedInstanceIndex;

            // Reading the value of pipeline activity variables from the context.
            completedInstanceIndex = this.lastIndexHint.Get(executionContext);

            PSDataCollection<PSObject> outValue = this.GetData(executionContext, this.OutputStream);
            PSDataCollection<PSObject> inValue = this.GetData(executionContext, this.InputStream);

            // Re-checking the index of the the child activity, which has just completed its execution.
            if (completedInstanceIndex >= this.Activities.Count || this.Activities[completedInstanceIndex] != completedInstance.Activity)
            {
                completedInstanceIndex = this.Activities.IndexOf((PSActivity) completedInstance.Activity);
            }

            // Calculating next child activity.
            int nextChildIndex = completedInstanceIndex + 1;

            // Checking for pipeline activity completion.
            if (nextChildIndex == this.Activities.Count)
            {
                if (inValue != null) inValue.Dispose();
                if (outValue != null) outValue.Dispose();
                return;
            }

            // Setting up the environment for next child activity to run.
            if (outValue != null) outValue.Complete();
            if (inValue != null) inValue.Dispose();

            inValue = outValue;
            outValue = new PSDataCollection<PSObject>();

            // The pipeline is complete if there is no input
            // PS > function foo { $input | Write-Output "Hello" }
            // PS > foo
            // PS >
            if ((inValue == null) || (inValue.Count == 0))
            {
                if (outValue != null) outValue.Dispose();
                return;
            }

            this.SetData(executionContext, this.OutputStream, outValue);
            this.SetData(executionContext, this.InputStream, inValue);
            
            // Executing the next child activity.
            PipelineEnabledActivity nextChild = this.Activities[nextChildIndex];
            
            executionContext.ScheduleActivity(nextChild, new CompletionCallback(InternalExecute));

            this.lastIndexHint.Set(executionContext, nextChildIndex);
        }

        /// <summary>
        /// Get the data from the pipeline variable.
        /// </summary>
        /// <param name="context">The activity context.</param>
        /// <param name="variable">The variable which value to get.</param>
        /// <returns>Returns the value of the variable.</returns>
        private PSDataCollection<PSObject> GetData(ActivityContext context, Variable<PSDataCollection<PSObject>> variable)
        {
            PropertyDescriptor prop = context.DataContext.GetProperties()[variable.Name];
            return (PSDataCollection<PSObject>)prop.GetValue(context.DataContext);
        }

        /// <summary>
        /// Set the data to the pipeline variable.
        /// </summary>
        /// <param name="context">The activity context.</param>
        /// <param name="variable">The variable which needs to set.</param>
        /// <param name="value">The value for the variable.</param>
        private void SetData(ActivityContext context, Variable<PSDataCollection<PSObject>> variable, PSDataCollection<PSObject> value)
        {
            PropertyDescriptor prop = context.DataContext.GetProperties()[variable.Name];
            prop.SetValue(context.DataContext, value);
        }

    }
}
