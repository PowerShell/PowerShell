// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Provider;
using Microsoft.Win32;
using Dbg = System.Management.Automation;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings
#pragma warning disable 56500

namespace System.Management.Automation
{
    /// <summary>
    /// Holds the state of a Monad Shell session.
    /// </summary>
    internal sealed partial class SessionStateInternal
    {
        #region IDynamicPropertyCmdletProvider accessors

        #region NewProperty

        /// <summary>
        /// Creates a new property on the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) on which the new property should be created.
        /// </param>
        /// <param name="property">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="type">
        /// The type of the property that should be created.
        /// </param>
        /// <param name="value">
        /// The new value of the property that should be created.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// A property table containing the properties and their values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal Collection<PSObject> NewProperty(
            string[] paths,
            string property,
            RegistryValueKind type,
            object value,
            bool force,
            bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (property == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(property));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            NewProperty(paths, property, type, value, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        /// <summary>
        /// Creates a new property on the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) on which the new property should be created.
        /// </param>
        /// <param name="property">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="type">
        /// The type of the property that should be created.
        /// </param>
        /// <param name="value">
        /// The new value of the property that should be created.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// Nothing. The property should be passed to the context as a PSObject.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal void NewProperty(
            string[] paths,
            string property,
            RegistryValueKind type,
            object value,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (property == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(property));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        false,
                        context,
                        out provider,
                        out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    NewProperty(providerInstance, providerPath, property, type, value, context);
                }
            }
        }

        /// <summary>
        /// Creates a new property on the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="property">
        /// The name of the property to create.
        /// </param>
        /// <param name="type">
        /// The type of the property to create.
        /// </param>
        /// <param name="value">
        /// The value of the property to create.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private void NewProperty(
            CmdletProvider providerInstance,
            string path,
            string property,
            RegistryValueKind type,
            object value,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                property != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.NewProperty(path, property, type, value, context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "NewPropertyProviderException",
                    SessionStateStrings.NewPropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the new-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="type">
        /// The type of the property that should be created.
        /// </param>
        /// <param name="value">
        /// The new value of the property that should be created.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal object NewPropertyDynamicParameters(
             string path,
            string propertyName,
            RegistryValueKind type,
            object value,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            CmdletProviderContext newContext =
                new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            if (providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path

                return NewPropertyDynamicParameters(providerInstance, providerPaths[0], propertyName, type, value, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the new-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property to create.
        /// </param>
        /// <param name="type">
        /// The type of the property to create.
        /// </param>
        /// <param name="value">
        /// The value of the property.
        /// </param>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private object NewPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string propertyName,
            RegistryValueKind type,
            object value,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            object result = null;
            try
            {
                result = providerInstance.NewPropertyDynamicParameters(path, propertyName, type, value, context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "NewPropertyDynamicParametersProviderException",
                    SessionStateStrings.NewPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion NewProperty

        #region RemoveProperty

        /// <summary>
        /// Removes the specified property from the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to remove the property from.
        /// </param>
        /// <param name="property">
        /// The name of the property to remove
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="property"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal void RemoveProperty(string[] paths, string property, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (property == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(property));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            RemoveProperty(paths, property, context);

            context.ThrowFirstErrorOrDoNothing();
        }

        /// <summary>
        /// Removes the specified properties from the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to remove the properties from.
        /// </param>
        /// <param name="property">
        /// The name of the property to remove
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="property"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal void RemoveProperty(
            string[] paths,
            string property,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (property == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(property));
            }

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                ProviderInfo provider = null;
                CmdletProvider providerInstance = null;

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        false,
                        context,
                        out provider,
                        out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    RemoveProperty(providerInstance, providerPath, property, context);
                }
            }
        }

        /// <summary>
        /// Removes the property from the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="property">
        /// The name of the property to remove.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private void RemoveProperty(
            CmdletProvider providerInstance,
            string path,
            string property,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                property != null,
                "Caller should validate property before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.RemoveProperty(path, property, context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "RemovePropertyProviderException",
                    SessionStateStrings.RemovePropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the remove-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be created.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal object RemovePropertyDynamicParameters(
             string path,
            string propertyName,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            CmdletProviderContext newContext =
               new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths =
                 Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            if (providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path

                return RemovePropertyDynamicParameters(providerInstance, providerPaths[0], propertyName, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the remove-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property to remove.
        /// </param>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private object RemovePropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string propertyName,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            object result = null;
            try
            {
                result = providerInstance.RemovePropertyDynamicParameters(path, propertyName, context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "RemovePropertyDynamicParametersProviderException",
                    SessionStateStrings.RemovePropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion RemoveProperty

        #region CopyProperty

        /// <summary>
        /// Copies the specified property on the specified item to the specified property
        /// on the destination item.  The source and destination items can be the same item.
        /// </summary>
        /// <param name="sourcePaths">
        /// The path(s) to the item(s) to copy the property from.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to copy the property to.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal Collection<PSObject> CopyProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            bool force,
            bool literalPath)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            CopyProperty(sourcePaths, sourceProperty, destinationPath, destinationProperty, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        /// <summary>
        /// Copies the specified property on the specified item to the specified property
        /// on the destination item.  The source and destination items can be the same item.
        /// </summary>
        /// <param name="sourcePaths">
        /// The path(s) to the item(s) to copy the property from.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to copy the property to.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal void CopyProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            foreach (string sourcePath in sourcePaths)
            {
                if (sourcePath == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
                }

                ProviderInfo provider = null;
                CmdletProvider providerInstance = null;

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        sourcePath,
                        false,
                        context,
                        out provider,
                        out providerInstance);

                if (providerPaths.Count > 0)
                {
                    // Save off the original filters
                    Collection<string> includeFilters = context.Include;
                    Collection<string> excludeFilters = context.Exclude;
                    string filterString = context.Filter;

                    // now modify the filters so that the destination isn't filtered

                    context.SetFilters(
                        new Collection<string>(),
                        new Collection<string>(),
                        null);

                    Collection<string> providerDestinationPaths =
                        Globber.GetGlobbedProviderPathsFromMonadPath(
                            destinationPath,
                            false,
                            context,
                            out provider,
                            out providerInstance);

                    // Now reapply the filters

                    context.SetFilters(
                        includeFilters,
                        excludeFilters,
                        filterString);

                    foreach (string providerPath in providerPaths)
                    {
                        foreach (string providerDestinationPath in providerDestinationPaths)
                        {
                            CopyProperty(providerInstance, providerPath, sourceProperty, providerDestinationPath, destinationProperty, context);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copies the property.
        /// </summary>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="sourcePath">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to copy the property to.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private void CopyProperty(
            CmdletProvider providerInstance,
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                sourcePath != null,
                "Caller should validate sourcePath before calling this method");

            Dbg.Diagnostics.Assert(
                sourceProperty != null,
                "Caller should validate sourceProperty before calling this method");

            Dbg.Diagnostics.Assert(
                destinationPath != null,
                "Caller should validate destinationPath before calling this method");

            Dbg.Diagnostics.Assert(
                destinationProperty != null,
                "Caller should validate destinationProperty before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.CopyProperty(sourcePath, sourceProperty, destinationPath, destinationProperty, context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "CopyPropertyProviderException",
                    SessionStateStrings.CopyPropertyProviderException,
                    providerInstance.ProviderInfo,
                    sourcePath,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the copy-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be copied.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to copy the property to.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal object CopyPropertyDynamicParameters(
             string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            CmdletProviderContext newContext =
                new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            if (providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path

                return CopyPropertyDynamicParameters(
                    providerInstance,
                    providerPaths[0],
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the copy-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to copy.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to copy the property to on the destination item.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private object CopyPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            object result = null;
            try
            {
                result = providerInstance.CopyPropertyDynamicParameters(
                    path,
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "CopyPropertyDynamicParametersProviderException",
                    SessionStateStrings.CopyPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion CopyProperty

        #region MoveProperty

        /// <summary>
        /// Moves the specified property on the specified item to the specified property
        /// on the destination item.  The source and destination items can be the same item.
        /// </summary>
        /// <param name="sourcePaths">
        /// The path(s) to the item(s) to move the property from.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be moved.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to move the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to move the property to.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="destinationPath"/> resolves to more than one item.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal Collection<PSObject> MoveProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            bool force,
            bool literalPath)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            MoveProperty(sourcePaths, sourceProperty, destinationPath, destinationProperty, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        /// <summary>
        /// Moves the specified property on the specified item to the specified property
        /// on the destination item.  The source and destination items can be the same item.
        /// </summary>
        /// <param name="sourcePaths">
        /// The path(s) to the item(s) to move the property from.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be moved.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to move the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to move the property to.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="sourcePath"/>, <paramref name="sourceProperty"/>,
        /// <paramref name="destinationPath"/>, or <paramref name="destinationProperty"/>
        ///  is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// If <paramref name="destinationPath"/> resolves to more than one item.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="sourcePath"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="sourcePath"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal void MoveProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            // We shouldn't be filtering the destination path

            CmdletProviderContext destinationContext = new CmdletProviderContext(context);

            destinationContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> destinationProviderPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    destinationPath,
                    false,
                    destinationContext,
                    out provider,
                    out providerInstance);

            if (destinationProviderPaths.Count > 1)
            {
                ArgumentException argException =
                    PSTraceSource.NewArgumentException(
                        nameof(destinationPath),
                        SessionStateStrings.MovePropertyDestinationResolveToSingle);

                context.WriteError(new ErrorRecord(argException, argException.GetType().FullName, ErrorCategory.InvalidArgument, destinationProviderPaths));
            }
            else
            {
                foreach (string sourcePath in sourcePaths)
                {
                    if (sourcePath == null)
                    {
                        throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
                    }

                    Collection<string> providerPaths =
                        Globber.GetGlobbedProviderPathsFromMonadPath(
                            sourcePath,
                            false,
                            context,
                            out provider,
                            out providerInstance);

                    foreach (string providerPath in providerPaths)
                    {
                        MoveProperty(providerInstance, providerPath, sourceProperty, destinationProviderPaths[0], destinationProperty, context);
                    }
                }
            }
        }

        /// <summary>
        /// Moves the property from one item to another.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="sourcePath">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The property to be moved.
        /// </param>
        /// <param name="destinationPath">
        /// The path of the item to move the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to move the property to.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private void MoveProperty(
            CmdletProvider providerInstance,
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                sourcePath != null,
                "Caller should validate sourcePath before calling this method");

            Dbg.Diagnostics.Assert(
                sourceProperty != null,
                "Caller should validate sourceProperty before calling this method");

            Dbg.Diagnostics.Assert(
                destinationPath != null,
                "Caller should validate destinationPath before calling this method");

            Dbg.Diagnostics.Assert(
                destinationProperty != null,
                "Caller should validate destinationProperty before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.MoveProperty(sourcePath, sourceProperty, destinationPath, destinationProperty, context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "MovePropertyProviderException",
                    SessionStateStrings.MovePropertyProviderException,
                    providerInstance.ProviderInfo,
                    sourcePath,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the move-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be moved.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to move the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to move the property to.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal object MovePropertyDynamicParameters(
             string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            CmdletProviderContext newContext =
                new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            if (providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path

                return MovePropertyDynamicParameters(
                    providerInstance,
                    providerPaths[0],
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the move-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to move.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item to move the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property on the destination item to move the property to.
        /// </param>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private object MovePropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string sourceProperty,
            string destinationPath,
            string destinationProperty,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            object result = null;

            try
            {
                result = providerInstance.MovePropertyDynamicParameters(
                    path,
                    sourceProperty,
                    destinationPath,
                    destinationProperty,
                    context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "MovePropertyDynamicParametersProviderException",
                    SessionStateStrings.MovePropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion MoveProperty

        #region RenameProperty

        /// <summary>
        /// Renames the specified property on the specified item to the specified property.
        /// </summary>
        /// <param name="sourcePaths">
        /// The path(s) to the item(s) to rename the property on.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be renamed.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to rename the property to.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/>, <paramref name="sourceProperty"/>,
        /// or <paramref name="destinationProperty"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        internal Collection<PSObject> RenameProperty(
            string[] sourcePaths,
            string sourceProperty,
            string destinationProperty,
            bool force,
            bool literalPath)
        {
            if (sourcePaths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePaths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            RenameProperty(sourcePaths, sourceProperty, destinationProperty, context);

            context.ThrowFirstErrorOrDoNothing();
            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        /// <summary>
        /// Renames the specified property on the specified item to the specified property.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to rename the property on.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be renamed.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to rename the property to.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/>, <paramref name="sourceProperty"/>,
        /// or <paramref name="destinationProperty"/> is null.
        /// </exception>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal void RenameProperty(
            string[] paths,
            string sourceProperty,
            string destinationProperty,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (sourceProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourceProperty));
            }

            if (destinationProperty == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationProperty));
            }

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                ProviderInfo provider = null;
                CmdletProvider providerInstance = null;

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        false,
                        context,
                        out provider,
                        out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    RenameProperty(providerInstance, providerPath, sourceProperty, destinationProperty, context);
                }
            }
        }

        /// <summary>
        /// Renames the property of the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="sourcePath">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to rename.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private void RenameProperty(
            CmdletProvider providerInstance,
            string sourcePath,
            string sourceProperty,
            string destinationProperty,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                sourcePath != null,
                "Caller should validate sourcePath before calling this method");

            Dbg.Diagnostics.Assert(
                sourceProperty != null,
                "Caller should validate sourceProperty before calling this method");

            Dbg.Diagnostics.Assert(
                destinationProperty != null,
                "Caller should validate destinationProperty before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.RenameProperty(sourcePath, sourceProperty, destinationProperty, context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "RenamePropertyProviderException",
                    SessionStateStrings.RenamePropertyProviderException,
                    providerInstance.ProviderInfo,
                    sourcePath,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the rename-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to be renamed.
        /// </param>
        /// <param name="destinationProperty">
        /// The name of the property to rename the property to.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="ProviderNotFoundException">
        /// If the <paramref name="path"/> refers to a provider that could not be found.
        /// </exception>
        /// <exception cref="DriveNotFoundException">
        /// If the <paramref name="path"/> refers to a drive that could not be found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If the provider that the <paramref name="path"/> refers to does
        /// not support this operation.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        /// <exception cref="ItemNotFoundException">
        /// If <paramref name="path"/> does not contain glob characters and
        /// could not be found.
        /// </exception>
        internal object RenamePropertyDynamicParameters(
             string path,
            string sourceProperty,
            string destinationProperty,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            CmdletProviderContext newContext =
                new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            if (providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path

                return RenamePropertyDynamicParameters(
                    providerInstance,
                    providerPaths[0],
                    sourceProperty,
                    destinationProperty,
                    newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the rename-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to rename.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name for the property.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// If the <paramref name="providerInstance"/> does not support this operation.
        /// </exception>
        /// <exception cref="PipelineStoppedException">
        /// If the pipeline is being stopped while executing the command.
        /// </exception>
        /// <exception cref="ProviderInvocationException">
        /// If the provider threw an exception.
        /// </exception>
        private object RenamePropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string sourceProperty,
            string destinationProperty,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            object result = null;
            try
            {
                result = providerInstance.RenamePropertyDynamicParameters(
                    path,
                    sourceProperty,
                    destinationProperty,
                    context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "RenamePropertyDynamicParametersProviderException",
                    SessionStateStrings.RenamePropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion RenameProperty

        #endregion IDynamicPropertyCmdletProvider accessors
    }
}

#pragma warning restore 56500
