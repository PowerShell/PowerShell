// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Provider;

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
        #region IPropertyCmdletProvider accessors

        #region GetProperty

        /// <summary>
        /// Gets the specified properties from the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to get the properties from.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of the properties that the provider should return.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// A property table container the properties and their values.
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
        internal Collection<PSObject> GetProperty(
            string[] paths,
            Collection<string> providerSpecificPickList,
            bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException("path");
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.SuppressWildcardExpansion = literalPath;

            GetProperty(paths, providerSpecificPickList, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        /// <summary>
        /// Gets the specified properties from the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to get the properties from.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of the properties that the provider should return.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject representing the properties should be written to the
        /// context.
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
        internal void GetProperty(
            string[] paths,
            Collection<string> providerSpecificPickList,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
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
                    GetPropertyPrivate(
                        providerInstance,
                        providerPath,
                        providerSpecificPickList,
                        context);
                }
            }
        }

        /// <summary>
        /// Gets the property from the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// The names of the properties to get.
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
        private void GetPropertyPrivate(
            CmdletProvider providerInstance,
            string path,
            Collection<string> providerSpecificPickList,
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

            try
            {
                providerInstance.GetProperty(path, providerSpecificPickList, context);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (LoopFlowException)
            {
                throw;
            }
            catch (ActionPreferenceStopException)
            {
                throw;
            }
            catch (PipelineStoppedException)
            {
                throw;
            }
            catch (Exception e) // Catch-all OK, 3rd party callout.
            {
                throw NewProviderInvocationException(
                    "GetPropertyProviderException",
                    SessionStateStrings.GetPropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of the properties that the provider should return.
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
        internal object GetPropertyDynamicParameters(
            string path,
            Collection<string> providerSpecificPickList,
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

                return GetPropertyDynamicParameters(providerInstance, providerPaths[0], providerSpecificPickList, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// The names of the properties to get.
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
        private object GetPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            Collection<string> providerSpecificPickList,
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
                result = providerInstance.GetPropertyDynamicParameters(path, providerSpecificPickList, context);
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
                    "GetPropertyDynamicParametersProviderException",
                    SessionStateStrings.GetPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetProperty

        #region SetProperty

        /// <summary>
        /// Sets the specified properties on the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to set the properties on.
        /// </param>
        /// <param name="property">
        /// A PSObject containing the properties to be changed.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// An array of PSObjects representing the properties that were set on each item.
        /// </returns>
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
        internal Collection<PSObject> SetProperty(string[] paths, PSObject property, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (property == null)
            {
                throw PSTraceSource.NewArgumentNullException("properties");
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            SetProperty(paths, property, context);

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        /// <summary>
        /// Sets the specified properties on specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to set the properties on.
        /// </param>
        /// <param name="property">
        /// A property table containing the properties and values to be set on the object.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject is passed to the context for the properties on each item
        /// that were modified.
        /// </returns>
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
        internal void SetProperty(
            string[] paths,
            PSObject property,
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

                if (providerPaths != null)
                {
                    foreach (string providerPath in providerPaths)
                    {
                        SetPropertyPrivate(providerInstance, providerPath, property, context);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the property of the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="property">
        /// The name of the property to set.
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
        private void SetPropertyPrivate(
            CmdletProvider providerInstance,
            string path,
            PSObject property,
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
                "Caller should validate properties before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.SetProperty(path, property, context);
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
                    "SetPropertyProviderException",
                    SessionStateStrings.SetPropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the clear-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyValue">
        /// A property table containing the properties and values to be set on the object.
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
        internal object SetPropertyDynamicParameters(
            string path,
            PSObject propertyValue,
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

                return SetPropertyDynamicParameters(providerInstance, providerPaths[0], propertyValue, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the set-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyValue">
        /// The value of the property to set.
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
        private object SetPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            PSObject propertyValue,
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
                result = providerInstance.SetPropertyDynamicParameters(path, propertyValue, context);
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
                    "SetPropertyDynamicParametersProviderException",
                    SessionStateStrings.SetPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion SetProperty

        #region ClearProperty

        /// <summary>
        /// Clears the specified property on the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to clear the property on.
        /// </param>
        /// <param name="propertyToClear">
        /// The name of the property to clear.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
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
        internal void ClearProperty(
            string[] paths,
            Collection<string> propertyToClear,
            bool force,
            bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (propertyToClear == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyToClear));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            ClearProperty(paths, propertyToClear, context);

            context.ThrowFirstErrorOrDoNothing();
        }

        /// <summary>
        /// Clears the specified property in the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to clear the property on.
        /// </param>
        /// <param name="propertyToClear">
        /// A property table containing the property to clear.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="propertyToClear"/> is null.
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
        internal void ClearProperty(
            string[] paths,
            Collection<string> propertyToClear,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            if (propertyToClear == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyToClear));
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
                    ClearPropertyPrivate(providerInstance, providerPath, propertyToClear, context);
                }
            }
        }

        /// <summary>
        /// Clears the value of the property from the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyToClear">
        /// The name of the property to clear.
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
        private void ClearPropertyPrivate(
            CmdletProvider providerInstance,
            string path,
            Collection<string> propertyToClear,
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
                propertyToClear != null,
                "Caller should validate propertyToClear before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            try
            {
                providerInstance.ClearProperty(path, propertyToClear, context);
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
                    "ClearPropertyProviderException",
                    SessionStateStrings.ClearPropertyProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the clear-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyToClear">
        /// A property table containing the property to clear.
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
        internal object ClearPropertyDynamicParameters(
            string path,
            Collection<string> propertyToClear,
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

                return ClearPropertyDynamicParameters(providerInstance, providerPaths[0], propertyToClear, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the clear-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="propertyToClear">
        /// The name of the property to clear.
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
        private object ClearPropertyDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            Collection<string> propertyToClear,
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
                result = providerInstance.ClearPropertyDynamicParameters(path, propertyToClear, context);
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
                    "ClearPropertyDynamicParametersProviderException",
                    SessionStateStrings.ClearPropertyDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion ClearProperty

        #endregion IPropertyCmdletProvider accessors
    }
}

#pragma warning restore 56500

