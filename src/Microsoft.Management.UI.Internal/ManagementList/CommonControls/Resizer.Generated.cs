// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Microsoft.Management.UI.Internal
{

    /// <summary>
    /// Resizer adds a resizing grip and behavior to any control.
    /// </summary>
    /// <remarks>
    ///
    ///
    /// If a custom template is provided for this control, then the template MUST provide the following template parts:
    ///
    ///     PART_LeftGrip - A required template part which must be of type Thumb.  The grip on the left.
    ///     PART_RightGrip - A required template part which must be of type Thumb.  The grip on the right.
    ///
    /// </remarks>
    [TemplatePart(Name="PART_LeftGrip", Type=typeof(Thumb))]
    [TemplatePart(Name="PART_RightGrip", Type=typeof(Thumb))]
    [Localizability(LocalizationCategory.None)]
    partial class Resizer
    {
        //
        // Fields
        //
        private Thumb leftGrip;
        private Thumb rightGrip;

        //
        // DraggingTemplate dependency property
        //
        /// <summary>
        /// Identifies the DraggingTemplate dependency property.
        /// </summary>
        public static readonly DependencyProperty DraggingTemplateProperty = DependencyProperty.Register( "DraggingTemplate", typeof(DataTemplate), typeof(Resizer), new PropertyMetadata( null, DraggingTemplateProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets the template used for the dragging indicator when ResizeWhileDragging is false.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the template used for the dragging indicator when ResizeWhileDragging is false.")]
        [Localizability(LocalizationCategory.None)]
        public DataTemplate DraggingTemplate
        {
            get
            {
                return (DataTemplate) GetValue(DraggingTemplateProperty);
            }
            set
            {
                SetValue(DraggingTemplateProperty,value);
            }
        }

        static private void DraggingTemplateProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Resizer obj = (Resizer) o;
            obj.OnDraggingTemplateChanged( new PropertyChangedEventArgs<DataTemplate>((DataTemplate)e.OldValue, (DataTemplate)e.NewValue) );
        }

        /// <summary>
        /// Occurs when DraggingTemplate property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<DataTemplate>> DraggingTemplateChanged;

        /// <summary>
        /// Called when DraggingTemplate property changes.
        /// </summary>
        protected virtual void OnDraggingTemplateChanged(PropertyChangedEventArgs<DataTemplate> e)
        {
            OnDraggingTemplateChangedImplementation(e);
            RaisePropertyChangedEvent(DraggingTemplateChanged, e);
        }

        partial void OnDraggingTemplateChangedImplementation(PropertyChangedEventArgs<DataTemplate> e);

        //
        // GripBrush dependency property
        //
        /// <summary>
        /// Identifies the GripBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty GripBrushProperty = DependencyProperty.Register( "GripBrush", typeof(Brush), typeof(Resizer), new PropertyMetadata( new SolidColorBrush(Colors.Black), GripBrushProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets the color of the resize grips.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the color of the resize grips.")]
        [Localizability(LocalizationCategory.None)]
        public Brush GripBrush
        {
            get
            {
                return (Brush) GetValue(GripBrushProperty);
            }
            set
            {
                SetValue(GripBrushProperty,value);
            }
        }

        static private void GripBrushProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Resizer obj = (Resizer) o;
            obj.OnGripBrushChanged( new PropertyChangedEventArgs<Brush>((Brush)e.OldValue, (Brush)e.NewValue) );
        }

        /// <summary>
        /// Occurs when GripBrush property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<Brush>> GripBrushChanged;

        /// <summary>
        /// Called when GripBrush property changes.
        /// </summary>
        protected virtual void OnGripBrushChanged(PropertyChangedEventArgs<Brush> e)
        {
            OnGripBrushChangedImplementation(e);
            RaisePropertyChangedEvent(GripBrushChanged, e);
        }

        partial void OnGripBrushChangedImplementation(PropertyChangedEventArgs<Brush> e);

        //
        // GripLocation dependency property
        //
        /// <summary>
        /// Identifies the GripLocation dependency property.
        /// </summary>
        public static readonly DependencyProperty GripLocationProperty = DependencyProperty.Register( "GripLocation", typeof(ResizeGripLocation), typeof(Resizer), new PropertyMetadata( ResizeGripLocation.Right, GripLocationProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value of what grips.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value of what grips.")]
        [Localizability(LocalizationCategory.None)]
        public ResizeGripLocation GripLocation
        {
            get
            {
                return (ResizeGripLocation) GetValue(GripLocationProperty);
            }
            set
            {
                SetValue(GripLocationProperty,value);
            }
        }

        static private void GripLocationProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Resizer obj = (Resizer) o;
            obj.OnGripLocationChanged( new PropertyChangedEventArgs<ResizeGripLocation>((ResizeGripLocation)e.OldValue, (ResizeGripLocation)e.NewValue) );
        }

        /// <summary>
        /// Occurs when GripLocation property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<ResizeGripLocation>> GripLocationChanged;

        /// <summary>
        /// Called when GripLocation property changes.
        /// </summary>
        protected virtual void OnGripLocationChanged(PropertyChangedEventArgs<ResizeGripLocation> e)
        {
            OnGripLocationChangedImplementation(e);
            RaisePropertyChangedEvent(GripLocationChanged, e);
        }

        partial void OnGripLocationChangedImplementation(PropertyChangedEventArgs<ResizeGripLocation> e);

        //
        // GripWidth dependency property
        //
        /// <summary>
        /// Identifies the GripWidth dependency property.
        /// </summary>
        public static readonly DependencyProperty GripWidthProperty = DependencyProperty.Register( "GripWidth", typeof(double), typeof(Resizer), new PropertyMetadata( 4.0, GripWidthProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets the width of the grips.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the width of the grips.")]
        [Localizability(LocalizationCategory.None)]
        public double GripWidth
        {
            get
            {
                return (double) GetValue(GripWidthProperty);
            }
            set
            {
                SetValue(GripWidthProperty,value);
            }
        }

        static private void GripWidthProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Resizer obj = (Resizer) o;
            obj.OnGripWidthChanged( new PropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue) );
        }

        /// <summary>
        /// Occurs when GripWidth property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<double>> GripWidthChanged;

        /// <summary>
        /// Called when GripWidth property changes.
        /// </summary>
        protected virtual void OnGripWidthChanged(PropertyChangedEventArgs<double> e)
        {
            OnGripWidthChangedImplementation(e);
            RaisePropertyChangedEvent(GripWidthChanged, e);
        }

        partial void OnGripWidthChangedImplementation(PropertyChangedEventArgs<double> e);

        //
        // ResizeWhileDragging dependency property
        //
        /// <summary>
        /// Identifies the ResizeWhileDragging dependency property.
        /// </summary>
        public static readonly DependencyProperty ResizeWhileDraggingProperty = DependencyProperty.Register( "ResizeWhileDragging", typeof(bool), typeof(Resizer), new PropertyMetadata( BooleanBoxes.TrueBox, ResizeWhileDraggingProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets a value indicating if resizing occurs while dragging.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets a value indicating if resizing occurs while dragging.")]
        [Localizability(LocalizationCategory.None)]
        public bool ResizeWhileDragging
        {
            get
            {
                return (bool) GetValue(ResizeWhileDraggingProperty);
            }
            set
            {
                SetValue(ResizeWhileDraggingProperty,BooleanBoxes.Box(value));
            }
        }

        static private void ResizeWhileDraggingProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Resizer obj = (Resizer) o;
            obj.OnResizeWhileDraggingChanged( new PropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue) );
        }

        /// <summary>
        /// Occurs when ResizeWhileDragging property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<bool>> ResizeWhileDraggingChanged;

        /// <summary>
        /// Called when ResizeWhileDragging property changes.
        /// </summary>
        protected virtual void OnResizeWhileDraggingChanged(PropertyChangedEventArgs<bool> e)
        {
            OnResizeWhileDraggingChangedImplementation(e);
            RaisePropertyChangedEvent(ResizeWhileDraggingChanged, e);
        }

        partial void OnResizeWhileDraggingChangedImplementation(PropertyChangedEventArgs<bool> e);

        //
        // ThumbGripLocation dependency property
        //
        /// <summary>
        /// Identifies the ThumbGripLocation dependency property.
        /// </summary>
        public static readonly DependencyProperty ThumbGripLocationProperty = DependencyProperty.RegisterAttached( "ThumbGripLocation", typeof(ResizeGripLocation), typeof(Resizer), new PropertyMetadata( ResizeGripLocation.Right, ThumbGripLocationProperty_PropertyChanged) );

        /// <summary>
        /// Gets the location for a grip.
        /// </summary>
        /// <param name="element">The dependency object that the property is attached to.</param>
        /// <returns>
        /// The value of ThumbGripLocation that is attached to element.
        /// </returns>
        static public ResizeGripLocation GetThumbGripLocation(DependencyObject element)
        {
            return (ResizeGripLocation) element.GetValue(ThumbGripLocationProperty);
        }

        /// <summary>
        /// Sets the location for a grip.
        /// </summary>
        /// <param name="element">The dependency object that the property will be attached to.</param>
        /// <param name="value">The new value.</param>
        static public void SetThumbGripLocation(DependencyObject element, ResizeGripLocation value)
        {
            element.SetValue(ThumbGripLocationProperty,value);
        }

        static private void ThumbGripLocationProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ThumbGripLocationProperty_PropertyChangedImplementation(o, e);
        }

        static partial void ThumbGripLocationProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

        //
        // VisibleGripWidth dependency property
        //
        /// <summary>
        /// Identifies the VisibleGripWidth dependency property.
        /// </summary>
        public static readonly DependencyProperty VisibleGripWidthProperty = DependencyProperty.Register( "VisibleGripWidth", typeof(double ), typeof(Resizer), new PropertyMetadata( 1.0, VisibleGripWidthProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets the visible width of the grips.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the visible width of the grips.")]
        [Localizability(LocalizationCategory.None)]
        public double  VisibleGripWidth
        {
            get
            {
                return (double ) GetValue(VisibleGripWidthProperty);
            }
            set
            {
                SetValue(VisibleGripWidthProperty,value);
            }
        }

        static private void VisibleGripWidthProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Resizer obj = (Resizer) o;
            obj.OnVisibleGripWidthChanged( new PropertyChangedEventArgs<double >((double )e.OldValue, (double )e.NewValue) );
        }

        /// <summary>
        /// Occurs when VisibleGripWidth property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<double >> VisibleGripWidthChanged;

        /// <summary>
        /// Called when VisibleGripWidth property changes.
        /// </summary>
        protected virtual void OnVisibleGripWidthChanged(PropertyChangedEventArgs<double > e)
        {
            OnVisibleGripWidthChangedImplementation(e);
            RaisePropertyChangedEvent(VisibleGripWidthChanged, e);
        }

        partial void OnVisibleGripWidthChangedImplementation(PropertyChangedEventArgs<double > e);

        /// <summary>
        /// Called when a property changes.
        /// </summary>
        private void RaisePropertyChangedEvent<T>(EventHandler<PropertyChangedEventArgs<T>> eh, PropertyChangedEventArgs<T> e)
        {
            if (eh != null)
            {
                eh(this,e);
            }
        }

        //
        // OnApplyTemplate
        //

        /// <summary>
        /// Called when ApplyTemplate is called.
        /// </summary>
        public override void OnApplyTemplate()
        {
            PreOnApplyTemplate();
            base.OnApplyTemplate();
            this.leftGrip = WpfHelp.GetTemplateChild<Thumb>(this,"PART_LeftGrip");
            this.rightGrip = WpfHelp.GetTemplateChild<Thumb>(this,"PART_RightGrip");
            PostOnApplyTemplate();
        }

        partial void PreOnApplyTemplate();

        partial void PostOnApplyTemplate();

        //
        // Static constructor
        //

        /// <summary>
        /// Called when the type is initialized.
        /// </summary>
        static Resizer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Resizer), new FrameworkPropertyMetadata(typeof(Resizer)));
            StaticConstructorImplementation();
        }

        static partial void StaticConstructorImplementation();

    }
}
#endregion
