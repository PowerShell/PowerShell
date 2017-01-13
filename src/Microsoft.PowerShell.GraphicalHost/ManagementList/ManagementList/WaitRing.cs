//-----------------------------------------------------------------------
// <copyright file="WaitRing.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Waiting Ring definition
//</summary>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;

    /// <summary>
    /// Waiting Ring class.
    /// </summary>
    public class WaitRing : Control
    {
        /// <summary>
        /// Static constructor for WaitRing.
        /// </summary>
        static WaitRing()
        {
            // This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            // This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaitRing), new FrameworkPropertyMetadata(typeof(WaitRing)));
        }
    }
}
