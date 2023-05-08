// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    /// <summary>
    /// Implements the ImageButtonBase base class to the ImageButton and ImageToggleButton.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes", Justification = "Required by XAML")]
    public class ImageButtonBase : Grid
    {
        /// <summary>
        /// Command associated with this button.
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(RoutedUICommand), typeof(ImageButton));

        /// <summary>
        /// Image to be used for the enabled state.
        /// </summary>
        public static readonly DependencyProperty EnabledImageSourceProperty =
            DependencyProperty.Register("EnabledImageSource", typeof(ImageSource), typeof(ImageButton));

        /// <summary>
        /// Image to be used for the disabled state.
        /// </summary>
        public static readonly DependencyProperty DisabledImageSourceProperty =
            DependencyProperty.Register("DisabledImageSource", typeof(ImageSource), typeof(ImageButton));

        /// <summary>
        /// Gets or sets the image to be used for the enabled state.
        /// </summary>
        public ImageSource EnabledImageSource
        {
            get { return (ImageSource)GetValue(ImageButton.EnabledImageSourceProperty); }
            set { SetValue(ImageButton.EnabledImageSourceProperty, value); }
        }

        /// <summary>
        /// Gets or sets the image to be used for the disabled state.
        /// </summary>
        public ImageSource DisabledImageSource
        {
            get { return (ImageSource)GetValue(ImageButton.DisabledImageSourceProperty); }
            set { SetValue(ImageButton.DisabledImageSourceProperty, value); }
        }

        /// <summary>
        /// Gets or sets the command associated with this button.
        /// </summary>
        public RoutedUICommand Command
        {
            get { return (RoutedUICommand)GetValue(ImageButton.CommandProperty); }
            set { SetValue(ImageButton.CommandProperty, value); }
        }
    }
}
