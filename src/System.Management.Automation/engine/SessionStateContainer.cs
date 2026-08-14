// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Provider;
using System.Management.Automation.Runspaces;
using System.Reflection;

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
        #region ContainerCmdletProvider accessors

        #region ItemExists

        /// <summary>
        /// Determines if the monad virtual namespace path exists.
        /// </summary>
        /// <param name="path">
        /// The path to the object to determine if it exists.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// true if the object specified by path exists, false otherwise.
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
        internal bool ItemExists(string path, bool force, bool literalPath)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            bool result = ItemExists(path, context);

            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        /// <summary>
        /// Determines if the monad virtual namespace path exists.
        /// </summary>
        /// <param name="path">
        /// The path to the object to determine if it exists.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// true if the object specified by path exists, false otherwise.
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
        internal bool ItemExists(
            string path,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            bool result = false;
            try
            {
                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        true,
                        context,
                        out provider,
                        out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    result = ItemExists(providerInstance, providerPath, context);
                    if (result)
                    {
                        break;
                    }
                }
            }
            catch (ItemNotFoundException)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Determines if the item at the specified path exists.
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
        internal bool ItemExists(
            CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerId before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ItemCmdletProvider itemCmdletProvider =
                GetItemProviderInstance(providerInstance);

            bool result = false;

            try
            {
                result = itemCmdletProvider.ItemExists(path, context);
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
                    "ItemExistsProviderException",
                    SessionStateStrings.ItemExistsProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        /// <summary>
        /// Gets the dynamic parameters for the test-path cmdlet.
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
        internal object ItemExistsDynamicParameters(string path, CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
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

                return ItemExistsDynamicParameters(providerInstance, providerPaths[0], newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the test-path cmdlet.
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
        private object ItemExistsDynamicParameters(
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = containerCmdletProvider.ItemExistsDynamicParameters(path, context);
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
                    "ItemExistsDynamicParametersProviderException",
                    SessionStateStrings.ItemExistsDynamicParametersProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion Exists

        #region IsValidPath

        /// <summary>
        /// Determines if the MSH path is a syntactically and semantically valid path for the provider.
        /// </summary>
        /// <param name="path">
        /// The path to validate.
        /// </param>
        /// <returns>
        /// true if the object specified by path is syntactically and semantically valid, false otherwise.
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
        internal bool IsValidPath(string path)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            bool result = IsValidPath(path, context);

            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        /// <summary>
        /// Determines if the MSH path is a syntactically and semantically valid path for the provider.
        /// </summary>
        /// <param name="path">
        /// The path to validate.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// true if the object specified by path is syntactically and semantically valid, false otherwise.
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
        internal bool IsValidPath(
             string path,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            ProviderInfo provider = null;
            PSDriveInfo driveInfo = null;

            string providerPath =
                Globber.GetProviderPath(
                    path,
                    context,
                    out provider,
                    out driveInfo);

            ItemCmdletProvider providerInstance = GetItemProviderInstance(provider);

            return IsValidPath(providerInstance, providerPath, context);
        }

        /// <summary>
        /// Determines if the specified path is valid.
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
        private bool IsValidPath(
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

            bool result = false;

            try
            {
                result = itemCmdletProvider.IsValidPath(path, context);
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
                    "IsValidPathProviderException",
                    SessionStateStrings.IsValidPathProviderException,
                    itemCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion IsValidPath

        #region IsItemContainer

        /// <summary>
        /// Determines if the monad virtual namespace path is a container.
        /// </summary>
        /// <param name="path">
        /// The path to the object to determine if it is a container.
        /// </param>
        /// <returns>
        /// true if the object specified by path is a container, false otherwise.
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
        internal bool IsItemContainer(string path)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            bool result = IsItemContainer(path, context);

            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        /// <summary>
        /// Determines if the monad virtual namespace path is a container.
        /// </summary>
        /// <param name="path">
        /// The path to the object to determine if it is a container.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// true if the object specified by path is a container, false otherwise.
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
        internal bool IsItemContainer(
            string path,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            bool result = false;
            try
            {
                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        true,
                        context,
                        out provider,
                        out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    result = IsItemContainer(providerInstance, providerPath, context);
                    if (!result)
                    {
                        break;
                    }
                }
            }
            catch (ItemNotFoundException)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Determines if the item at the specified path is a container.
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
        private bool IsItemContainer(
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

            bool result = false;

            NavigationCmdletProvider navigationCmdletProvider = null;

            try
            {
                // If it is a NavigationProvider instance then we
                // can ask the provider if the path is to a container.

                navigationCmdletProvider =
                    GetNavigationProviderInstance(providerInstance, false);

                try
                {
                    result = navigationCmdletProvider.IsItemContainer(path, context);
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
                        "IsItemContainerProviderException",
                    SessionStateStrings.IsItemContainerProviderException,
                        navigationCmdletProvider.ProviderInfo,
                        path,
                        e);
                }
            }
            catch (NotSupportedException)
            {
                try
                {
                    // If it is a ContainerProvider instance then the
                    // root (empty path) is considered a container
                    // but everything else is not.

                    GetContainerProviderInstance(providerInstance);

                    if (path.Length == 0)
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
                catch (NotSupportedException)
                {
                    // If the provider is neither a NavigationProvider nor a
                    // ContainerProvider then the path cannot be a container

                    result = false;
                }
            }

            return result;
        }

        #endregion IsItemContainer

        #region RemoveItem

        /// <summary>
        /// Deletes the specified object.
        /// </summary>
        /// <param name="paths">
        /// A relative or absolute path to the object to be deleted.
        /// </param>
        /// <param name="recurse">
        /// The delete should occur in all sub-containers of the specified path.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
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
        internal void RemoveItem(string[] paths, bool recurse, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            RemoveItem(paths, recurse, context);

            context.ThrowFirstErrorOrDoNothing();
        }

        /// <summary>
        /// Deletes the specified object.
        /// </summary>
        /// <param name="paths">
        /// A relative or absolute path to the object to be deleted.
        /// </param>
        /// <param name="recurse">
        /// The delete should occur in all sub-containers of the specified path.
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
        internal void RemoveItem(
            string[] paths,
            bool recurse,
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
                    RemoveItem(providerInstance, providerPath, recurse, context);
                }
            }
        }

        /// <summary>
        /// Internal remove item method that just calls the provider directly without globbing.
        /// </summary>
        /// <param name="providerId">
        /// The name of the provider to use.
        /// </param>
        /// <param name="path">
        /// The path of the item to remove.
        /// </param>
        /// <param name="recurse">
        /// True if all items should be removed recursively.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
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
        internal void RemoveItem(
            string providerId,
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerId != null,
                "Caller should validate providerId before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            CmdletProvider providerInstance = GetProviderInstance(providerId);
            RemoveItem(providerInstance, path, recurse, context);
        }

        /// <summary>
        /// Internal remove item method that just calls the provider directly without globbing.
        /// </summary>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="path">
        /// The path of the item to remove.
        /// </param>
        /// <param name="recurse">
        /// True if all items should be removed recursively.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
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
        internal void RemoveItem(
            CmdletProvider providerInstance,
            string path,
            bool recurse,
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            try
            {
                if (context.HasIncludeOrExclude)
                {
                    int childrenNotMatchingFilterCriteria = 0;

                    // Use the new code path only if either Include or Exclude is specified
                    // This will take care of all the child items.
                    // This will also take care of the case where "path" is not a container.
                    ProcessPathItems(providerInstance, path, recurse, context, out childrenNotMatchingFilterCriteria, ProcessMode.Delete, skipIsItemContainerCheck: false);

                    // Now delete the container if it matches the filter(s)
                    // and the container does not have any children.
                    if (IsItemContainer(providerInstance, path, context))
                    {
                        string item = GetChildName(path, context, false);
                        bool isIncludeMatch =
                            SessionStateUtilities.MatchesAnyWildcardPattern(
                                item,
                                SessionStateUtilities.CreateWildcardsFromStrings(
                                    context.Include, WildcardOptions.IgnoreCase),
                                true);

                        if (isIncludeMatch)
                        {
                            if (!SessionStateUtilities.MatchesAnyWildcardPattern(
                                item,
                                SessionStateUtilities.CreateWildcardsFromStrings(
                                    context.Exclude, WildcardOptions.IgnoreCase),
                                false))
                            {
                                // Earlier, we used to check if "path" has any child items remaining. If not, we remove "path".
                                // This does not work for some providers (for e.g. IIS provider) which do not support removing the child items
                                // So, instead of checking for any child items remaining (which are applicable to only those providers which support them - like File, Registry),
                                // we check if there are any items that were never intended to be deleted. If there are no such items, then, we can remove "path".
                                // WinBlue: 289907
                                if (childrenNotMatchingFilterCriteria == 0)
                                {
                                    containerCmdletProvider.RemoveItem(path, false, context);
                                }
                            }
                        }
                    }
                }
                else
                {
                    containerCmdletProvider.RemoveItem(path, recurse, context);
                }
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
                    "RemoveItemProviderException",
                    SessionStateStrings.RemoveItemProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the remove-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="recurse">
        /// The delete should occur in all sub-containers of the specified path.
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
        internal object RemoveItemDynamicParameters(
            string path,
            bool recurse,
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

                return RemoveItemDynamicParameters(providerInstance, providerPaths[0], recurse, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the remove-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="recurse">
        /// If true, all items in the subtree should be removed.
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
        private object RemoveItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            bool recurse,
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = containerCmdletProvider.RemoveItemDynamicParameters(path, recurse, context);
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
                    "RemoveItemProviderException",
                    SessionStateStrings.RemoveItemProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion RemoveItem

        #region GetChildItems

        /// <summary>
        /// Gets the children of the specified item.
        /// </summary>
        /// <param name="paths">
        /// An array of relative or absolute paths to the object to get the children of.
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the children in all the sub-containers of the specified
        /// container. If false, only gets the immediate children of the specified
        /// container.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <remarks>
        /// This method does not provider streaming of the results. If you want streaming
        /// then you must call the overload that takes a CmdletProviderContext.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
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
        internal Collection<PSObject> GetChildItems(string[] paths, bool recurse, uint depth, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                GetChildItems(path, recurse, depth, context);
            }

            context.ThrowFirstErrorOrDoNothing();

            return context.GetAccumulatedObjects();
        }

        /// <summary>
        /// Gets the children of the specified item.
        /// </summary>
        /// <param name="path">
        /// A relative or absolute path to the object to get the children of.
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the children in all the sub-containers of the specified
        /// container. If false, only gets the immediate children of the specified
        /// container.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> or <paramref name="context"/> is null.
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
        internal void GetChildItems(
            string path,
            bool recurse,
            uint depth,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (context == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(context));
            }

            ProviderInfo provider = null;

            if ((recurse && !context.SuppressWildcardExpansion) || LocationGlobber.ShouldPerformGlobbing(path, context))
            {
                bool modifiedInclude = false;

                try
                {
                    // If we're recursing, do some path fixups to match user
                    // expectations, but only if the last part is a file and not a directory:
                    if (recurse && !path.EndsWith(Path.DirectorySeparatorChar) && !path.EndsWith(Path.AltDirectorySeparatorChar))
                    {
                        string childName = GetChildName(path, context);

                        // If -File or -Directory is specified and path is ended with '*', we should include the parent path as search path

                        bool isFileOrDirectoryPresent = false;

                        if (context.DynamicParameters is Microsoft.PowerShell.Commands.GetChildDynamicParameters dynParam)
                        {
                            isFileOrDirectoryPresent = dynParam.File.IsPresent || dynParam.Directory.IsPresent;
                        }

                        if (string.Equals(childName, "*", StringComparison.OrdinalIgnoreCase) && isFileOrDirectoryPresent)
                        {
                            string parentName = path.Substring(0, path.Length - childName.Length);
                            path = parentName;
                        }
                        // dir c:\tem* -include *.ps1 -rec => No change
                        if ((context.Include == null) || (context.Include.Count == 0))
                        {
                            // dir c:\tem* -rec => dir c:\ -include tem* -rec
                            // dir tem* -rec => dir -include tem* -rec
                            // dir temp -rec

                            // Should glob paths and files that match tem*, but then
                            // recurse into all subdirectories and do the same for
                            // those directories.
                            if (!string.IsNullOrEmpty(path) && !IsItemContainer(path))
                            {
                                if (!string.Equals(childName, "*", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (context.Include != null)
                                    {
                                        context.Include.Add(childName);
                                        modifiedInclude = true;
                                    }
                                }

                                string parentName = path.Substring(0, path.Length - childName.Length);
                                path = parentName;
                            }
                        }
                    }

                    // Save the include and exclude filters so that we can ignore
                    // them when doing recursion

                    Collection<string> include = context.Include;
                    Collection<string> exclude = context.Exclude;
                    string filter = context.Filter;

                    if (recurse)
                    {
                        context.SetFilters(
                            new Collection<string>(),
                            new Collection<string>(),
                            null);
                    }

                    CmdletProvider providerInstance = null;
                    Collection<string> providerPaths = null;

                    try
                    {
                        providerPaths = Globber.GetGlobbedProviderPathsFromMonadPath(
                                path,
                                false,
                                context,
                                out provider,
                                out providerInstance);
                    }
                    finally
                    {
                        // Reset the include and exclude filters
                        context.SetFilters(
                            include,
                            exclude,
                            filter);
                    }

                    // Ensure this is a container provider before recursing.
                    // GetContainerProviderInstance throws otherwise (as was done in V2.)
                    if (recurse)
                    {
                        ContainerCmdletProvider unused = GetContainerProviderInstance(provider);
                    }

                    bool getChildrenBecauseNoGlob = !LocationGlobber.StringContainsGlobCharacters(path);
                    // If we are doing recursion and we have include or exclude
                    // filters the recursion must be done manually.
                    // Win8: 279933 Perf degradation: recursive get-childitem is 100% slower than win7
                    // To fix this Perf regression, using getChildrenBecauseNoGlob (+recurse) variables
                    // to decide when to use ManualGetChildItems and when not to.
                    if (
                        (recurse && !getChildrenBecauseNoGlob && (include != null) && (include.Count == 0)) ||
                        (include != null && include.Count > 0) ||
                        (exclude != null && exclude.Count > 0))
                    {
                        // Do the recursion manually so that we can apply the
                        // include and exclude filters
                        foreach (string providerPath in providerPaths)
                        {
                            // Making sure to obey the StopProcessing.
                            if (context.Stopping)
                            {
                                return;
                            }

                            ProcessPathItems(providerInstance, providerPath, recurse, depth, context, out _, ProcessMode.Enumerate);
                        }
                    }
                    else
                    {
                        // If the path wasn't globbed or we are recursing then we want to get the
                        // children of the path. If we were globbing and we are not recursing
                        // then we just want to get the item for the resolved paths.

                        foreach (string providerPath in providerPaths)
                        {
                            // Making sure to obey the StopProcessing.
                            if (context.Stopping)
                            {
                                return;
                            }

                            if ((getChildrenBecauseNoGlob || recurse) && IsItemContainer(providerInstance, providerPath, context))
                            {
                                GetChildItems(providerInstance, providerPath, recurse, depth, context);
                            }
                            else
                            {
                                GetItemPrivate(providerInstance, providerPath, context);
                            }
                        }
                    }
                }
                finally
                {
                    if (modifiedInclude)
                    {
                        context.Include.Clear();
                    }
                }
            }
            else
            {
                PSDriveInfo drive = null;

                string originalPath = path;
                path =
                    Globber.GetProviderPath(
                        context.SuppressWildcardExpansion ? path : WildcardPattern.Unescape(path),
                        context,
                        out provider,
                        out drive);

                if (drive != null)
                {
                    context.Drive = drive;
                }

                ContainerCmdletProvider providerInstance = GetContainerProviderInstance(provider);

                if (
                    (context.Include != null && context.Include.Count > 0) ||
                    (context.Exclude != null && context.Exclude.Count > 0))
                {
                    // Do the recursion manually so that we can apply the
                    // include and exclude filters
                    try
                    {
                        // Temporary set literal path as false to apply filter
                        context.SuppressWildcardExpansion = false;
                        ProcessPathItems(providerInstance, path, recurse, depth, context, out _, ProcessMode.Enumerate);
                    }
                    finally
                    {
                        context.SuppressWildcardExpansion = true;
                    }
                }
                else if (path != null && this.ItemExists(providerInstance, path, context))
                {
                    if (IsItemContainer(providerInstance, path, context))
                    {
                        GetChildItems(providerInstance, path, recurse, depth, context);
                    }
                    else
                    {
                        GetItemPrivate(providerInstance, path, context);
                    }
                }
                else
                {
                    ItemNotFoundException pathNotFound =
                        new ItemNotFoundException(
                            path,
                            "PathNotFound",
                            SessionStateStrings.PathNotFound);
                    throw pathNotFound;
                }
            }
        }

        /// <summary>
        /// Gets the child items of the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="recurse">
        /// If true, all the child items in the subtree are returned.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
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
        private void GetChildItems(
            CmdletProvider providerInstance,
            string path,
            bool recurse,
            uint depth,
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            try
            {
                containerCmdletProvider.GetChildItems(path, recurse, depth, context);
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
                    "GetChildrenProviderException",
                    SessionStateStrings.GetChildrenProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Determines if the item at the specified path is a container.
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
        private bool IsPathContainer(
            CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context)
        {
            bool itemContainer = false;
            try
            {
                itemContainer = IsItemContainer(providerInstance, path, context);
            }
            catch (UnauthorizedAccessException accessException)
            {
                context.WriteError(new ErrorRecord(accessException, "GetItemUnauthorizedAccessError", ErrorCategory.PermissionDenied, path));
            }
            catch (ProviderInvocationException accessException)
            {
                // if providerinvocationexception is wrapping access denied error, it is ok to not terminate the pipeline
                if (accessException.InnerException != null &&
                    accessException.InnerException.GetType().Equals(typeof(System.UnauthorizedAccessException)))
                {
                    context.WriteError(new ErrorRecord(accessException, "GetItemUnauthorizedAccessError", ErrorCategory.PermissionDenied, path));
                }
                else
                {
                    throw;
                }
            }

            return itemContainer;
        }

        /// <summary>
        /// Since we can't do include and exclude filtering on items we have to
        /// do the recursion ourselves. We get each child name and see if it matches
        /// the include and exclude filters. If the child is a container we recurse
        /// into that container.
        /// </summary>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="path">
        /// The path to the item to get the children from.
        /// </param>
        /// <param name="recurse">
        /// Recurse into sub-containers when getting children.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <param name="childrenNotMatchingFilterCriteria">
        /// The count of items that do not match any include/exclude criteria.
        /// </param>
        /// <param name="processMode">Indicates if this is a Enumerate/Remove operation.</param>
        /// <param name="skipIsItemContainerCheck">A hint used to skip IsItemContainer checks.</param>
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
        private void ProcessPathItems(
            CmdletProvider providerInstance,
            string path,
            bool recurse,
            CmdletProviderContext context,
            out int childrenNotMatchingFilterCriteria,
            ProcessMode processMode = ProcessMode.Enumerate,
            bool skipIsItemContainerCheck = false)
        {
            // Call ProcessPathItems with 'depth' set to maximum value for infinite recursion when needed.
            ProcessPathItems(providerInstance, path, recurse, uint.MaxValue, context, out childrenNotMatchingFilterCriteria, processMode, skipIsItemContainerCheck);
        }

        /// <summary>
        /// Since we can't do include and exclude filtering on items we have to
        /// do the recursion ourselves. We get each child name and see if it matches
        /// the include and exclude filters. If the child is a container we recurse
        /// into that container.
        /// </summary>
        /// <param name="providerInstance">
        /// The instance of the provider to use.
        /// </param>
        /// <param name="path">
        /// The path to the item to get the children from.
        /// </param>
        /// <param name="recurse">
        /// Recurse into sub-containers when getting children.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
        /// </param>
        /// <param name="childrenNotMatchingFilterCriteria">
        /// The count of items that do not match any include/exclude criteria.
        /// </param>
        /// <param name="processMode">Indicates if this is a Enumerate/Remove operation.</param>
        /// <param name="skipIsItemContainerCheck">A hint used to skip IsItemContainer checks.</param>
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
        private void ProcessPathItems(
            CmdletProvider providerInstance,
            string path,
            bool recurse,
            uint depth,
            CmdletProviderContext context,
            out int childrenNotMatchingFilterCriteria,
            ProcessMode processMode = ProcessMode.Enumerate,
            bool skipIsItemContainerCheck = false)
        {
            ContainerCmdletProvider containerCmdletProvider = GetContainerProviderInstance(providerInstance);
            childrenNotMatchingFilterCriteria = 0;

            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "The caller should have verified the providerInstance");

            Dbg.Diagnostics.Assert(
                path != null,
                "The caller should have verified the path");

            Dbg.Diagnostics.Assert(
                context != null,
                "The caller should have verified the context");

            // Construct the include filter

            Collection<WildcardPattern> includeMatcher =
                SessionStateUtilities.CreateWildcardsFromStrings(
                    context.Include,
                    WildcardOptions.IgnoreCase);

            // Construct the exclude filter

            Collection<WildcardPattern> excludeMatcher =
                SessionStateUtilities.CreateWildcardsFromStrings(
                    context.Exclude,
                    WildcardOptions.IgnoreCase);

            // If the item is a container we have to filter its children
            // Use a hint + lazy evaluation to skip a container check
            if (skipIsItemContainerCheck || IsPathContainer(providerInstance, path, context))
            {
                CmdletProviderContext newContext =
                    new CmdletProviderContext(context);

                Collection<PSObject> childNameObjects = null;
                System.Collections.Generic.Dictionary<string, bool> filteredChildNameDictionary = null;

                try
                {
                    // Get all the child names
                    GetChildNames(
                        providerInstance,
                        path,
                        (recurse) ? ReturnContainers.ReturnAllContainers : ReturnContainers.ReturnMatchingContainers,
                        newContext);
                    newContext.WriteErrorsToContext(context);
                    childNameObjects = newContext.GetAccumulatedObjects();

                    // The code above initially retrieves all of the containers so that it doesn't limit the recursion,
                    // but then emits the non-matching container further down. The public API doesn't support a way to
                    // differentiate the two, so we need to do a diff.
                    // So if there was a filter, do it again to get the fully filtered items.
                    if (recurse && (providerInstance.IsFilterSet()))
                    {
                        newContext.RemoveStopReferral();
                        newContext = new CmdletProviderContext(context);
                        filteredChildNameDictionary = new System.Collections.Generic.Dictionary<string, bool>();

                        GetChildNames(
                            providerInstance,
                            path,
                            ReturnContainers.ReturnMatchingContainers,
                            newContext);
                        var filteredChildNameObjects = newContext.GetAccumulatedObjects();

                        foreach (PSObject filteredChildName in filteredChildNameObjects)
                        {
                            string filteredName = filteredChildName.BaseObject as string;
                            if (filteredName != null)
                            {
                                filteredChildNameDictionary[filteredName] = true;
                            }
                        }
                    }
                }
                finally
                {
                    newContext.RemoveStopReferral();
                }

                // Now loop through all the child objects matching the filters and recursing
                // into containers
                for (int index = 0; index < childNameObjects.Count; ++index)
                {
                    // Making sure to obey the StopProcessing.
                    if (context.Stopping)
                    {
                        return;
                    }

                    if (childNameObjects[index].BaseObject is not string childName)
                    {
                        continue;
                    }

                    // Generate the provider path for the child

                    string qualifiedPath = MakePath(providerInstance, path, childName, context);

                    if (qualifiedPath == null)
                    {
                        continue;
                    }

                    bool isIncludeMatch = !context.SuppressWildcardExpansion &&
                        SessionStateUtilities.MatchesAnyWildcardPattern(
                            childName,
                            includeMatcher,
                            true);

                    if (isIncludeMatch)
                    {
                        if (!SessionStateUtilities.MatchesAnyWildcardPattern(
                            childName,
                            excludeMatcher,
                            false))
                        {
                            bool emitItem = true;
                            if (filteredChildNameDictionary != null)
                            {
                                bool isChildNameInDictionary = false;
                                emitItem = filteredChildNameDictionary.TryGetValue(childName, out isChildNameInDictionary);
                            }

                            if (emitItem)
                            {
                                if (processMode == ProcessMode.Delete)
                                {
                                    containerCmdletProvider.RemoveItem(qualifiedPath, false, context);
                                }
                                else if (processMode != ProcessMode.Delete)
                                {
                                    // The object is a match so get it and write it out.
                                    GetItemPrivate(providerInstance, qualifiedPath, context);
                                }
                            }
                        }
                        else
                        {
                            childrenNotMatchingFilterCriteria++;
                        }
                    }
                    else
                    {
                        childrenNotMatchingFilterCriteria++;
                    }

                    // Now recurse if it is a container
                    if (recurse && IsPathContainer(providerInstance, qualifiedPath, context) && depth > 0)
                    {
                        // Making sure to obey the StopProcessing.
                        if (context.Stopping)
                        {
                            return;
                        }
                        // The item is a container so recurse into it.
                        ProcessPathItems(providerInstance, qualifiedPath, recurse, depth - 1, context, out childrenNotMatchingFilterCriteria, processMode, skipIsItemContainerCheck: true);
                    }
                }
            }
            else
            {
                // The path is not a container so write it out if its name
                // matches the filter

                string childName = path;
                childName = GetChildName(providerInstance, path, context, true);

                // Write out the object if it is a match

                bool isIncludeMatch =
                    SessionStateUtilities.MatchesAnyWildcardPattern(
                        childName,
                        includeMatcher,
                        true);

                if (isIncludeMatch)
                {
                    if (!SessionStateUtilities.MatchesAnyWildcardPattern(
                            childName,
                            excludeMatcher,
                            false))
                    {
                        if (processMode != ProcessMode.Delete)
                        {
                            // The object is a match so get it and write it out.
                            GetItemPrivate(providerInstance, path, context);
                        }
                        else
                        {
                            // The object is a match so, remove it.
                            containerCmdletProvider.RemoveItem(path, recurse, context);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-childitem cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="recurse">
        /// The delete should occur in all sub-containers of the specified path.
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
        internal object GetChildItemsDynamicParameters(
            string path,
            bool recurse,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                return null;
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            // Get the provider that will handle this path
            Globber.GetProviderPath(path, out provider);

            // See if it supports dynamic parameters. If not, we don't need to
            // glob the path.
            if (!HasGetChildItemDynamicParameters(provider))
            {
                return null;
            }

            CmdletProviderContext newContext =
                new CmdletProviderContext(context);
            newContext.SetFilters(
                new Collection<string>(),
                new Collection<string>(),
                null);

            Collection<string> providerPaths = null;

            try
            {
                providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        true,
                        newContext,
                        out provider,
                        out providerInstance);
            }
            catch (ItemNotFoundException)
            {
                // If the provided path is like c:\fakepath\aa*, since we cannot resolve c:\fakepath, an
                // ItemNotFoundException will be thrown out. In this case, we catch the exception
                // and check if the "providerInstance" is identified. If providerInstance is not null,
                // we can carry on with the get-dynamic-parameters method.
                if (providerInstance == null)
                {
                    throw;
                }
            }

            if (providerPaths != null && providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path
                return GetChildItemsDynamicParameters(providerInstance, providerPaths[0], recurse, newContext);
            }
            else
            {
                if (providerInstance != null)
                {
                    PSDriveInfo drive = null;
                    // If we get here, the GetProviderPath should always succeed. This method was already invoked
                    // in the call to GetGlobbedProviderPathsFromMonadPath, and since "providerInstance" is not null,
                    // the invocation in method GetGlobbedProviderPathsFromMonadPath should succeed.
                    string providerPath = Globber.GetProviderPath(path, context, out provider, out drive);
                    if (providerPath != null)
                    {
                        return GetChildItemsDynamicParameters(providerInstance, providerPath, recurse, newContext);
                    }
                }
            }

            return null;
        }

        // Detect if the GetChildItemDynamicParameters has been overridden.
        private static bool HasGetChildItemDynamicParameters(ProviderInfo providerInfo)
        {
            Type providerType = providerInfo.ImplementingType;

            MethodInfo mi = null;

            do
            {
                mi = providerType.GetMethod("GetChildItemsDynamicParameters",
                 BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                providerType = providerType.BaseType;
            } while (
                (mi == null) &&
                (providerType != null) &&
                (providerType != typeof(ContainerCmdletProvider))
            );

            return (mi != null);
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-childitem cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="recurse">
        /// If true, all child items in the subtree should be returned.
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
        private object GetChildItemsDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            bool recurse,
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            object result = null;

            try
            {
                result = containerCmdletProvider.GetChildItemsDynamicParameters(path, recurse, context);
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
                    "GetChildrenDynamicParametersProviderException",
                    SessionStateStrings.GetChildrenDynamicParametersProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetChildItems

        #region GetChildNames

        /// <summary>
        /// Gets names of the children of the specified path.
        /// </summary>
        /// <param name="paths">
        /// The paths to the items from which to retrieve the child names.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the relative paths of all the children
        /// in all the sub-containers of the specified
        /// container. If false, only gets the immediate child names of the specified
        /// container.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// An array of strings that contains the names of the children of the specified
        /// container.
        /// </returns>
        /// <remarks>
        /// The child names are the leaf portion of the path. Example, for the file system
        /// the name for the path c:\windows\system32\foo.dll would be foo.dll or for
        /// the directory c:\windows\system32 would be system32. For Active Directory the
        /// child names would be RDN values of the child objects of the container.
        /// </remarks>
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
        internal Collection<string> GetChildNames(
            string[] paths,
            ReturnContainers returnContainers,
            bool recurse,
            uint depth,
            bool force,
            bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                GetChildNames(path, returnContainers, recurse, depth, context);
            }

            context.ThrowFirstErrorOrDoNothing();

            Collection<PSObject> objectResults = context.GetAccumulatedObjects();

            Collection<string> results = new Collection<string>();

            foreach (PSObject resultObject in objectResults)
            {
                results.Add(resultObject.BaseObject as string);
            }

            return results;
        }

        /// <summary>
        /// Gets names of the children of the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to the item from which to retrieve the child names.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="recurse">
        /// If true, gets all the relative paths of all the children
        /// in all the sub-containers of the specified
        /// container. If false, only gets the immediate child names of the specified
        /// container.
        /// </param>
        /// <param name="depth">
        /// Limits the depth of recursion; uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all names should be written to the context object.
        /// </returns>
        /// <remarks>
        /// The child names are the leaf portion of the path. Example, for the file system
        /// the name for the path c:\windows\system32\foo.dll would be foo.dll or for
        /// the directory c:\windows\system32 would be system32. For Active Directory the
        /// child names would be RDN values of the child objects of the container.
        /// </remarks>
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
        internal void GetChildNames(
            string path,
            ReturnContainers returnContainers,
            bool recurse,
            uint depth,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            // Construct the include filter

            Collection<WildcardPattern> includeMatcher =
                SessionStateUtilities.CreateWildcardsFromStrings(
                    context.Include,
                    WildcardOptions.IgnoreCase);

            // Construct the exclude filter

            Collection<WildcardPattern> excludeMatcher =
                SessionStateUtilities.CreateWildcardsFromStrings(
                    context.Exclude,
                    WildcardOptions.IgnoreCase);

            if (LocationGlobber.ShouldPerformGlobbing(path, context))
            {
                ProviderInfo provider = null;
                CmdletProvider providerInstance = null;

                // We don't want to process include/exclude filters
                // when globbing the targets of the operation, so
                // copy the context without the filters.

                CmdletProviderContext resolvePathContext =
                    new CmdletProviderContext(context);
                resolvePathContext.SetFilters(
                    new Collection<string>(),
                    new Collection<string>(),
                    null);

                // Resolve the path

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        false,
                        resolvePathContext,
                        out provider,
                        out providerInstance);

                if (resolvePathContext.Drive != null)
                {
                    context.Drive = resolvePathContext.Drive;
                }

                bool pathContainsGlobCharacters = LocationGlobber.StringContainsGlobCharacters(path);
                foreach (string providerPath in providerPaths)
                {
                    // Making sure to obey the StopProcessing.
                    if (context.Stopping)
                    {
                        return;
                    }

                    if ((!pathContainsGlobCharacters || recurse) && IsItemContainer(providerInstance, providerPath, context))
                    {
                        // Since the path contained glob characters or we are recursing and the
                        // path is a container, do the name enumeration manually

                        DoGetChildNamesManually(
                            providerInstance,
                            providerPath,
                            string.Empty,
                            returnContainers,
                            includeMatcher,
                            excludeMatcher,
                            context,
                            recurse,
                            depth);
                    }
                    else
                    {
                        // Since the original path did not contain glob characters,
                        // if the provider is a NavigationCmdletProvider, write
                        // out the child name, else write out the name as it
                        // was resolved.

                        if (providerInstance is NavigationCmdletProvider)
                        {
                            string childName =
                                GetChildName(
                                    providerInstance,
                                    providerPath,
                                    context, false);

                            bool isIncludeMatch =
                                SessionStateUtilities.MatchesAnyWildcardPattern(
                                    childName,
                                    includeMatcher,
                                    true);

                            bool isExcludeMatch =
                                SessionStateUtilities.MatchesAnyWildcardPattern(
                                    childName,
                                    excludeMatcher,
                                    false);

                            if (isIncludeMatch && !isExcludeMatch)
                            {
                                context.WriteObject(childName);
                            }
                        }
                        else
                        {
                            context.WriteObject(providerPath);
                        }
                    }
                }
            }
            else
            {
                // Figure out which provider to use

                ProviderInfo provider = null;
                PSDriveInfo drive = null;

                string providerPath =
                    Globber.GetProviderPath(
                        context.SuppressWildcardExpansion ? path : WildcardPattern.Unescape(path),
                        context,
                        out provider,
                        out drive);

                ContainerCmdletProvider providerInstance = GetContainerProviderInstance(provider);

                if (drive != null)
                {
                    context.Drive = drive;
                }

                if (!providerInstance.ItemExists(providerPath, context))
                {
                    ItemNotFoundException pathNotFound =
                        new ItemNotFoundException(
                            providerPath,
                            "PathNotFound",
                            SessionStateStrings.PathNotFound);
                    throw pathNotFound;
                }

                if (recurse)
                {
                    // The path did not contain glob characters but recurse was specified
                    // so do the enumeration manually

                    DoGetChildNamesManually(
                        providerInstance,
                        providerPath,
                        string.Empty,
                        returnContainers,
                        includeMatcher,
                        excludeMatcher,
                        context,
                        recurse,
                        depth);
                }
                else
                {
                    // Since the path did not contain glob characters and recurse wasn't
                    // specified, we can have the provider write out the child names directly

                    GetChildNames(
                        providerInstance,
                        providerPath,
                        returnContainers,
                        context);
                }
            }
        }

        /// <summary>
        /// Gets the child names of the item at the specified path by
        /// manually recursing through all the containers instead of
        /// allowing the provider to do the recursion.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="providerPath">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="relativePath">
        /// The path the name is relative to.
        /// </param>
        /// <param name="recurse">
        /// If true all names in the subtree should be returned.
        /// </param>
        /// <param name="depth">
        /// Current depth of recursion; special case uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="includeMatcher">
        /// A set of filters that the names must match to be returned.
        /// </param>
        /// <param name="excludeMatcher">
        /// A set of filters that the names cannot match to be returned.
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
        private void DoGetChildNamesManually(
            CmdletProvider providerInstance,
            string providerPath,
            string relativePath,
            ReturnContainers returnContainers,
            Collection<WildcardPattern> includeMatcher,
            Collection<WildcardPattern> excludeMatcher,
            CmdletProviderContext context,
            bool recurse,
            uint depth)
        {
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "The providerInstance should have been verified by the caller");

            Dbg.Diagnostics.Assert(
                providerPath != null,
                "The paths should have been verified by the caller");

            Dbg.Diagnostics.Assert(
                context != null,
                "The context should have been verified by the caller");

            string newProviderPath =
                MakePath(
                    providerInstance,
                    providerPath,
                    relativePath,
                    context);

            CmdletProviderContext childNamesContext =
                new CmdletProviderContext(context);

            try
            {
                // First get all children that match the filters and write them out

                GetChildNames(
                    providerInstance,
                    newProviderPath,
                    ReturnContainers.ReturnMatchingContainers,
                    childNamesContext);

                Collection<PSObject> results = childNamesContext.GetAccumulatedObjects();

                foreach (PSObject result in results)
                {
                    // Making sure to obey the StopProcessing.
                    if (context.Stopping)
                    {
                        return;
                    }

                    if (result.BaseObject is not string name)
                    {
                        continue;
                    }

                    bool isIncludeMatch =
                        SessionStateUtilities.MatchesAnyWildcardPattern(
                            name,
                            includeMatcher,
                            true);

                    if (isIncludeMatch)
                    {
                        if (!SessionStateUtilities.MatchesAnyWildcardPattern(
                                name,
                                excludeMatcher,
                                false))
                        {
                            string resultPath = MakePath(providerInstance, relativePath, name, context);

                            context.WriteObject(resultPath);
                        }
                    }
                }

                if (recurse)
                {
                    // Now get all the children that are containers and recurse into them

                    // Limiter for recursion
                    if (depth > 0) // this includes special case 'depth == uint.MaxValue' for unlimited recursion
                    {
                        GetChildNames(
                            providerInstance,
                            newProviderPath,
                            ReturnContainers.ReturnAllContainers,
                            childNamesContext);

                        results = childNamesContext.GetAccumulatedObjects();

                        foreach (PSObject result in results)
                        {
                            // Making sure to obey the StopProcessing.
                            if (context.Stopping)
                            {
                                return;
                            }

                            if (result.BaseObject is not string name)
                            {
                                continue;
                            }

                            // Generate the relative path from the provider path

                            string resultRelativePath =
                                MakePath(
                                    providerInstance,
                                    relativePath,
                                    name,
                                    context);

                            // Generate the provider path for the child item to see
                            // if it is a container

                            string resultProviderPath =
                                    MakePath(
                                        providerInstance,
                                        providerPath,
                                        resultRelativePath,
                                        context);

                            // If the item is a container recurse into it and output its
                            // child names

                            if (IsItemContainer(providerInstance, resultProviderPath, context))
                            {
                                DoGetChildNamesManually(
                                    providerInstance,
                                    providerPath,
                                    resultRelativePath,
                                    returnContainers,
                                    includeMatcher,
                                    excludeMatcher,
                                    context,
                                    true,
                                    depth - 1);
                            }
                        }
                    }
                }
            }
            finally
            {
                childNamesContext.RemoveStopReferral();
            }
        }

        /// <summary>
        /// Gets the names of the children of the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
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
        private void GetChildNames(
            CmdletProvider providerInstance,
            string path,
            ReturnContainers returnContainers,
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            try
            {
                containerCmdletProvider.GetChildNames(path, returnContainers, context);
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
                    "GetChildNamesProviderException",
                    SessionStateStrings.GetChildNamesProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-childitem -name cmdlet.
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
        internal object GetChildNamesDynamicParameters(
            string path,
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

            Collection<string> providerPaths = null;

            try
            {
                providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        true,
                        newContext,
                        out provider,
                        out providerInstance);
            }
            catch (ItemNotFoundException)
            {
                // If the provided path is like c:\fakepath\aa*, since we cannot resolve c:\fakepath, an
                // ItemNotFoundException will be thrown out. In this case, we catch the exception
                // and check if the "providerInstance" is identified. If providerInstance is not null,
                // we can carry on with the get-dynamic-parameters method.
                if (providerInstance == null)
                {
                    throw;
                }
            }

            object result = null;
            if (providerPaths != null && providerPaths.Count > 0)
            {
                // Get the dynamic parameters for the first resolved path
                result = GetChildNamesDynamicParameters(providerInstance, providerPaths[0], newContext);
            }
            else
            {
                if (providerInstance != null)
                {
                    PSDriveInfo drive = null;
                    // If we get here, the GetProviderPath should always succeed. This method was already invoked
                    // in the call to GetGlobbedProviderPathsFromMonadPath, and since "providerInstance" is not null,
                    // the invocation in method GetGlobbedProviderPathsFromMonadPath should succeed.
                    string providerPath = Globber.GetProviderPath(path, context, out provider, out drive);
                    if (providerPath != null)
                    {
                        result = GetChildNamesDynamicParameters(providerInstance, providerPath, newContext);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-childitem -names cmdlet.
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
        private object GetChildNamesDynamicParameters(
             CmdletProvider providerInstance,
            string path,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerId before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            object result = null;

            try
            {
                result = containerCmdletProvider.GetChildNamesDynamicParameters(path, context);
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
                    "GetChildNamesDynamicParametersProviderException",
                    SessionStateStrings.GetChildNamesDynamicParametersProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetChildNames

        #region RenameItem

        /// <summary>
        /// Renames the item at the specified path to the new name provided.
        /// </summary>
        /// <param name="path">
        /// The path to the item to rename.
        /// </param>
        /// <param name="newName">
        /// The name to which the item should be renamed. This name should always be
        /// relative to the parent container.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <returns>
        /// The item that was renamed at the specified path.
        /// </returns>
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
        internal Collection<PSObject> RenameItem(string path, string newName, bool force)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;

            RenameItem(path, newName, context);

            context.ThrowFirstErrorOrDoNothing();

            // Since there was no errors return the accumulated objects

            return context.GetAccumulatedObjects();
        }

        /// <summary>
        /// Renames the item at the specified path to the new name provided.
        /// </summary>
        /// <param name="path">
        /// The path to the item to rename.
        /// </param>
        /// <param name="newName">
        /// The name to which the item should be renamed. This name should always be
        /// relative to the parent container.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// Nothing. All items that are renamed are written into the context object.
        /// </returns>
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
        internal void RenameItem(
            string path,
            string newName,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
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

            // Can only rename one item at a time, so if we glob more than
            // one item write out an error.

            if (providerPaths.Count == 1)
            {
                RenameItem(providerInstance, providerPaths[0], newName, context);
            }
            else
            {
                ArgumentException argException =
                    PSTraceSource.NewArgumentException(
                        nameof(path),
                        SessionStateStrings.RenameMultipleItemError);

                context.WriteError(
                    new ErrorRecord(
                        argException,
                        "RenameMultipleItemError",
                        ErrorCategory.InvalidArgument,
                        providerPaths));
            }
        }

        /// <summary>
        /// Renames the item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="newName">
        /// The new name of the item.
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
        private void RenameItem(
            CmdletProvider providerInstance,
            string path,
            string newName,
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            try
            {
                containerCmdletProvider.RenameItem(path, newName, context);
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
                    "RenameItemProviderException",
                    SessionStateStrings.RenameItemProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the rename-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="newName">
        /// The name to which the item should be renamed. This name should always be
        /// relative to the parent container.
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
        internal object RenameItemDynamicParameters(
            string path,
            string newName,
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

                return RenameItemDynamicParameters(providerInstance, providerPaths[0], newName, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the rename-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="newName">
        /// The new name of the item.
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
        private object RenameItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string newName,
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            object result = null;

            try
            {
                result = containerCmdletProvider.RenameItemDynamicParameters(path, newName, context);
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
                    "RenameItemDynamicParametersProviderException",
                    SessionStateStrings.RenameItemDynamicParametersProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion RenameItem

        #region NewItem

        /// <summary>
        /// Creates a new item at the specified path.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the container(s) to create the item in.
        /// </param>
        /// <param name="name">
        /// The name of the item to create.
        /// </param>
        /// <param name="type">
        /// The provider specific type of the object to be created.
        /// </param>
        /// <param name="content">
        /// The content of the new item to create.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <returns>
        /// The item(s) that was created.
        /// </returns>
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
        internal Collection<PSObject> NewItem(string[] paths, string name, string type, object content, bool force)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;

            NewItem(paths, name, type, content, context);

            context.ThrowFirstErrorOrDoNothing();
            // Since there was no errors return the accumulated objects

            return context.GetAccumulatedObjects();
        }

        /// <summary>
        /// Creates a new item at the specified path.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to create.
        /// </param>
        /// <param name="name">
        /// The name of the item to create.
        /// </param>
        /// <param name="type">
        /// The provider specific type of the item to be created.
        /// </param>
        /// <param name="content">
        /// The content to create the new item with.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// Nothing. The item created is written to the context object.
        /// </returns>
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
        internal void NewItem(
            string[] paths,
            string name,
            string type,
            object content,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            foreach (string path in paths)
            {
                string resolvePath = null;
                if (path == null)
                {
                    PSTraceSource.NewArgumentNullException(nameof(paths));
                }
                else if (path.EndsWith((":" + Path.DirectorySeparatorChar), StringComparison.Ordinal) ||
                         path.EndsWith((":" + Path.AltDirectorySeparatorChar), StringComparison.Ordinal))
                {
                    // path is Windows root
                    resolvePath = path;
                }
                else
                {
                    // To be compatible with Linux OS. Which will be either '/' or '\' depends on the OS type.
                    char[] charsToTrim = { ' ', Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar };
                    resolvePath = path.TrimEnd(charsToTrim);
                }

                ProviderInfo provider = null;
                PSDriveInfo driveInfo;
                CmdletProvider providerInstance = null;

                Collection<string> providerPaths = new Collection<string>();

                // Only glob the path if the name is specified

                if (string.IsNullOrEmpty(name))
                {
                    string providerPath =
                        Globber.GetProviderPath(resolvePath, context, out provider, out driveInfo);

                    providerInstance = GetProviderInstance(provider);
                    providerPaths.Add(providerPath);
                }
                else
                {
                    providerPaths =
                        Globber.GetGlobbedProviderPathsFromMonadPath(
                                resolvePath,
                                true,
                                context,
                                out provider,
                                out providerInstance);
                }

                foreach (string providerPath in providerPaths)
                {
                    // Compose the globbed container and the name together to get a path
                    // to pass on to the provider.

                    string composedPath = providerPath;
                    if (!string.IsNullOrEmpty(name))
                    {
                        composedPath = MakePath(providerInstance, providerPath, name, context);
                    }

                    // Don't support 'New-Item -Type Directory' on the Function provider
                    // if the runspace has ever been in constrained language mode, as the mkdir
                    // function can be abused
                    if (context.ExecutionContext.HasRunspaceEverUsedConstrainedLanguageMode &&
                        (providerInstance is Microsoft.PowerShell.Commands.FunctionProvider) &&
                        (string.Equals(type, "Directory", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw
                            PSTraceSource.NewNotSupportedException(SessionStateStrings.DriveCmdletProvider_NotSupported);
                    }

                    bool isSymbolicJunctionOrHardLink = false;
                    // Symbolic link targets are allowed to not exist on both Windows and Linux
                    bool allowNonexistingPath = false;

                    if (type != null)
                    {
                        WildcardPattern typeEvaluator = WildcardPattern.Get(type + "*", WildcardOptions.IgnoreCase | WildcardOptions.Compiled);

                        if (typeEvaluator.IsMatch("symboliclink") || typeEvaluator.IsMatch("junction") || typeEvaluator.IsMatch("hardlink"))
                        {
                            isSymbolicJunctionOrHardLink = true;
                            allowNonexistingPath = typeEvaluator.IsMatch("symboliclink");
                        }
                    }

                    if (isSymbolicJunctionOrHardLink)
                    {
                        string targetPath;

                        if (content is null || string.IsNullOrEmpty(targetPath = content.ToString()))
                        {
                            throw PSTraceSource.NewArgumentNullException(nameof(content), SessionStateStrings.NewLinkTargetNotSpecified, path);
                        }

                        content = targetPath;
                    }

                    NewItemPrivate(providerInstance, composedPath, type, content, context);
                }
            }
        }

        /// <summary>
        /// Creates a new item at the specified path.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="type">
        /// The type of the item to create.
        /// </param>
        /// <param name="content">
        /// The content of the item to create.
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
        private void NewItemPrivate(
            CmdletProvider providerInstance,
            string path,
            string type,
            object content,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                path != null,
                "Caller should validate path before calling this method");

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            try
            {
                containerCmdletProvider.NewItem(path, type, content, context);
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
                    "NewItemProviderException",
                    SessionStateStrings.NewItemProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the new-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="type">
        /// The provider specific type of the item to be created.
        /// </param>
        /// <param name="newItemValue">
        /// The content to create the new item with.
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
        internal object NewItemDynamicParameters(
            string path,
            string type,
            object newItemValue,
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

                return NewItemDynamicParameters(providerInstance, providerPaths[0], type, newItemValue, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the new-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="type">
        /// The type of the new item.
        /// </param>
        /// <param name="newItemValue">
        /// The value of the new item
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
        private object NewItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string type,
            object newItemValue,
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = containerCmdletProvider.NewItemDynamicParameters(path, type, newItemValue, context);
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
                    "NewItemDynamicParametersProviderException",
                    SessionStateStrings.NewItemDynamicParametersProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion NewItem

        #region HasChildItems

        /// <summary>
        /// Determines if the item at the specified path has children.
        /// </summary>
        /// <param name="path">
        /// The path to the item to see if it has children.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// True if the item has children, false otherwise.
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
        internal bool HasChildItems(string path, bool force, bool literalPath)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            bool result = HasChildItems(path, context);

            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        /// <summary>
        /// Determines if the item at the specified path has children.
        /// </summary>
        /// <param name="path">
        /// The path to the item to see if it has children.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// True if the item has children, false otherwise.
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
        internal bool HasChildItems(
             string path,
            CmdletProviderContext context)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
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

            bool result = false;
            foreach (string providerPath in providerPaths)
            {
                result = HasChildItems(providerInstance, providerPath, context);
                if (result)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if the item at the specified path has children.
        /// </summary>
        /// <param name="providerId">
        /// The provider to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
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
        internal bool HasChildItems(
            string providerId,
            string path)
        {
            bool result = false;

            if (string.IsNullOrEmpty(providerId))
            {
                throw PSTraceSource.NewArgumentException(nameof(providerId));
            }

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);

            result = HasChildItems(providerId, path, context);
            context.ThrowFirstErrorOrDoNothing();

            return result;
        }

        /// <summary>
        /// Determines if the item at the specified path has children.
        /// </summary>
        /// <param name="providerId">
        /// The provider to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="context">
        /// The context under which the command is running.
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
        internal bool HasChildItems(
            string providerId,
            string path,
            CmdletProviderContext context)
        {
            ContainerCmdletProvider providerInstance = GetContainerProviderInstance(providerId);

            return HasChildItems(providerInstance, path, context);
        }

        /// <summary>
        /// Determines if the item at the specified path has children.
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
        private bool HasChildItems(
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            bool result = false;

            try
            {
                result = containerCmdletProvider.HasChildItems(path, context);
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
                    "HasChildItemsProviderException",
                    SessionStateStrings.HasChildItemsProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion HasChildItems

        #region CopyItem

        /// <summary>
        /// Copies an item at the specified path to an item at the <paramref name="copyPath"/>.
        /// </summary>
        /// <param name="paths">
        /// The path(s) of the item(s) to copy.
        /// </param>
        /// <param name="copyPath">
        /// The path of the item to copy to.
        /// </param>
        /// <param name="recurse">
        /// Tells the provider to recurse sub-containers when copying.
        /// </param>
        /// <param name="copyContainers">
        /// Determines how the source container is used in the copy operation.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The objects that were copied.
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
        internal Collection<PSObject> CopyItem(string[] paths,
                                               string copyPath,
                                               bool recurse,
                                               CopyContainers copyContainers,
                                               bool force,
                                               bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            copyPath ??= string.Empty;

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            CopyItem(paths, copyPath, recurse, copyContainers, context);

            context.ThrowFirstErrorOrDoNothing();

            return context.GetAccumulatedObjects();
        }

        /// <summary>
        /// Copies an item at the specified path to an item at the <paramref name="copyPath"/>.
        /// </summary>
        /// <param name="paths">
        /// The path(s) of the item(s) to copy.
        /// </param>
        /// <param name="copyPath">
        /// The path of the item to copy to.
        /// </param>
        /// <param name="recurse">
        /// Tells the provider to recurse sub-containers when copying.
        /// </param>
        /// <param name="copyContainers">
        /// Determines how the source container is used in the copy operation.
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
        internal void CopyItem(
            string[] paths,
            string copyPath,
            bool recurse,
            CopyContainers copyContainers,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            copyPath ??= string.Empty;

            // Get the provider specific path for the destination

            ProviderInfo destinationProvider = null;
            Microsoft.PowerShell.Commands.CopyItemDynamicParameters dynamicParams = context.DynamicParameters as Microsoft.PowerShell.Commands.CopyItemDynamicParameters;
            bool destinationIsRemote = false;
            bool sourceIsRemote = false;
            string providerDestinationPath;
            Runspaces.PSSession session = null;

            if (dynamicParams != null)
            {
                if (dynamicParams.FromSession != null)
                {
                    sourceIsRemote = true;
                    session = dynamicParams.FromSession;
                }

                if (dynamicParams.ToSession != null)
                {
                    destinationIsRemote = true;
                    session = dynamicParams.ToSession;
                }
            }

            if (sourceIsRemote && destinationIsRemote)
            {
                context.WriteError(new ErrorRecord(
                           new ArgumentException(
                               string.Format(System.Globalization.CultureInfo.InvariantCulture, SessionStateStrings.CopyItemFromSessionToSession, "FromSession", "ToSession")),
                               "InvalidInput",
                                ErrorCategory.InvalidArgument,
                                dynamicParams));
                return;
            }

            // Validate that the session is available and the language mode is not set to ConstrainedLanguage or NoLanguage.
            PSLanguageMode? remoteLanguageMode = null;
            if (sourceIsRemote || destinationIsRemote)
            {
                if (!isValidSession(session, context, out remoteLanguageMode))
                {
                    return;
                }
            }

            if (!destinationIsRemote)
            {
                providerDestinationPath =
                   Globber.GetProviderPath(
                       copyPath,
                       context,
                       out destinationProvider,
                       out _);
            }
            else
            {
                // Validate remote destination path
                providerDestinationPath = copyPath;
                if (string.IsNullOrEmpty(providerDestinationPath))
                {
                    context.WriteError(new ErrorRecord(
                                            new ArgumentNullException(
                                                    string.Format(
                                                    System.Globalization.CultureInfo.InvariantCulture,
                                                    SessionStateStrings.CopyItemRemotelyPathIsNullOrEmpty,
                                                    "Destination")),
                                            "CopyItemRemoteDestinationIsNullOrEmpty",
                                            ErrorCategory.InvalidArgument,
                                            providerDestinationPath));
                    return;
                }

                string root = ValidateRemotePathAndGetRoot(providerDestinationPath, session, context, remoteLanguageMode, false);
                if (root == null)
                {
                    return;
                }
            }

            s_tracer.WriteLine("providerDestinationPath = {0}", providerDestinationPath);

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                Collection<string> providerPaths;

                if (sourceIsRemote)
                {
                    // Get the root for the remote path
                    string root = ValidateRemotePathAndGetRoot(path, session, context, remoteLanguageMode, true);
                    if (root == null)
                    {
                        return;
                    }

                    providerInstance = ExecutionContext.SessionState.Internal.GetProviderInstance("FileSystem");
                    providerPaths = new Collection<string>();
                    providerPaths.Add(path);
                }
                else
                {
                    providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        false,
                        context,
                        out provider,
                        out providerInstance);
                }

                // Ensure that the source and destination are the same provider. Copying between
                // providers is not supported

                if (!sourceIsRemote && !destinationIsRemote && provider != destinationProvider)
                {
                    ArgumentException argException =
                        PSTraceSource.NewArgumentException(
                            "path",
                            SessionStateStrings.CopyItemSourceAndDestinationNotSameProvider);

                    context.WriteError(
                        new ErrorRecord(
                            argException,
                            "CopyItemSourceAndDestinationNotSameProvider",
                            ErrorCategory.InvalidArgument,
                            providerPaths));

                    return;
                }

                bool destinationIsContainer = false;

                if (!destinationIsRemote)
                {
                    destinationIsContainer = IsItemContainer(
                        providerInstance,
                        providerDestinationPath,
                        context);

                    s_tracer.WriteLine("destinationIsContainer = {0}", destinationIsContainer);
                }

                foreach (string providerPath in providerPaths)
                {
                    // Making sure to obey the StopProcessing.
                    if (context.Stopping)
                    {
                        return;
                    }

                    if (sourceIsRemote || destinationIsRemote)
                    {
                        CopyItem(
                            providerInstance,
                            providerPath,
                            providerDestinationPath,
                            recurse,
                            context);
                        continue;
                    }

                    bool sourceIsContainer =
                         IsItemContainer(
                            providerInstance,
                            providerPath,
                            context);

                    s_tracer.WriteLine("sourceIsContainer = {0}", sourceIsContainer);

                    if (sourceIsContainer)
                    {
                        if (destinationIsContainer)
                        {
                            if (!recurse && copyContainers == CopyContainers.CopyChildrenOfTargetContainer)
                            {
                                // Copying a container to a container without the -container or -recurse
                                // switch is an error.

                                Exception e =
                                    PSTraceSource.NewArgumentException(
                                        "path",
                                        SessionStateStrings.CopyContainerToContainerWithoutRecurseOrContainer);

                                context.WriteError(new ErrorRecord(
                                    e,
                                    "CopyContainerToContainerWithoutRecurseOrContainer",
                                    ErrorCategory.InvalidArgument,
                                    providerPath));
                            }
                            else if (recurse && copyContainers == CopyContainers.CopyChildrenOfTargetContainer)
                            {
                                // Copy all the leaf items to a single container

                                CopyRecurseToSingleContainer(
                                    providerInstance,
                                    providerPath,
                                    providerDestinationPath,
                                    context);
                            }
                            else
                            {
                                // Call the provider to do a recurse copy of all the items

                                CopyItem(
                                    providerInstance,
                                    providerPath,
                                    providerDestinationPath,
                                    recurse,
                                    context);
                            }
                        }
                        else
                        {
                            // Since we know the destination isn't a container, check to
                            // see if it exists.

                            if (ItemExists(providerInstance, providerDestinationPath, context))
                            {
                                // Since the item exists and is not a container it must
                                // be a leaf. Copying a container to a leaf is an error

                                Exception e =
                                    PSTraceSource.NewArgumentException(
                                        "path",
                                        SessionStateStrings.CopyContainerItemToLeafError);

                                context.WriteError(new ErrorRecord(
                                    e,
                                    "CopyContainerItemToLeafError",
                                    ErrorCategory.InvalidArgument,
                                    providerPath));
                            }
                            else
                            {
                                // Copy the container to a non-existing path

                                CopyItem(
                                    providerInstance,
                                    providerPath,
                                    providerDestinationPath,
                                    recurse,
                                    context);
                            }
                        }
                    }
                    else
                    {
                        // Copy a leaf to the destination

                        CopyItem(
                            providerInstance,
                            providerPath,
                            providerDestinationPath,
                            recurse,
                            context);
                    }
                }
            }
        }

        /// <summary>
        /// Copies the specified item(s) to the specified destination.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="copyPath">
        /// The path to copy the item(s) to.
        /// </param>
        /// <param name="recurse">
        /// If true all sub-containers and their children should be copied.
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
        private void CopyItem(
            CmdletProvider providerInstance,
            string path,
            string copyPath,
            bool recurse,
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

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            try
            {
                containerCmdletProvider.CopyItem(path, copyPath, recurse, context);
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
                    "CopyItemProviderException",
                    SessionStateStrings.CopyItemProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Recursively copies many items to a single container.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="sourcePath">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="destinationPath">
        /// The path to copy the item(s) to.
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
        private void CopyRecurseToSingleContainer(
            CmdletProvider providerInstance,
            string sourcePath,
            string destinationPath,
            CmdletProviderContext context)
        {
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "The providerInstance should have been verified by the caller");

            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(sourcePath),
                "The sourcePath should have been verified by the caller");

            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(destinationPath),
                "The destinationPath should have been verified by the caller");

            Dbg.Diagnostics.Assert(
                context != null,
                "The context should have been verified by the caller");

            ContainerCmdletProvider containerProviderInstance =
                GetContainerProviderInstance(providerInstance);

            // Use GetChildNames to get the relative paths of all the children
            // to be copied

            Collection<string> children =
                GetChildNames(
                    new string[] { sourcePath },
                    ReturnContainers.ReturnMatchingContainers,
                    true, uint.MaxValue, false, false);

            foreach (string childName in children)
            {
                // Making sure to obey the StopProcessing.
                if (context.Stopping)
                {
                    return;
                }

                // Now convert each relative path into a provider-internal path

                string childPath = MakePath(providerInstance.ProviderInfo, sourcePath, childName, context);

                // And then copy the item to the destination

                CopyItem(containerProviderInstance, childPath, destinationPath, false, context);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the copy-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="destination">
        /// The path of the item to copy to.
        /// </param>
        /// <param name="recurse">
        /// Tells the provider to recurse sub-containers when copying.
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
        internal object CopyItemDynamicParameters(
            string path,
            string destination,
            bool recurse,
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

            string providerPath = null;
            bool pathNotFoundOnClient = false;
            try
            {
                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        true,
                        newContext,
                        out provider,
                        out providerInstance);

                if (providerPaths.Count > 0)
                    providerPath = providerPaths[0];
            }
            catch (DriveNotFoundException)
            {
                // This exception is expected for remote sessions where drives exist in a remote session but not
                // on the client.
                pathNotFoundOnClient = true;
            }
            catch (ItemNotFoundException)
            {
                // This exception is expected for remote sessions where item exist in a remote session but not
                // on the client.
                pathNotFoundOnClient = true;
            }

            if (pathNotFoundOnClient)
            {
                // At this point, we don't know if this is a remote use of copy-item because the FromSession
                // and ToSession dynamic parameters have not been retrieved yet.
                // Ignore these exceptions and use the FileSystem provider as default.  If this is a real drive
                // issue, or if the item does not exist, it will be caught later when the drive or item path is used.
                var fileSystemProviders = Providers["FileSystem"];
                if (fileSystemProviders.Count > 0)
                {
                    providerPath = path;
                    providerInstance = ExecutionContext.EngineSessionState.GetProviderInstance(
                        fileSystemProviders[0]);
                }
            }

            if (providerInstance != null)
            {
                // Get the dynamic parameters for the first resolved path
                return CopyItemDynamicParameters(providerInstance, providerPath, destination, recurse, newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the copy-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="destination">
        /// The path to copy the item to.
        /// </param>
        /// <param name="recurse">
        /// If true, subcontainers and their children should be copied.
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
        private object CopyItemDynamicParameters(
            CmdletProvider providerInstance,
            string path,
            string destination,
            bool recurse,
            CmdletProviderContext context)
        {
            // All parameters should have been validated by caller
            Dbg.Diagnostics.Assert(
                providerInstance != null,
                "Caller should validate providerInstance before calling this method");

            Dbg.Diagnostics.Assert(
                context != null,
                "Caller should validate context before calling this method");

            ContainerCmdletProvider containerCmdletProvider =
                GetContainerProviderInstance(providerInstance);

            object result = null;
            try
            {
                result = containerCmdletProvider.CopyItemDynamicParameters(path, destination, recurse, context);
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
                    "CopyItemDynamicParametersProviderException",
                    SessionStateStrings.CopyItemDynamicParametersProviderException,
                    containerCmdletProvider.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        // This function validates a remote path, and if it exists, it returns the root path.
        //
        private static string ValidateRemotePathAndGetRoot(string path, Runspaces.PSSession session, CmdletProviderContext context, PSLanguageMode? languageMode, bool sourceIsRemote)
        {
            Hashtable op = null;

            using (PowerShell ps = PowerShell.Create())
            {
                ps.Runspace = session.Runspace;

                // Check to see if the remote PSSession is running in constrained or no language mode and if so
                // then also if the path validation function already exists in the session (for the User drive
                // custom endpoint case).  Otherwise error out.
                if (languageMode.HasValue &&
                    (languageMode.Value == PSLanguageMode.ConstrainedLanguage || languageMode.Value == PSLanguageMode.NoLanguage))
                {
                    ps.Runspace = session.Runspace;
                    ps.AddCommand("Get-Command").AddArgument(CopyFileRemoteUtils.PSCopyRemoteUtilsName);
                    var result = ps.Invoke<bool>();

                    if (result.Count == 0)
                    {
                        context.WriteError(new ErrorRecord(
                            new InvalidOperationException(
                                string.Format(
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    SessionStateStrings.CopyItemSessionProperties,
                                    "LanguageMode",
                                    session.Runspace.SessionStateProxy.LanguageMode)
                                ),
                                "SessionIsNotInFullLanguageMode",
                                ErrorCategory.InvalidOperation,
                                session.Availability
                            )
                        );

                        return null;
                    }

                    ps.Commands.Clear();
                    ps.Streams.ClearStreams();
                    ps.AddCommand(CopyFileRemoteUtils.PSCopyRemoteUtilsName);
                }
                else
                {
                    ps.AddScript(CopyFileRemoteUtils.PSValidatePathDefinition);
                }

                ps.AddParameter("pathToValidate", path);

                if (sourceIsRemote)
                {
                    ps.AddParameter(nameof(sourceIsRemote), true);
                }

                op = Microsoft.PowerShell.Commands.SafeInvokeCommand.Invoke(ps, null, context);
            }

            if (op == null)
            {
                context.WriteError(new ErrorRecord(
                                new InvalidOperationException(
                                    string.Format(
                                    System.Globalization.CultureInfo.InvariantCulture, SessionStateStrings.CopyItemValidateRemotePath, path)),
                                    "FailedToValidateRemotePath",
                                    ErrorCategory.InvalidOperation,
                                    path));
                return null;
            }

            // If the remote path is not absolute, display an error to the user.
            if (op["IsAbsolute"] != null)
            {
                bool isAbsolute = (bool)op["IsAbsolute"];
                if (!isAbsolute)
                {
                    context.WriteError(new ErrorRecord(
                                        new ArgumentException(
                                            string.Format(
                                            System.Globalization.CultureInfo.InvariantCulture, SessionStateStrings.CopyItemRemotelyPathIsNotAbsolute, path)),
                                            "RemotePathIsNotAbsolute",
                                            ErrorCategory.InvalidArgument,
                                            path));
                    return null;
                }
            }

            bool pathExist = false;
            string root = null;

            if (op["Exists"] != null)
                pathExist = (bool)op["Exists"];

            if (op["Root"] != null)
                root = (string)op["Root"];

            // Here there are two scenarios:
            // 1) If the source is remote and the path does not exist, error out.
            bool invalidRemoteSource = (sourceIsRemote && (!pathExist));

            // 2) For a remote destination, if the root does not exist, error out.
            bool invalidRemoteDestination = (root == null);

            if (invalidRemoteSource || invalidRemoteDestination)
            {
                context.WriteError(new ErrorRecord(
                                            new ArgumentException(
                                                string.Format(
                                                System.Globalization.CultureInfo.InvariantCulture, SessionStateStrings.PathNotFound, path)),
                                                "RemotePathNotFound",
                                                ErrorCategory.InvalidArgument,
                                                path));
                return null;
            }

            return root;
        }

        private static bool isValidSession(PSSession session, CmdletProviderContext context, out PSLanguageMode? languageMode)
        {
            // session == null is validated by the parameter binding
            if (session.Availability != RunspaceAvailability.Available)
            {
                context.WriteError(new ErrorRecord(
                                    new InvalidOperationException(
                                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                            SessionStateStrings.CopyItemSessionProperties,
                                            "Availability", session.Availability)),
                                            "SessionIsNotAvailable",
                                        ErrorCategory.InvalidOperation,
                                        session.Availability));

                languageMode = null;
                return false;
            }

            languageMode = session.Runspace.SessionStateProxy.LanguageMode;

            return true;
        }

        #endregion CopyItem

        #endregion ContainerCmdletProvider accessors
    }

    /// <summary>
    /// Defines the action to be taken for Navigation cmdlets.
    /// </summary>
    internal enum ProcessMode
    {
        /// <summary>
        /// Write out the details.
        /// </summary>
        Enumerate = 1,

        /// <summary>
        /// Delete the item.
        /// </summary>
        Delete = 2
    }
}

#pragma warning restore 56500
