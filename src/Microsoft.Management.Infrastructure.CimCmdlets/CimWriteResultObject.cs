// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Write result object to ps pipeline
    /// </para>
    /// </summary>
    internal sealed class CimWriteResultObject : CimBaseAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimWriteResultObject"/> class.
        /// </summary>
        public CimWriteResultObject(object result, XOperationContextBase theContext)
        {
            this.Result = result;
            this.Context = theContext;
        }

        /// <summary>
        /// <para>
        /// Write result object to ps pipeline
        /// </para>
        /// </summary>
        /// <param name="cmdlet"></param>
        public override void Execute(CmdletOperationBase cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");
            cmdlet.WriteObject(Result, this.Context);
        }

        #region members
        /// <summary>
        /// Result object.
        /// </summary>
        internal object Result { get; }
        #endregion
    }
}
