// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Microsoft.Management.UI.Internal
{
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
