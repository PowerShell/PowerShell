/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
#pragma warning disable 1634, 1691

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Activities;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Tracing;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;

namespace Microsoft.PowerShell.Activities
{

    /// <summary>
    /// Workflow activity wrapping the Get-Wmiobject cmdlet
    /// </summary>
    public sealed class GetWmiObject : WmiActivity
    {
        /// <summary>
        /// Sets the default display name of the activity
        /// </summary>
        public GetWmiObject()
        {
            this.DisplayName = "Get-WmiObject";
        }

        /// <summary>
        /// Specifies the name of a WMI class. When this parameter is used, the cmdlet
        /// retrieves instances of the WMI class.
        /// </summary>summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [OverloadGroup("Class")]
        public InArgument<string> Class { get; set; }

        /// <summary>
        /// Specifies the WMI class property or set of properties to retrieve.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        public InArgument<string[]> Property { get; set; }

        /// <summary>
        /// Specifies a Where clause to use as a filter. Uses the syntax of the WMI Query Language (WQL).
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [OverloadGroup("Class")]
        public InArgument<string> Filter { get; set; }

        /// <summary>
        ///  Specifies a WMI Query Language (WQL) statement to run. Event queries are not supported by this parameter.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [OverloadGroup("Query")]
        public InArgument<string> Query { get; set; }

        /// <summary>
        /// Indicates whether the objects that are returned from WMI should contain amended
        /// information. Typically, amended information is localizable information, such as object
        /// and property descriptions, that is attached to the WMI object.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        public bool Amended { get; set; }

        /// <summary>
        /// Specifies whether direct access to the WMI provider is requested for the specified
        /// class without any regard to its base class or to its derived classes.
        ///</summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        public bool DirectRead { get; set; }

        /// <summary>
        /// Execute the logic for the activity
        /// </summary>
        /// <param name="context">The native activity context to use in this activity</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected override ActivityImplementationContext GetPowerShell(NativeActivityContext context)
        {
            System.Management.Automation.PowerShell command;
            command = GetWmiCommandCore(context, "Get-WmiObject");
            if (Class.Get(context) != null)
            {
                command.AddParameter("Class", Class.Get(context));
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Class", Class.Get(context)));
            }

            if (Property.Get(context) != null)
            {
                command.AddParameter("Property", Property.Get(context));
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Property", Property.Get(context)));

            }
            if (Filter.Get(context) != null)
            {
                command.AddParameter("Filter", Filter.Get(context));
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Filter", Filter.Get(context)));

            }
            if (Amended)
            {
                command.AddParameter("Amended", Amended);
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Amended", Amended));

            }
            if (DirectRead)
            {
                command.AddParameter("DirectRead", DirectRead);
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "DirectRead", DirectRead));

            }
            if (Query.Get(context) != null)
            {
                command.AddParameter("Query", Query.Get(context));
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Query", Query.Get(context)));
            }

            return new ActivityImplementationContext() { PowerShellInstance = command };
        }
    }

    /// <summary>
    /// Wraps the Invoke-WmiMethod cmdlet
    /// </summary>
    public sealed class InvokeWmiMethod : WmiActivity
    {
        /// <summary>
        /// Sets the default display name of the activity
        /// </summary>
        public InvokeWmiMethod()
        {
            this.DisplayName = "Invoke-WmiMethod";
        }

        /// <summary>
        /// A WMI path specification which should be of the form "Win32_Printer.DeviceID='TestPrinter'"
        /// this will select instances of objects on which to call the method.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [OverloadGroup("path")]
        public InArgument<string> Path { get; set; }

        /// <summary>
        /// The name of the WMI class to use for when static methods.
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        [OverloadGroup("class")]
        public InArgument<string> Class { get; set; }

        /// <summary>
        /// THe name of the instance or static method to call
        /// </summary>
        [BehaviorCategory]
        [DefaultValue(null)]
        public InArgument<string> Name { get; set; }

        /// <summary>
        /// The collection of arguments to use when calling the method
        /// </summary>
        [BehaviorCategory]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification = "This is forced by the interaction of PowerShell and Workflow.")]
        public InArgument<PSDataCollection<PSObject>> ArgumentList { get; set; }

        /// <summary>
        /// Implements the logic of this activity
        /// </summary>
        /// <param name="context">The activity context to refer to</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected override ActivityImplementationContext GetPowerShell(NativeActivityContext context)
        {
            System.Management.Automation.PowerShell command;
            command = GetWmiCommandCore(context, "Invoke-WmiMethod");

            if (!String.IsNullOrEmpty(Path.Get(context)))
            {
                command.AddParameter("Path", Path.Get(context));
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Path", Path.Get(context)));

            }

            if (!String.IsNullOrEmpty(Class.Get(context)))
            {
                command.AddParameter("Class", Class.Get(context));
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture, "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                    context.ActivityInstanceId, "Class", Class.Get(context)));

            }

            if (!String.IsNullOrEmpty(Name.Get(context)))
            {
                command.AddParameter("Name", Name.Get(context));
                Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                    "PowerShell activity ID={0}: Setting parameter {1} to {2}.",
                        context.ActivityInstanceId, "Name", Name.Get(context)));
            }

            if (ArgumentList.Get(context) != null)
            {
                Collection<PSObject> argCollection = ArgumentList.Get(context).ReadAll();
                if (argCollection.Count > 0)
                {
                    command.AddParameter("ArgumentList", argCollection);
                    Tracer.WriteMessage(String.Format(CultureInfo.InvariantCulture,
                        "PowerShell activity ID={0}: Setting parameter {1} to '{2}'.",
                            context.ActivityInstanceId, "ArgumentList", string.Join("','", argCollection)));
                }
            }

            return new ActivityImplementationContext() { PowerShellInstance = command };
        }
    }

}
