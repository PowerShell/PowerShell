// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Net;
using System.Text;
using Microsoft.Management.Infrastructure.Options;
#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    #region Parameter Set Resolving Classes

    /// <summary>
    /// <para>
    /// Define class <c>ParameterDefinitionEntry</c>.
    /// </para>
    /// </summary>
    internal class ParameterDefinitionEntry
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parameterSetName"></param>
        /// <param name="mandatory"></param>
        internal ParameterDefinitionEntry(string parameterSetName, bool mandatory)
        {
            this.mandatory = mandatory;
            this.parameterSetName = parameterSetName;
        }

        /// <summary>
        /// Property ParameterSetName.
        /// </summary>
        internal string ParameterSetName
        {
            get
            {
                return this.parameterSetName;
            }
        }

        private readonly string parameterSetName = null;

        /// <summary>
        /// Whether the parameter is mandatory to the set.
        /// </summary>
        internal bool IsMandatory
        {
            get
            {
                return this.mandatory;
            }
        }

        private readonly bool mandatory = false;
    }

    /// <summary>
    /// <para>
    /// Define class <c>ParameterSetEntry</c>.
    /// </para>
    /// </summary>
    internal class ParameterSetEntry
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mandatoryParameterCount"></param>
        internal ParameterSetEntry(UInt32 mandatoryParameterCount)
        {
            this.mandatoryParameterCount = mandatoryParameterCount;
            this.isDefaultParameterSet = false;
            reset();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="toClone"></param>
        internal ParameterSetEntry(ParameterSetEntry toClone)
        {
            this.mandatoryParameterCount = toClone.MandatoryParameterCount;
            this.isDefaultParameterSet = toClone.IsDefaultParameterSet;
            reset();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mandatoryParameterCount"></param>
        /// <param name="mandatory"></param>
        internal ParameterSetEntry(UInt32 mandatoryParameterCount, bool isDefault)
        {
            this.mandatoryParameterCount = mandatoryParameterCount;
            this.isDefaultParameterSet = isDefault;
            reset();
        }

        /// <summary>
        /// Reset the internal status.
        /// </summary>
        internal void reset()
        {
            this.setMandatoryParameterCount = this.setMandatoryParameterCountAtBeginProcess;
            this.isValueSet = this.isValueSetAtBeginProcess;
        }

        /// <summary>
        /// Property <c>DefaultParameterSet</c>
        /// </summary>
        internal bool IsDefaultParameterSet
        {
            get
            {
                return this.isDefaultParameterSet;
            }
        }

        private readonly bool isDefaultParameterSet = false;

        /// <summary>
        /// Property <c>MandatoryParameterCount</c>
        /// </summary>
        internal UInt32 MandatoryParameterCount
        {
            get
            {
                return this.mandatoryParameterCount;
            }
        }

        private readonly UInt32 mandatoryParameterCount = 0;

        /// <summary>
        /// Property <c>IsValueSet</c>
        /// </summary>
        internal bool IsValueSet
        {
            get
            {
                return this.isValueSet;
            }

            set
            {
                this.isValueSet = value;
            }
        }

        private bool isValueSet = false;

        /// <summary>
        /// Property <c>IsValueSetAtBeginProcess</c>
        /// </summary>
        internal bool IsValueSetAtBeginProcess
        {
            get
            {
                return this.isValueSetAtBeginProcess;
            }

            set
            {
                this.isValueSetAtBeginProcess = value;
            }
        }

        private bool isValueSetAtBeginProcess = false;

        /// <summary>
        /// Property <c>SetMandatoryParameterCount</c>
        /// </summary>
        internal UInt32 SetMandatoryParameterCount
        {
            get
            {
                return this.setMandatoryParameterCount;
            }

            set
            {
                this.setMandatoryParameterCount = value;
            }
        }

        private UInt32 setMandatoryParameterCount = 0;

        /// <summary>
        /// Property <c>SetMandatoryParameterCountAtBeginProcess</c>
        /// </summary>
        internal UInt32 SetMandatoryParameterCountAtBeginProcess
        {
            get
            {
                return this.setMandatoryParameterCountAtBeginProcess;
            }

            set
            {
                this.setMandatoryParameterCountAtBeginProcess = value;
            }
        }

        private UInt32 setMandatoryParameterCountAtBeginProcess = 0;
    }

    /// <summary>
    /// Define class <c>ParameterBinder</c>.
    /// </summary>
    internal class ParameterBinder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="sets"></param>
        internal ParameterBinder(
            Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters,
            Dictionary<string, ParameterSetEntry> sets)
        {
            this.CloneParameterEntries(parameters, sets);
        }

        #region Two dictionaries used to determine the bound parameter set

        /// <summary>
        /// Define the parameter definition entries,
        /// each parameter may belong a set of parameterSets, each parameter set
        /// are defined by a <seealso cref="ParameterDefinitionEntry"/>.
        /// </summary>
        private Dictionary<string, HashSet<ParameterDefinitionEntry>> parameterDefinitionEntries;

        /// <summary>
        /// <para>
        /// Define parameter set entries,
        /// each cmdlet has a list of parameter set, each has number of mandatory parameters, etc.
        /// This data structure is used to track the number of mandatory parameter has been set for
        /// current parameterset, whether the parameter set was been set by user.
        /// </para>
        /// </summary>
        private Dictionary<string, ParameterSetEntry> parameterSetEntries;

        #endregion

        /// <summary>
        /// <para>
        /// Used to remember the set of parameterset were set
        /// if any conflict occurred with current parameter,
        /// throw exception
        /// </para>
        /// </summary>
        private List<string> parametersetNamesList = new List<string>();

        /// <summary>
        /// Parameter names list.
        /// </summary>
        private List<string> parameterNamesList = new List<string>();

        /// <summary>
        /// <para>
        /// Used to remember the set of parameterset were set before begin process
        /// if any conflict occurred with current parameter,
        /// throw exception
        /// </para>
        /// </summary>
        private List<string> parametersetNamesListAtBeginProcess = new List<string>();

        /// <summary>
        /// Parameter names list before begin process.
        /// </summary>
        private List<string> parameterNamesListAtBeginProcess = new List<string>();

        /// <summary>
        /// <para>
        /// Reset the status of parameter set entries
        /// </para>
        /// </summary>
        internal void reset()
        {
            foreach (KeyValuePair<string, ParameterSetEntry> setEntry in parameterSetEntries)
            {
                setEntry.Value.reset();
            }

            this.parametersetNamesList.Clear();
            foreach (string parametersetName in this.parametersetNamesListAtBeginProcess)
            {
                this.parametersetNamesList.Add(parametersetName);
            }

            this.parameterNamesList.Clear();
            foreach (string parameterName in this.parameterNamesListAtBeginProcess)
            {
                this.parameterNamesList.Add(parameterName);
            }
        }

        /// <summary>
        /// <para>
        /// A given parameter's value was set by cmdlet caller,
        /// check and change the status of parameter set,
        /// throw exception if confliction occurred
        /// </para>
        /// </summary>
        /// <param name="parameterName"></param>
        /// <exception cref="PSArgumentException">Throw if conflict parameter was set.</exception>
        internal void SetParameter(string parameterName, bool isBeginProcess)
        {
            DebugHelper.WriteLogEx("ParameterName = {0}, isBeginProcess = {1}", 0, parameterName, isBeginProcess);

            if (this.parameterNamesList.Contains(parameterName))
            {
                DebugHelper.WriteLogEx("ParameterName {0} is already bound ", 1, parameterName);
                return;
            }
            else
            {
                this.parameterNamesList.Add(parameterName);
                if (isBeginProcess)
                {
                    this.parameterNamesListAtBeginProcess.Add(parameterName);
                }
            }

            if (this.parametersetNamesList.Count == 0)
            {
                List<string> nameset = new List<string>();
                foreach (ParameterDefinitionEntry parameterDefinitionEntry in this.parameterDefinitionEntries[parameterName])
                {
                    DebugHelper.WriteLogEx("parameterset name = '{0}'; mandatory = '{1}'", 1, parameterDefinitionEntry.ParameterSetName, parameterDefinitionEntry.IsMandatory);
                    ParameterSetEntry psEntry = this.parameterSetEntries[parameterDefinitionEntry.ParameterSetName];
                    if (psEntry == null)
                        continue;

                    if (parameterDefinitionEntry.IsMandatory)
                    {
                        psEntry.SetMandatoryParameterCount++;
                        if (isBeginProcess)
                        {
                            psEntry.SetMandatoryParameterCountAtBeginProcess++;
                        }

                        DebugHelper.WriteLogEx("parameterset name = '{0}'; SetMandatoryParameterCount = '{1}'", 1, parameterDefinitionEntry.ParameterSetName, psEntry.SetMandatoryParameterCount);
                    }

                    if (!psEntry.IsValueSet)
                    {
                        psEntry.IsValueSet = true;
                        if (isBeginProcess)
                        {
                            psEntry.IsValueSetAtBeginProcess = true;
                        }
                    }

                    nameset.Add(parameterDefinitionEntry.ParameterSetName);
                }

                this.parametersetNamesList = nameset;
                if (isBeginProcess)
                {
                    this.parametersetNamesListAtBeginProcess = nameset;
                }
            }
            else
            {
                List<string> nameset = new List<string>();
                foreach (ParameterDefinitionEntry entry in this.parameterDefinitionEntries[parameterName])
                {
                    if (this.parametersetNamesList.Contains(entry.ParameterSetName))
                    {
                        nameset.Add(entry.ParameterSetName);
                        if (entry.IsMandatory)
                        {
                            ParameterSetEntry psEntry = this.parameterSetEntries[entry.ParameterSetName];
                            psEntry.SetMandatoryParameterCount++;
                            if (isBeginProcess)
                            {
                                psEntry.SetMandatoryParameterCountAtBeginProcess++;
                            }

                            DebugHelper.WriteLogEx("parameterset name = '{0}'; SetMandatoryParameterCount = '{1}'",
                                1,
                                entry.ParameterSetName,
                                psEntry.SetMandatoryParameterCount);
                        }
                    }
                }

                if (nameset.Count == 0)
                {
                    throw new PSArgumentException(Strings.UnableToResolveParameterSetName);
                }
                else
                {
                    this.parametersetNamesList = nameset;
                    if (isBeginProcess)
                    {
                        this.parametersetNamesListAtBeginProcess = nameset;
                    }
                }
            }
        }

        /// <summary>
        /// Get the parameter set name based on current binding results.
        /// </summary>
        /// <returns></returns>
        internal string GetParameterSet()
        {
            DebugHelper.WriteLogEx();

            string boundParameterSetName = null;
            string defaultParameterSetName = null;
            List<string> noMandatoryParameterSet = new List<string>();

            // Looking for parameter set which have mandatory parameters
            foreach (string parameterSetName in this.parameterSetEntries.Keys)
            {
                ParameterSetEntry entry = this.parameterSetEntries[parameterSetName];
                DebugHelper.WriteLogEx(
                    "parameterset name = {0}, {1}/{2} mandatory parameters.",
                    1,
                    parameterSetName,
                    entry.SetMandatoryParameterCount,
                    entry.MandatoryParameterCount);

                // Ignore the parameter set which has no mandatory parameter firstly
                if (entry.MandatoryParameterCount == 0)
                {
                    if (entry.IsDefaultParameterSet)
                    {
                        defaultParameterSetName = parameterSetName;
                    }

                    if (entry.IsValueSet)
                    {
                        noMandatoryParameterSet.Add(parameterSetName);
                    }

                    continue;
                }

                if ((entry.SetMandatoryParameterCount == entry.MandatoryParameterCount) &&
                    this.parametersetNamesList.Contains(parameterSetName))
                {
                    if (boundParameterSetName != null)
                    {
                        throw new PSArgumentException(Strings.UnableToResolveParameterSetName);
                    }

                    boundParameterSetName = parameterSetName;
                }
            }

            // Looking for parameter set which has no mandatory parameters
            if (boundParameterSetName == null)
            {
                // throw if there are > 1 parameter set
                if (noMandatoryParameterSet.Count > 1)
                {
                    throw new PSArgumentException(Strings.UnableToResolveParameterSetName);
                }
                else if (noMandatoryParameterSet.Count == 1)
                {
                    boundParameterSetName = noMandatoryParameterSet[0];
                }
            }

            // Looking for default parameter set
            if (boundParameterSetName == null)
            {
                boundParameterSetName = defaultParameterSetName;
            }

            // throw if still can not find the parameter set name
            if (boundParameterSetName == null)
            {
                throw new PSArgumentException(Strings.UnableToResolveParameterSetName);
            }

            return boundParameterSetName;
        }

        /// <summary>
        /// Deep clone the parameter entries to member variable.
        /// </summary>
        private void CloneParameterEntries(
            Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters,
            Dictionary<string, ParameterSetEntry> sets)
        {
            this.parameterDefinitionEntries = parameters;
            this.parameterSetEntries = new Dictionary<string, ParameterSetEntry>();
            foreach (KeyValuePair<string, ParameterSetEntry> parameterSet in sets)
            {
                this.parameterSetEntries.Add(parameterSet.Key, new ParameterSetEntry(parameterSet.Value));
            }
        }
    }

    #endregion

    /// <summary>
    /// Base command for all cim cmdlets.
    /// </summary>
    public class CimBaseCommand : Cmdlet, IDisposable
    {

        #region resolve parameter set name
        /// <summary>
        /// <para>
        /// Check set parameters and set ParameterSetName
        /// </para>
        /// <para>
        /// Following are special types to be handled
        /// Microsoft.Management.Infrastructure.Options.PacketEncoding
        /// Microsoft.Management.Infrastructure.Options.ImpersonationType
        /// UInt32
        /// Authentication.None  (default value)?
        /// ProxyType.None
        /// </para>
        /// </summary>
        internal void CheckParameterSet()
        {
            if (this.parameterBinder != null)
            {
                try
                {
                    this.parameterSetName = this.parameterBinder.GetParameterSet();
                }
                finally
                {
                    this.parameterBinder.reset();
                }
            }

            DebugHelper.WriteLog("current parameterset is: " + this.parameterSetName, 4);
        }

        /// <summary>
        /// Redirect to parameterBinder to set one parameter.
        /// </summary>
        /// <param name="parameterName"></param>
        internal void SetParameter(object value, string parameterName)
        {
            // Ignore the null value being set,
            // Null value could be set by caller unintentionally,
            // or by powershell to reset the parameter to default value
            // before the next parameter binding, and ProcessRecord call
            if (value == null)
            {
                return;
            }

            if (this.parameterBinder != null)
            {
                this.parameterBinder.SetParameter(parameterName, this.AtBeginProcess);
            }
        }
        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        internal CimBaseCommand()
        {
            this.disposed = false;
            this.parameterBinder = null;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        internal CimBaseCommand(Dictionary<string, HashSet<ParameterDefinitionEntry>> parameters,
            Dictionary<string, ParameterSetEntry> sets)
        {
            this.disposed = false;
            this.parameterBinder = new ParameterBinder(parameters, sets);
        }

        #endregion

        #region override functions of Cmdlet

        /// <summary>
        /// StopProcessing method.
        /// </summary>
        protected override void StopProcessing()
        {
            Dispose();
        }

        #endregion

        #region IDisposable interface
        /// <summary>
        /// IDisposable interface.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// <para>
        /// Dispose() calls Dispose(true).
        /// Implement IDisposable. Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <para>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </para>
        /// </summary>
        /// <param name="disposing">Whether it is directly called.</param>
        protected void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    DisposeInternal();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                disposed = true;
            }
        }

        /// <summary>
        /// Clean up resources.
        /// </summary>
        protected virtual void DisposeInternal()
        {
            // Dispose managed resources.
            if (this.operation != null)
            {
                this.operation.Dispose();
            }
        }
        #endregion

        #region private members

        /// <summary>
        /// Parameter binder used to resolve parameter set name.
        /// </summary>
        private ParameterBinder parameterBinder;

        /// <summary>
        /// <para>
        /// Async operation handler
        /// </para>
        /// </summary>
        private CimAsyncOperation operation;

        /// <summary>
        /// Lock object.
        /// </summary>
        private readonly object myLock = new object();

        /// <summary>
        /// <para>
        /// parameter set name
        /// </para>
        /// </summary>
        private string parameterSetName;

        /// <summary>
        /// This flag is introduced to resolve the parameter set name
        /// during process record
        /// Whether at begin process time, false means in processrecord.
        /// </summary>
        private bool atBeginProcess = true;
        internal bool AtBeginProcess
        {
            get
            {
                return this.atBeginProcess;
            }

            set
            {
                this.atBeginProcess = value;
            }
        }
        #endregion

        #region internal properties

        /// <summary>
        /// <para>
        /// Set <see cref="CimAsyncOperation"/> object, to which
        /// current cmdlet will delegate all operations.
        /// </para>
        /// </summary>
        internal CimAsyncOperation AsyncOperation
        {
            set
            {
                lock (this.myLock)
                {
                    Debug.Assert(this.operation == null, "Caller should verify that operation is null");
                    this.operation = value;
                }
            }

            get
            {
                return this.operation;
            }
        }

        /// <summary>
        /// <para>
        /// Get current ParameterSetName of the cmdlet
        /// </para>
        /// </summary>
        internal string ParameterSetName
        {
            get
            {
                return this.parameterSetName;
            }
        }

        /// <summary>
        /// Gets/Sets cmdlet operation wrapper object.
        /// </summary>
        internal virtual CmdletOperationBase CmdletOperation
        {
            get;
            set;
        }

        /// <summary>
        /// <para>
        /// Throw terminating error
        /// </para>
        /// </summary>
        internal void ThrowTerminatingError(Exception exception, string operation)
        {
            ErrorRecord errorRecord = new ErrorRecord(exception, operation, ErrorCategory.InvalidOperation, this);
            this.CmdletOperation.ThrowTerminatingError(errorRecord);
        }

        #endregion

        #region internal const strings

        /// <summary>
        /// Alias CN - computer name.
        /// </summary>
        internal const string AliasCN = "CN";

        /// <summary>
        /// Alias ServerName - computer name.
        /// </summary>
        internal const string AliasServerName = "ServerName";

        /// <summary>
        /// Alias OT - operation timeout.
        /// </summary>
        internal const string AliasOT = "OT";

        /// <summary>
        /// Session set name.
        /// </summary>
        internal const string SessionSetName = "SessionSet";

        /// <summary>
        /// Computer set name.
        /// </summary>
        internal const string ComputerSetName = "ComputerSet";

        /// <summary>
        /// Class name computer set name.
        /// </summary>
        internal const string ClassNameComputerSet = "ClassNameComputerSet";

        /// <summary>
        /// Resource Uri computer set name.
        /// </summary>
        internal const string ResourceUriComputerSet = "ResourceUriComputerSet";

        /// <summary>
        /// <see cref="CimInstance"/> computer set name.
        /// </summary>
        internal const string CimInstanceComputerSet = "CimInstanceComputerSet";

        /// <summary>
        /// Query computer set name.
        /// </summary>
        internal const string QueryComputerSet = "QueryComputerSet";

        /// <summary>
        /// Class name session set name.
        /// </summary>
        internal const string ClassNameSessionSet = "ClassNameSessionSet";

        /// <summary>
        /// Resource Uri session set name.
        /// </summary>
        internal const string ResourceUriSessionSet = "ResourceUriSessionSet";

        /// <summary>
        /// <see cref="CimInstance"/> session set name.
        /// </summary>
        internal const string CimInstanceSessionSet = "CimInstanceSessionSet";

        /// <summary>
        /// Query session set name.
        /// </summary>
        internal const string QuerySessionSet = "QuerySessionSet";

        /// <summary>
        /// <see cref="CimClass"/> computer set name.
        /// </summary>
        internal const string CimClassComputerSet = "CimClassComputerSet";

        /// <summary>
        /// <see cref="CimClass"/> session set name.
        /// </summary>
        internal const string CimClassSessionSet = "CimClassSessionSet";

        #region Session related parameter set name

        internal const string ComputerNameSet = "ComputerNameSet";
        internal const string SessionIdSet = "SessionIdSet";
        internal const string InstanceIdSet = "InstanceIdSet";
        internal const string NameSet = "NameSet";
        internal const string CimSessionSet = "CimSessionSet";
        internal const string WSManParameterSet = "WSManParameterSet";
        internal const string DcomParameterSet = "DcomParameterSet";
        internal const string ProtocolNameParameterSet = "ProtocolTypeSet";
        #endregion

        #region register cimindication parameter set name
        internal const string QueryExpressionSessionSet = "QueryExpressionSessionSet";
        internal const string QueryExpressionComputerSet = "QueryExpressionComputerSet";
        #endregion

        /// <summary>
        /// Credential parameter set.
        /// </summary>
        internal const string CredentialParameterSet = "CredentialParameterSet";

        /// <summary>
        /// Certificate parameter set.
        /// </summary>
        internal const string CertificateParameterSet = "CertificateParameterSet";

        /// <summary>
        /// CimInstance parameter alias.
        /// </summary>
        internal const string AliasCimInstance = "CimInstance";

        #endregion

        #region internal helper function

        /// <summary>
        /// <para>
        /// Throw invalid AuthenticationType
        /// </para>
        /// </summary>
        /// <param name="operationName"></param>
        /// <param name="parameterName"></param>
        /// <param name="authentication"></param>
        internal void ThrowInvalidAuthenticationTypeError(
            string operationName,
            string parameterName,
            PasswordAuthenticationMechanism authentication)
        {
            string message = string.Format(CultureInfo.CurrentUICulture, Strings.InvalidAuthenticationTypeWithNullCredential,
                authentication,
                ImpersonatedAuthenticationMechanism.None,
                ImpersonatedAuthenticationMechanism.Negotiate,
                ImpersonatedAuthenticationMechanism.Kerberos,
                ImpersonatedAuthenticationMechanism.NtlmDomain);
            PSArgumentOutOfRangeException exception = new PSArgumentOutOfRangeException(
                parameterName, authentication, message);
            ThrowTerminatingError(exception, operationName);
        }

        /// <summary>
        /// Throw conflict parameter error.
        /// </summary>
        /// <param name="operationName"></param>
        /// <param name="parameterName"></param>
        /// <param name="conflictParameterName"></param>
        internal void ThrowConflictParameterWasSet(
            string operationName,
            string parameterName,
            string conflictParameterName)
        {
            string message = string.Format(CultureInfo.CurrentUICulture,
                Strings.ConflictParameterWasSet,
                parameterName, conflictParameterName);
            PSArgumentException exception = new PSArgumentException(message, parameterName);
            ThrowTerminatingError(exception, operationName);
        }

        /// <summary>
        /// <para>
        /// Throw not found property error
        /// </para>
        /// </summary>
        internal void ThrowInvalidProperty(
            IEnumerable<string> propertiesList,
            string className,
            string parameterName,
            string operationName,
            IDictionary actualValue)
        {
            StringBuilder propList = new StringBuilder();
            foreach (string property in propertiesList)
            {
                if (propList.Length > 0)
                {
                    propList.Append(",");
                }

                propList.Append(property);
            }

            string message = string.Format(CultureInfo.CurrentUICulture, Strings.CouldNotFindPropertyFromGivenClass,
                className, propList);
            PSArgumentOutOfRangeException exception = new PSArgumentOutOfRangeException(
                parameterName, actualValue, message);
            ThrowTerminatingError(exception, operationName);
        }

        /// <summary>
        /// Create credentials based on given authentication type and PSCredential.
        /// </summary>
        /// <param name="psCredentials"></param>
        /// <param name="passwordAuthentication"></param>
        /// <returns></returns>
        internal CimCredential CreateCimCredentials(PSCredential psCredentials,
            PasswordAuthenticationMechanism passwordAuthentication,
            string operationName,
            string parameterName)
        {
            DebugHelper.WriteLogEx("PSCredential:{0}; PasswordAuthenticationMechanism:{1}; operationName:{2}; parameterName:{3}.", 0, psCredentials, passwordAuthentication, operationName, parameterName);

            CimCredential credentials = null;
            if (psCredentials != null)
            {
                NetworkCredential networkCredential = psCredentials.GetNetworkCredential();
                DebugHelper.WriteLog("Domain:{0}; UserName:{1}; Password:{2}.", 1, networkCredential.Domain, networkCredential.UserName, psCredentials.Password);
                credentials = new CimCredential(passwordAuthentication, networkCredential.Domain, networkCredential.UserName, psCredentials.Password);

            }
            else
            {
                ImpersonatedAuthenticationMechanism impersonatedAuthentication;
                switch (passwordAuthentication)
                {
                    case PasswordAuthenticationMechanism.Default:
                        impersonatedAuthentication = ImpersonatedAuthenticationMechanism.None;
                        break;
                    case PasswordAuthenticationMechanism.Negotiate:
                        impersonatedAuthentication = ImpersonatedAuthenticationMechanism.Negotiate;
                        break;
                    case PasswordAuthenticationMechanism.Kerberos:
                        impersonatedAuthentication = ImpersonatedAuthenticationMechanism.Kerberos;
                        break;
                    case PasswordAuthenticationMechanism.NtlmDomain:
                        impersonatedAuthentication = ImpersonatedAuthenticationMechanism.NtlmDomain;
                        break;
                    default:
                        ThrowInvalidAuthenticationTypeError(operationName, parameterName, passwordAuthentication);
                        return null;
                }

                credentials = new CimCredential(impersonatedAuthentication);
            }

            DebugHelper.WriteLogEx("return credential {0}", 1, credentials);
            return credentials;
        }
        #endregion
    }
}
