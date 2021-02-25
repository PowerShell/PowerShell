// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using Microsoft.Management.Infrastructure.Options;
#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    /// <summary>
    /// <para>
    /// Prompt user the message coming from provider.
    /// </para>
    /// <para>
    /// At the same time <see cref="CimPromptUser"/> class will prepare the
    /// message for -whatif parameter, while the message represents
    /// what will happen if execute the operation, but not do the operation.
    /// For example, Remove-CimInstance, the whatif message will like,
    /// "CIM Instance: Win32_Process@{Key=1} will be deleted."
    /// </para>
    /// </summary>
    internal sealed class CimPromptUser : CimSyncAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CimPromptUser"/> class.
        /// </summary>
        public CimPromptUser(string message,
            CimPromptType prompt)
        {
            this.Message = message;
            this.prompt = prompt;
        }

        /// <summary>
        /// <para>
        /// Prompt user with the given message and prepared whatif message.
        /// </para>
        /// </summary>
        /// <param name="cmdlet">
        /// cmdlet wrapper object, to which write result.
        /// <see cref="CmdletOperationBase"/> for details.
        /// </param>
        public override void Execute(CmdletOperationBase cmdlet)
        {
            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");

            bool yestoall = false;
            bool notoall = false;
            bool result = false;

            switch (this.prompt)
            {
                case CimPromptType.Critical:
                    // NOTES: prepare the whatif message and caption
                    try
                    {
                        result = cmdlet.ShouldContinue(Message, "caption", ref yestoall, ref notoall);
                        if (yestoall)
                        {
                            this.responseType = CimResponseType.YesToAll;
                        }
                        else if (notoall)
                        {
                            this.responseType = CimResponseType.NoToAll;
                        }
                        else if (result)
                        {
                            this.responseType = CimResponseType.Yes;
                        }
                        else if (!result)
                        {
                            this.responseType = CimResponseType.No;
                        }
                    }
                    catch
                    {
                        this.responseType = CimResponseType.NoToAll;
                        throw;
                    }
                    finally
                    {
                        // unblocking the waiting thread
                        this.OnComplete();
                    }

                    break;
                case CimPromptType.Normal:
                    try
                    {
                        result = cmdlet.ShouldProcess(Message);
                        if (result)
                        {
                            this.responseType = CimResponseType.Yes;
                        }
                        else if (!result)
                        {
                            this.responseType = CimResponseType.No;
                        }
                    }
                    catch
                    {
                        this.responseType = CimResponseType.NoToAll;
                        throw;
                    }
                    finally
                    {
                        // unblocking the waiting thread
                        this.OnComplete();
                    }

                    break;
                default:
                    break;
            }

            this.OnComplete();
        }

        #region members

        /// <summary>
        /// Prompt message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Prompt type -Normal or Critical.
        /// </summary>
        private readonly CimPromptType prompt;

        #endregion
    }
}
