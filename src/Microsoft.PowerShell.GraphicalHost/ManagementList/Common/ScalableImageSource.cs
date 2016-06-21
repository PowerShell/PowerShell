//-----------------------------------------------------------------------
// <copyright file="ScalableImageSource.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Represents the source of an image that can render as a vector or as a bitmap.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    #region Using Directives

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Media.Animation;

    #endregion

    /// <content>
    /// Partial class implementation for SeparatedList control.
    /// </content>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public partial class ScalableImageSource : Freezable
    {
        #region Structors

        /// <summary>
        /// Initializes a new instance of the <see cref="Microsoft.Management.UI.Internal.ScalableImageSource" /> class.
        /// </summary>
        public ScalableImageSource()
        {
            // This constructor intentionally left blank
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Creates a new instance of the Freezable derived class.
        /// </summary>
        /// <returns>The new instance of the Freezable derived class.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new ScalableImageSource();
        }

        #endregion Overrides
    }
}