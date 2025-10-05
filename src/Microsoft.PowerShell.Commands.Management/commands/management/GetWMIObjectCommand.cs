// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Globalization;
using System.Management;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// A command to get WMI Objects.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "WmiObject", DefaultParameterSetName = "query",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113337", RemotingCapability = RemotingCapability.OwnedByCommand)]
    public class GetWmiObjectCommand : WmiBaseCmdlet
    {
        #region Parameters

        /// <summary>
        /// The WMI class to query.
        /// </summary>
        [Alias("ClassName")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "query")]
        [Parameter(Position = 1, ParameterSetName = "list")]
        [ValidateNotNullOrEmpty]
        public string Class { get; set; }

        /// <summary>
        /// To specify whether to get the results recursively.
        /// </summary>
        [Parameter(ParameterSetName = "list")]
        public SwitchParameter Recurse { get; set; } = false;

        /// <summary>
        /// The WMI properties to retrieve.
        /// </summary>
        [Parameter(Position = 1, ParameterSetName = "query")]
        [ValidateNotNullOrEmpty]
        public string[] Property
        {
            get { return (string[])_property.Clone(); }

            set { _property = value; }
        }

        /// <summary>
        /// The filter to be used in the search.
        /// </summary>
        [Parameter(ParameterSetName = "query")]
        public string Filter { get; set; }

        /// <summary>
        /// If Amended qualifier to use.
        /// </summary>
        [Parameter]
        public SwitchParameter Amended { get; set; }

        /// <summary>
        /// If Enumerate Deep flag to use. When 'list' parameter is specified 'EnumerateDeep' parameter is ignored.
        /// </summary>
        [Parameter(ParameterSetName = "WQLQuery")]
        [Parameter(ParameterSetName = "query")]
        public SwitchParameter DirectRead { get; set; }

        /// <summary>
        /// The list of classes.
        /// </summary>
        [Parameter(ParameterSetName = "list")]
        public SwitchParameter List { get; set; } = false;

        /// <summary>
        /// The query string to search for objects.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "WQLQuery")]
        public string Query { get; set; }

        #endregion Parameters

        #region parameter data

        private string[] _property = new string[] { "*" };

        #endregion parameter data

        #region Command code

        /// <summary>
        /// Uses this.filter, this.wmiClass and this.property to retrieve the filter.
        /// </summary>
        internal string GetQueryString()
        {
            StringBuilder returnValue = new StringBuilder("select ");
            returnValue.Append(string.Join(", ", _property));
            returnValue.Append(" from ");
            returnValue.Append(Class);
            if (!string.IsNullOrEmpty(Filter))
            {
                returnValue.Append(" where ");
                returnValue.Append(Filter);
            }

            return returnValue.ToString();
        }
        /// <summary>
        /// Uses filter table to convert the class into WMI understandable language.
        ///            Character   Description Example Match   Comment
        ///             *   Matches zero or more characters starting at the specified position  A*  A,ag,Apple  Supported by PowerShell.
        ///              ?   Matches any character at the specified position ?n  An,in,on (does not match ran)   Supported by PowerShell.
        ///              _   Matches any character at the specified position    _n  An,in,on (does not match ran)   Supported by WMI
        ///             %   Matches zero or more characters starting at the specified position   A%  A,ag,Apple  Supported by WMI
        ///             []  Matches a range of characters  [a-l]ook    Book,cook,look (does not match took)    Supported by WMI and powershell
        ///              []  Matches specified characters   [bc]ook Book,cook, (does not match look)    Supported by WMI and powershell
        ///              ^   Does not Match specified characters. [^bc]ook    Look, took (does not match book, cook)  Supported by WMI.
        /// </summary>

        internal string GetFilterClassName()
        {
            if (string.IsNullOrEmpty(this.Class))
                return string.Empty;
            string filterClass = string.Copy(this.Class);
            filterClass = filterClass.Replace('*', '%');
            filterClass = filterClass.Replace('?', '_');
            return filterClass;
        }

        internal bool IsLocalizedNamespace(string sNamespace)
        {
            bool toReturn = false;
            if (sNamespace.StartsWith("ms_", StringComparison.OrdinalIgnoreCase))
            {
                toReturn = true;
            }

            return toReturn;
        }

        internal bool ValidateClassFormat()
        {
            string filterClass = this.Class;
            if (string.IsNullOrEmpty(filterClass))
                return true;
            StringBuilder newClassName = new StringBuilder();
            for (int i = 0; i < filterClass.Length; i++)
            {
                if (char.IsLetterOrDigit(filterClass[i]) ||
                    filterClass[i].Equals('[') || filterClass[i].Equals(']') ||
                    filterClass[i].Equals('*') || filterClass[i].Equals('?') ||
                    filterClass[i].Equals('-'))
                {
                    newClassName.Append(filterClass[i]);
                    continue;
                }
                else if (filterClass[i].Equals('_'))
                {
                    newClassName.Append('[');
                    newClassName.Append(filterClass[i]);
                    newClassName.Append(']');
                    continue;
                }

                return false;
            }

            this.Class = newClassName.ToString();
            return true;
        }

        /// <summary>
        /// Gets the ManagementObjectSearcher object.
        /// </summary>
        internal ManagementObjectSearcher GetObjectList(ManagementScope scope)
        {
            StringBuilder queryStringBuilder = new StringBuilder();
            if (string.IsNullOrEmpty(this.Class))
            {
                queryStringBuilder.Append("select * from meta_class");
            }
            else
            {
                string filterClass = GetFilterClassName();
                if (filterClass == null)
                    return null;
                queryStringBuilder.Append("select * from meta_class where __class like '");
                queryStringBuilder.Append(filterClass);
                queryStringBuilder.Append("'");
            }

            ObjectQuery classQuery = new ObjectQuery(queryStringBuilder.ToString());

            EnumerationOptions enumOptions = new EnumerationOptions();
            enumOptions.EnumerateDeep = true;
            enumOptions.UseAmendedQualifiers = this.Amended;
            var searcher = new ManagementObjectSearcher(scope, classQuery, enumOptions);
            return searcher;
        }
        /// <summary>
        /// Gets the properties of an item at the specified path.
        /// </summary>
        protected override void BeginProcessing()
        {
            ConnectionOptions options = GetConnectionOption();
            if (this.AsJob)
            {
                RunAsJob("Get-WMIObject");
                return;
            }
            else
            {
                if (List)
                {
                    if (!this.ValidateClassFormat())
                    {
                        ErrorRecord errorRecord = new ErrorRecord(
                       new ArgumentException(
                           string.Format(
                               Thread.CurrentThread.CurrentCulture,
                               "Class", this.Class)),
                       "INVALID_QUERY_IDENTIFIER",
                       ErrorCategory.InvalidArgument,
                       null);
                        errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiFilterInvalidClass", this.Class);

                        WriteError(errorRecord);
                        return;
                    }

                    foreach (string name in ComputerName)
                    {
                        if (this.Recurse)
                        {
                            Queue namespaceElement = new Queue();
                            namespaceElement.Enqueue(this.Namespace);
                            while (namespaceElement.Count > 0)
                            {
                                string connectNamespace = (string)namespaceElement.Dequeue();
                                ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(name, connectNamespace), options);
                                try
                                {
                                    scope.Connect();
                                }
                                catch (ManagementException e)
                                {
                                    ErrorRecord errorRecord = new ErrorRecord(
                                         e,
                                         "INVALID_NAMESPACE_IDENTIFIER",
                                         ErrorCategory.ObjectNotFound,
                                         null);
                                    errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", connectNamespace, e.Message);
                                    WriteError(errorRecord);
                                    continue;
                                }
                                catch (System.Runtime.InteropServices.COMException e)
                                {
                                    ErrorRecord errorRecord = new ErrorRecord(
                                         e,
                                         "INVALID_NAMESPACE_IDENTIFIER",
                                         ErrorCategory.ObjectNotFound,
                                         null);
                                    errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", connectNamespace, e.Message);
                                    WriteError(errorRecord);
                                    continue;
                                }
                                catch (System.UnauthorizedAccessException e)
                                {
                                    ErrorRecord errorRecord = new ErrorRecord(
                                         e,
                                         "INVALID_NAMESPACE_IDENTIFIER",
                                         ErrorCategory.ObjectNotFound,
                                         null);
                                    errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", connectNamespace, e.Message);
                                    WriteError(errorRecord);
                                    continue;
                                }

                                ManagementClass namespaceClass = new ManagementClass(scope, new ManagementPath("__Namespace"), new ObjectGetOptions());
                                foreach (ManagementBaseObject obj in namespaceClass.GetInstances())
                                {
                                    if (!IsLocalizedNamespace((string)obj["Name"]))
                                    {
                                        namespaceElement.Enqueue(connectNamespace + "\\" + obj["Name"]);
                                    }
                                }

                                ManagementObjectSearcher searcher = this.GetObjectList(scope);
                                if (searcher == null)
                                    continue;
                                foreach (ManagementBaseObject obj in searcher.Get())
                                {
                                    WriteObject(obj);
                                }
                            }
                        }
                        else
                        {
                            ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(name, this.Namespace), options);
                            try
                            {
                                scope.Connect();
                            }
                            catch (ManagementException e)
                            {
                                ErrorRecord errorRecord = new ErrorRecord(
                                     e,
                                     "INVALID_NAMESPACE_IDENTIFIER",
                                     ErrorCategory.ObjectNotFound,
                                     null);
                                errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", this.Namespace, e.Message);
                                WriteError(errorRecord);
                                continue;
                            }
                            catch (System.Runtime.InteropServices.COMException e)
                            {
                                ErrorRecord errorRecord = new ErrorRecord(
                                     e,
                                     "INVALID_NAMESPACE_IDENTIFIER",
                                     ErrorCategory.ObjectNotFound,
                                     null);
                                errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", this.Namespace, e.Message);
                                WriteError(errorRecord);
                                continue;
                            }
                            catch (System.UnauthorizedAccessException e)
                            {
                                ErrorRecord errorRecord = new ErrorRecord(
                                     e,
                                     "INVALID_NAMESPACE_IDENTIFIER",
                                     ErrorCategory.ObjectNotFound,
                                     null);
                                errorRecord.ErrorDetails = new ErrorDetails(this, "WmiResources", "WmiNamespaceConnect", this.Namespace, e.Message);
                                WriteError(errorRecord);
                                continue;
                            }

                            ManagementObjectSearcher searcher = this.GetObjectList(scope);
                            if (searcher == null)
                                continue;
                            foreach (ManagementBaseObject obj in searcher.Get())
                            {
                                WriteObject(obj);
                            }
                        }
                    }

                    return;
                }

                // When -List is not specified and -Recurse is specified, we need the -Class parameter to compose the right query string
                if (this.Recurse && string.IsNullOrEmpty(Class))
                {
                    string errorMsg = string.Format(CultureInfo.InvariantCulture, WmiResources.WmiParameterMissing, "-Class");
                    ErrorRecord er = new ErrorRecord(new InvalidOperationException(errorMsg), "InvalidOperationException", ErrorCategory.InvalidOperation, null);
                    WriteError(er);
                    return;
                }

                string queryString = string.IsNullOrEmpty(this.Query) ? GetQueryString() : this.Query;
                ObjectQuery query = new ObjectQuery(queryString.ToString());

                foreach (string name in ComputerName)
                {
                    try
                    {
                        ManagementScope scope = new ManagementScope(WMIHelper.GetScopeString(name, this.Namespace), options);
                        EnumerationOptions enumOptions = new EnumerationOptions();
                        enumOptions.UseAmendedQualifiers = Amended;
                        enumOptions.DirectRead = DirectRead;
                        ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query, enumOptions);
                        foreach (ManagementBaseObject obj in searcher.Get())
                        {
                            WriteObject(obj);
                        }
                    }
                    catch (ManagementException e)
                    {
                        ErrorRecord errorRecord = null;
                        if (e.ErrorCode.Equals(ManagementStatus.InvalidClass))
                        {
                            string className = GetClassNameFromQuery(queryString);
                            string errorMsg = string.Format(CultureInfo.InvariantCulture, WmiResources.WmiQueryFailure,
                                                        e.Message, className);
                            errorRecord = new ErrorRecord(new ManagementException(errorMsg), "GetWMIManagementException", ErrorCategory.InvalidType, null);
                        }
                        else if (e.ErrorCode.Equals(ManagementStatus.InvalidQuery))
                        {
                            string errorMsg = string.Format(CultureInfo.InvariantCulture, WmiResources.WmiQueryFailure,
                                                        e.Message, queryString);
                            errorRecord = new ErrorRecord(new ManagementException(errorMsg), "GetWMIManagementException", ErrorCategory.InvalidArgument, null);
                        }
                        else if (e.ErrorCode.Equals(ManagementStatus.InvalidNamespace))
                        {
                            string errorMsg = string.Format(CultureInfo.InvariantCulture, WmiResources.WmiQueryFailure,
                                                        e.Message, this.Namespace);
                            errorRecord = new ErrorRecord(new ManagementException(errorMsg), "GetWMIManagementException", ErrorCategory.InvalidArgument, null);
                        }
                        else
                        {
                            errorRecord = new ErrorRecord(e, "GetWMIManagementException", ErrorCategory.InvalidOperation, null);
                        }

                        WriteError(errorRecord);
                        continue;
                    }
                    catch (System.Runtime.InteropServices.COMException e)
                    {
                        ErrorRecord errorRecord = new ErrorRecord(e, "GetWMICOMException", ErrorCategory.InvalidOperation, null);
                        WriteError(errorRecord);
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Get the class name from a query string.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private string GetClassNameFromQuery(string query)
        {
            System.Management.Automation.Diagnostics.Assert(query.Contains("from"),
                                                            "Only get called when ErrorCode is InvalidClass, which means the query string contains 'from' and the class name");

            if (Class != null)
            {
                return Class;
            }

            int fromIndex = query.IndexOf(" from ", StringComparison.OrdinalIgnoreCase);
            string subQuery = query.Substring(fromIndex + " from ".Length);
            string className = subQuery.Split(' ')[0];
            return className;
        }

        #endregion Command code
    }
}
