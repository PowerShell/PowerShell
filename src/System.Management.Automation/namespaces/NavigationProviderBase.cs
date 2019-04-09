// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Internal;
using System.Text;

namespace System.Management.Automation.Provider
{
    #region NavigationCmdletProvider

    /// <summary>
    /// The base class for a Cmdlet provider that expose a hierarchy of items and containers.
    /// </summary>
    /// <remarks>
    /// The NavigationCmdletProvider class is a base class that provider can derive from
    /// to implement a set of methods that allow
    /// the use of a set of core commands against the data store that the provider
    /// gives access to. By implementing this interface users can take advantage
    /// the recursive commands, nested containers, and relative paths.
    /// </remarks>
    public abstract class NavigationCmdletProvider : ContainerCmdletProvider
    {
        #region Internal methods

        /// <summary>
        /// Internal wrapper for the MakePath protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="parent">
        /// The parent segment of a path to be joined with the child.
        /// </param>
        /// <param name="child">
        /// The child segment of a path to be joined with the parent.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// A string that represents the parent and child segments of the path
        /// joined by a path separator.
        /// </returns>
        /// <remarks>
        /// This method should use lexical joining of two path segments with a path
        /// separator character. It should not validate the path as a legal fully
        /// qualified path in the provider namespace as each parameter could be only
        /// partial segments of a path and joined they may not generate a fully
        /// qualified path.
        /// Example: the file system provider may get "windows\system32" as the parent
        /// parameter and "foo.dll" as the child parameter. The method should join these
        /// with the "\" separator and return "windows\system32\foo.dll". Note that
        /// the returned path is not a fully qualified file system path.
        ///
        /// Also beware that the path segments may contain characters that are illegal
        /// in the provider namespace. These characters are most likely being used
        /// for globbing and should not be removed by the implementation of this method.
        /// </remarks>
        internal string MakePath(
            string parent,
            string child,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return MakePath(parent, child);
        }

        /// <summary>
        /// Internal wrapper for the GetParentPath protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// A fully qualified provider specific path to an item. The item may or
        /// may not exist.
        /// </param>
        /// <param name="root">
        /// The fully qualified path to the root of a drive. This parameter may be null
        /// or empty if a mounted drive is not in use for this operation. If this parameter
        /// is not null or empty the result of the method should not be a path to a container
        /// that is a parent or in a different tree than the root.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// The path of the parent of the path parameter.
        /// </returns>
        /// <remarks>
        /// This should be a lexical splitting of the path on the path separator character
        /// for the provider namespace. For example, the file system provider should look
        /// for the last "\" and return everything to the left of the "\".
        /// </remarks>
        internal string GetParentPath(
            string path,
            string root,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return GetParentPath(path, root);
        }

        /// <summary>
        /// Internal wrapper for the NormalizeRelativePath method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// A fully qualified provider specific path to an item. The item should exist
        /// or the provider should write out an error.
        /// </param>
        /// <param name="basePath">
        /// The path that the return value should be relative to.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// A normalized path that is relative to the basePath that was passed. The
        /// provider should parse the path parameter, normalize the path, and then
        /// return the normalized path relative to the basePath.
        /// </returns>
        /// <remarks>
        /// This method does not have to be purely syntactical parsing of the path. It
        /// is encouraged that the provider actually use the path to lookup in its store
        /// and create a relative path that matches the casing, and standardized path syntax.
        /// </remarks>
        internal string NormalizeRelativePath(
            string path,
            string basePath,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return NormalizeRelativePath(path, basePath);
        }

        /// <summary>
        /// Internal wrapper for the GetChildName protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The fully qualified path to the item
        /// </param>
        /// <returns>
        /// The leaf element in the path.
        /// </returns>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <remarks>
        /// This should be implemented as a split on the path separator. The characters
        /// in the fullPath may not be legal characters in the namespace but may be
        /// used in globing or regular expression matching. The provider should not error
        /// unless there are no path separators in the fully qualified path.
        /// </remarks>
        internal string GetChildName(
            string path,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return GetChildName(path);
        }

        /// <summary>
        /// Internal wrapper for the IsItemContainer protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it is a container.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// true if the item specified by path is a container, false otherwise.
        /// </returns>
        internal bool IsItemContainer(
            string path,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            return IsItemContainer(path);
        }

        /// <summary>
        /// Internal wrapper for the MoveItem protected method. It is called instead
        /// of the protected method that is overridden by derived classes so that the
        /// context of the command can be set.
        /// </summary>
        /// <param name="path">
        /// The path to the item to be moved.
        /// </param>
        /// <param name="destination">
        /// The path of the destination container.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// Nothing. All objects that are moved should be written to the WriteObject method.
        /// </returns>
        internal void MoveItem(
            string path,
            string destination,
            CmdletProviderContext context)
        {
            Context = context;

            // Call virtual method

            MoveItem(path, destination);
        }

        /// <summary>
        /// Gives the provider to attach additional parameters to
        /// the move-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="destination">
        /// The path of the destination container.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object MoveItemDynamicParameters(
            string path,
            string destination,
            CmdletProviderContext context)
        {
            Context = context;
            return MoveItemDynamicParameters(path, destination);
        }

        #endregion Internal methods

        #region protected methods

        /// <summary>
        /// Joins two strings with a path a provider specific path separator.
        /// </summary>
        /// <param name="parent">
        /// The parent segment of a path to be joined with the child.
        /// </param>
        /// <param name="child">
        /// The child segment of a path to be joined with the parent.
        /// </param>
        /// <returns>
        /// A string that represents the parent and child segments of the path
        /// joined by a path separator.
        /// </returns>
        /// <remarks>
        /// This method should use lexical joining of two path segments with a path
        /// separator character. It should not validate the path as a legal fully
        /// qualified path in the provider namespace as each parameter could be only
        /// partial segments of a path and joined they may not generate a fully
        /// qualified path.
        /// Example: the file system provider may get "windows\system32" as the parent
        /// parameter and "foo.dll" as the child parameter. The method should join these
        /// with the "\" separator and return "windows\system32\foo.dll". Note that
        /// the returned path is not a fully qualified file system path.
        ///
        /// Also beware that the path segments may contain characters that are illegal
        /// in the provider namespace. These characters are most likely being used
        /// for globbing and should not be removed by the implementation of this method.
        /// </remarks>
        protected virtual string MakePath(string parent, string child)
        {
            return MakePath(parent, child, childIsLeaf: false);
        }

        /// <summary>
        /// Joins two strings with a path a provider specific path separator.
        /// </summary>
        /// <param name="parent">
        /// The parent segment of a path to be joined with the child.
        /// </param>
        /// <param name="child">
        /// The child segment of a path to be joined with the parent.
        /// </param>
        /// <param name="childIsLeaf">
        /// Indicate that the <paramref name="child"/> is the name of a child item that's guaranteed to exist
        /// </param>
        /// <remarks>
        /// If the <paramref name="childIsLeaf"/> is True, then we don't normalize the child path, and would do
        /// some checks to decide whether to normalize the parent path.
        /// </remarks>
        /// <returns></returns>
        protected string MakePath(string parent, string child, bool childIsLeaf)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                string result = null;

                if (parent == null &&
                    child == null)
                {
                    // If both are null it is an error

                    throw PSTraceSource.NewArgumentException("parent");
                }
                else if (string.IsNullOrEmpty(parent) &&
                         string.IsNullOrEmpty(child))
                {
                    // If both are empty, just return the empty string.

                    result = string.Empty;
                }
                else if (string.IsNullOrEmpty(parent) &&
                         !string.IsNullOrEmpty(child))
                {
                    // If the parent is empty but the child is not, return the
                    // child

                    result = child.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);
                }
                else if (!string.IsNullOrEmpty(parent) &&
                         string.IsNullOrEmpty(child))
                {
                    // If the child is empty but the parent is not, return the
                    // parent with the path separator appended.

                    // Append the default path separator

                    if (parent.EndsWith(StringLiterals.DefaultPathSeparator))
                    {
                        result = parent;
                    }
                    else
                    {
                        result = parent + StringLiterals.DefaultPathSeparator;
                    }
                }
                else
                {
                    // Both parts are not empty so join them

                    // 'childIsLeaf == true' indicates that 'child' is actually the name of a child item and
                    // guaranteed to exist. In this case, we don't normalize the child path.
                    if (childIsLeaf)
                    {
                        parent = NormalizePath(parent);
                    }
                    else
                    {
                        // Normalize the path so that only the default path separator is used as a
                        // separator even if the user types the alternate slash.

                        parent = parent.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);
                        child = child.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);
                    }

                    // Joins the paths

                    StringBuilder builder = new StringBuilder(parent, parent.Length + child.Length + 1);

                    if (parent.EndsWith(StringLiterals.DefaultPathSeparator))
                    {
                        if (child.StartsWith(StringLiterals.DefaultPathSeparator))
                        {
                            builder.Append(child, 1, child.Length - 1);
                        }
                        else
                        {
                            builder.Append(child);
                        }
                    }
                    else
                    {
                        if (child.StartsWith(StringLiterals.DefaultPathSeparator))
                        {
                            if (parent.Length == 0)
                            {
                                builder.Append(child, 1, child.Length - 1);
                            }
                            else
                            {
                                builder.Append(child);
                            }
                        }
                        else
                        {
                            if (parent.Length > 0 && child.Length > 0)
                            {
                                builder.Append(StringLiterals.DefaultPathSeparator);
                            }

                            builder.Append(child);
                        }
                    }

                    result = builder.ToString();
                }

                return result;
            }
        }

        /// <summary>
        /// Removes the child segment of a path and returns the remaining parent
        /// portion.
        /// </summary>
        /// <param name="path">
        /// A fully qualified provider specific path to an item. The item may or
        /// may not exist.
        /// </param>
        /// <param name="root">
        /// The fully qualified path to the root of a drive. This parameter may be null
        /// or empty if a mounted drive is not in use for this operation. If this parameter
        /// is not null or empty the result of the method should not be a path to a container
        /// that is a parent or in a different tree than the root.
        /// </param>
        /// <returns>
        /// The path of the parent of the path parameter.
        /// </returns>
        /// <remarks>
        /// This should be a lexical splitting of the path on the path separator character
        /// for the provider namespace. For example, the file system provider should look
        /// for the last "\" and return everything to the left of the "\".
        /// </remarks>
        protected virtual string GetParentPath(string path, string root)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                string parentPath = null;

                // Verify the parameters

                if (string.IsNullOrEmpty(path))
                {
                    throw PSTraceSource.NewArgumentException("path");
                }

                if (root == null)
                {
                    if (PSDriveInfo != null)
                    {
                        root = PSDriveInfo.Root;
                    }
                }

                // Normalize the path

                path = NormalizePath(path);
                path = path.TrimEnd(StringLiterals.DefaultPathSeparator);
                string rootPath = string.Empty;

                if (root != null)
                {
                    rootPath = NormalizePath(root);
                }

                // Check to see if the path is equal to the root
                // of the virtual drive

                if (string.Compare(
                    path,
                    rootPath,
                    StringComparison.OrdinalIgnoreCase) == 0)
                {
                    parentPath = string.Empty;
                }
                else
                {
                    int lastIndex = path.LastIndexOf(StringLiterals.DefaultPathSeparator);

                    if (lastIndex != -1)
                    {
                        if (lastIndex == 0)
                        {
                            ++lastIndex;
                        }
                        // Get the parent directory

                        parentPath = path.Substring(0, lastIndex);
                    }
                    else
                    {
                        parentPath = string.Empty;
                    }
                }

                return parentPath;
            }
        }

        /// <summary>
        /// Normalizes the path that was passed in and returns the normalized path
        /// as a relative path to the basePath that was passed.
        /// </summary>
        /// <param name="path">
        /// A fully qualified provider specific path to an item. The item should exist
        /// or the provider should write out an error.
        /// </param>
        /// <param name="basePath">
        /// The path that the return value should be relative to.
        /// </param>
        /// <returns>
        /// A normalized path that is relative to the basePath that was passed. The
        /// provider should parse the path parameter, normalize the path, and then
        /// return the normalized path relative to the basePath.
        /// </returns>
        /// <remarks>
        /// This method does not have to be purely syntactical parsing of the path. It
        /// is encouraged that the provider actually use the path to lookup in its store
        /// and create a relative path that matches the casing, and standardized path syntax.
        ///
        /// Note, the base class implementation uses GetParentPath, GetChildName, and MakePath
        /// to normalize the path and then make it relative to basePath. All string comparisons
        /// are done using StringComparison.InvariantCultureIgnoreCase.
        /// </remarks>
        protected virtual string NormalizeRelativePath(
            string path,
            string basePath)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return ContractRelativePath(path, basePath, false, Context);
            }
        }

        internal string ContractRelativePath(
            string path,
            string basePath,
            bool allowNonExistingPaths,
            CmdletProviderContext context)
        {
            Context = context;

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException("path");
            }

            if (path.Length == 0)
            {
                return string.Empty;
            }

            if (basePath == null)
            {
                basePath = string.Empty;
            }

            providerBaseTracer.WriteLine("basePath = {0}", basePath);

            string result = path;
            bool originalPathHadTrailingSlash = false;

            string normalizedPath = path;
            string normalizedBasePath = basePath;

            // NTRAID#Windows 7-697922-2009/06/29-leeholm
            // WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND
            //
            // This path normalization got moved here from the MakePath override in V2 to prevent
            // over-normalization of paths. This was a net-improvement for providers that use the default
            // implementations, but now incorrectly replaces forward slashes with back slashes during the call to
            // GetParentPath and GetChildName. This breaks providers that are sensitive to slash direction, the only
            // one we are aware of being the Active Directory provider. This change prevents this over-normalization
            // from being done on AD paths.
            //
            // For more information, see Win7:695292. Do not change this code without closely working with the
            // Active Directory team.
            //
            // WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND WORKAROUND
            if (!string.Equals(context.ProviderInstance.ProviderInfo.FullName,
                @"Microsoft.ActiveDirectory.Management\ActiveDirectory", StringComparison.OrdinalIgnoreCase))
            {
                normalizedPath = NormalizePath(path);
                normalizedBasePath = NormalizePath(basePath);
            }

            do // false loop
            {
                // Convert to the correct path separators and trim trailing separators
                string originalPath = path;
                Stack<string> tokenizedPathStack = null;

                if (path.EndsWith(StringLiterals.DefaultPathSeparator))
                {
                    path = path.TrimEnd(StringLiterals.DefaultPathSeparator);
                    originalPathHadTrailingSlash = true;
                }

                basePath = basePath.TrimEnd(StringLiterals.DefaultPathSeparator);

                // See if the base and the path are already the same. We resolve this to
                // ..\Leaf, since resolving "." to "." doesn't offer much information.
                if (string.Equals(normalizedPath, normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
                    (!originalPath.EndsWith(StringLiterals.DefaultPathSeparator)))
                {
                    string childName = GetChildName(path);
                    result = MakePath("..", childName);
                    break;
                }

                // If the base path isn't really a base, then we resolve to a parent
                // path (such as ../../foo)
                if (!normalizedPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
                    (basePath.Length > 0))
                {
                    result = string.Empty;
                    string commonBase = GetCommonBase(normalizedPath, normalizedBasePath);

                    Stack<string> parentNavigationStack = TokenizePathToStack(normalizedBasePath, commonBase);
                    int parentPopCount = parentNavigationStack.Count;

                    if (string.IsNullOrEmpty(commonBase))
                    {
                        parentPopCount--;
                    }

                    for (int leafCounter = 0; leafCounter < parentPopCount; leafCounter++)
                    {
                        result = MakePath("..", result);
                    }

                    // This is true if we get passed a base path like:
                    //    c:\directory1\directory2
                    // and an actual path of
                    //    c:\directory1
                    // Which happens when the user is in c:\directory1\directory2
                    // and wants to resolve something like:
                    // ..\..\dir*
                    // In that case (as above,) we keep the ..\..\directory1
                    // instead of ".." as would usually be returned
                    if (!string.IsNullOrEmpty(commonBase))
                    {
                        if (string.Equals(normalizedPath, commonBase, StringComparison.OrdinalIgnoreCase) &&
                            (!normalizedPath.EndsWith(StringLiterals.DefaultPathSeparator)))
                        {
                            string childName = GetChildName(path);
                            result = MakePath("..", result);
                            result = MakePath(result, childName);
                        }
                        else
                        {
                            string[] childNavigationItems = TokenizePathToStack(normalizedPath, commonBase).ToArray();

                            for (int leafCounter = 0; leafCounter < childNavigationItems.Length; leafCounter++)
                            {
                                result = MakePath(result, childNavigationItems[leafCounter]);
                            }
                        }
                    }
                }
                // Otherwise, we resolve to a child path (such as foo/bar)
                else
                {
                    tokenizedPathStack = TokenizePathToStack(path, basePath);

                    // Now we have to normalize the path
                    // by processing each token on the stack
                    Stack<string> normalizedPathStack;

                    try
                    {
                        normalizedPathStack = NormalizeThePath(tokenizedPathStack, path, basePath, allowNonExistingPaths);
                    }
                    catch (ArgumentException argumentException)
                    {
                        WriteError(new ErrorRecord(argumentException, argumentException.GetType().FullName, ErrorCategory.InvalidArgument, null));
                        result = null;
                        break;
                    }

                    // Now that the path has been normalized, create the relative path
                    result = CreateNormalizedRelativePathFromStack(normalizedPathStack);
                }
            } while (false);

            if (originalPathHadTrailingSlash)
            {
                result = result + StringLiterals.DefaultPathSeparator;
            }

            return result;
        }

        /// <summary>
        /// Get the common base path of two paths.
        /// </summary>
        /// <param name="path1">One path.</param>
        /// <param name="path2">Another path.</param>
        private string GetCommonBase(string path1, string path2)
        {
            // Always see if the shorter path is a substring of the
            // longer path. If it is not, take the child off of the longer
            // path and compare again.

            while (!string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase))
            {
                if (path2.Length > path1.Length)
                {
                    path2 = GetParentPath(path2, null);
                }
                else
                {
                    path1 = GetParentPath(path1, null);
                }
            }

            return path1;
        }

        /// <summary>
        /// Gets the name of the leaf element in the specified path.
        /// </summary>
        /// <param name="path">
        /// The fully qualified path to the item
        /// </param>
        /// <returns>
        /// The leaf element in the path.
        /// </returns>
        /// <remarks>
        /// This should be implemented as a split on the path separator. The characters
        /// in the fullPath may not be legal characters in the namespace but may be
        /// used in globing or regular expression matching. The provider should not error
        /// unless there are no path separators in the fully qualified path.
        /// </remarks>
        protected virtual string GetChildName(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                // Verify the parameters

                if (string.IsNullOrEmpty(path))
                {
                    throw PSTraceSource.NewArgumentException("path");
                }

                // Normalize the path
                path = NormalizePath(path);
                // Trim trailing back slashes
                path = path.TrimEnd(StringLiterals.DefaultPathSeparator);
                string result = null;

                int separatorIndex = path.LastIndexOf(StringLiterals.DefaultPathSeparator);

                // Since there was no path separator return the entire path
                if (separatorIndex == -1)
                {
                    result = path;
                }
                // If the full path existed, we must semantically evaluate the parent path
                else if (ItemExists(path, Context))
                {
                    string parentPath = GetParentPath(path, null);

                    // No parent, return the entire path
                    if (string.IsNullOrEmpty(parentPath))
                        result = path;
                    // If the parent path ends with the path separator, we can't split
                    // the path based on that
                    else if (parentPath.IndexOf(StringLiterals.DefaultPathSeparator) == (parentPath.Length - 1))
                    {
                        separatorIndex = path.IndexOf(parentPath, StringComparison.OrdinalIgnoreCase) + parentPath.Length;
                        result = path.Substring(separatorIndex);
                    }
                    else
                    {
                        separatorIndex = path.IndexOf(parentPath, StringComparison.OrdinalIgnoreCase) + parentPath.Length;
                        result = path.Substring(separatorIndex + 1);
                    }
                }
                // Otherwise, use lexical parsing
                else
                {
                    result = path.Substring(separatorIndex + 1);
                }

                return result;
            }
        }

        /// <summary>
        /// Determines if the item specified by the path is a container.
        /// </summary>
        /// <param name="path">
        /// The path to the item to determine if it is a container.
        /// </param>
        /// <returns>
        /// true if the item specified by path is a container, false otherwise.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to check
        /// to see if a provider object is a container using the test-path -container cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
        /// requirements by accessing the appropriate property from the base class.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual bool IsItemContainer(string path)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Moves the item specified by path to the specified destination.
        /// </summary>
        /// <param name="path">
        /// The path to the item to be moved.
        /// </param>
        /// <param name="destination">
        /// The path of the destination container.
        /// </param>
        /// <returns>
        /// Nothing is returned, but all the objects that were moved should be written to the WriteItemObject method.
        /// </returns>
        /// <remarks>
        /// Providers override this method to give the user the ability to move provider objects using
        /// the move-item cmdlet.
        ///
        /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
        /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path and items being moved
        /// meets those requirements by accessing the appropriate property from the base class.
        ///
        /// By default overrides of this method should not move objects over existing items unless the Force
        /// property is set to true. For instance, the FileSystem provider should not move c:\temp\foo.txt over
        /// c:\bar.txt if c:\bar.txt already exists unless the Force parameter is true.
        ///
        /// If <paramref name="destination"/> exists and is a container then Force isn't required and <paramref name="path"/>
        /// should be moved into the <paramref name="destination"/> container as a child.
        ///
        /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
        /// </remarks>
        protected virtual void MoveItem(
            string path,
            string destination)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                throw
                    PSTraceSource.NewNotSupportedException(
                        SessionStateStrings.CmdletProvider_NotSupported);
            }
        }

        /// <summary>
        /// Gives the provider an opportunity to attach additional parameters to
        /// the move-item cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="destination">
        /// The path of the destination container.
        /// </param>
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object MoveItemDynamicParameters(
            string path,
            string destination)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        #endregion Protected methods

        #region private members

        /// <summary>
        /// When a path contains both forward slash and backslash, we may introduce some errors by
        /// normalizing the path. This method does some smart checks to reduce the chances of making
        /// those errors.
        /// </summary>
        /// <param name="path">
        /// The path to normalize
        /// </param>
        /// <returns>
        /// Normalized path or the original path
        /// </returns>
        private string NormalizePath(string path)
        {
            // If we have a mix of slashes, then we may introduce an error by normalizing the path.
            // For example: path HKCU:\Test\/ is pointing to a subkey '/' of 'HKCU:\Test', if we
            // normalize it, then we will get a wrong path.
            bool pathHasForwardSlash = path.IndexOf(StringLiterals.AlternatePathSeparator) != -1;
            bool pathHasBackSlash = path.IndexOf(StringLiterals.DefaultPathSeparator) != -1;
            bool pathHasMixedSlashes = pathHasForwardSlash && pathHasBackSlash;
            bool shouldNormalizePath = true;

            string normalizedPath = path.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);

            // There is a mix of slashes & the path is rooted & the path exists without normalization.
            // In this case, we might want to skip the normalization to the path.
            if (pathHasMixedSlashes && IsAbsolutePath(path) && ItemExists(path))
            {
                // 1. The path exists and ends with a forward slash, in this case, it's very possible the ending forward slash
                //    make sense to the underlying provider, so we skip normalization
                // 2. The path exists, but not anymore after normalization, then we skip normalization
                bool parentEndsWithForwardSlash = path.EndsWith(StringLiterals.AlternatePathSeparatorString, StringComparison.Ordinal);
                if (parentEndsWithForwardSlash || !ItemExists(normalizedPath))
                {
                    shouldNormalizePath = false;
                }
            }

            return shouldNormalizePath ? normalizedPath : path;
        }

        /// <summary>
        /// Test if the path is an absolute path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsAbsolutePath(string path)
        {
            bool result = false;

            if (LocationGlobber.IsAbsolutePath(path))
            {
                result = true;
            }
            else if (this.PSDriveInfo != null && !string.IsNullOrEmpty(this.PSDriveInfo.Root) &&
                     path.StartsWith(this.PSDriveInfo.Root, StringComparison.OrdinalIgnoreCase))
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Tokenizes the specified path onto a stack.
        /// </summary>
        /// <param name="path">
        /// The path to tokenize.
        /// </param>
        /// <param name="basePath">
        /// The base part of the path that should not be tokenized.
        /// </param>
        /// <returns>
        /// A stack containing the tokenized path with leaf elements on the bottom
        /// of the stack and the most ancestral parent at the top.
        /// </returns>
        private Stack<string> TokenizePathToStack(string path, string basePath)
        {
            Stack<string> tokenizedPathStack = new Stack<string>();
            string tempPath = path;
            string previousParent = path;

            while (tempPath.Length > basePath.Length)
            {
                // Get the child name and push it onto the stack
                // if its valid

                string childName = GetChildName(tempPath);
                if (string.IsNullOrEmpty(childName))
                {
                    // Push the parent on and then stop
                    tokenizedPathStack.Push(tempPath);
                    break;
                }

                providerBaseTracer.WriteLine("tokenizedPathStack.Push({0})", childName);
                tokenizedPathStack.Push(childName);

                // Get the parent path and verify if we have to continue
                // tokenizing

                tempPath = GetParentPath(tempPath, basePath);
                if (tempPath.Length >= previousParent.Length)
                {
                    break;
                }

                previousParent = tempPath;
            }

            return tokenizedPathStack;
        }

        /// <summary>
        /// Given the tokenized path, the relative path elements are removed.
        /// </summary>
        /// <param name="tokenizedPathStack">
        /// A stack containing path elements where the leaf most element is at
        /// the bottom of the stack and the most ancestral parent is on the top.
        /// Generally this stack comes from TokenizePathToStack().
        /// </param>
        /// <param name="path">
        /// The path being normalized. Just used for error reporting.
        /// </param>
        /// <param name="basePath">
        /// The base path to make the path relative to. Just used for error reporting.
        /// </param>
        /// <param name="allowNonExistingPaths">
        /// Determines whether to throw an exception on non-existing paths.
        /// </param>
        /// <returns>
        /// A stack in reverse order with the path elements normalized and all relative
        /// path tokens removed.
        /// </returns>
        private static Stack<string> NormalizeThePath(
            Stack<string> tokenizedPathStack, string path,
            string basePath, bool allowNonExistingPaths)
        {
            Stack<string> normalizedPathStack = new Stack<string>();

            while (tokenizedPathStack.Count > 0)
            {
                string childName = tokenizedPathStack.Pop();

                providerBaseTracer.WriteLine("childName = {0}", childName);

                // Ignore the current directory token
                if (childName.Equals(".", StringComparison.OrdinalIgnoreCase))
                {
                    // Just ignore it and move on.
                    continue;
                }

                // Make sure we don't have
                if (childName.Equals("..", StringComparison.OrdinalIgnoreCase))
                {
                    if (normalizedPathStack.Count > 0)
                    {
                        // Pop the result and continue processing
                        string poppedName = normalizedPathStack.Pop();
                        providerBaseTracer.WriteLine("normalizedPathStack.Pop() : {0}", poppedName);
                        continue;
                    }
                    else
                    {
                        if (!allowNonExistingPaths)
                        {
                            PSArgumentException e =
                                (PSArgumentException)
                                PSTraceSource.NewArgumentException(
                                    "path",
                                    SessionStateStrings.NormalizeRelativePathOutsideBase,
                                    path,
                                    basePath);
                            throw e;
                        }
                    }
                }

                providerBaseTracer.WriteLine("normalizedPathStack.Push({0})", childName);
                normalizedPathStack.Push(childName);
            }

            return normalizedPathStack;
        }

        /// <summary>
        /// Pops each leaf element of the stack and uses MakePath to generate the relative path.
        /// </summary>
        /// <param name="normalizedPathStack">
        /// The stack containing the leaf elements of the path.
        /// </param>
        /// <returns>
        /// A path that is made up of the leaf elements on the given stack.
        /// </returns>
        /// <remarks>
        /// The elements on the stack start from the leaf element followed by its parent
        /// followed by its parent, etc. Each following element on the stack is the parent
        /// of the one before it.
        /// </remarks>
        private string CreateNormalizedRelativePathFromStack(Stack<string> normalizedPathStack)
        {
            string leafElement = string.Empty;

            while (normalizedPathStack.Count > 0)
            {
                if (string.IsNullOrEmpty(leafElement))
                {
                    leafElement = normalizedPathStack.Pop();
                }
                else
                {
                    string parentElement = normalizedPathStack.Pop();
                    leafElement = MakePath(parentElement, leafElement);
                }
            }

            return leafElement;
        }

        #endregion private members
    }

    #endregion NavigationCmdletProvider
}

