/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The implementation of the "remove-alias" cmdlet
    /// </summary>
    ///
    [Cmdlet(VerbsCommon.Remove, "Alias", DefaultParameterSetName = "Default", HelpUri = "")]
    public class RemoveAliasCommand : PSCmdlet
    {
         #region Parameters

        /// <summary>
        /// The Name parameter for the command
        /// </summary>
        ///
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }
        
        /// <summary>
        /// The scope parameter for the command determines
        /// which scope the alias is removed from.
        /// </summary>
        ///
        [Parameter]
        public string Scope { get; set; }

        /// <summary>
        /// If set to true and an existing alias of the same name exists
        /// and is ReadOnly, it will still be deleted.
        /// </summary>
        ///
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }
        private bool _force;

        #endregion Parameters

        #region Command code

        /// <summary>
        /// The main processing loop of the command.
        /// </summary>
        ///
        protected override void ProcessRecord()
        {
            
            AliasInfo existingAlias = null;
            try
            {
                //See if the Alias exists
                if (String.IsNullOrEmpty(Scope))
                {
                    existingAlias = SessionState.Internal.GetAlias(Name);
                }
                else
                {
                    existingAlias = SessionState.Internal.GetAliasAtScope(Name, Scope);
                }
            }
            catch (SessionStateException sessionStateException)
            {
                WriteError(
                    new ErrorRecord(
                        sessionStateException.ErrorRecord,
                        sessionStateException));
                return;
            }

            //If the alias exists, proceed to remove it
            if (existingAlias != null)
            {

                try{
                    SessionState.Internal.RemoveAlias(Name, Force);
                }
                catch (SessionStateException sessionStateException)
                {
                    WriteError(
                        new ErrorRecord(
                            sessionStateException.ErrorRecord,
                            sessionStateException));
                    return;
                }
            }
            else{
                
                ItemNotFoundException notAliasFound = new ItemNotFoundException(StringUtil.Format(AliasCommandStrings.NoAliasFound, "name", Name));
                ErrorRecord error = new ErrorRecord(notAliasFound, "ItemNotFoundException",ErrorCategory.ObjectNotFound, Name);

            }


        } // ProcessRecord
        #endregion Command code

    } // class GetAliasCommand
}//Microsoft.PowerShell.Commands

