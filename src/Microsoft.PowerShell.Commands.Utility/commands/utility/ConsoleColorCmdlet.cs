/********************************************************************++

Copyright (c) Microsoft Corporation.  All rights reserved.

--********************************************************************/

using System;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// 
    /// Base class for a variety of commandlets that take color parameters
    /// 
    /// </summary>
    
    public
    class ConsoleColorCmdlet : PSCmdlet
    {
        /// <summary>
        /// Default ctor
        /// </summary>
        public ConsoleColorCmdlet()
        {
            consoleColorEnumType = typeof(ConsoleColor);
        }

        /// <summary>
        /// 
        /// The -ForegroundColor parameter
        /// 
        /// </summary>
        /// <value></value>

        [Parameter]
        public
        ConsoleColor
        ForegroundColor
        {
            get
            {
                if (!isFgColorSet)
                {
                    fgColor = this.Host.UI.RawUI.ForegroundColor;
                    isFgColorSet = true;
                }

                return fgColor;
            }
            set
            {
                if (value >= (ConsoleColor) 0 && value <= (ConsoleColor) 15)
                {
                    fgColor = value;
                    isFgColorSet = true;
                }
                else
                {
                    ThrowTerminatingError(BuildOutOfRangeErrorRecord(value, "SetInvalidForegroundColor")); 
                }
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        
        [Parameter]
        public
        ConsoleColor
        BackgroundColor
        {
            get
            {
                if (!isBgColorSet)
                {
                    bgColor = this.Host.UI.RawUI.BackgroundColor;
                    isBgColorSet = true;
                }

                return bgColor;
            }
            set
            {
                if (value >= (ConsoleColor) 0 && value <= (ConsoleColor) 15)
                {
                    bgColor = value;
                    isBgColorSet = true;
                }
                else
                {
                    ThrowTerminatingError(BuildOutOfRangeErrorRecord(value, "SetInvalidBackgroundColor"));
                }
            }
        }

        #region helper
        private static ErrorRecord BuildOutOfRangeErrorRecord(object val, string errorId)
        {
            string msg = StringUtil.Format(HostStrings.InvalidColorErrorTemplate, val.ToString());
            ArgumentOutOfRangeException e = new ArgumentOutOfRangeException("value", val, msg);
            return new ErrorRecord(e, errorId, ErrorCategory.InvalidArgument, null);
        }
        #endregion helper

        private ConsoleColor fgColor;
        private ConsoleColor bgColor;

        private bool isFgColorSet = false;
        private bool isBgColorSet = false;

        private readonly Type consoleColorEnumType;
    }
}

