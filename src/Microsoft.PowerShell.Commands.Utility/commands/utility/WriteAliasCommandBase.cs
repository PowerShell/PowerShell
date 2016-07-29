/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation;
using Dbg = System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The base class for the SetAliasCommand and NewAliasCommand
    /// </summary>
    /// 
    public class WriteAliasCommandBase : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// The Name parameter for the command
        /// </summary>
        /// 
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        private string _name;

        /// <summary>
        /// The Value parameter for the command
        /// </summary>
        /// 
        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Value
        {
            get
            {
                return _aliasValue;
            }
            set
            {
                _aliasValue = value;
            }
        }
        private string _aliasValue;

        /// <summary>
        /// The description for the alias.
        /// </summary>
        /// 
        [Parameter]
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                _description = value;
            }
        }
        private string _description = String.Empty;

        /// <summary>
        /// The Option parameter allows the alias to be set to 
        /// ReadOnly (for existing aliases) and/or Constant (only
        /// for new aliases).
        /// </summary>
        /// 
        [Parameter]
        public ScopedItemOptions Option
        {
            get
            {
                return _options;
            }

            set
            {
                _options = value;
            }
        }
        private ScopedItemOptions _options = ScopedItemOptions.None;

        /// <summary>
        /// If set to true, the alias that is set is passed to the
        /// pipeline.
        /// </summary>
        /// 
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return _passThru;
            }

            set
            {
                _passThru = value;
            }
        }
        private bool _passThru;

        /// <summary>
        /// The scope parameter for the command determines
        /// which scope the alias is set in.
        /// </summary>
        /// 
        [Parameter]
        public string Scope
        {
            get
            {
                return _scope;
            }

            set
            {
                _scope = value;
            }
        }
        private string _scope;


        /// <summary>
        /// If set to true and an existing alias of the same name exists
        /// and is ReadOnly, the alias will be overwritten.
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

    } // class WriteAliasCommandBase
}//Microsoft.PowerShell.Commands

