// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;

namespace Microsoft.Management.UI.Internal
{

    /// <summary>
    /// Represents an image that can render as a vector or as a bitmap.
    /// </summary>
    [Localizability(LocalizationCategory.None)]
    partial class ScalableImage
    {
        //
        // Source dependency property
        //
        /// <summary>
        /// Identifies the Source dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register( "Source", typeof(ScalableImageSource), typeof(ScalableImage), new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions.AffectsRender, SourceProperty_PropertyChanged) );

        /// <summary>
        /// Gets or sets the ScalableImageSource used to render the image. This is a dependency property.
        /// </summary>
        [Bindable(true)]
        [Category("Common Properties")]
        [Description("Gets or sets the ScalableImageSource used to render the image. This is a dependency property.")]
        [Localizability(LocalizationCategory.None)]
        public ScalableImageSource Source
        {
            get
            {
                return (ScalableImageSource) GetValue(SourceProperty);
            }
            set
            {
                SetValue(SourceProperty,value);
            }
        }

        static private void SourceProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ScalableImage obj = (ScalableImage) o;
            obj.OnSourceChanged( new PropertyChangedEventArgs<ScalableImageSource>((ScalableImageSource)e.OldValue, (ScalableImageSource)e.NewValue) );
        }

        /// <summary>
        /// Occurs when Source property changes.
        /// </summary>
        public event EventHandler<PropertyChangedEventArgs<ScalableImageSource>> SourceChanged;

        /// <summary>
        /// Called when Source property changes.
        /// </summary>
        protected virtual void OnSourceChanged(PropertyChangedEventArgs<ScalableImageSource> e)
        {
            OnSourceChangedImplementation(e);
            RaisePropertyChangedEvent(SourceChanged, e);
        }

        partial void OnSourceChangedImplementation(PropertyChangedEventArgs<ScalableImageSource> e);

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
        // CreateAutomationPeer
        //
        /// <summary>
        /// Create an instance of the AutomationPeer.
        /// </summary>
        /// <returns>
        /// An instance of the AutomationPeer.
        /// </returns>
        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new ExtendedFrameworkElementAutomationPeer(owner: this, controlType: AutomationControlType.Image, isControlElement: false);
        }

    }
}
#endregion
