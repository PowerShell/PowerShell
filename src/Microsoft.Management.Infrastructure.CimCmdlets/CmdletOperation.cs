// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Wrapper of Cmdlet, forward the operation to Cmdlet directly.
    /// This is for unit test purpose, unit test can derive from this class,
    /// to hook up all of the cmdlet related operation and verify the correctness.
    /// </para>
    /// </summary>
    internal class CmdletOperationBase
    {
        /// <summary>
        /// <para>
        /// Wrap the Cmdlet object.
        /// </para>
        /// </summary>
        private readonly Cmdlet cmdlet;

        /// <summary>
        /// <para>
        /// Wrap the Cmdlet methods, for testing purpose.
        /// Test binary can define a child class of CmdletOperationBase.
        /// While Execute method of <seealso cref="CimBaseAction"/> accept the
        /// object of CmdletOperationBase as parameter.
        /// </para>
        /// </summary>
        #region CMDLET methods

        public virtual bool ShouldContinue(string query, string caption)
        {
            return cmdlet.ShouldContinue(query, caption);
        }

        public virtual bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll)
        {
            return cmdlet.ShouldContinue(query, caption, ref yesToAll, ref noToAll);
        }

        public virtual bool ShouldProcess(string target)
        {
            return cmdlet.ShouldProcess(target);
        }

        public virtual bool ShouldProcess(string target, string action)
        {
            return cmdlet.ShouldProcess(target, action);
        }

        public virtual bool ShouldProcess(string verboseDescription, string verboseWarning, string caption)
        {
            return cmdlet.ShouldProcess(verboseDescription, verboseWarning, caption);
        }

        public virtual bool ShouldProcess(string verboseDescription, string verboseWarning, string caption, out ShouldProcessReason shouldProcessReason)
        {
            return cmdlet.ShouldProcess(verboseDescription, verboseWarning, caption, out shouldProcessReason);
        }

        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        public virtual void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            cmdlet.ThrowTerminatingError(errorRecord);
        }

        public virtual void WriteCommandDetail(string text)
        {
            cmdlet.WriteCommandDetail(text);
        }

        public virtual void WriteDebug(string text)
        {
            cmdlet.WriteDebug(text);
        }

        public virtual void WriteError(ErrorRecord errorRecord)
        {
            cmdlet.WriteError(errorRecord);
        }

        public virtual void WriteObject(object sendToPipeline, XOperationContextBase context)
        {
            cmdlet.WriteObject(sendToPipeline);
        }

        public virtual void WriteObject(object sendToPipeline, bool enumerateCollection, XOperationContextBase context)
        {
            cmdlet.WriteObject(sendToPipeline, enumerateCollection);
        }

        public virtual void WriteProgress(ProgressRecord progressRecord)
        {
            cmdlet.WriteProgress(progressRecord);
        }

        public virtual void WriteVerbose(string text)
        {
            cmdlet.WriteVerbose(text);
        }

        public virtual void WriteWarning(string text)
        {
            cmdlet.WriteWarning(text);
        }

        /// <summary>
        /// <para>
        /// Throw terminating error
        /// </para>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        internal void ThrowTerminatingError(Exception exception, string operation)
        {
            ErrorRecord errorRecord = new(exception, operation, ErrorCategory.InvalidOperation, this);
            cmdlet.ThrowTerminatingError(errorRecord);
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="CmdletOperationBase"/> class.
        /// </summary>
        public CmdletOperationBase(Cmdlet cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");
            this.cmdlet = cmdlet;
        }
    }

    #region Class CmdletOperationRemoveCimInstance

    /// <summary>
    /// <para>
    /// Wrapper of Cmdlet, override WriteObject function call since
    /// we need to remove <see cref="CimInstance"/>.
    /// </para>
    /// </summary>
    internal class CmdletOperationRemoveCimInstance : CmdletOperationBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CmdletOperationRemoveCimInstance"/> class.
        /// </summary>
        /// <param name="cmdlet"></param>
        public CmdletOperationRemoveCimInstance(Cmdlet cmdlet,
            CimRemoveCimInstance cimRemoveCimInstance)
            : base(cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(cimRemoveCimInstance, cimRemoveCimInstanceParameterName);
            this.removeCimInstance = cimRemoveCimInstance;
        }

        /// <summary>
        /// <para>
        /// Object here need to be removed if it is CimInstance
        /// </para>
        /// </summary>
        /// <param name="sendToPipeline"></param>
        public override void WriteObject(object sendToPipeline, XOperationContextBase context)
        {
            if (sendToPipeline is CimInstance)
            {
                DebugHelper.WriteLog(">>>>CmdletOperationRemoveCimInstance::WriteObject", 4);
                this.removeCimInstance.RemoveCimInstance(sendToPipeline as CimInstance, context, this);
            }
            else
            {
                base.WriteObject(sendToPipeline, context);
            }
        }

        public override void WriteObject(object sendToPipeline, bool enumerateCollection, XOperationContextBase context)
        {
            if (sendToPipeline is CimInstance)
            {
                this.WriteObject(sendToPipeline, context);
            }
            else
            {
                base.WriteObject(sendToPipeline, enumerateCollection, context);
            }
        }

        #region private methods

        private readonly CimRemoveCimInstance removeCimInstance;

        private const string cimRemoveCimInstanceParameterName = @"cimRemoveCimInstance";

        #endregion
    }

    #endregion

    #region Class CmdletOperationSetCimInstance

    /// <summary>
    /// <para>
    /// Wrapper of Cmdlet, override WriteObject function call since
    /// we need to set <see cref="CimInstance"/>.
    /// </para>
    /// </summary>
    internal class CmdletOperationSetCimInstance : CmdletOperationBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CmdletOperationSetCimInstance"/> class.
        /// </summary>
        /// <param name="cmdlet"></param>
        public CmdletOperationSetCimInstance(Cmdlet cmdlet,
            CimSetCimInstance theCimSetCimInstance)
            : base(cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(theCimSetCimInstance, theCimSetCimInstanceParameterName);
            this.setCimInstance = theCimSetCimInstance;
        }

        /// <summary>
        /// <para>
        /// Object here need to be removed if it is CimInstance
        /// </para>
        /// </summary>
        /// <param name="sendToPipeline"></param>
        public override void WriteObject(object sendToPipeline, XOperationContextBase context)
        {
            DebugHelper.WriteLogEx();

            if (sendToPipeline is CimInstance)
            {
                if (context is CimSetCimInstanceContext setContext)
                {
                    if (string.Equals(setContext.ParameterSetName, CimBaseCommand.QueryComputerSet, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(setContext.ParameterSetName, CimBaseCommand.QuerySessionSet, StringComparison.OrdinalIgnoreCase))
                    {
                        this.setCimInstance.SetCimInstance(sendToPipeline as CimInstance, setContext, this);
                        return;
                    }
                    else
                    {
                        DebugHelper.WriteLog("Write the cimInstance to pipeline since this CimInstance is returned by SetCimInstance.", 4);
                    }
                }
                else
                {
                    DebugHelper.WriteLog("Assert. CimSetCimInstance::SetCimInstance has NULL CimSetCimInstanceContext", 4);
                }
            }

            base.WriteObject(sendToPipeline, context);
        }

        public override void WriteObject(object sendToPipeline, bool enumerateCollection, XOperationContextBase context)
        {
            if (sendToPipeline is CimInstance)
            {
                this.WriteObject(sendToPipeline, context);
            }
            else
            {
                base.WriteObject(sendToPipeline, enumerateCollection, context);
            }
        }

        #region private methods

        private readonly CimSetCimInstance setCimInstance;

        private const string theCimSetCimInstanceParameterName = @"theCimSetCimInstance";

        #endregion
    }
    #endregion

    #region Class CmdletOperationInvokeCimMethod
    /// <summary>
    /// <para>
    /// Wrapper of Cmdlet, override WriteObject function call since
    /// we need to invoke cim method.
    /// </para>
    /// </summary>
    internal class CmdletOperationInvokeCimMethod : CmdletOperationBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CmdletOperationInvokeCimMethod"/> class.
        /// </summary>
        /// <param name="cmdlet"></param>
        public CmdletOperationInvokeCimMethod(Cmdlet cmdlet,
            CimInvokeCimMethod theCimInvokeCimMethod)
            : base(cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(theCimInvokeCimMethod, theCimInvokeCimMethodParameterName);
            this.cimInvokeCimMethod = theCimInvokeCimMethod;
        }

        /// <summary>
        /// <para>
        /// Object here need to be removed if it is CimInstance
        /// </para>
        /// </summary>
        /// <param name="sendToPipeline"></param>
        public override void WriteObject(object sendToPipeline, XOperationContextBase context)
        {
            DebugHelper.WriteLogEx();

            if (sendToPipeline is CimInstance)
            {
                this.cimInvokeCimMethod.InvokeCimMethodOnCimInstance(sendToPipeline as CimInstance, context, this);
            }
            else
            {
                base.WriteObject(sendToPipeline, context);
            }
        }

        public override void WriteObject(object sendToPipeline, bool enumerateCollection, XOperationContextBase context)
        {
            if (sendToPipeline is CimInstance)
            {
                this.WriteObject(sendToPipeline, context);
            }
            else
            {
                base.WriteObject(sendToPipeline, enumerateCollection, context);
            }
        }

        #region private methods

        private readonly CimInvokeCimMethod cimInvokeCimMethod;

        private const string theCimInvokeCimMethodParameterName = @"theCimInvokeCimMethod";

        #endregion
    }

    #endregion

    #region Class CmdletOperationTestCimSession

    /// <summary>
    /// <para>
    /// Wrapper of Cmdlet, override WriteObject function call since
    /// we need to add cim session to global cache.
    /// </para>
    /// </summary>
    internal class CmdletOperationTestCimSession : CmdletOperationBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CmdletOperationTestCimSession"/> class.
        /// </summary>
        /// <param name="cmdlet"></param>
        public CmdletOperationTestCimSession(Cmdlet cmdlet,
            CimNewSession theCimNewSession)
            : base(cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(theCimNewSession, theCimNewSessionParameterName);
            this.cimNewSession = theCimNewSession;
        }

        /// <summary>
        /// <para>
        /// Add session object to cache
        /// </para>
        /// </summary>
        /// <param name="sendToPipeline"></param>
        public override void WriteObject(object sendToPipeline, XOperationContextBase context)
        {
            DebugHelper.WriteLogEx();

            if (sendToPipeline is CimSession)
            {
                DebugHelper.WriteLog("Call CimNewSession::AddSessionToCache", 1);

                this.cimNewSession.AddSessionToCache(sendToPipeline as CimSession, context, this);
            }
            else if (sendToPipeline is PSObject)
            {
                DebugHelper.WriteLog("Write PSObject to pipeline", 1);
                base.WriteObject(sendToPipeline, context);
            }
            else
            {
                // NOTES: May need to output for warning message/verbose message
                DebugHelper.WriteLog("Ignore other type object {0}", 1, sendToPipeline);
            }
        }

        #region private methods

        private readonly CimNewSession cimNewSession;

        private const string theCimNewSessionParameterName = @"theCimNewSession";

        #endregion
    }

    #endregion
}
