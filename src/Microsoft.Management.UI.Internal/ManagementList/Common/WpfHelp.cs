// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Defines a method which will be called when
    /// a condition is met.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="item">The parameter to pass to the method.</param>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal delegate void RetryActionCallback<T>(T item);

    /// <summary>
    /// Provides common WPF methods for use in the library.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    internal static class WpfHelp
    {
        #region RetryActionAfterLoaded
        private static Dictionary<FrameworkElement, RetryActionAfterLoadedDataQueue> retryActionData =
            new Dictionary<FrameworkElement, RetryActionAfterLoadedDataQueue>();

        /// <summary>
        /// Calls a method when the Loaded event is fired on a FrameworkElement.
        /// </summary>
        /// <typeparam name="T">The type of the parameter to pass to the callback method.</typeparam>
        /// <param name="element">The element whose Loaded state we are interested in.</param>
        /// <param name="callback">The method we will call if element.IsLoaded is false.</param>
        /// <param name="parameter">The parameter to pass to the callback method.</param>
        /// <returns>
        /// Returns true if the element is not loaded and the callback will be called
        /// when the element is loaded, false otherwise.
        /// </returns>
        public static bool RetryActionAfterLoaded<T>(FrameworkElement element, RetryActionCallback<T> callback, T parameter)
        {
            if (element.IsLoaded)
            {
                return false;
            }

            RetryActionAfterLoadedDataQueue data;
            if (!retryActionData.TryGetValue(element, out data))
            {
                data = new RetryActionAfterLoadedDataQueue();
                retryActionData.Add(element, data);
            }

            data.Enqueue(callback, parameter);

            element.Loaded += Element_Loaded;
            element.ApplyTemplate();

            return true;
        }

        private static void Element_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            element.Loaded -= Element_Loaded;

            RetryActionAfterLoadedDataQueue data;
            if (!retryActionData.TryGetValue(element, out data)
                || data.IsEmpty)
            {
                throw new InvalidOperationException("Event loaded callback data expected.");
            }

            Delegate callback;
            object parameter;
            data.Dequeue(out callback, out parameter);

            if (data.IsEmpty)
            {
                retryActionData.Remove(element);
            }

            callback.DynamicInvoke(parameter);
        }

        private class RetryActionAfterLoadedDataQueue
        {
            private Queue<Delegate> callbacks = new Queue<Delegate>();
            private Queue<object> parameters = new Queue<object>();

            /// <summary>
            /// Adds a callback with its associated parameter to the collection.
            /// </summary>
            /// <param name="callback">The callback to invoke.</param>
            /// <param name="parameter">The parameter to pass to the callback.</param>
            public void Enqueue(Delegate callback, object parameter)
            {
                this.callbacks.Enqueue(callback);
                this.parameters.Enqueue(parameter);
            }

            /// <summary>
            /// Removes a callback with its associated parameter from the head of
            /// the collection.
            /// </summary>
            /// <param name="callback">The callback to invoke.</param>
            /// <param name="parameter">The parameter to pass to the callback.</param>
            public void Dequeue(out Delegate callback, out object parameter)
            {
                callback = null;
                parameter = null;

                if (this.callbacks.Count < 1)
                {
                    throw new InvalidOperationException("Trying to remove when there is no data");
                }

                callback = this.callbacks.Dequeue();
                parameter = this.parameters.Dequeue();
            }

            /// <summary>
            /// Gets whether there is any callback data available.
            /// </summary>
            public bool IsEmpty
            {
                get
                {
                    return this.callbacks.Count == 0;
                }
            }
        }
        #endregion RetryActionAfterLoaded

        #region RemoveFromParent/AddChild
        /// <summary>
        /// Removes the specified element from its parent.
        /// </summary>
        /// <param name="element">The element to remove.</param>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        /// <exception cref="NotSupportedException">The specified value does not have a parent that supports removal.</exception>
        public static void RemoveFromParent(FrameworkElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            // If the element has already been detached, do nothing \\
            if (element.Parent == null)
            {
                return;
            }

            ContentControl parentContentControl = element.Parent as ContentControl;

            if (parentContentControl != null)
            {
                parentContentControl.Content = null;
                return;
            }

            var parentDecorator = element.Parent as Decorator;

            if (parentDecorator != null)
            {
                parentDecorator.Child = null;
                return;
            }

            ItemsControl parentItemsControl = element.Parent as ItemsControl;

            if (parentItemsControl != null)
            {
                parentItemsControl.Items.Remove(element);
                return;
            }

            Panel parentPanel = element.Parent as Panel;

            if (parentPanel != null)
            {
                parentPanel.Children.Remove(element);
                return;
            }

            var parentAdorner = element.Parent as UIElementAdorner;

            if (parentAdorner != null)
            {
                parentAdorner.Child = null;
                return;
            }

            throw new NotSupportedException("The specified value does not have a parent that supports removal.");
        }

        /// <summary>
        /// Removes the specified element from its parent.
        /// </summary>
        /// <param name="parent">The parent element.</param>
        /// <param name="element">The element to add.</param>
        /// <exception cref="NotSupportedException">The specified value does not have a parent that supports removal.</exception>
        public static void AddChild(FrameworkElement parent, FrameworkElement element)
        {
            ArgumentNullException.ThrowIfNull(element);

            ArgumentNullException.ThrowIfNull(parent, nameof(element));

            ContentControl parentContentControl = parent as ContentControl;

            if (parentContentControl != null)
            {
                parentContentControl.Content = element;
                return;
            }

            var parentDecorator = parent as Decorator;

            if (parentDecorator != null)
            {
                parentDecorator.Child = element;
                return;
            }

            ItemsControl parentItemsControl = parent as ItemsControl;

            if (parentItemsControl != null)
            {
                parentItemsControl.Items.Add(element);
                return;
            }

            Panel parentPanel = parent as Panel;

            if (parentPanel != null)
            {
                parentPanel.Children.Add(element);
                return;
            }

            throw new NotSupportedException("The specified parent doesn't support children.");
        }
        #endregion RemoveFromParent/AddChild

        #region VisualChild
        /// <summary>
        /// Returns the first visual child that matches the type T.
        /// Performs a breadth-first search.
        /// </summary>
        /// <typeparam name="T">The type of the child to find.</typeparam>
        /// <param name="obj">The object with a visual tree.</param>
        /// <returns>Returns an object of type T if found, otherwise null.</returns>
        public static T GetVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj == null)
            {
                return null;
            }

            var elementQueue = new Queue<DependencyObject>();
            elementQueue.Enqueue(obj);

            while (elementQueue.Count > 0)
            {
                var element = elementQueue.Dequeue();

                T item = element as T;
                if (item != null)
                {
                    return item;
                }

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
                {
                    var child = VisualTreeHelper.GetChild(element, i);
                    elementQueue.Enqueue(child);
                }
            }

            return null;
        }

        /// <summary>
        /// Finds all children of type within the specified object's visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the child to find.</typeparam>
        /// <param name="obj">The object with a visual tree.</param>
        /// <returns>All children of the specified object matching the specified type.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public static List<T> FindVisualChildren<T>(DependencyObject obj)
            where T : DependencyObject
        {
            Debug.Assert(obj != null, "obj is null");

            ArgumentNullException.ThrowIfNull(obj);

            List<T> childrenOfType = new List<T>();

            // Recursively loop through children looking for children of type within their trees \\
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject childObj = VisualTreeHelper.GetChild(obj, i);
                T child = childObj as T;

                if (child != null)
                {
                    childrenOfType.Add(child);
                }
                else
                {
                    // Recurse \\
                    childrenOfType.AddRange(FindVisualChildren<T>(childObj));
                }
            }

            return childrenOfType;
        }
        #endregion VisualChild

        /// <summary>
        /// Searches ancestors for data of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the data to find.</typeparam>
        /// <param name="obj">The visual whose ancestors are searched.</param>
        /// <returns>The data of the specified type; or if not found, <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public static T FindVisualAncestorData<T>(this DependencyObject obj)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(obj);

            FrameworkElement parent = obj.FindVisualAncestor<FrameworkElement>();

            if (parent != null)
            {
                T data = parent.DataContext as T;

                if (data != null)
                {
                    return data;
                }
                else
                {
                    return parent.FindVisualAncestorData<T>();
                }
            }

            return null;
        }

        /// <summary>
        /// Walks up the visual tree looking for an ancestor of a given type.
        /// </summary>
        /// <typeparam name="T">The type to look for.</typeparam>
        /// <param name="object">The object to start from.</param>
        /// <returns>The parent of the right type, or null.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public static T FindVisualAncestor<T>(this DependencyObject @object) where T : class
        {
            ArgumentNullException.ThrowIfNull(@object, nameof(@object));

            DependencyObject parent = VisualTreeHelper.GetParent(@object);

            if (parent != null)
            {
                T parentObj = parent as T;

                if (parentObj != null)
                {
                    return parentObj;
                }

                return parent.FindVisualAncestor<T>();
            }

            return null;
        }

        /// <summary>
        /// Executes the <see cref="RoutedCommand"/> on the current command target if it is allowed.
        /// </summary>
        /// <param name="command">The routed command.</param>
        /// <param name="parameter">A user defined data type.</param>
        /// <param name="target">The command target.</param>
        /// <returns><c>true</c> if the command could execute; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        public static bool TryExecute(this RoutedCommand command, object parameter, IInputElement target)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (command.CanExecute(parameter, target))
            {
                command.Execute(parameter, target);
                return true;
            }

            return false;
        }

        #region TemplateChild
        /// <summary>
        /// Gets the named child of an item from a templated control.
        /// </summary>
        /// <typeparam name="T">The type of the child.</typeparam>
        /// <param name="templateParent">The parent of the control.</param>
        /// <param name="childName">The name of the child.</param>
        /// <returns>The reference to the child, or null if the template part wasn't found.</returns>
        public static T GetOptionalTemplateChild<T>(Control templateParent, string childName) where T : FrameworkElement
        {
            ArgumentNullException.ThrowIfNull(templateParent);

            if (string.IsNullOrEmpty(childName))
            {
                throw new ArgumentNullException("childName");
            }

            object templatePart = templateParent.Template.FindName(childName, templateParent);
            T item = templatePart as T;

            if (item == null && templatePart != null)
            {
                HandleWrongTemplatePartType<T>(childName);
            }

            return item;
        }

        /// <summary>
        /// Gets the named child of an item from a templated control.
        /// </summary>
        /// <typeparam name="T">The type of the child.</typeparam>
        /// <param name="templateParent">The parent of the control.</param>
        /// <param name="childName">The name of the child.</param>
        /// <returns>The reference to the child.</returns>
        public static T GetTemplateChild<T>(Control templateParent, string childName) where T : FrameworkElement
        {
            T item = GetOptionalTemplateChild<T>(templateParent, childName);

            if (item == null)
            {
                HandleMissingTemplatePart<T>(childName);
            }

            return item;
        }

        /// <summary>
        /// Throws an exception with information about the template part with the wrong type.
        /// </summary>
        /// <typeparam name="T">The type of the expected template part.</typeparam>
        /// <param name="name">The name of the expected template part.</param>
        private static void HandleWrongTemplatePartType<T>(string name)
        {
            throw new ApplicationException(string.Format(
                CultureInfo.CurrentCulture,
                "A template part with the name of '{0}' is not of type {1}.",
                name,
                typeof(T).Name));
        }

        /// <summary>
        /// Throws an exception with information about the missing template part.
        /// </summary>
        /// <typeparam name="T">The type of the expected template part.</typeparam>
        /// <param name="name">The name of the expected template part.</param>
        public static void HandleMissingTemplatePart<T>(string name)
        {
            throw new ApplicationException(string.Format(
                CultureInfo.CurrentCulture,
                "A template part with the name of '{0}' and type of {1} was not found.",
                name,
                typeof(T).Name));
        }
        #endregion TemplateChild

        #region SetComponentResourceStyle
        /// <summary>
        /// Sets Style for control given a component resource key.
        /// </summary>
        /// <typeparam name="T">Type in which Component Resource Style is Defined.</typeparam>
        /// <param name="element">Element whose style need to be set.</param>
        /// <param name="keyName">Component Resource Key for Style.</param>
        public static void SetComponentResourceStyle<T>(FrameworkElement element, string keyName) where T : FrameworkElement
        {
            ComponentResourceKey styleKey = new ComponentResourceKey(typeof(T), keyName);
            element.Style = (Style)element.FindResource(styleKey);
        }
        #endregion SetComponentResourceStyle

        #region CreateRoutedPropertyChangedEventArgs
        /// <summary>
        /// Helper function to create a RoutedPropertyChangedEventArgs from a DependencyPropertyChangedEventArgs.
        /// </summary>
        /// <typeparam name="T">The type for the RoutedPropertyChangedEventArgs.</typeparam>
        /// <param name="propertyEventArgs">The DependencyPropertyChangedEventArgs data source.</param>
        /// <returns>The created event args, configured from the parameter.</returns>
        public static RoutedPropertyChangedEventArgs<T> CreateRoutedPropertyChangedEventArgs<T>(DependencyPropertyChangedEventArgs propertyEventArgs)
        {
            RoutedPropertyChangedEventArgs<T> eventArgs = new RoutedPropertyChangedEventArgs<T>(
                                                                    (T)propertyEventArgs.OldValue,
                                                                    (T)propertyEventArgs.NewValue);

            return eventArgs;
        }

        /// <summary>
        /// Helper function to create a RoutedPropertyChangedEventArgs from a DependencyPropertyChangedEventArgs.
        /// </summary>
        /// <typeparam name="T">The type for the RoutedPropertyChangedEventArgs.</typeparam>
        /// <param name="propertyEventArgs">The DependencyPropertyChangedEventArgs data source.</param>
        /// <param name="routedEvent">The routed event the property change is associated with.</param>
        /// <returns>The created event args, configured from the parameter.</returns>
        public static RoutedPropertyChangedEventArgs<T> CreateRoutedPropertyChangedEventArgs<T>(DependencyPropertyChangedEventArgs propertyEventArgs, RoutedEvent routedEvent)
        {
            RoutedPropertyChangedEventArgs<T> eventArgs = new RoutedPropertyChangedEventArgs<T>(
                                                                    (T)propertyEventArgs.OldValue,
                                                                    (T)propertyEventArgs.NewValue,
                                                                    routedEvent);

            return eventArgs;
        }
        #endregion CreateRoutedPropertyChangedEventArgs

        #region ChangeIndex
        /// <summary>
        /// Moves the item in the specified collection to the specified index.
        /// </summary>
        /// <param name="items">The collection to move the item in.</param>
        /// <param name="item">The item to move.</param>
        /// <param name="newIndex">The new index of the item.</param>
        /// <exception cref="ArgumentException">The specified item is not in the specified collection.</exception>
        /// <exception cref="ArgumentNullException">The specified value is a null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The specified index is not valid for the specified collection.</exception>
        public static void ChangeIndex(ItemCollection items, object item, int newIndex)
        {
            ArgumentNullException.ThrowIfNull(items);

            if (!items.Contains(item))
            {
                throw new ArgumentException("The specified item is not in the specified collection.", "item");
            }

            if (newIndex < 0 || newIndex > items.Count)
            {
                throw new ArgumentOutOfRangeException("newIndex", "The specified index is not valid for the specified collection.");
            }

            int oldIndex = items.IndexOf(item);

            // If the tile isn't moving, don't do anything \\
            if (newIndex == oldIndex)
            {
                return;
            }

            items.Remove(item);

            // If adding to the end, add instead of inserting \\
            if (newIndex > items.Count)
            {
                items.Add(item);
            }
            else
            {
                items.Insert(newIndex, item);
            }
        }
        #endregion ChangeIndex
    }
}
