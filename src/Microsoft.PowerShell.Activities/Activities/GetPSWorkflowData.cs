using System;
using System.Activities;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Tracing;
using System.ComponentModel;

namespace Microsoft.PowerShell.Activities
{

    /// <summary>
    /// 
    /// </summary>
    public enum PSWorkflowRuntimeVariable
    {
        // Command Parameters
        /// <summary>
        /// 
        /// </summary>
        PSComputerName = 0,
        /// <summary>
        /// 
        /// </summary>
        PSCredential = 1,
        /// <summary>
        /// 
        /// </summary>
        PSPort = 2,
        /// <summary>
        /// 
        /// </summary>
        PSUseSsl = 3,
        /// <summary>
        /// 
        /// </summary>
        PSConfigurationName = 4,
        /// <summary>
        /// 
        /// </summary>
        PSApplicationName = 5,
        /// <summary>
        /// 
        /// </summary>
        PSConnectionUri = 6,
        /// <summary>
        /// 
        /// </summary>
        PSAllowRedirection = 7,
        /// <summary>
        /// 
        /// </summary>
        PSSessionOption = 8,
        /// <summary>
        /// 
        /// </summary>
        PSAuthentication = 9,
        /// <summary>
        /// 
        /// </summary>
        PSAuthenticationLevel = 10,
        /// <summary>
        /// 
        /// </summary>
        PSCertificateThumbprint = 11,
        /// <summary>
        /// 
        /// </summary>
        Input = 13,
        /// <summary>
        /// 
        /// </summary>
        Verbose = 15,

        // Retry policy constants
        /// <summary>
        /// 
        /// </summary>
        PSConnectionRetryCount = 19,
        /// <summary>
        /// 
        /// </summary>
        PSConnectionRetryIntervalSec = 21,

        /// <summary>
        /// 
        /// </summary>
        PSPrivateMetadata = 24,

        // Timers
        /// <summary>
        /// 
        /// </summary>
        PSRunningTimeoutSec = 27,
        /// <summary>
        /// 
        /// </summary>
        PSElapsedTimeoutSec = 28,

        /// <summary>
        /// 
        /// </summary>
        PSWorkflowRoot = 31,

        /// <summary>
        /// 
        /// </summary>
        JobName = 32,
        /// <summary>
        /// 
        /// </summary>
        JobInstanceId = 33,
        /// <summary>
        /// 
        /// </summary>
        JobId = 34,

        /// <summary>
        /// 
        /// </summary>
        JobCommandName = 36, 

        /// <summary>
        /// 
        /// </summary>
        ParentJobInstanceId = 40,

        /// <summary>
        /// 
        /// </summary>
        ParentJobName = 41,

        /// <summary>
        /// 
        /// </summary>
        ParentJobId = 42,

        /// <summary>
        /// 
        /// </summary>
        ParentCommandName = 43,

        /// <summary>
        /// 
        /// </summary>
        WorkflowInstanceId = 48,

        /// <summary>
        /// 
        /// </summary>
        PSSenderInfo = 49,

        /// <summary>
        /// 
        /// </summary>
        PSCulture = 50,

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        PSUICulture = 51,

        /// <summary>
        /// 
        /// </summary>
        PSVersionTable = 52,

        /// <summary>
        /// PSPersist
        /// </summary>
        PSPersist = 53,

        /// <summary>
        /// ErrorAction
        /// </summary>
        ErrorAction = 54,

        /// <summary>
        /// WarningAction
        /// </summary>
        WarningAction = 55,

        /// <summary>
        /// InformationAction
        /// </summary>
        InformationAction = 56,

        /// <summary>
        /// Tell the activity to look for a custom string
        /// </summary>
        Other = 1000,

        /// <summary>
        /// Return all values as a hashtable
        /// </summary>
        All = 1001,
    }

    /// <summary>
    /// Activity to retrieve the value of a workflow runtime variable.
    /// </summary>
    public sealed class GetPSWorkflowData<T> : NativeActivity<T>
    {
        /// <summary>
        /// The variable to retrieve.
        /// </summary>
        [RequiredArgument]
        public PSWorkflowRuntimeVariable VariableToRetrieve
        {
            get;
            set;
        }

        /// <summary>
        /// The variable to retrieve, if not included in the PSWorkflowRuntimeVariable enum.
        /// </summary>
        [DefaultValue(null)]
        public InArgument<string> OtherVariableName
        {
            get;
            set;
        }

        /// <summary>
        /// Execute the logic for this activity...
        /// </summary>
        /// <param name="context"></param>
        protected override void Execute(NativeActivityContext context)
        {
            // Retrieve our host overrides
            HostParameterDefaults hostValues = context.GetExtension<HostParameterDefaults>();

            PropertyDescriptorCollection col = context.DataContext.GetProperties();
            string variableName = null;

            if (VariableToRetrieve != PSWorkflowRuntimeVariable.Other)
            {
                // Get the symbolic name for the enum
                variableName = LanguagePrimitives.ConvertTo<string>(VariableToRetrieve);
            }
            else
            {
                if (OtherVariableName.Expression != null)
                {
                    string value = OtherVariableName.Get(context);

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        variableName = value;
                    }
                }
            }

            //BUGBUG need a better exception here, could also do this as a custom validator
            // Make sure we have a variable here...
            if (string.IsNullOrWhiteSpace(variableName))
            {
                throw new InvalidOperationException("OtherVariable");
            }

            object valueToReturn = null;
            PSDataCollection<PSObject> outputStream = null;
            foreach (System.ComponentModel.PropertyDescriptor property in context.DataContext.GetProperties())
            {
                if (string.Equals(property.Name, "ParameterDefaults", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var parameter in ((Microsoft.PowerShell.Activities.HostParameterDefaults)property.GetValue(context.DataContext)).Parameters)
                    {
                        if (parameter.Key.Equals(variableName, StringComparison.OrdinalIgnoreCase))
                        {
                            valueToReturn = parameter.Value;
                        }
                        else if (parameter.Key.Equals("Result"))
                        {
                            outputStream = parameter.Value as PSDataCollection<PSObject>;
                        }
                    }
                    
                    //
                    // If the property to return was all, then just return the entire collection as a hashtable.
                    // (We still needed to loop to find the output stream to write into.)
                    //
                    if (VariableToRetrieve == PSWorkflowRuntimeVariable.All)
                    {
                        System.Collections.Hashtable workflowRuntimeVariables = new System.Collections.Hashtable(StringComparer.OrdinalIgnoreCase);

                        string[] enumNames = VariableToRetrieve.GetType().GetEnumNames();

                        // Skipping last two enum names, Other and All, as they are not actual variable names
                        //
                        for (int i=0; i < (enumNames.Length - 2); i++)
                        {
                            workflowRuntimeVariables.Add(enumNames[i], null);
                        }

                        Dictionary<String, Object> dictionaryParam = ((Microsoft.PowerShell.Activities.HostParameterDefaults)property.GetValue(context.DataContext)).Parameters;

                        foreach(string varKey in dictionaryParam.Keys)
                        {
                            // We need to get the values of required runtime variables only, not everything from DataContext parameters
                            //
                            if (workflowRuntimeVariables.ContainsKey(varKey))
                            {
                                Object value = null; 
                                dictionaryParam.TryGetValue(varKey, out value);
                                workflowRuntimeVariables[varKey] = value;
                            }
                        }

                        valueToReturn = workflowRuntimeVariables;
                    }
                    break;
                }
            }

            if (this.Result.Expression != null)
            {
                this.Result.Set(context, valueToReturn);
            }
            else if (outputStream != null)
            {
                if (valueToReturn != null)
                {
                    outputStream.Add(PSObject.AsPSObject(valueToReturn));
                }
            }
            else
            {
                //BUGBUG need a better exception here...
                throw new InvalidOperationException("Result");
            }
        }
    }
}
