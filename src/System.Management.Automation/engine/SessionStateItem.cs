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
        #region ItemCmdletProvider accessors

        #region GetItem

        /// <summary>
        /// Gets the specified object.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the object(s). They can be either a relative (most common)
        /// or absolute path.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The item at the specified path.
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
        internal Collection<PSObject> GetItem(string[] paths, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            GetItem(paths, context);

            context.ThrowFirstErrorOrDoNothing();

            // Since there was not errors return the accumulated objects

            Collection<PSObject> results = context.GetAccumulatedObjects();

            return results;
        }

        /// <summary>
        /// Gets the specified object.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the object(s). They can be either a relative (most common)
        /// or absolute path.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all objects should be written to the WriteObject
        /// method of the <paramref name="context"/> parameter.
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
        internal void GetItem(
            string[] paths,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
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
                    GetItemPrivate(providerInstance, providerPath, context);
                }
            }
        }

        /// <summary>
        /// Gets the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        private void GetItemPrivate(
            CmdletProvider providerInstance,
            string path,
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

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            try
            {
                itemCmdletProvider.GetItem(path, context);
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
                    "GetItemProviderException",
                    SessionStateStrings.GetItemProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        internal object GetItemDynamicParameters(string path, CmdletProviderContext context)
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

                return GetItemDynamicParameters(providerInstance, providerPaths[0], newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        private object GetItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
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

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = itemCmdletProvider.GetItemDynamicParameters(path, context);
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
                    "GetItemDynamicParametersProviderException",
                    SessionStateStrings.GetItemDynamicParametersProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetItem

        #region SetItem

        /// <summary>
        /// Gets the specified object.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the object. It can be either a relative (most common)
        /// or absolute path.
        /// </param>
        /// <param name="value">
        /// The new value for the item at the specified path.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The item that was modified at the specified path.
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
        internal Collection<PSObject> SetItem(string[] paths, object value, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            SetItem(paths, value, context);

            context.ThrowFirstErrorOrDoNothing();

            // Since there was no errors return the accumulated objects

            return context.GetAccumulatedObjects();
        }

        /// <summary>
        /// Sets the specified object to the specified value.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the object. It can be either a relative (most common)
        /// or absolute path.
        /// </param>
        /// <param name="value">
        /// The new value of the item at the specified path.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
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
        internal void SetItem(
            string[] paths,
            object value,
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
                        true,
                        context,
                        out provider,
                        out providerInstance);

                if (providerPaths != null)
                {
                    foreach (string providerPath in providerPaths)
                    {
                        SetItem(providerInstance, providerPath, value, context);
                    }
                }
            }
        }

        /// <summary>
        /// Sets item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="value">
        /// The value of the item.
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
        private void SetItem(
            CmdletProvider providerInstance,
            string path,
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

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            try
            {
                itemCmdletProvider.SetItem(path, value, context);
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
                    "SetItemProviderException",
                    SessionStateStrings.SetItemProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the set-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="value">
        /// The new value of the item at the specified path.
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
        internal object SetItemDynamicParameters(string path, object value, CmdletProviderContext context)
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

                return SetItemDynamicParameters(providerInstance, providerPaths[0], value, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the set-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="value">
        /// The value to be set.
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
        private object SetItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
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

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = itemCmdletProvider.SetItemDynamicParameters(path, value, context);
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
                    "SetItemDynamicParametersProviderException",
                    SessionStateStrings.SetItemDynamicParametersProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion SetItem

        #region ClearItem

        /// <summary>
        /// Clears the specified object. Depending on the provider that the path
        /// maps to, this could mean the properties and/or content and/or value is
        /// cleared.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the object. It can be either a relative (most common)
        /// or absolute path.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The items that were cleared.
        /// </returns>
        /// <remarks>
        /// If an error occurs that error will be thrown.
        /// </remarks>
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
        internal Collection<PSObject> ClearItem(string[] paths, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            ClearItem(paths, context);

            context.ThrowFirstErrorOrDoNothing();

            return context.GetAccumulatedObjects();
        }

        /// <summary>
        /// Clears the specified item. Depending on the provider that the path
        /// maps to, this could mean the properties and/or content and/or value is
        /// cleared.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the object. It can be either a relative (most common)
        /// or absolute path.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
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
        internal void ClearItem(
            string[] paths,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
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

                if (providerPaths != null)
                {
                    foreach (string providerPath in providerPaths)
                    {
                        ClearItemPrivate(providerInstance, providerPath, context);
                    }
                }
            }
        }

        /// <summary>
        /// Clears the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        private void ClearItemPrivate(
            CmdletProvider providerInstance,
            string path,
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

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            try
            {
                itemCmdletProvider.ClearItem(path, context);
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
                    "ClearItemProviderException",
                    SessionStateStrings.ClearItemProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the clear-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        internal object ClearItemDynamicParameters(string path, CmdletProviderContext context)
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

                return ClearItemDynamicParameters(providerInstance, providerPaths[0], newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the clear-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        private object ClearItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
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

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = itemCmdletProvider.ClearItemDynamicParameters(path, context);
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
                    "ClearItemProviderException",
                    SessionStateStrings.ClearItemProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion ClearItem

        #region InvokeDefaultAction

        /// <summary>
        /// Performs the default action on the specified item. The default action is
        /// determined by the provider.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the object(s). They can be either a relative (most common)
        /// or absolute path(s).
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <remarks>
        /// If an error occurs that error will be thrown.
        /// </remarks>
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
        internal void InvokeDefaultAction(string[] paths, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.SuppressWildcardExpansion = literalPath;

            InvokeDefaultAction(paths, context);

            context.ThrowFirstErrorOrDoNothing();
        }

        /// <summary>
        /// Performs the default action on the specified item. The default action
        /// is determined by the provider.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the object(s). They can be either a relative (most common)
        /// or absolute paths.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
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
        internal void InvokeDefaultAction(
            string[] paths,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
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

                if (providerPaths != null)
                {
                    foreach (string providerPath in providerPaths)
                    {
                        InvokeDefaultActionPrivate(providerInstance, providerPath, context);
                    }
                }
            }
        }

        /// <summary>
        /// Invokes the default action on the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        private void InvokeDefaultActionPrivate(
            CmdletProvider providerInstance,
            string path,
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

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            try
            {
                itemCmdletProvider.InvokeDefaultAction(path, context);
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
                    "InvokeDefaultActionProviderException",
                    SessionStateStrings.InvokeDefaultActionProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the invoke-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        internal object InvokeDefaultActionDynamicParameters(string path, CmdletProviderContext context)
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

                return InvokeDefaultActionDynamicParameters(providerInstance, providerPaths[0], newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the invoke-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        private object InvokeDefaultActionDynamicParameters(
            CmdletProvider providerInstance,
            string path,
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

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = itemCmdletProvider.InvokeDefaultActionDynamicParameters(path, context);
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
                    "InvokeDefaultActionDynamicParametersProviderException",
                    SessionStateStrings.InvokeDefaultActionDynamicParametersProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion InvokeDefaultAction

        #endregion ItemCmdletProvider accessors
    }
}

#pragma warning restore 56500

