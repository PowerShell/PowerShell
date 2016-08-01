/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System.Management.Automation.Provider;
using System.Xml;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// The ProviderContext class.
    /// </summary>
    internal class ProviderContext
    {
        /// <summary>
        /// Requested path.
        /// </summary>
        private readonly string _requestedPath;
        private readonly ExecutionContext _executionContext;
        private readonly PathIntrinsics _pathIntrinsics;

        /// <summary>
        /// Requested path.
        /// </summary>
        internal string RequestedPath
        {
            get
            {
                return _requestedPath;
            }
        }

        /// <summary>
        /// Create a new instance of ProviderContext.
        /// </summary>
        internal ProviderContext(
            string requestedPath,
            ExecutionContext executionContext,
            PathIntrinsics pathIntrinsics)
        {
            Dbg.Assert(null != executionContext, "ExecutionContext cannot be null.");
            _requestedPath = requestedPath;
            _executionContext = executionContext;
            _pathIntrinsics = pathIntrinsics;
        }

        /// <summary>
        /// Get provider specific help info.
        /// </summary>
        internal MamlCommandHelpInfo GetProviderSpecificHelpInfo(string helpItemName)
        {
            // Get the provider.
            ProviderInfo providerInfo = null;
            PSDriveInfo driveInfo = null;
            string resolvedProviderPath = null;
            CmdletProviderContext cmdletProviderContext = new CmdletProviderContext(_executionContext);

            try
            {
                string psPath = _requestedPath;
                if (string.IsNullOrEmpty(_requestedPath))
                {
                    psPath = _pathIntrinsics.CurrentLocation.Path;
                }

                resolvedProviderPath = _executionContext.LocationGlobber.GetProviderPath(
                     psPath,
                     cmdletProviderContext,
                     out providerInfo,
                     out driveInfo);
            }
            // ignore exceptions caused by provider resolution
            catch (ArgumentNullException)
            {
            }
            catch (ProviderNotFoundException)
            {
            }
            catch (DriveNotFoundException)
            {
            }
            catch (ProviderInvocationException)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (ItemNotFoundException)
            {
            }

            if (providerInfo == null)
            {
                return null;
            }

            // Does the provider know how to generate MAML.
            CmdletProvider cmdletProvider = providerInfo.CreateInstance();
            ICmdletProviderSupportsHelp provider = cmdletProvider as ICmdletProviderSupportsHelp;
            if (provider == null)
            {
                return null;
            }

            if (resolvedProviderPath == null)
            {
                throw new ItemNotFoundException(_requestedPath, "PathNotFound", SessionStateStrings.PathNotFound);
            }

            // ok we have path and valid provider that supplys content..intialize the provider
            // and get the help content for the path.
            cmdletProvider.Start(providerInfo, cmdletProviderContext);
            // There should be exactly one resolved path.
            string providerPath = resolvedProviderPath;
            // Get the MAML help info. Don't catch exceptions thrown by provider.
            string mamlXmlString = provider.GetHelpMaml(helpItemName, providerPath);
            if (string.IsNullOrEmpty(mamlXmlString))
            {
                return null;
            }
            // process the MAML content only if it is non-empty.
            XmlDocument mamlDoc = InternalDeserializer.LoadUnsafeXmlDocument(
                mamlXmlString,
                false, /* ignore whitespace, comments, etc. */
                null); /* default maxCharactersInDocument */
            MamlCommandHelpInfo providerSpecificHelpInfo = MamlCommandHelpInfo.Load(mamlDoc.DocumentElement, HelpCategory.Provider);
            return providerSpecificHelpInfo;
        }
    }
}
