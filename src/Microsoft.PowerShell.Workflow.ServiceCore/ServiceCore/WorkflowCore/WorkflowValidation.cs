/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Diagnostics;

namespace Microsoft.PowerShell.Workflow
{
    using System;
    using System.Activities;
    using System.Activities.Statements;
    using System.Activities.Validation;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation.Tracing;
    using Microsoft.PowerShell.Activities;
	using System.Management.Automation;
    using System.Collections.Concurrent;
    using System.Reflection;

    /// <summary>
    /// Contains members that allow for controlling the PowerShell workflow
    /// engine validation mechanism.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public static class Validation
    {
        /// <summary>
        /// The custom validator delegate to use in this engine
        /// </summary>
        static public Func<Activity, bool> CustomHandler { get; set; }
    }

    internal class PSWorkflowValidationResults
    {
        internal PSWorkflowValidationResults()
        {
            this.IsWorkflowSuspendable = false;
            this.Results = null;
        }

        internal bool IsWorkflowSuspendable { get; set; }
        internal ValidationResults Results { get; set; }
    }

    /// <summary>
    /// Validate all the activities in the workflow to check if they are allowed or not.
    /// </summary>
    public class PSWorkflowValidator
    {

        #region Privates

        private static readonly string Facility = "WorkflowValidation : ";
        private static readonly PowerShellTraceSource Tracer = PowerShellTraceSourceFactory.GetTraceSource();
        private static readonly Tracer _structuredTracer = new Tracer();
        private readonly ConcurrentDictionary<Guid, PSWorkflowValidationResults> _validationCache = new ConcurrentDictionary<Guid, PSWorkflowValidationResults>();

        private static readonly List<string> MandatorySystemActivities = new List<string> { "SuspendOnError" };
        private static readonly List<string> AllowedSystemActivities = new List<string>
        {
            "DynamicActivity",
            "DoWhile",
            "ForEach`1",
            "If",
            "Parallel",
            "ParallelForEach`1",
            "Sequence",
            "Switch`1",
            "While",
            "Assign",
            "Assign`1",
            "Delay",
            "InvokeMethod",
            "TerminateWorkflow",
            "WriteLine",
            "Rethrow",
            "Throw",
            "TryCatch",
            "Literal`1",
            "VisualBasicValue`1",
            "VisualBasicReference`1",
            "LocationReferenceValue`1",
            "VariableValue`1",
            "VariableReference`1",
            "LocationReferenceReference`1",
            "LambdaValue`1",
            "Flowchart",
            "FlowDecision",
            "FlowSwitch`1",
            "AddToCollection`1",
            "ExistsInCollection`1",
            "RemoveFromCollection`1",
            "ClearCollection`1",
        };

        private static HashSet<string> PowerShellActivitiesAssemblies = new HashSet<string>() {
            "microsoft.powershell.activities",
            "microsoft.powershell.core.activities",
            "microsoft.powershell.diagnostics.activities",
            "microsoft.powershell.management.activities",
            "microsoft.powershell.security.activities",
            "microsoft.powershell.utility.activities",
            "microsoft.powershell.workflow.servicecore",
            "microsoft.wsman.management.activities",
        };

        private void ValidateWorkflowInternal(Activity workflow, string runtimeAssembly, PSWorkflowValidationResults validationResults)
        {
            Tracer.WriteMessage(Facility + "Validating a workflow.");
            _structuredTracer.WorkflowValidationStarted(Guid.Empty);

            ValidationSettings validationSettings = new ValidationSettings
            {
                AdditionalConstraints =
                    {
                        { typeof(Activity), new List<Constraint> { ValidateActivitiesConstraint(runtimeAssembly, validationResults)} }
                    }
            };

            try
            {
                validationResults.Results = ActivityValidationServices.Validate(workflow, validationSettings);
            }
            catch (Exception e)
            {
                Tracer.TraceException(e);
                ValidationException exception = new ValidationException(Resources.ErrorWhileValidatingWorkflow, e);
                throw exception;
            }

            _structuredTracer.WorkflowValidationFinished(Guid.Empty);

        }

        private Constraint ValidateActivitiesConstraint(string runtimeAssembly, PSWorkflowValidationResults validationResults)
        {
            DelegateInArgument<Activity> activityToValidate = new DelegateInArgument<Activity>();
            DelegateInArgument<ValidationContext> validationContext = new DelegateInArgument<ValidationContext>();

            return new Constraint<Activity>
            {
                Body = new ActivityAction<Activity, ValidationContext>
                {
                    Argument1 = activityToValidate,
                    Argument2 = validationContext,
                    Handler = new AssertValidation
                    {
                        IsWarning = false,

                        Assertion = new InArgument<bool>(
                            env => ValidateActivity(activityToValidate.Get(env), runtimeAssembly, validationResults)),

                        Message = new InArgument<string>(
                            env => string.Format(CultureInfo.CurrentCulture, Resources.InvalidActivity, activityToValidate.Get(env).GetType().FullName))
                    }
                }
            };
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "It is called by another private method 'ValidateActivitiesConstraint'")]
        private bool ValidateActivity(Activity activity, string runtimeAssembly, PSWorkflowValidationResults validationResult)
        {
            if (validationResult != null && validationResult.IsWorkflowSuspendable == false)
            {
                validationResult.IsWorkflowSuspendable = CheckIfSuspendable(activity);
            }

            bool allowed = false;
            Type activityType = activity.GetType();

            // If there is a custom validator activity, then call it. If it returns true
            // then just return the activity.
            if (Validation.CustomHandler != null && Validation.CustomHandler.Invoke(activity))
            {
                allowed = true;
            }
            else if (MandatorySystemActivities.Contains(activityType.Name))
            {
                allowed = true;
            }
            else if (string.Equals(activityType.Assembly.FullName, "System.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35", StringComparison.OrdinalIgnoreCase))
            {
                allowed = AllowedSystemActivities.Contains(activityType.Name);
            }
            else if (string.Equals(runtimeAssembly, activityType.Assembly.GetName().Name, StringComparison.OrdinalIgnoreCase))
            {
                // Allow the activities which belong to runtime assembly containing dependent Xamls.
                allowed = true;
            }
            else if (Configuration.PSDefaultActivitiesAreAllowed
              && PowerShellActivitiesAssemblies.Contains(activityType.Assembly.GetName().Name.ToLowerInvariant()))
            {
                // Allow any of the activities from our product assemblies provided that product activities were
                // specified as allowed activities
                allowed = true;
            }
            else
            {
                //  Check if the activityId matches any activity in the allowed list
                //  as a full name or as  pattern.
                foreach (string allowedActivity in Configuration.AllowedActivity ?? new string[0])
                {
                    // skipping * because it has already been checked above
                    if (string.Equals(allowedActivity, PSWorkflowConfigurationProvider.PSDefaultActivities, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (activity is System.Activities.Activity)
                    {
                        if (false
                            || IsMatched(allowedActivity, activityType.Name)
                            || IsMatched(allowedActivity, activityType.FullName)
                            || IsMatched(allowedActivity, activityType.Assembly.GetName().Name + "\\" + activityType.Name)
                            || IsMatched(allowedActivity, activityType.Assembly.GetName().Name + "\\" + activityType.FullName)
                            || IsMatched(allowedActivity, activityType.Assembly.GetName().FullName + "\\" + activityType.Name)
                            || IsMatched(allowedActivity, activityType.Assembly.GetName().FullName + "\\" + activityType.FullName))
                        {
                            allowed = true;
                            break;
                        }
                    }
                }
            }

            string displayName = activity.DisplayName;

            if (string.IsNullOrEmpty(displayName))
                displayName = this.GetType().Name;

            if (allowed)
            {
                _structuredTracer.WorkflowActivityValidated(Guid.Empty, displayName, activityType.FullName);
            }
            else
            {
                _structuredTracer.WorkflowActivityValidationFailed(Guid.Empty, displayName, activityType.FullName);
            }

            return allowed;
        }

        private bool CheckIfSuspendable(Activity activity)
        {
            if (string.Equals(activity.GetType().ToString(), "Microsoft.PowerShell.Activities.PSPersist", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(activity.GetType().ToString(), "Microsoft.PowerShell.Activities.Suspend", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            PSActivity psactivity = activity as PSActivity;
            if (psactivity != null && psactivity.PSPersist != null && psactivity.PSPersist.Expression != null)
            {
                return true;
            }

            Sequence seqActivity = activity as Sequence;
            if (seqActivity != null && seqActivity.Variables != null && seqActivity.Variables.Count > 0)
            {
                foreach (Variable var in seqActivity.Variables)
                {
                    if (string.Equals(var.Name, WorkflowPreferenceVariables.PSPersistPreference, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            Parallel parActivity = activity as Parallel;
            if (parActivity != null && parActivity.Variables != null && parActivity.Variables.Count > 0)
            {
                foreach (Variable var in parActivity.Variables)
                {
                    if (string.Equals(var.Name, WorkflowPreferenceVariables.PSPersistPreference, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static bool IsMatched(string allowedActivity, string match)
        {
            return (WildcardPattern.ContainsWildcardCharacters(allowedActivity)
                ? new WildcardPattern(allowedActivity, WildcardOptions.IgnoreCase).IsMatch(match)
                : string.Equals(allowedActivity, match, StringComparison.OrdinalIgnoreCase));
        }

        #endregion Privates

        #region Public methods

        /// <summary>
        /// PSWorkflowValidator
        /// </summary>
        /// <param name="configuration"></param>
        public PSWorkflowValidator(PSWorkflowConfigurationProvider configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            if (TestMode)
            {
                System.Threading.Interlocked.Increment(ref ObjectCounter);
            }

            this.Configuration = configuration;
        }

        /// <summary>
        /// Validate the workflow, if it is using the allowed activities.
        /// </summary>
        /// <param name="workflow">The workflow, which needs to be validated.</param>
        /// <param name="runtimeAssembly">The additional runtime assembly, which is needed in validation process.</param>
        public ValidationResults ValidateWorkflow(Activity workflow, string runtimeAssembly)
        {
            PSWorkflowValidationResults validationResults = new PSWorkflowValidationResults();
            ValidateWorkflowInternal(workflow, runtimeAssembly, validationResults);

            return validationResults.Results;
        }

        #endregion Public methods

        #region Internal Accessors

        internal PSWorkflowConfigurationProvider Configuration
        {
            get;
            private set;
        }

        internal PSWorkflowValidationResults ValidateWorkflow(Guid referenceId, Activity workflow, string runtimeAssembly)
        {
            PSWorkflowValidationResults validationResults = null;

            if (_validationCache.ContainsKey(referenceId))
            {
                _validationCache.TryGetValue(referenceId, out validationResults);
            }
            else
            {
                validationResults = new PSWorkflowValidationResults();

                this.ValidateWorkflowInternal(workflow, runtimeAssembly, validationResults);

                // sanity check to ensure cache isn't growing unbounded
                if (_validationCache.Keys.Count == Configuration.ValidationCacheLimit)
                {
                    _validationCache.Clear();
                }
                _validationCache.TryAdd(referenceId, validationResults);

            }

            return validationResults;
        }

        internal void ProcessValidationResults(ValidationResults results)
        {
            if (results == null)
                throw new ArgumentNullException("results");

            if (results.Errors != null && results.Errors.Count == 0)
                return;

            string errorMessage = string.Empty;

            foreach (ValidationError error in results.Errors)
            {
                errorMessage += error.Message;
                errorMessage += "\n";
            }

            ValidationException exception = new ValidationException(errorMessage);

            Tracer.TraceException(exception);
            _structuredTracer.WorkflowValidationError(Guid.Empty);
            throw exception;
        }

        #endregion Internal Accessors

        # region Test Only Variables & Accessors

        // For testing purpose ONLY
        internal static bool TestMode = false;
        // For testing purpose ONLY
        internal static long ObjectCounter = 0;


        internal bool IsActivityAllowed(Activity activity, string runtimeAssembly)
        {
            return ValidateActivity(activity, runtimeAssembly, null);
        }
        # endregion Test Only Variables & Accessors

    }
}
