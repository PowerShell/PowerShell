// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// The resize grip possibilities.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public enum ResizeGripLocation
    {
        /// <summary>
        /// One grip is shown, on the right side.
        /// </summary>
        Right,

        /// <summary>
        /// One grip is shown, on the left side.
        /// </summary>
        Left,
    }

    /// <content>
    /// Partial class implementation for Resizer control.
    /// </content>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
    public partial class Resizer : ContentControl
    {
        private AdornerLayer adornerLayer;
        private UIElementAdorner adorner;
        private ContentControl adornerContent;

        /// <summary>
        /// Creates an instance of Resizer.
        /// </summary>
        public Resizer()
        {
            // nothing
        }

        internal static Thickness CreateGripThickness(double visibleGripWidth, ResizeGripLocation gripLocation)
        {
            Thickness thickness;

            if (visibleGripWidth < 0.0 || double.IsNaN(visibleGripWidth))
            {
                throw new ArgumentOutOfRangeException("visibleGripWidth", "The value must be greater than or equal to 0.");
            }

            if (double.IsInfinity(visibleGripWidth))
            {
                throw new ArgumentOutOfRangeException("visibleGripWidth", "The value must be less than infinity.");
            }

            switch (gripLocation)
            {
                case ResizeGripLocation.Right:
                    thickness = new Thickness(0, 0, visibleGripWidth, 0);
                    break;
                case ResizeGripLocation.Left:
                    thickness = new Thickness(visibleGripWidth, 0, 0, 0);
                    break;
                default:
                    throw new InvalidEnumArgumentException("gripLocation", (int)gripLocation, typeof(ResizeGripLocation));
            }

            return thickness;
        }

        partial void PreOnApplyTemplate()
        {
            if (this.rightGrip != null)
            {
                this.rightGrip.DragDelta -= this.OnRightGripDragDelta;
                this.rightGrip.DragStarted -= this.OnRightGripDragStarted;
                this.rightGrip.DragCompleted -= this.OnRightGripDragCompleted;
            }

            if (this.leftGrip != null)
            {
                this.leftGrip.DragDelta -= this.OnLeftGripDragDelta;
                this.leftGrip.DragStarted -= this.OnLeftGripDragStarted;
                this.leftGrip.DragCompleted -= this.OnLeftGripDragCompleted;
            }
        }

        partial void PostOnApplyTemplate()
        {
            this.rightGrip.DragDelta += this.OnRightGripDragDelta;
            this.rightGrip.DragStarted += this.OnRightGripDragStarted;
            this.rightGrip.DragCompleted += this.OnRightGripDragCompleted;

            this.leftGrip.DragDelta += this.OnLeftGripDragDelta;
            this.leftGrip.DragStarted += this.OnLeftGripDragStarted;
            this.leftGrip.DragCompleted += this.OnLeftGripDragCompleted;
        }

        private void CreateAdorner()
        {
            this.adornerLayer = AdornerLayer.GetAdornerLayer(this);

            this.adorner = new UIElementAdorner(this);

            this.adornerContent = new ContentControl();
            this.adornerContent.Name = "ResizerAdornerContent";
            this.adornerContent.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            this.adornerContent.VerticalContentAlignment = VerticalAlignment.Stretch;
            this.adornerContent.ContentTemplate = this.DraggingTemplate;
            this.adorner.Child = this.adornerContent;
        }

        private void RemoveAdorner()
        {
            this.adornerLayer.Remove(this.adorner);
        }

        private void OnLeftGripDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            this.StopDragging(ResizeGripLocation.Left, e);
        }

        private void OnLeftGripDragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            this.StartDragging(ResizeGripLocation.Left);
        }

        private void OnLeftGripDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            this.PerformDrag(ResizeGripLocation.Left, e);
        }

        private void OnRightGripDragCompleted(object sender, DragCompletedEventArgs e)
        {
            this.StopDragging(ResizeGripLocation.Right, e);
        }

        private void OnRightGripDragStarted(object sender, DragStartedEventArgs e)
        {
            this.StartDragging(ResizeGripLocation.Right);
        }

        private void OnRightGripDragDelta(object sender, DragDeltaEventArgs e)
        {
            this.PerformDrag(ResizeGripLocation.Right, e);
        }

        private void PerformDrag(ResizeGripLocation location, DragDeltaEventArgs e)
        {
            double newWidth = this.GetNewWidth(location, e.HorizontalChange);

            if (this.ResizeWhileDragging)
            {
                this.Width = newWidth;
            }
            else
            {
                this.adorner.Width = newWidth;
            }
        }

        private void StartDragging(ResizeGripLocation location)
        {
            if (this.ResizeWhileDragging == false)
            {
                if (this.adornerLayer == null)
                {
                    this.CreateAdorner();
                }

                this.adornerContent.Content = location;
                this.adornerLayer.Add(this.adorner);
                this.adorner.Height = this.ActualHeight;
                this.adorner.Width = this.ActualWidth;
            }
        }

        private void StopDragging(ResizeGripLocation location, DragCompletedEventArgs e)
        {
            if (this.ResizeWhileDragging == false)
            {
                this.RemoveAdorner();
                double newWidth = this.GetNewWidth(location, e.HorizontalChange);
                this.Width = newWidth;
            }
        }

        private double GetNewWidth(ResizeGripLocation location, double horzDelta)
        {
            var realDelta = this.GetHorizontalDelta(location, horzDelta);

            double newWidth = this.ActualWidth + realDelta;

            return this.GetConstrainedValue(newWidth, this.MaxWidth, this.MinWidth);
        }

        [SuppressMessage("Performance", "CA1822: Mark members as static", Justification = "Potential breaking change")]
        private double GetHorizontalDelta(ResizeGripLocation location, double horzDelta)
        {
            double realDelta;

            if (location == ResizeGripLocation.Right)
            {
                realDelta = horzDelta;
            }
            else
            {
                Debug.Assert(location == ResizeGripLocation.Left, "location is left");
                realDelta = -horzDelta;
            }

            return realDelta;
        }

        [SuppressMessage("Performance", "CA1822: Mark members as static", Justification = "Potential breaking change")]
        private double GetConstrainedValue(double value, double max, double min)
        {
            return Math.Min(max, Math.Max(value, min));
        }
    }
}
