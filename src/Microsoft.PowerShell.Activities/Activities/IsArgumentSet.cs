//
//    Copyright (C) Microsoft.  All rights reserved.
//
﻿using System;
using System.Activities;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.ComponentModel;
using System.Text;
using System.Reflection;

namespace Microsoft.PowerShell.Activities.Internal
{
    /// <summary>
    /// Determines whether an argument to a PSActivity activity
    /// has been set.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Needed Internal use only")]
    public class IsArgumentSet : CodeActivity<bool>
    {
        /// <summary>
        /// The argument to investigate.
        /// </summary>
        [DefaultValue(null)]
        public Argument Argument { get; set; }

        /// <summary>
        /// Invokes the activity
        /// </summary>
        /// <param name="context">The activity context.</param>
        /// <returns>True if the given argument is set.</returns>
        protected override bool Execute(CodeActivityContext context)
        {
            return Argument != null && Argument.Expression != null;
        }
    }
}