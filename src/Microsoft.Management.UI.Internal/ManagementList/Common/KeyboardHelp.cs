// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    internal enum LogicalDirection
    {
        None,
        Left,
        Right
    }

    internal static class KeyboardHelp
    {
        /// <summary>
        /// Gets the logical direction for a key, taking into account RTL settings.
        /// </summary>
        /// <param name="element">The element to get FlowDirection from.</param>
        /// <param name="key">The key pressed.</param>
        /// <returns>The logical direction.</returns>
        public static LogicalDirection GetLogicalDirection(DependencyObject element, Key key)
        {
            Debug.Assert(element != null, "element not null");

            bool rightToLeft = IsElementRightToLeft(element);

            switch (key)
            {
                case Key.Right:
                    if (rightToLeft)
                    {
                        return LogicalDirection.Left;
                    }
                    else
                    {
                        return LogicalDirection.Right;
                    }

                case Key.Left:
                    if (rightToLeft)
                    {
                        return LogicalDirection.Right;
                    }
                    else
                    {
                        return LogicalDirection.Left;
                    }

                default:
                    return LogicalDirection.None;
            }
        }

        /// <summary>
        /// Gets the focus direction for a key, taking into account RTL settings.
        /// </summary>
        /// <param name="element">The element to get FlowDirection from.</param>
        /// <param name="key">The key pressed.</param>
        /// <returns>The focus direction.</returns>
        public static FocusNavigationDirection GetNavigationDirection(DependencyObject element, Key key)
        {
            Debug.Assert(element != null, "element not null");
            Debug.Assert(IsFlowDirectionKey(key));

            bool rightToLeft = IsElementRightToLeft(element);

            switch (key)
            {
                case Key.Right:
                    if (rightToLeft)
                    {
                        return FocusNavigationDirection.Left;
                    }
                    else
                    {
                        return FocusNavigationDirection.Right;
                    }

                case Key.Left:
                    if (rightToLeft)
                    {
                        return FocusNavigationDirection.Right;
                    }
                    else
                    {
                        return FocusNavigationDirection.Left;
                    }

                case Key.Down:
                    return FocusNavigationDirection.Down;
                case Key.Up:
                    return FocusNavigationDirection.Up;
                default:
                    Debug.Fail("Non-direction key specified");
                    return FocusNavigationDirection.First;
            }
        }

        /// <summary>
        /// Determines if the control key is pressed.
        /// </summary>
        /// <returns>True if a control is is pressed.</returns>
        public static bool IsControlPressed()
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if the key is a navigation key.
        /// </summary>
        /// <param name="key">The key pressed.</param>
        /// <returns>True if the key is a navigation key.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static bool IsFlowDirectionKey(Key key)
        {
            switch (key)
            {
                case Key.Right:
                case Key.Left:
                case Key.Down:
                case Key.Up:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsElementRightToLeft(DependencyObject element)
        {
            FlowDirection flowDirection = FrameworkElement.GetFlowDirection(element);
            bool rightToLeft = flowDirection == FlowDirection.RightToLeft;
            return rightToLeft;
        }
    }
}
