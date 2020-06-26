// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Attached property provider to <see cref="TextBlock"/> control.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public static partial class TextBlockService
    {
        static partial void IsTextTrimmedMonitoringEnabledProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            TextBlock tb = o as TextBlock;
            if (tb == null)
            {
                return;
            }

            if ((bool)e.OldValue)
            {
                tb.SizeChanged -= OnTextBlockSizeChanged;
            }
            else
            {
                tb.SizeChanged += OnTextBlockSizeChanged;
            }
        }

        private static void OnTextBlockSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var textBlock = (TextBlock)sender;
            UpdateIsTextTrimmed(textBlock);
        }

        private static void OnTextBlockPropertyChanged(object sender, EventArgs e)
        {
            var textBlock = (TextBlock)sender;
            UpdateIsTextTrimmed(textBlock);
        }

        private static void UpdateIsTextTrimmed(TextBlock textBlock)
        {
            Debug.Assert(textBlock != null, "textblock not null");

            if (textBlock.TextWrapping != TextWrapping.NoWrap || textBlock.TextTrimming == TextTrimming.None)
            {
                SetIsTextTrimmed(textBlock, false);
            }
            else
            {
                SetIsTextTrimmed(textBlock, CalculateIsTextTrimmed(textBlock));
            }
        }

        private static bool CalculateIsTextTrimmed(TextBlock textBlock)
        {
            if (!textBlock.IsArrangeValid)
            {
                return GetIsTextTrimmed(textBlock);
            }

            Typeface typeface = new Typeface(
                textBlock.FontFamily,
                textBlock.FontStyle,
                textBlock.FontWeight,
                textBlock.FontStretch);

#pragma warning disable 612, 618
            // FormattedText is used to measure the whole width of the text held up by TextBlock container
            FormattedText formattedText = new FormattedText(
                textBlock.Text,
                System.Threading.Thread.CurrentThread.CurrentCulture,
                textBlock.FlowDirection,
                typeface,
                textBlock.FontSize,
                textBlock.Foreground);
#pragma warning restore 612, 618

            return formattedText.Width > textBlock.ActualWidth;
        }
    }
}
