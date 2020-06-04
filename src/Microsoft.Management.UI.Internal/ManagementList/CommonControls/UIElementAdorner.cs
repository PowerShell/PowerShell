// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{
    /// <content>
    /// Partial class implementation for UIElementAdorner.
    /// </content>
    internal partial class UIElementAdorner : Adorner
    {
        private VisualCollection children;

        /// <summary>
        /// Constructs an instance of UIElementAdorner.
        /// </summary>
        /// <param name="adornedElement">The adorned element.</param>
        public UIElementAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            this.children = new VisualCollection(this);
        }

        /// <summary>
        /// Overrides Visual.GetVisualChild, and returns a child at the specified index from a collection of child elements.
        /// </summary>
        /// <param name="index">The zero-based index of the requested child element in the collection..</param>
        /// <returns>The requested child element. This should not return null; if the provided index is out of range, an exception is thrown.</returns>
        protected override Visual GetVisualChild(int index)
        {
            return this.children[index];
        }

        /// <summary>
        /// Gets the number of visual child elements within this element.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                return this.children.Count;
            }
        }

        /// <summary>
        /// Implements any custom measuring behavior for the popupAdorner.
        /// </summary>
        /// <param name="constraint">A size to constrain the popupAdorner to..</param>
        /// <returns>A Size object representing the amount of layout space needed by the popupAdorner.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            if (this.Child != null)
            {
                this.Child.Measure(constraint);
                return this.Child.DesiredSize;
            }
            else
            {
                return base.MeasureOverride(constraint);
            }
        }

        /// <summary>
        /// When overridden in a derived class, positions child elements and determines a size for a FrameworkElement derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.Child != null)
            {
                Point location = new Point(0, 0);
                Rect rect = new Rect(location, finalSize);
                this.Child.Arrange(rect);
                return this.Child.RenderSize;
            }
            else
            {
                return base.ArrangeOverride(finalSize);
            }
        }

        partial void OnChildChangedImplementation(PropertyChangedEventArgs<UIElement> e)
        {
            if (e.OldValue != null)
            {
                this.children.Remove(e.OldValue);
                this.RemoveLogicalChild(e.OldValue);
            }

            if (this.Child != null)
            {
                this.children.Add(this.Child);
                this.AddLogicalChild(this.Child);
            }
        }
    }
}
