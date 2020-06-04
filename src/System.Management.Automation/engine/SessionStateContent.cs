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
        #region IContentCmdletProvider accessors

        #region GetContentReader

        /// <summary>
        /// Gets the content reader for the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to get the content reader for.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The content readers for all items that the path resolves to.
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
        internal Collection<IContentReader> GetContentReader(string[] paths, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            Collection<IContentReader> results = GetContentReader(paths, context);

            context.ThrowFirstErrorOrDoNothing();

            return results;
        }

        /// <summary>
        /// Gets the content reader for the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to get the content reader from.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// The content readers for all items that the path resolves to.
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
        internal Collection<IContentReader> GetContentReader(
             string[] paths,
             CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;

            Collection<IContentReader> results = new Collection<IContentReader>();

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
                    IContentReader reader = GetContentReaderPrivate(providerInstance, providerPath, context);

                    if (reader != null)
                    {
                        results.Add(reader);
                    }

                    context.ThrowFirstErrorOrDoNothing(true);
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the content reader for the item at the specified path.
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
        private IContentReader GetContentReaderPrivate(
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

            IContentReader result = null;

            try
            {
                result = providerInstance.GetContentReader(path, context);
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
                    "GetContentReaderProviderException",
                    SessionStateStrings.GetContentReaderProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-content cmdlet.
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
        internal object GetContentReaderDynamicParameters(
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

            Collection<string> providerPaths =
                Globber.GetGlobbedProviderPathsFromMonadPath(
                    path,
                    true,
                    newContext,
                    out provider,
                    out providerInstance);

            return GetContentReaderDynamicParameters(providerInstance, path, newContext);
        }

        /// <summary>
        /// Gets the dynamic parameters for the get-content cmdlet.
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
        private object GetContentReaderDynamicParameters(
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

            object result = null;
            try
            {
                result = providerInstance.GetContentReaderDynamicParameters(path, context);
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
                    "GetContentReaderDynamicParametersProviderException",
                    SessionStateStrings.GetContentReaderDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetContentReader

        #region GetContentWriter

        /// <summary>
        /// Gets the content writer for the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to get the content writer for.
        /// </param>
        /// <param name="force">
        /// Passed on to providers to force operations.
        /// </param>
        /// <param name="literalPath">
        /// If true, globbing is not done on paths.
        /// </param>
        /// <returns>
        /// The content writers for all items that the path resolves to.
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
        internal Collection<IContentWriter> GetContentWriter(string[] paths, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            Collection<IContentWriter> results = GetContentWriter(paths, context);

            context.ThrowFirstErrorOrDoNothing();

            return results;
        }

        /// <summary>
        /// Gets the content writer for the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to get the content writer from.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        /// <returns>
        /// The content writers for all items that the path resolves to.
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
        internal Collection<IContentWriter> GetContentWriter(
            string[] paths,
            CmdletProviderContext context)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            ProviderInfo provider = null;
            CmdletProvider providerInstance = null;
            Collection<IContentWriter> results = new Collection<IContentWriter>();

            foreach (string path in paths)
            {
                if (path == null)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(paths));
                }

                Collection<string> providerPaths =
                    Globber.GetGlobbedProviderPathsFromMonadPath(
                        path,
                        true,
                        context,
                        out provider,
                        out providerInstance);

                foreach (string providerPath in providerPaths)
                {
                    IContentWriter result =
                        GetContentWriterPrivate(providerInstance, providerPath, context);

                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the content writer for the item at the specified path.
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
        private IContentWriter GetContentWriterPrivate(
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

            IContentWriter result = null;
            try
            {
                result = providerInstance.GetContentWriter(path, context);
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
                    "GetContentWriterProviderException",
                    SessionStateStrings.GetContentWriterProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        /// <summary>
        /// Gets the dynamic parameters for the set-content and add-content cmdlet.
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
        internal object GetContentWriterDynamicParameters(
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

                return GetContentWriterDynamicParameters(providerInstance, providerPaths[0], newContext);
            }

            return null;
        }

        /// <summary>
        /// Gets the dynamic parameters for the set-content and add-content cmdlet.
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
        private object GetContentWriterDynamicParameters(
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

            object result = null;
            try
            {
                result = providerInstance.GetContentWriterDynamicParameters(path, context);
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
                    "GetContentWriterDynamicParametersProviderException",
                    SessionStateStrings.GetContentWriterDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion GetContentWriter

        #region ClearContent

        /// <summary>
        /// Clears all the content from the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path(s) to the item(s) to clear the content from.
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
        internal void ClearContent(string[] paths, bool force, bool literalPath)
        {
            if (paths == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new CmdletProviderContext(this.ExecutionContext);
            context.Force = force;
            context.SuppressWildcardExpansion = literalPath;

            ClearContent(paths, context);

            context.ThrowFirstErrorOrDoNothing();
        }

        /// <summary>
        /// Clears all of the content from the specified item.
        /// </summary>
        /// <param name="paths">
        /// The path to the item to clear the content from.
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
        internal void ClearContent(
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
                    PSTraceSource.NewArgumentNullException(nameof(paths));
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
                    ClearContentPrivate(providerInstance, providerPath, context);
                }
            }
        }

        /// <summary>
        /// Clears the content from the item at the specified path.
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
        private void ClearContentPrivate(
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

            try
            {
                providerInstance.ClearContent(path, context);
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
                    "ClearContentProviderException",
                    SessionStateStrings.ClearContentProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the clear-content cmdlet.
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
        internal object ClearContentDynamicParameters(
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

                return ClearContentDynamicParameters(providerInstance, providerPaths[0], newContext);
            }

            return null;
        }

        /// <summary>
        /// Calls the provider to get the clear-content dynamic parameters.
        /// </summary>
        /// <param name="providerInstance">
        /// The instance of the provider to call
        /// </param>
        /// <param name="path">
        /// The path to pass to the provider.
        /// </param>
        /// <param name="context">
        /// The context the command is executing under.
        /// </param>
        /// <returns>
        /// The dynamic parameter object returned by the provider.
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
        private object ClearContentDynamicParameters(
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

            object result = null;
            try
            {
                result = providerInstance.ClearContentDynamicParameters(path, context);
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
                    "ClearContentDynamicParametersProviderException",
                    SessionStateStrings.ClearContentDynamicParametersProviderException,
                    providerInstance.ProviderInfo,
                    path,
                    e);
            }

            return result;
        }

        #endregion ClearContent

        #endregion IContentCmdletProvider accessors
    }
}

#pragma warning restore 56500
