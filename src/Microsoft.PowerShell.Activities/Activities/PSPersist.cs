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

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Persist the current workflow. Also defines the persistence point where suspend-job is getting suspended.
    /// </summary>
    public class PSPersist : NativeActivity
    {
        /// <summary>
        /// Returns true if the activity can induce an idle.
        /// </summary>
        protected override bool CanInduceIdle { get { return true; } }

        /// <summary>
        /// Invokes the activity
        /// </summary>
        /// <param name="context">The activity context.</param>
        protected override void Execute(NativeActivityContext context)
        {
            string bookmarkname = PSActivity.PSPersistBookmarkPrefix + Guid.NewGuid().ToString().Replace("-", "_");
            context.CreateBookmark(bookmarkname, BookmarkResumed);

        }

        private void BookmarkResumed(NativeActivityContext context, Bookmark bookmark, object value)
        {
        }
    }
}
