// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region StyleCop Suppression - generated code
using System;
using System.ComponentModel;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{

    /// <summary>
    /// Provides attached properties for TextBlock control.
    /// </summary>
    [Localizability(LocalizationCategory.None)]
    partial class TextBlockService
    {
        //
        // IsTextTrimmed dependency property
        //
        /// <summary>
        /// Identifies the IsTextTrimmed dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey IsTextTrimmedPropertyKey = DependencyProperty.RegisterAttachedReadOnly( "IsTextTrimmed", typeof(bool), typeof(TextBlockService), new PropertyMetadata( BooleanBoxes.FalseBox, IsTextTrimmedProperty_PropertyChanged) );
        /// <summary>
        /// Identifies the IsTextTrimmed dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTextTrimmedProperty = IsTextTrimmedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the value for IsTextTrimmedProperty that is attached to the element.
        /// </summary>
        /// <param name="element">The dependency object that the property is attached to.</param>
        /// <returns>
        /// The value of IsTextTrimmed that is attached to element.
        /// </returns>
        static public bool GetIsTextTrimmed(DependencyObject element)
        {
            return (bool) element.GetValue(IsTextTrimmedProperty);
        }

        /// <summary>
        /// Sets the value for IsTextTrimmedProperty that is attached to the element.
        /// </summary>
        /// <param name="element">The dependency object that the property will be attached to.</param>
        /// <param name="value">The new value.</param>
        static private void SetIsTextTrimmed(DependencyObject element, bool value)
        {
            element.SetValue(IsTextTrimmedPropertyKey,BooleanBoxes.Box(value));
        }

        static private void IsTextTrimmedProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            IsTextTrimmedProperty_PropertyChangedImplementation(o, e);
        }

        static partial void IsTextTrimmedProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

        //
        // IsTextTrimmedExternally dependency property
        //
        /// <summary>
        /// Identifies the IsTextTrimmedExternally dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTextTrimmedExternallyProperty = DependencyProperty.RegisterAttached( "IsTextTrimmedExternally", typeof(bool), typeof(TextBlockService), new PropertyMetadata( BooleanBoxes.FalseBox, IsTextTrimmedExternallyProperty_PropertyChanged) );

        /// <summary>
        /// Gets a value indicating that the Text has been trimmed external to the element.
        /// </summary>
        /// <param name="element">The dependency object that the property is attached to.</param>
        /// <returns>
        /// The value of IsTextTrimmedExternally that is attached to element.
        /// </returns>
        static public bool GetIsTextTrimmedExternally(DependencyObject element)
        {
            return (bool) element.GetValue(IsTextTrimmedExternallyProperty);
        }

        /// <summary>
        /// Sets a value indicating that the Text has been trimmed external to the element.
        /// </summary>
        /// <param name="element">The dependency object that the property will be attached to.</param>
        /// <param name="value">The new value.</param>
        static public void SetIsTextTrimmedExternally(DependencyObject element, bool value)
        {
            element.SetValue(IsTextTrimmedExternallyProperty,BooleanBoxes.Box(value));
        }

        static private void IsTextTrimmedExternallyProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            IsTextTrimmedExternallyProperty_PropertyChangedImplementation(o, e);
        }

        static partial void IsTextTrimmedExternallyProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

        //
        // IsTextTrimmedMonitoringEnabled dependency property
        //
        /// <summary>
        /// Identifies the IsTextTrimmedMonitoringEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTextTrimmedMonitoringEnabledProperty = DependencyProperty.RegisterAttached( "IsTextTrimmedMonitoringEnabled", typeof(bool), typeof(TextBlockService), new PropertyMetadata( BooleanBoxes.FalseBox, IsTextTrimmedMonitoringEnabledProperty_PropertyChanged) );

        /// <summary>
        /// Gets the value for IsTextTrimMonitoringEnabled that is attached to the element.
        /// </summary>
        /// <param name="element">The dependency object that the property is attached to.</param>
        /// <returns>
        /// The value of IsTextTrimmedMonitoringEnabled that is attached to element.
        /// </returns>
        static public bool GetIsTextTrimmedMonitoringEnabled(DependencyObject element)
        {
            return (bool) element.GetValue(IsTextTrimmedMonitoringEnabledProperty);
        }

        /// <summary>
        /// Sets the value for IsTextTrimMonitoringEnabled that is attached to the element.
        /// </summary>
        /// <param name="element">The dependency object that the property will be attached to.</param>
        /// <param name="value">The new value.</param>
        static public void SetIsTextTrimmedMonitoringEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsTextTrimmedMonitoringEnabledProperty,BooleanBoxes.Box(value));
        }

        static private void IsTextTrimmedMonitoringEnabledProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            IsTextTrimmedMonitoringEnabledProperty_PropertyChangedImplementation(o, e);
        }

        static partial void IsTextTrimmedMonitoringEnabledProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

        //
        // UntrimmedText dependency property
        //
        /// <summary>
        /// Identifies the UntrimmedText dependency property.
        /// </summary>
        public static readonly DependencyProperty UntrimmedTextProperty = DependencyProperty.RegisterAttached( "UntrimmedText", typeof(string), typeof(TextBlockService), new PropertyMetadata( string.Empty, UntrimmedTextProperty_PropertyChanged) );

        /// <summary>
        /// Gets the untrimmed text.
        /// </summary>
        /// <param name="element">The dependency object that the property is attached to.</param>
        /// <returns>
        /// The value of UntrimmedText that is attached to element.
        /// </returns>
        static public string GetUntrimmedText(DependencyObject element)
        {
            return (string) element.GetValue(UntrimmedTextProperty);
        }

        /// <summary>
        /// Sets the untrimmed text.
        /// </summary>
        /// <param name="element">The dependency object that the property will be attached to.</param>
        /// <param name="value">The new value.</param>
        static public void SetUntrimmedText(DependencyObject element, string value)
        {
            element.SetValue(UntrimmedTextProperty,value);
        }

        static private void UntrimmedTextProperty_PropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            UntrimmedTextProperty_PropertyChangedImplementation(o, e);
        }

        static partial void UntrimmedTextProperty_PropertyChangedImplementation(DependencyObject o, DependencyPropertyChangedEventArgs e);

    }
}
#endregion
