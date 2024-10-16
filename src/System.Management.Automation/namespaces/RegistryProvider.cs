// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !UNIX

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Microsoft.Win32;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Provider;
using Dbg = System.Management.Automation;
using Microsoft.PowerShell.Commands.Internal;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Provider that provides access to Registry through cmdlets. This provider
    /// implements <see cref="System.Management.Automation.Provider.NavigationCmdletProvider"/>,
    /// <see cref="System.Management.Automation.Provider.IPropertyCmdletProvider"/>,
    /// <see cref="System.Management.Automation.Provider.IDynamicPropertyCmdletProvider"/>,
    /// <see cref="System.Management.Automation.Provider.ISecurityDescriptorCmdletProvider"/>
    /// interfaces.
    /// </summary>
    /// <!--
    ///
    /// INSTALLATION:
    ///
    /// Type the following at a PowerShell prompt:
    ///
    /// new-PSProvider -Path "REG.cmdletprovider" -description "My registry navigation provider"
    ///
    /// TO EXERCISE THE PROVIDER:
    ///
    /// Get-PSDrive
    /// set-location HKLM:\software
    /// get-childitem
    /// New-PSDrive -PSProvider REG -name HKCR -root HKEY_CLASSES_ROOT\CLSID
    /// set-location HKCR:
    /// get-childitem "{0000*"
    ///
    /// The CmdletProvider attribute defines the name and capabilities of the provider.
    /// The first parameter is the default friendly name for the provider. The second parameter
    /// is the provider name which, along with some assembly information like version, company, etc.
    /// is used as a fully-qualified provider name which can be used for disambiguation.
    /// The third parameter states the capabilities of the provider.
    ///
    /// -->
#if CORECLR // System.Transaction namespace is not in CoreClr.
    [CmdletProvider(RegistryProvider.ProviderName, ProviderCapabilities.ShouldProcess)]
#else
    [CmdletProvider(RegistryProvider.ProviderName, ProviderCapabilities.ShouldProcess | ProviderCapabilities.Transactions)]
#endif
    [OutputType(typeof(string), ProviderCmdlet = ProviderCmdlet.MoveItemProperty)]
    [OutputType(typeof(RegistryKey), typeof(string), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
    [OutputType(typeof(RegistryKey), ProviderCmdlet = ProviderCmdlet.GetItem)]
    [OutputType(typeof(System.Security.AccessControl.RegistrySecurity), ProviderCmdlet = ProviderCmdlet.GetAcl)]
    [OutputType(typeof(Microsoft.Win32.RegistryKey), ProviderCmdlet = ProviderCmdlet.GetChildItem)]
    [OutputType(typeof(RegistryKey), ProviderCmdlet = ProviderCmdlet.GetItem)]
    [OutputType(typeof(RegistryKey), typeof(string), typeof(Int32), typeof(Int64), ProviderCmdlet = ProviderCmdlet.GetItemProperty)]
    [OutputType(typeof(RegistryKey), ProviderCmdlet = ProviderCmdlet.NewItem)]
    [OutputType(typeof(string), typeof(PathInfo), ProviderCmdlet = ProviderCmdlet.ResolvePath)]
    [OutputType(typeof(PathInfo), ProviderCmdlet = ProviderCmdlet.PushLocation)]
    [OutputType(typeof(PathInfo), ProviderCmdlet = ProviderCmdlet.PopLocation)]
    public sealed partial class RegistryProvider :
        NavigationCmdletProvider,
        IPropertyCmdletProvider,
        IDynamicPropertyCmdletProvider,
        ISecurityDescriptorCmdletProvider
    {
        #region tracer

        /// <summary>
        /// An instance of the PSTraceSource class used for trace output
        /// using "ProviderProvider" as the category.
        /// </summary>
        [Dbg.TraceSourceAttribute(
            "RegistryProvider",
            "The namespace navigation provider for the Windows Registry")]
        private static readonly Dbg.PSTraceSource s_tracer =
            Dbg.PSTraceSource.GetTracer("RegistryProvider",
            "The namespace navigation provider for the Windows Registry");

        #endregion tracer

        /// <summary>
        /// Gets the name of the provider.
        /// </summary>
        public const string ProviderName = "Registry";

        #region CmdletProvider overrides

        /// <summary>
        /// Gets the alternate item separator character for this provider.
        /// </summary>
        public override char AltItemSeparator => ItemSeparator;

        #endregion

        #region DriveCmdletProvider overrides

        /// <summary>
        /// Verifies that the new drive has a valid root.
        /// </summary>
        /// <returns>A PSDriveInfo object.</returns>
        /// <!--
        /// It also givesthe provider an opportunity to return a
        /// derived class of PSDriveInfo which can contain provider specific
        /// information about the drive.This may be done for performance
        /// or reliability reasons or toprovide extra data to all calls
        /// using the drive
        /// -->
        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            if (drive == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(drive));
            }

            if (!ItemExists(drive.Root))
            {
                Exception e = new ArgumentException(RegistryProviderStrings.NewDriveRootDoesNotExist);
                WriteError(new ErrorRecord(
                    e,
                    e.GetType().FullName,
                    ErrorCategory.InvalidArgument,
                    drive.Root));
            }

            return drive;
        }

        /// <summary>
        /// Creates HKEY_LOCAL_MACHINE and HKEY_CURRENT_USER registry drives during provider initialization.
        /// </summary>
        /// <remarks>
        /// After the Start method is called on a provider, the InitializeDefaultDrives
        /// method is called. This is an opportunity for the provider to
        /// mount drives that are important to it. For instance, the Active Directory
        /// provider might mount a drive for the defaultNamingContext if the
        /// machine is joined to a domain.  The FileSystem mounts all drives then available.
        /// </remarks>
        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();

            drives.Add(
                new PSDriveInfo(
                    "HKLM",
                    ProviderInfo,
                    "HKEY_LOCAL_MACHINE",
                    RegistryProviderStrings.HKLMDriveDescription,
                    null));

            drives.Add(
                new PSDriveInfo(
                    "HKCU",
                    ProviderInfo,
                    "HKEY_CURRENT_USER",
                    RegistryProviderStrings.HKCUDriveDescription,
                    null));

            return drives;
        }

        #endregion DriveCmdletProvider overrides

        #region ItemCmdletProvider overrides

        /// <summary>
        /// Determines if the specified <paramref name="path"/> is syntactically and semantically valid.
        /// </summary>
        /// <param name="path">
        /// The path to validate.
        /// </param>
        /// <returns>
        /// True if the path is valid, or False otherwise.
        /// </returns>
        protected override bool IsValidPath(string path)
        {
            bool result = true;

            do // false loop
            {
                // There really aren't any illegal characters or syntactical patterns
                // to validate, so just ensure that the path starts with one of the hive roots.

                string root = NormalizePath(path);
                root = root.TrimStart(StringLiterals.DefaultPathSeparator);
                root = root.TrimEnd(StringLiterals.DefaultPathSeparator);

                int pathSeparator = root.IndexOf(StringLiterals.DefaultPathSeparator);

                if (pathSeparator != -1)
                {
                    root = root.Substring(0, pathSeparator);
                }

                if (string.IsNullOrEmpty(root))
                {
                    // An empty path means that we are at the root and should
                    // enumerate the hives. So that is a valid path.
                    result = true;
                    break;
                }

                if (GetHiveRoot(root) == null)
                {
                    result = false;
                }
            } while (false);

            return result;
        }

        /// <summary>
        /// Gets the RegistryKey item at the specified <paramref name="path"/>
        /// and writes it to the pipeline using the WriteObject method.
        /// Any non-terminating exceptions are written to the WriteError method.
        /// </summary>
        /// <param name="path">
        /// The path to the key to retrieve.
        /// </param>
        protected override void GetItem(string path)
        {
            // Get the registry item

            IRegistryWrapper result = GetRegkeyForPathWriteIfError(path, false);

            if (result == null)
            {
                return;
            }

            // Write out the result

            WriteRegistryItemObject(result, path);
        }

        /// <summary>
        /// Sets registry values at <paramref name="path "/> to the <paramref name="value"/> specified.
        /// </summary>
        /// <param name="path">
        /// The path to the item that is to be set. Only registry values can be set using
        /// this method.
        /// </param>
        /// <param name="value">
        /// The new value for the registry value.
        /// </param>
        protected override void SetItem(string path, object value)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            // Confirm the set item with the user

            string action = RegistryProviderStrings.SetItemAction;

            string resourceTemplate = RegistryProviderStrings.SetItemResourceTemplate;

            string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    path,
                    value);

            if (ShouldProcess(resource, action))
            {
                // Get the registry item

                IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, true);

                if (key == null)
                {
                    return;
                }

                // Check to see if the type was specified by the user

                bool valueSet = false;
                if (DynamicParameters != null)
                {
                    RegistryProviderSetItemDynamicParameter dynParams =
                        DynamicParameters as RegistryProviderSetItemDynamicParameter;

                    if (dynParams != null)
                    {
                        try
                        {
                            // Convert the parameter to a RegistryValueKind

                            RegistryValueKind kind = dynParams.Type;

                            key.SetValue(null, value, kind);
                            valueSet = true;
                        }
                        catch (ArgumentException argException)
                        {
                            WriteError(new ErrorRecord(argException, argException.GetType().FullName, ErrorCategory.InvalidArgument, null));
                            key.Close();
                            return;
                        }
                        catch (System.IO.IOException ioException)
                        {
                            // An exception occurred while trying to get the key. Write
                            // out the error.

                            WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
                            key.Close();
                            return;
                        }
                        catch (System.Security.SecurityException securityException)
                        {
                            // An exception occurred while trying to get the key. Write
                            // out the error.

                            WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                            key.Close();
                            return;
                        }
                        catch (System.UnauthorizedAccessException unauthorizedAccessException)
                        {
                            // An exception occurred while trying to get the key. Write
                            // out the error.
                            WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                            key.Close();
                            return;
                        }
                    }
                }

                if (!valueSet)
                {
                    try
                    {
                        // Set the value
                        key.SetValue(null, value);
                    }
                    catch (System.IO.IOException ioException)
                    {
                        // An exception occurred while trying to get the key. Write
                        // out the error.

                        WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
                        key.Close();
                        return;
                    }
                    catch (System.Security.SecurityException securityException)
                    {
                        // An exception occurred while trying to get the key. Write
                        // out the error.

                        WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                        key.Close();
                        return;
                    }
                    catch (System.UnauthorizedAccessException unauthorizedAccessException)
                    {
                        // An exception occurred while trying to get the key. Write
                        // out the error.

                        WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                        key.Close();
                        return;
                    }
                }

                // Write out the result
                object result = value;

                // Since SetValue can munge the data to a specified
                // type (RegistryValueKind), retrieve the value again
                // to output it in the correct form to the user.
                result = ReadExistingKeyValue(key, null);
                key.Close();

                WriteItemObject(result, path, false);
            }
        }

        /// <summary>
        /// Gets the dynamic parameters for the SetItem method.
        /// </summary>
        /// <param name="path">
        /// Ignored.
        /// </param>
        /// <param name="value">
        /// Ignored.
        /// </param>
        /// <returns>
        /// An instance of the <see cref="Microsoft.PowerShell.Commands.RegistryProviderSetItemDynamicParameter"/> class which
        /// contains a parameter for the Type.
        /// </returns>
        protected override object SetItemDynamicParameters(string path, object value)
        {
            return new RegistryProviderSetItemDynamicParameter();
        }

        /// <summary>
        /// Clears the item at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the item that is to be cleared. Only registry values can be cleared using
        /// this method.
        /// </param>
        /// <remarks>
        /// The registry provider implements this by removing all the values for the specified key.
        /// The item that is cleared is written to the WriteObject method.
        /// If the path is to a value, then an ArgumentException is written.
        /// </remarks>
        protected override void ClearItem(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            // Confirm the clear item with the user

            string action = RegistryProviderStrings.ClearItemAction;

            string resourceTemplate = RegistryProviderStrings.ClearItemResourceTemplate;

            string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    path);

            if (ShouldProcess(resource, action))
            {
                // Get the registry item

                IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, true);

                if (key == null)
                {
                    return;
                }

                string[] valueNames;

                try
                {
                    // Remove each value
                    valueNames = key.GetValueNames();
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.ReadError, path));
                    return;
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }

                for (int index = 0; index < valueNames.Length; ++index)
                {
                    try
                    {
                        key.DeleteValue(valueNames[index]);
                    }
                    catch (System.IO.IOException ioException)
                    {
                        // An exception occurred while trying to delete the value. Write
                        // out the error.

                        WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
                    }
                    catch (System.Security.SecurityException securityException)
                    {
                        // An exception occurred while trying to delete the value. Write
                        // out the error.

                        WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    }
                    catch (System.UnauthorizedAccessException unauthorizedAccessException)
                    {
                        // An exception occurred while trying to get the key. Write
                        // out the error.

                        WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    }
                }

                // Write out the key

                WriteRegistryItemObject(key, path);
            }
        }

        #endregion ItemCmdletProvider overrides

        #region ContainerCmdletProvider overrides

        /// <summary>
        /// Gets all the child keys and values of the key at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the key to get the child keys of.
        /// </param>/
        /// <param name="recurse">
        /// Determines if the call should be recursive. If true, all subkeys of
        /// the key at the specified path will be written. If false, only the
        /// immediate children of the key at the specified path will be written.
        /// </param>
        /// <param name="depth">
        /// Current depth of recursion; special case uint.MaxValue performs full recursion.
        /// </param>
        protected override void GetChildItems(
            string path,
            bool recurse,
            uint depth)
        {
            s_tracer.WriteLine("recurse = {0}, depth = {1}", recurse, depth);

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (IsHiveContainer(path))
            {
                // If the path is empty or it is / or \, return all the hives

                foreach (string hiveName in s_hiveNames)
                {
                    // Making sure to obey the StopProcessing.
                    if (Stopping)
                    {
                        return;
                    }

                    GetItem(hiveName);
                }
            }
            else
            {
                // Get the key at the specified path

                IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, false);

                if (key == null)
                {
                    return;
                }

                try
                {
                    // Get all the subkeys of the specified path

                    string[] keyNames = key.GetSubKeyNames();
                    key.Close();

                    if (keyNames != null)
                    {
                        foreach (string subkeyName in keyNames)
                        {
                            // Making sure to obey the StopProcessing.
                            if (Stopping)
                            {
                                return;
                            }

                            if (!string.IsNullOrEmpty(subkeyName))
                            {
                                string keypath = path;

                                try
                                {
                                    // Generate the path for each key name

                                    keypath = MakePath(path, subkeyName, childIsLeaf: true);

                                    if (!string.IsNullOrEmpty(keypath))
                                    {
                                        // Call GetItem to retrieve the RegistryKey object
                                        // and write it to the WriteObject method.

                                        IRegistryWrapper resultKey = GetRegkeyForPath(keypath, false);

                                        if (resultKey != null)
                                        {
                                            WriteRegistryItemObject(resultKey, keypath);
                                        }

                                        // Now recurse if necessary

                                        if (recurse)
                                        {
                                            // Limiter for recursion
                                            if (depth > 0) // this includes special case 'depth == uint.MaxValue' for unlimited recursion
                                            {
                                                GetChildItems(keypath, recurse, depth - 1);
                                            }
                                        }
                                    }
                                }
                                catch (System.IO.IOException ioException)
                                {
                                    // An exception occurred while trying to get the key. Write
                                    // out the error.

                                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.ReadError, keypath));
                                }
                                catch (System.Security.SecurityException securityException)
                                {
                                    // An exception occurred while trying to get the key. Write
                                    // out the error.

                                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, keypath));
                                }
                                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                                {
                                    // An exception occurred while trying to get the key. Write
                                    // out the error.

                                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, keypath));
                                }
                            }
                        }
                    }
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.ReadError, path));
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
            }
        }

        /// <summary>
        /// Gets all the child key and value names of the key at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the key to get the child names from.
        /// </param>
        /// <param name="returnContainers">
        /// Ignored since the registry provider does not implement filtering.
        /// Normally, if this parameter is ReturnAllContainers then all subkeys should be
        /// returned. If it is false, then only those subkeys that match the
        /// filter should be returned.
        /// </param>
        protected override void GetChildNames(
            string path,
            ReturnContainers returnContainers)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (path.Length == 0)
            {
                // If the path is empty get the names of the hives

                foreach (string hiveName in s_hiveNames)
                {
                    // Making sure to obey the StopProcessing.
                    if (Stopping)
                    {
                        return;
                    }

                    WriteItemObject(hiveName, hiveName, true);
                }
            }
            else
            {
                // Get the key at the specified path

                IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, false);

                if (key == null)
                {
                    return;
                }

                try
                {
                    // Get the child key names

                    string[] results = key.GetSubKeyNames();
                    key.Close();

                    // Write the child key names to the WriteItemObject method

                    for (int index = 0; index < results.Length; ++index)
                    {
                        // Making sure to obey the StopProcessing.
                        if (Stopping)
                        {
                            return;
                        }

                        string childName = EscapeChildName(results[index]);
                        string childPath = MakePath(path, childName, childIsLeaf: true);

                        WriteItemObject(childName, childPath, true);
                    }
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.ReadError, path));
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
            }
        }

        private const string charactersThatNeedEscaping = ".*?[]:";

        /// <summary>
        /// Escapes the characters in the registry key path that are used by globbing and
        /// path.
        /// </summary>
        /// <param name="path">
        /// The path to escape.
        /// </param>
        /// <returns>
        /// The escaped path.
        /// </returns>
        /// <remarks>
        /// This method handles surrogate pairs. Please see msdn documentation
        /// </remarks>
        private static string EscapeSpecialChars(string path)
        {
            StringBuilder result = new StringBuilder();

            // Get the text enumerator..this will iterate through each character
            // the character can be a surrogate pair
            System.Globalization.TextElementEnumerator textEnumerator =
                System.Globalization.StringInfo.GetTextElementEnumerator(path);

            Dbg.Diagnostics.Assert(
                textEnumerator != null,
                string.Create(CultureInfo.CurrentCulture, $"Cannot get a text enumerator for name {path}"));

            while (textEnumerator.MoveNext())
            {
                // Iterate through each element and findout whether
                // any text needs escaping
                string textElement = textEnumerator.GetTextElement();

                // NTRAID#Windows Out of Band Releases-939036-2006/07/12-LeeHolm
                // A single character can never contain a string of
                // charactersThatNeedEscaping, so this method does nothing.  The fix
                // is to remove all calls to this escaping code, though, as this escaping
                // should not be done.
                if (textElement.Contains(charactersThatNeedEscaping))
                {
                    // This text element needs escaping
                    result.Append('`');
                }

                result.Append(textElement);
            }

            return result.ToString();
        }

        /// <summary>
        /// Escapes the characters in the registry key name that are used by globbing and
        /// path.
        /// </summary>
        /// <param name="name">
        /// The name to escape.
        /// </param>
        /// <returns>
        /// The escaped name.
        /// </returns>
        /// <remarks>
        /// This method handles surrogate pairs. Please see msdn documentation
        /// </remarks>
        private static string EscapeChildName(string name)
        {
            StringBuilder result = new StringBuilder();

            // Get the text enumerator..this will iterate through each character
            // the character can be a surrogate pair
            System.Globalization.TextElementEnumerator textEnumerator =
                System.Globalization.StringInfo.GetTextElementEnumerator(name);

            Dbg.Diagnostics.Assert(
                textEnumerator != null,
                string.Create(CultureInfo.CurrentCulture, $"Cannot get a text enumerator for name {name}"));

            while (textEnumerator.MoveNext())
            {
                // Iterate through each element and findout whether
                // any text needs escaping
                string textElement = textEnumerator.GetTextElement();

                // NTRAID#Windows Out of Band Releases-939036-2006/07/12-LeeHolm
                // A single character can never contain a string of
                // charactersThatNeedEscaping, so this method does nothing.  The fix
                // is to remove all calls to this escaping code, though, as this escaping
                // should not be done.
                if (textElement.Contains(charactersThatNeedEscaping))
                {
                    // This text element needs escaping
                    result.Append('`');
                }

                result.Append(textElement);
            }

            return result.ToString();
        }

        /// <summary>
        /// Renames the key at the specified <paramref name="path"/> to <paramref name="newName"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the key to rename.
        /// </param>
        /// <param name="newName">
        /// The new name of the key.
        /// </param>
        protected override void RenameItem(
            string path,
            string newName)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (string.IsNullOrEmpty(newName))
            {
                throw PSTraceSource.NewArgumentException(nameof(newName));
            }

            s_tracer.WriteLine("newName = {0}", newName);

            string parentPath = GetParentPath(path, null);
            string newPath = MakePath(parentPath, newName);

            // Make sure we aren't going to overwrite an existing item

            bool exists = ItemExists(newPath);

            if (exists)
            {
                Exception e = new ArgumentException(RegistryProviderStrings.RenameItemAlreadyExists);
                WriteError(new ErrorRecord(
                    e,
                    e.GetType().FullName,
                    ErrorCategory.InvalidArgument,
                    newPath));

                return;
            }
            // Confirm the rename item with the user

            string action = RegistryProviderStrings.RenameItemAction;

            string resourceTemplate = RegistryProviderStrings.RenameItemResourceTemplate;

            string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    path,
                    newPath);

            if (ShouldProcess(resource, action))
            {
                // Implement rename as a move operation

                MoveRegistryItem(path, newPath);
            }
        }

        /// <summary>
        /// Creates a new registry key or value at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the new key to create.
        /// </param>
        /// <param name="type">
        /// The type is ignored because this provider only creates
        /// registry keys.
        /// </param>
        /// <param name="newItem">
        /// The newItem is ignored because the provider creates the
        /// key based on the path.
        /// </param>
        protected override void NewItem(
            string path,
            string type,
            object newItem)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            // Confirm the new item with the user

            string action = RegistryProviderStrings.NewItemAction;

            string resourceTemplate = RegistryProviderStrings.NewItemResourceTemplate;

            string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    path);
            if (ShouldProcess(resource, action))
            {
                // Check to see if the key already exists
                IRegistryWrapper resultKey = GetRegkeyForPath(path, false);

                if (resultKey != null)
                {
                    if (!Force)
                    {
                        Exception e = new System.IO.IOException(RegistryProviderStrings.KeyAlreadyExists);
                        WriteError(new ErrorRecord(
                            e,
                            e.GetType().FullName,
                            ErrorCategory.ResourceExists,
                            resultKey));

                        resultKey.Close();
                        return;
                    }
                    else
                    {
                        // Remove the existing key before creating the new one
                        resultKey.Close();
                        RemoveItem(path, false);
                    }
                }

                if (Force)
                {
                    if (!CreateIntermediateKeys(path))
                    {
                        // We are unable to create Intermediate keys. Just return.
                        return;
                    }
                }

                // Get the parent and child portions of the path

                string parentPath = GetParentPath(path, null);
                string childName = GetChildName(path);

                // Get the key at the specified path
                IRegistryWrapper key = GetRegkeyForPathWriteIfError(parentPath, true);

                if (key == null)
                {
                    return;
                }

                try
                {
                    // Create the new subkey
                    IRegistryWrapper newKey = key.CreateSubKey(childName);
                    key.Close();

                    try
                    {
                        // Set the default key value if the value and type were specified

                        if (newItem != null)
                        {
                            RegistryValueKind kind;
                            if (!ParseKind(type, out kind))
                            {
                                return;
                            }

                            SetRegistryValue(newKey, string.Empty, newItem, kind, path, false);
                        }
                    }
                    catch (Exception exception)
                    {
                        // The key has been created, but the default value failed to be set.
                        // If possible, just write an error instead of failing the entire operation.

                        if ((exception is ArgumentException) ||
                            (exception is InvalidCastException) ||
                            (exception is System.IO.IOException) ||
                            (exception is System.Security.SecurityException) ||
                            (exception is System.UnauthorizedAccessException) ||
                            (exception is NotSupportedException))
                        {
                            ErrorRecord rec = new ErrorRecord(
                                exception,
                                exception.GetType().FullName,
                                ErrorCategory.WriteError,
                                newKey);
                            rec.ErrorDetails = new ErrorDetails(StringUtil.Format(RegistryProviderStrings.KeyCreatedValueFailed, childName));
                            WriteError(rec);
                        }
                        else
                            throw;
                    }

                    // Write the new key out.
                    WriteRegistryItemObject(newKey, path);
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
                catch (ArgumentException argException)
                {
                    WriteError(new ErrorRecord(argException, argException.GetType().FullName, ErrorCategory.InvalidArgument, path));
                }
                catch (NotSupportedException notSupportedException)
                {
                    WriteError(new ErrorRecord(notSupportedException, notSupportedException.GetType().FullName, ErrorCategory.InvalidOperation, path));
                }
            }
        }

        /// <summary>
        /// Removes the specified registry key and all sub-keys.
        /// </summary>
        /// <param name="path">
        /// The path to the key to remove.
        /// </param>
        /// <param name="recurse">
        /// Ignored. All removes are recursive because the
        /// registry provider does not support filters.
        /// </param>
        protected override void RemoveItem(
            string path,
            bool recurse)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            s_tracer.WriteLine("recurse = {0}", recurse);

            // Get the parent and child portions of the path

            string parentPath = GetParentPath(path, null);
            string childName = GetChildName(path);

            // Get the parent key

            IRegistryWrapper key = GetRegkeyForPathWriteIfError(parentPath, true);

            if (key == null)
            {
                return;
            }

            // Confirm the remove item with the user

            string action = RegistryProviderStrings.RemoveKeyAction;

            string resourceTemplate = RegistryProviderStrings.RemoveKeyResourceTemplate;

            string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path);

            if (ShouldProcess(resource, action))
            {
                try
                {
                    key.DeleteSubKeyTree(childName);
                }
                catch (ArgumentException argumentException)
                {
                    WriteError(new ErrorRecord(argumentException, argumentException.GetType().FullName, ErrorCategory.WriteError, path));
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
                catch (NotSupportedException notSupportedException)
                {
                    WriteError(new ErrorRecord(notSupportedException, notSupportedException.GetType().FullName, ErrorCategory.InvalidOperation, path));
                }
            }

            key.Close();
        }

        /// <summary>
        /// Determines if the key at the specified path exists.
        /// </summary>
        /// <param name="path">
        /// The path to the key to determine if it exists.
        /// </param>
        /// <returns>
        /// True if the key at the specified path exists, false otherwise.
        /// </returns>
        protected override bool ItemExists(string path)
        {
            bool result = false;

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            try
            {
                if (IsHiveContainer(path))
                {
                    // an empty path, \ or / are valid because
                    // we will enumerate all the hives
                    result = true;
                }
                else
                {
                    IRegistryWrapper key = GetRegkeyForPath(path, false);

                    if (key != null)
                    {
                        result = true;
                        key.Close();
                    }
                }
            }
            // Catch known non-terminating exceptions
            catch (System.IO.IOException)
            {
            }
            // In these cases, the item does exist
            catch (System.Security.SecurityException)
            {
                result = true;
            }
            catch (System.UnauthorizedAccessException)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Determines if the specified key has subkeys.
        /// </summary>
        /// <param name="path">
        /// The path to the key to determine if it has sub keys.
        /// </param>
        /// <returns>
        /// True if the specified key has subkeys, false otherwise.
        /// </returns>
        protected override bool HasChildItems(string path)
        {
            bool result = false;

            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            try
            {
                if (IsHiveContainer(path))
                {
                    // An empty path will enumerate the hives

                    result = s_hiveNames.Length > 0;
                }
                else
                {
                    IRegistryWrapper key = GetRegkeyForPath(path, false);

                    if (key != null)
                    {
                        result = key.SubKeyCount > 0;
                        key.Close();
                    }
                }
            }
            catch (System.IO.IOException)
            {
                result = false;
            }
            catch (System.Security.SecurityException)
            {
                result = false;
            }
            catch (System.UnauthorizedAccessException)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Copies the specified registry key to the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path of the registry key to copy.
        /// </param>
        /// <param name="destination">
        /// The path to copy the key to.
        /// </param>
        /// <param name="recurse">
        /// If true all subkeys should be copied. If false, only the
        /// specified key should be copied.
        /// </param>
        protected override void CopyItem(
            string path,
            string destination,
            bool recurse)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (string.IsNullOrEmpty(destination))
            {
                throw PSTraceSource.NewArgumentException(nameof(destination));
            }

            s_tracer.WriteLine("destination = {0}", destination);
            s_tracer.WriteLine("recurse = {0}", recurse);

            IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, false);

            if (key == null)
            {
                return;
            }

            try
            {
                CopyRegistryKey(key, path, destination, recurse, true, false);
            }
            catch (System.IO.IOException ioException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
            }
            catch (System.Security.SecurityException securityException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
            }
            catch (System.UnauthorizedAccessException unauthorizedAccessException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
            }

            key.Close();
        }

        private bool CopyRegistryKey(
            IRegistryWrapper key,
            string path,
            string destination,
            bool recurse,
            bool streamResult,
            bool streamFirstOnly)
        {
            bool result = true;

            // Make sure we are not trying to do a recursive copy of a key
            // to itself or a child of itself.

            if (recurse)
            {
                if (ErrorIfDestinationIsSourceOrChildOfSource(path, destination))
                {
                    return false;
                }
            }

            Dbg.Diagnostics.Assert(
                key != null,
                "The key should have been validated by the caller");

            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(path),
                "The path should have been validated by the caller");

            Dbg.Diagnostics.Assert(
                !string.IsNullOrEmpty(destination),
                "The destination should have been validated by the caller");

            s_tracer.WriteLine("destination = {0}", destination);

            // Get the parent key of the destination
            // If the destination already exists and is a key, then it becomes
            // the container of the source. If the key doesn't already exist
            // the parent of the destination path becomes the container of source.

            IRegistryWrapper newParentKey = GetRegkeyForPath(destination, true);
            string destinationName = GetChildName(path);
            string destinationParent = destination;

            if (newParentKey == null)
            {
                destinationParent = GetParentPath(destination, null);
                destinationName = GetChildName(destination);

                newParentKey = GetRegkeyForPathWriteIfError(destinationParent, true);
            }

            if (newParentKey == null)
            {
                // The key was not found.
                // An error should have been written by GetRegkeyForPathWriteIfError
                return false;
            }

            string destinationPath = MakePath(destinationParent, destinationName);

            // Confirm the copy item with the user

            string action = RegistryProviderStrings.CopyKeyAction;

            string resourceTemplate = RegistryProviderStrings.CopyKeyResourceTemplate;

            string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path,
                        destination);

            if (ShouldProcess(resource, action))
            {
                // Create new key under the parent

                IRegistryWrapper newKey = null;
                try
                {
                    newKey = newParentKey.CreateSubKey(destinationName);
                }
                catch (NotSupportedException e)
                {
                    WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.InvalidOperation, destinationName));
                }

                if (newKey != null)
                {
                    // Now copy all the properties from the source to the destination

                    string[] valueNames = key.GetValueNames();

                    for (int index = 0; index < valueNames.Length; ++index)
                    {
                        // Making sure to obey the StopProcessing.
                        if (Stopping)
                        {
                            newParentKey.Close();
                            newKey.Close();
                            return false;
                        }

                        newKey.SetValue(
                            valueNames[index],
                            key.GetValue(valueNames[index], null, RegistryValueOptions.DoNotExpandEnvironmentNames),
                            key.GetValueKind(valueNames[index]));
                    }

                    if (streamResult)
                    {
                        // Write out the key that was copied

                        WriteRegistryItemObject(newKey, destinationPath);

                        if (streamFirstOnly)
                        {
                            streamResult = false;
                        }
                    }
                }
            }

            newParentKey.Close();

            if (recurse)
            {
                // Copy all the subkeys

                string[] subkeyNames = key.GetSubKeyNames();

                for (int keyIndex = 0; keyIndex < subkeyNames.Length; ++keyIndex)
                {
                    // Making sure to obey the StopProcessing.
                    if (Stopping)
                    {
                        return false;
                    }

                    // Make the new path under the copy path.

                    string subKeyPath = MakePath(path, subkeyNames[keyIndex]);
                    string newSubKeyPath = MakePath(destinationPath, subkeyNames[keyIndex]);

                    IRegistryWrapper childKey = GetRegkeyForPath(subKeyPath, false);

                    bool subtreeResult = CopyRegistryKey(childKey, subKeyPath, newSubKeyPath, recurse, streamResult, streamFirstOnly);

                    childKey.Close();

                    if (!subtreeResult)
                    {
                        result = subtreeResult;
                    }
                }
            }

            return result;
        }

        private bool ErrorIfDestinationIsSourceOrChildOfSource(
            string sourcePath,
            string destinationPath)
        {
            s_tracer.WriteLine("destinationPath = {0}", destinationPath);

            // Note the paths have already been normalized so case-insensitive
            // comparisons should be sufficient

            bool result = false;

            while (true)
            {
                // See if the paths are equal

                if (string.Equals(
                        sourcePath,
                        destinationPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    break;
                }

                string newDestinationPath = GetParentPath(destinationPath, null);

                if (string.IsNullOrEmpty(newDestinationPath))
                {
                    // We reached the root so the destination must not be a child
                    // of the source
                    break;
                }

                if (string.Equals(
                        newDestinationPath,
                        destinationPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    // We reached the root so the destination must not be a child
                    // of the source
                    break;
                }

                destinationPath = newDestinationPath;
            }

            if (result)
            {
                Exception e =
                    new ArgumentException(
                        RegistryProviderStrings.DestinationChildOfSource);
                WriteError(new ErrorRecord(
                    e,
                    e.GetType().FullName,
                    ErrorCategory.InvalidArgument,
                    destinationPath));
            }

            return result;
        }

        #endregion ContainerCmdletProvider overrides

        #region NavigationCmdletProvider overrides

        /// <summary>
        /// Determines if the key at the specified <paramref name="path"/> is a container.
        /// </summary>
        /// <param name="path">
        /// The path to a key.
        /// </param>
        /// <returns>
        /// Since all registry keys are containers this method just checks
        /// to see if the key exists and returns true if it is does or
        /// false otherwise.
        /// </returns>
        protected override bool IsItemContainer(string path)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            bool result = false;

            if (IsHiveContainer(path))
            {
                result = true;
            }
            else
            {
                try
                {
                    IRegistryWrapper key = GetRegkeyForPath(path, false);

                    if (key != null)
                    {
                        // All registry keys can be containers. Values are considered
                        // properties
                        key.Close();
                        result = true;
                    }
                }
                // Catch known exceptions that are not terminating
                catch (System.IO.IOException ioException)
                {
                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.ReadError, path));
                }
                catch (System.Security.SecurityException securityException)
                {
                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
                catch (UnauthorizedAccessException unauthorizedAccess)
                {
                    WriteError(new ErrorRecord(unauthorizedAccess, unauthorizedAccess.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
            }

            return result;
        }

        /// <summary>
        /// Moves the specified key.
        /// </summary>
        /// <param name="path">
        /// The path of the key to move.
        /// </param>
        /// <param name="destination">
        /// The path to move the key to.
        /// </param>
        protected override void MoveItem(
            string path,
            string destination)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (string.IsNullOrEmpty(destination))
            {
                throw PSTraceSource.NewArgumentException(nameof(destination));
            }

            s_tracer.WriteLine("destination = {0}", destination);

            // Confirm the rename item with the user

            string action = RegistryProviderStrings.MoveItemAction;

            string resourceTemplate = RegistryProviderStrings.MoveItemResourceTemplate;

            string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    path,
                    destination);

            if (ShouldProcess(resource, action))
            {
                MoveRegistryItem(path, destination);
            }
        }

        private void MoveRegistryItem(string path, string destination)
        {
            // Implement move by copying the item and then removing it.
            // The copy will write the item to the pipeline

            IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, false);

            if (key == null)
            {
                return;
            }

            bool continueWithRemove = false;
            try
            {
                continueWithRemove = CopyRegistryKey(key, path, destination, true, true, true);
            }
            catch (System.IO.IOException ioException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
                key.Close();
                return;
            }
            catch (System.Security.SecurityException securityException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                key.Close();
                return;
            }
            catch (System.UnauthorizedAccessException unauthorizedAccessException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                key.Close();
                return;
            }

            key.Close();

            string sourceParent = GetParentPath(path, null);

            // If the destination is the same container as the source container don't do remove
            // the source item because the source and destination are the same.

            if (string.Equals(sourceParent, destination, StringComparison.OrdinalIgnoreCase))
            {
                continueWithRemove = false;
            }

            if (continueWithRemove)
            {
                try
                {
                    RemoveItem(path, true);
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
                    return;
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                    return;
                }
            }
        }

        #endregion NavigationCmdletProvider overrides

        #region IPropertyCmdletProvider

        /// <summary>
        /// Gets the properties of the item specified by the <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the item to retrieve properties from.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of properties that should be retrieved. If this parameter is null
        /// or empty, all properties should be retrieved.
        /// </param>
        /// <returns>
        /// Nothing. An instance of PSObject representing the properties that were retrieved
        /// should be passed to the WriteObject() method.
        /// </returns>
        public void GetProperty(
            string path,
            Collection<string> providerSpecificPickList)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (!CheckOperationNotAllowedOnHiveContainer(path))
            {
                return;
            }

            // get a set of matching properties on the key itself
            IRegistryWrapper key;
            Collection<string> filteredPropertyCollection;
            GetFilteredRegistryKeyProperties(path,
                                            providerSpecificPickList,
                                            true,
                                            false,
                                            out key,
                                            out filteredPropertyCollection);
            if (key == null)
            {
                return;
            }

            bool valueAdded = false;
            PSObject propertyResults = new PSObject();
            foreach (string valueName in filteredPropertyCollection)
            {
                string notePropertyName = valueName;
                if (string.IsNullOrEmpty(valueName))
                {
                    // If the value name is empty then using "(default)"
                    // as the property name when adding the note, as
                    // PSObject does not allow an empty propertyName

                    notePropertyName = LocalizedDefaultToken;
                }

                try
                {
                    propertyResults.Properties.Add(new PSNoteProperty(notePropertyName, key.GetValue(valueName)));
                    valueAdded = true;
                }
                catch (InvalidCastException invalidCast)
                {
                    WriteError(new ErrorRecord(
                        invalidCast,
                        invalidCast.GetType().FullName,
                        ErrorCategory.WriteError, 
                        path));
                }
            }

            key.Close();

            if (valueAdded)
            {
                WritePropertyObject(propertyResults, path);
            }
        }

        /// <summary>
        /// Sets the specified properties of the item at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the item to set the properties on.
        /// </param>
        /// <param name="propertyValue">
        /// A PSObject which contains a collection of the name, type, value
        /// of the properties to be set.
        /// </param>
        /// <returns>
        /// Nothing. An instance of PSObject representing the properties that were set
        /// should be passed to the WriteObject() method.
        /// </returns>
        public void SetProperty(
            string path,
            PSObject propertyValue)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (!CheckOperationNotAllowedOnHiveContainer(path))
            {
                return;
            }

            if (propertyValue == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(propertyValue));
            }

            IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, true);

            if (key == null)
            {
                return;
            }

            RegistryValueKind kind = RegistryValueKind.Unknown;

            // Get the kind of the value using the dynamic parameters

            if (DynamicParameters != null)
            {
                RegistryProviderSetItemDynamicParameter dynParams =
                    DynamicParameters as RegistryProviderSetItemDynamicParameter;

                if (dynParams != null)
                {
                    kind = dynParams.Type;
                }
            }

            string action = RegistryProviderStrings.SetPropertyAction;

            string resourceTemplate = RegistryProviderStrings.SetPropertyResourceTemplate;

            foreach (PSMemberInfo property in propertyValue.Properties)
            {
                object newPropertyValue = property.Value;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path,
                        property.Name);

                if (ShouldProcess(resource, action))
                {
                    try
                    {
                        SetRegistryValue(key, property.Name, newPropertyValue, kind, path);
                    }
                    catch (InvalidCastException invalidCast)
                    {
                        WriteError(new ErrorRecord(invalidCast, invalidCast.GetType().FullName, ErrorCategory.WriteError, path));
                    }
                    catch (System.IO.IOException ioException)
                    {
                        // An exception occurred while trying to set the value. Write
                        // out the error.

                        WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, property.Name));
                    }
                    catch (System.Security.SecurityException securityException)
                    {
                        // An exception occurred while trying to set the value. Write
                        // out the error.

                        WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, property.Name));
                    }
                    catch (System.UnauthorizedAccessException unauthorizedAccessException)
                    {
                        // An exception occurred while trying to get the key. Write
                        // out the error.

                        WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, property.Name));
                    }
                }
            }

            key.Close();
        }

        /// <summary>
        /// Gives the provider a chance to attach additional parameters to the
        /// get-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="propertyValue">
        /// A PSObject which contains a collection of the name, type, value
        /// of the properties to be set.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        public object SetPropertyDynamicParameters(
            string path,
            PSObject propertyValue)
        {
            return new RegistryProviderSetItemDynamicParameter();
        }

        /// <summary>
        /// Clears a property of the item at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the item on which to clear the property.
        /// </param>
        /// <param name="propertyToClear">
        /// The name of the property to clear.
        /// </param>
        public void ClearProperty(
            string path,
            Collection<string> propertyToClear)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (!CheckOperationNotAllowedOnHiveContainer(path))
            {
                return;
            }

            // get a set of matching properties on the key itself
            IRegistryWrapper key;
            Collection<string> filteredPropertyCollection;
            GetFilteredRegistryKeyProperties(path,
                                            propertyToClear,
                                            false,
                                            true,
                                            out key,
                                            out filteredPropertyCollection);
            if (key == null)
            {
                return;
            }

            string action = RegistryProviderStrings.ClearPropertyAction;

            string resourceTemplate = RegistryProviderStrings.ClearPropertyResourceTemplate;

            bool addedOnce = false;
            PSObject result = new PSObject();

            foreach (string valueName in filteredPropertyCollection)
            {
                string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    path,
                    valueName);

                if (ShouldProcess(resource, action))
                {
                    // reset the value of the property to its default value
                    object defaultValue = ResetRegistryKeyValue(key, valueName);
                    string propertyNameToAdd = valueName;
                    if (string.IsNullOrEmpty(valueName))
                    {
                        propertyNameToAdd = LocalizedDefaultToken;
                    }

                    result.Properties.Add(new PSNoteProperty(propertyNameToAdd, defaultValue));
                    addedOnce = true;
                }
            }

            key.Close();

            if (addedOnce)
            {
                WritePropertyObject(result, path);
            }
        }

        #region Unimplemented methods

        /// <summary>
        /// Gives the provider a chance to attach additional parameters to the
        /// get-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="providerSpecificPickList">
        /// A list of properties that should be retrieved. If this parameter is null
        /// or empty, all properties should be retrieved.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        public object GetPropertyDynamicParameters(
            string path,
            Collection<string> providerSpecificPickList)
        {
            return null;
        }

        /// <summary>
        /// Gives the provider a chance to attach additional parameters to the
        /// clear-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="propertyToClear">
        /// The name of the property to clear.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        public object ClearPropertyDynamicParameters(
            string path,
            Collection<string> propertyToClear)
        {
            return null;
        }
        #endregion Unimplemented methods

        #endregion IPropertyCmdletProvider

        #region IDynamicPropertyCmdletProvider

        /// <summary>
        /// Creates a new property on the specified item.
        /// </summary>
        /// <param name="path">
        /// The path to the item on which the new property should be created.
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
        /// <returns>
        /// Nothing. A PSObject representing the property that was created should
        /// be passed to the WriteObject() method.
        /// </returns>
        /// <!--
        /// Implement this method when you are providing access to a data store
        /// that allows dynamic creation of properties.
        /// -->
        public void NewProperty(
            string path,
            string propertyName,
            string type,
            object value)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (!CheckOperationNotAllowedOnHiveContainer(path))
            {
                return;
            }

            IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, true);

            if (key == null)
            {
                return;
            }

            // Confirm the set item with the user

            string action = RegistryProviderStrings.NewPropertyAction;

            string resourceTemplate = RegistryProviderStrings.NewPropertyResourceTemplate;

            string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    path,
                    propertyName);

            if (ShouldProcess(resource, action))
            {
                // convert the type to a RegistryValueKind
                RegistryValueKind kind;
                if (!ParseKind(type, out kind))
                {
                    key.Close();
                    return;
                }

                try
                {
                    // Check to see if the property already exists
                    // or overwrite if frce is on
                    if (Force || key.GetValue(propertyName) == null)
                    {
                        // Create the value
                        SetRegistryValue(key, propertyName, value, kind, path);
                    }
                    else
                    {
                        // The property already exists

                        System.IO.IOException e =
                            new System.IO.IOException(
                                RegistryProviderStrings.PropertyAlreadyExists);
                        WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.ResourceExists, path));
                        key.Close();
                        return;
                    }
                }
                catch (ArgumentException argumentException)
                {
                    WriteError(new ErrorRecord(argumentException, argumentException.GetType().FullName, ErrorCategory.WriteError, path));
                }
                catch (InvalidCastException invalidCast)
                {
                    WriteError(new ErrorRecord(invalidCast, invalidCast.GetType().FullName, ErrorCategory.WriteError, path));
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
            }

            key.Close();
        }

        /// <summary>
        /// Removes a property on the item specified by the path.
        /// </summary>
        /// <param name="path">
        /// The path to the item on which the property should be removed.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property to be removed.
        /// </param>
        /// <remarks>
        /// Implement this method when you are providing access to a data store
        /// that allows dynamic removal of properties.
        /// </remarks>
        public void RemoveProperty(
            string path,
            string propertyName)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (!CheckOperationNotAllowedOnHiveContainer(path))
            {
                return;
            }

            IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, true);

            if (key == null)
            {
                return;
            }

            WildcardPattern propertyNamePattern =
                WildcardPattern.Get(propertyName, WildcardOptions.IgnoreCase);

            bool hadAMatch = false;

            foreach (string valueName in key.GetValueNames())
            {
                if (
                    ((!Context.SuppressWildcardExpansion) && (!propertyNamePattern.IsMatch(valueName))) ||
                    (Context.SuppressWildcardExpansion && (!string.Equals(valueName, propertyName, StringComparison.OrdinalIgnoreCase))))
                {
                    continue;
                }

                hadAMatch = true;
                // Confirm the set item with the user

                string action = RegistryProviderStrings.RemovePropertyAction;

                string resourceTemplate = RegistryProviderStrings.RemovePropertyResourceTemplate;

                string resource =
                    string.Format(
                        Host.CurrentCulture,
                        resourceTemplate,
                        path,
                        valueName);

                if (ShouldProcess(resource, action))
                {
                    string propertyNameToRemove = GetPropertyName(valueName);

                    try
                    {
                        // Remove the value
                        key.DeleteValue(propertyNameToRemove);
                    }
                    catch (System.IO.IOException ioException)
                    {
                        // An exception occurred while trying to get the key. Write
                        // out the error.

                        WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, propertyNameToRemove));
                    }
                    catch (System.Security.SecurityException securityException)
                    {
                        // An exception occurred while trying to get the key. Write
                        // out the error.

                        WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, propertyNameToRemove));
                    }
                    catch (System.UnauthorizedAccessException unauthorizedAccessException)
                    {
                        // An exception occurred while trying to get the key. Write
                        // out the error.

                        WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, propertyNameToRemove));
                    }
                }
            }

            key.Close();
            WriteErrorIfPerfectMatchNotFound(hadAMatch, path, propertyName);
        }

        /// <summary>
        /// Renames a property of the item at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to the item on which to rename the property.
        /// </param>
        /// <param name="sourceProperty">
        /// The property to rename.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject that represents the property that was renamed should be
        /// passed to the WriteObject() method.
        /// </returns>
        public void RenameProperty(
            string path,
            string sourceProperty,
            string destinationProperty)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (!CheckOperationNotAllowedOnHiveContainer(path))
            {
                return;
            }

            IRegistryWrapper key = GetRegkeyForPathWriteIfError(path, true);

            if (key == null)
            {
                return;
            }

            // Confirm the set item with the user

            string action = RegistryProviderStrings.RenamePropertyAction;

            string resourceTemplate = RegistryProviderStrings.RenamePropertyResourceTemplate;

            string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    path,
                    sourceProperty,
                    destinationProperty);

            if (ShouldProcess(resource, action))
            {
                try
                {
                    MoveProperty(key, key, sourceProperty, destinationProperty);
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, path));
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                }
            }

            key.Close();
        }

        /// <summary>
        /// Copies a property of the item at the specified <paramref name="path"/> to a new property on the
        /// destination <paramref name="path"/>.
        /// </summary>
        /// <param name="sourcePath">
        /// The path to the item on which to copy the property.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to copy.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item on which to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The destination property to copy to.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject that represents the property that was copied should be
        /// passed to the WriteObject() method.
        /// </returns>
        public void CopyProperty(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty)
        {
            if (sourcePath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePath));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (!CheckOperationNotAllowedOnHiveContainer(sourcePath, destinationPath))
            {
                return;
            }

            IRegistryWrapper key = GetRegkeyForPathWriteIfError(sourcePath, false);

            if (key == null)
            {
                return;
            }

            IRegistryWrapper destinationKey = GetRegkeyForPathWriteIfError(destinationPath, true);
            if (destinationKey == null)
            {
                return;
            }

            // Confirm the set item with the user

            string action = RegistryProviderStrings.CopyPropertyAction;

            string resourceTemplate = RegistryProviderStrings.CopyPropertyResourceTemplate;

            string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    sourcePath,
                    sourceProperty,
                    destinationPath,
                    destinationProperty);

            if (ShouldProcess(resource, action))
            {
                try
                {
                    CopyProperty(key, destinationKey, sourceProperty, destinationProperty, true);
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, sourcePath));
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, sourcePath));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, sourcePath));
                }
            }

            key.Close();
        }

        /// <summary>
        /// Moves a property on an item specified by <paramref name="sourcePath"/>.
        /// </summary>
        /// <param name="sourcePath">
        /// The path to the item on which to move the property.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to move.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item on which to move the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The destination property to move to.
        /// </param>
        /// <returns>
        /// Nothing. A PSObject that represents the property that was moved should be
        /// passed to the WriteObject() method.
        /// </returns>
        public void MoveProperty(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty)
        {
            if (sourcePath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(sourcePath));
            }

            if (destinationPath == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(destinationPath));
            }

            if (!CheckOperationNotAllowedOnHiveContainer(sourcePath, destinationPath))
            {
                return;
            }

            IRegistryWrapper key = GetRegkeyForPathWriteIfError(sourcePath, true);

            if (key == null)
            {
                return;
            }

            IRegistryWrapper destinationKey = GetRegkeyForPathWriteIfError(destinationPath, true);
            if (destinationKey == null)
            {
                return;
            }

            // Confirm the set item with the user

            string action = RegistryProviderStrings.MovePropertyAction;

            string resourceTemplate = RegistryProviderStrings.MovePropertyResourceTemplate;

            string resource =
                string.Format(
                    Host.CurrentCulture,
                    resourceTemplate,
                    sourcePath,
                    sourceProperty,
                    destinationPath,
                    destinationProperty);

            if (ShouldProcess(resource, action))
            {
                try
                {
                    MoveProperty(key, destinationKey, sourceProperty, destinationProperty);
                }
                catch (System.IO.IOException ioException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, sourcePath));
                }
                catch (System.Security.SecurityException securityException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, sourcePath));
                }
                catch (System.UnauthorizedAccessException unauthorizedAccessException)
                {
                    // An exception occurred while trying to get the key. Write
                    // out the error.

                    WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, sourcePath));
                }
            }

            key.Close();
            destinationKey.Close();
        }

        /// <summary>
        /// Gets the parent path of the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to get the parent of.
        /// </param>
        /// <param name="root">
        /// The root of the drive.
        /// </param>
        /// <returns>
        /// The parent path of the given path.
        /// </returns>
        /// <remarks>
        /// Since the base class implementation of GetParentPath of HKLM:\foo would return
        /// HKLM: we must add the \ back on.
        /// </remarks>
        protected override string GetParentPath(string path, string root)
        {
            string parentPath = base.GetParentPath(path, root);

            // If the main path existed, we must do a semantic analysis
            // to find the parent -- since path elements may contain
            // path delimiters. We only need to do this comparison
            // if the base implementation returns something in our namespace.
            if (!string.Equals(parentPath, root, StringComparison.OrdinalIgnoreCase))
            {
                bool originalPathExists = ItemExists(path);
                bool originalPathExistsWithRoot = false;

                // This is an expensive test, only do it if we need to.
                if (!originalPathExists)
                    originalPathExistsWithRoot = ItemExists(MakePath(root, path));

                if ((!string.IsNullOrEmpty(parentPath)) && (originalPathExists || originalPathExistsWithRoot))
                {
                    string parentPathToTest = parentPath;

                    do
                    {
                        parentPathToTest = parentPath;
                        if (originalPathExistsWithRoot)
                            parentPathToTest = MakePath(root, parentPath);

                        if (ItemExists(parentPathToTest))
                            break;

                        parentPath = base.GetParentPath(parentPath, root);
                    } while (!string.IsNullOrEmpty(parentPath));
                }
            }

            return EnsureDriveIsRooted(parentPath);
        }

        /// <summary>
        /// Gets the child name for the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">
        /// The path to get the leaf element of.
        /// </param>
        /// <returns>
        /// The leaf element of the given path.
        /// </returns>
        /// <remarks>
        /// Since the base class implementation of GetChildName will return
        /// normalized paths (with \), we must change them to forward slashes..
        /// </remarks>
        protected override string GetChildName(string path)
        {
            string childName = base.GetChildName(path);
            return childName.Replace('\\', '/');
        }

        private static string EnsureDriveIsRooted(string path)
        {
            string result = path;

            // Find the drive separator

            int index = path.IndexOf(':');

            if (index != -1)
            {
                // if the drive separator is the end of the path, add
                // the root path separator back

                if (index + 1 == path.Length)
                {
                    result = path + StringLiterals.DefaultPathSeparator;
                }
            }

            return result;
        }

        #region Unimplemented methods

        /// <summary>
        /// Gives the provider a chance to attach additional parameters to the
        /// new-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
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
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        public object NewPropertyDynamicParameters(
            string path,
            string propertyName,
            string type,
            object value)
        {
            return null;
        }

        /// <summary>
        /// Gives the provider a chance to attach additional parameters to the
        /// remove-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="propertyName">
        /// The name of the property that should be removed.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        public object RemovePropertyDynamicParameters(
            string path,
            string propertyName)
        {
            return null;
        }

        /// <summary>
        /// Gives the provider a chance to attach additional parameters to the
        /// rename-itemproperty cmdlet.
        /// </summary>
        /// <param name="path">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="sourceProperty">
        /// The property to rename.
        /// </param>
        /// <param name="destinationProperty">
        /// The new name of the property.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        public object RenamePropertyDynamicParameters(
            string path,
            string sourceProperty,
            string destinationProperty)
        {
            return null;
        }

        /// <summary>
        /// Gives the provider a chance to attach additional parameters to the
        /// copy-itemproperty cmdlet.
        /// </summary>
        /// <param name="sourcePath">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to copy.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item on which to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The destination property to copy to.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        public object CopyPropertyDynamicParameters(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty)
        {
            return null;
        }

        /// <summary>
        /// Gives the provider a chance to attach additional parameters to the
        /// move-itemproperty cmdlet.
        /// </summary>
        /// <param name="sourcePath">
        /// If the path was specified on the command line, this is the path
        /// to the item to get the dynamic parameters for.
        /// </param>
        /// <param name="sourceProperty">
        /// The name of the property to copy.
        /// </param>
        /// <param name="destinationPath">
        /// The path to the item on which to copy the property to.
        /// </param>
        /// <param name="destinationProperty">
        /// The destination property to copy to.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        public object MovePropertyDynamicParameters(
            string sourcePath,
            string sourceProperty,
            string destinationPath,
            string destinationProperty)
        {
            return null;
        }

        #endregion Unimplemented methods

        #endregion IDynamicPropertyCmdletProvider

        #region Private members

        private void CopyProperty(
            IRegistryWrapper sourceKey,
            IRegistryWrapper destinationKey,
            string sourceProperty,
            string destinationProperty,
            bool writeOnSuccess)
        {
            string realSourceProperty = GetPropertyName(sourceProperty);
            string realDestinationProperty = GetPropertyName(destinationProperty);

            object sourceValue = sourceKey.GetValue(sourceProperty);
            RegistryValueKind sourceKind = sourceKey.GetValueKind(sourceProperty);

            destinationKey.SetValue(destinationProperty, sourceValue, sourceKind);

            if (writeOnSuccess)
            {
                WriteWrappedPropertyObject(sourceValue, realSourceProperty, sourceKey.Name);
            }
        }

        private void MoveProperty(
            IRegistryWrapper sourceKey,
            IRegistryWrapper destinationKey,
            string sourceProperty,
            string destinationProperty)
        {
            string realSourceProperty = GetPropertyName(sourceProperty);
            string realDestinationProperty = GetPropertyName(destinationProperty);

            try
            {
                // If sourceProperty and destinationProperty happens to be the same
                // then we shouldn't remove the property
                bool continueWithRemove = true;

                if (string.Equals(sourceKey.Name, destinationKey.Name, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(realSourceProperty, realDestinationProperty, StringComparison.OrdinalIgnoreCase))
                {
                    continueWithRemove = false;
                }

                // Move is implemented by copying the value and then deleting the original
                // Copy property will throw an exception if it fails

                CopyProperty(
                    sourceKey,
                    destinationKey,
                    realSourceProperty,
                    realDestinationProperty,
                    false);

                // Delete sourceproperty only if it is not same as destination property
                if (continueWithRemove)
                {
                    sourceKey.DeleteValue(realSourceProperty);
                }

                object newValue = destinationKey.GetValue(realDestinationProperty);
                WriteWrappedPropertyObject(newValue, destinationProperty, destinationKey.Name);
            }
            catch (System.IO.IOException ioException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, sourceKey.Name));
                return;
            }
            catch (System.Security.SecurityException securityException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, sourceKey.Name));
                return;
            }
            catch (System.UnauthorizedAccessException unauthorizedAccessException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, sourceKey.Name));
                return;
            }
        }

        /// <summary>
        /// Converts all / in the path to \
        /// </summary>
        /// <param name="path">
        /// The path to normalize.
        /// </param>
        /// <returns>
        /// The path with all / normalized to \
        /// </returns>
        private string NormalizePath(string path)
        {
            string result = path;

            if (!string.IsNullOrEmpty(path))
            {
                result = path.Replace(StringLiterals.AlternatePathSeparator, StringLiterals.DefaultPathSeparator);

                // Remove relative path tokens
                if (HasRelativePathTokens(path))
                {
                    result = NormalizeRelativePath(result, null);
                }
            }

            return result;
        }

        private static bool HasRelativePathTokens(string path)
        {
            return (
                path.StartsWith('\\') ||
                path.Contains("\\.\\") ||
                path.Contains("\\..\\") ||
                path.EndsWith("\\..", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("\\.", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("..\\", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(".\\", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith('~'));
        }

        private void GetFilteredRegistryKeyProperties(string path,
                                                                    Collection<string> propertyNames,
                                                                    bool getAll,
                                                                    bool writeAccess,
                                                                    out IRegistryWrapper key,
                                                                    out Collection<string> filteredCollection)
        {
            bool expandAll = false;

            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            filteredCollection = new Collection<string>();
            key = GetRegkeyForPathWriteIfError(path, writeAccess);

            if (key == null)
            {
                return;
            }

            // If properties were not specified, get all the values

            propertyNames ??= new Collection<string>();

            if (propertyNames.Count == 0 && getAll)
            {
                propertyNames.Add("*");
                expandAll = true;
            }

            string[] valueNames;
            try
            {
                valueNames = key.GetValueNames();
            }
            catch (System.IO.IOException ioException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.ReadError, path));
                return;
            }
            catch (System.Security.SecurityException securityException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                return;
            }
            catch (System.UnauthorizedAccessException unauthorizedAccessException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                return;
            }

            foreach (string requestedValueName in propertyNames)
            {
                WildcardPattern valueNameMatcher =
                    WildcardPattern.Get(
                        requestedValueName,

                        WildcardOptions.IgnoreCase);

                bool hadAMatch = false;

                foreach (string valueName in valueNames)
                {
                    string valueNameToMatch = valueName;

                    // Need to convert the default value name to "(default)"
                    if (string.IsNullOrEmpty(valueName))
                    {
                        // Only do the conversion if the caller isn't asking for
                        // "" or null.

                        if (!string.IsNullOrEmpty(requestedValueName))
                        {
                            valueNameToMatch = LocalizedDefaultToken;
                        }
                    }

                    if (
                        expandAll ||
                        ((!Context.SuppressWildcardExpansion) && (valueNameMatcher.IsMatch(valueNameToMatch))) ||
                       ((Context.SuppressWildcardExpansion) && (string.Equals(valueNameToMatch, requestedValueName, StringComparison.OrdinalIgnoreCase))))
                    {
                        if (string.IsNullOrEmpty(valueNameToMatch))
                        {
                            // If the value name is empty then using "(default)"
                            // as the property name when adding the note, as
                            // PSObject does not allow an empty propertyName

                            valueNameToMatch = LocalizedDefaultToken;
                        }

                        hadAMatch = true;
                        filteredCollection.Add(valueName);
                    }
                }

                WriteErrorIfPerfectMatchNotFound(hadAMatch, path, requestedValueName);
            }
        }

        private void WriteErrorIfPerfectMatchNotFound(bool hadAMatch, string path, string requestedValueName)
        {
            if (!hadAMatch && !WildcardPattern.ContainsWildcardCharacters(requestedValueName))
            {
                // we did not have any match and the requested name did not have
                // any globbing characters (perfect match attempted)
                // we need to write an error

                string formatString = RegistryProviderStrings.PropertyNotAtPath;
                Exception e =
                    new PSArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            formatString,
                            requestedValueName,
                            path),
                        (Exception)null);
                WriteError(new ErrorRecord(
                    e,
                    e.GetType().FullName,
                    ErrorCategory.InvalidArgument,
                    requestedValueName));
            }
        }

        /// <summary>
        /// IT resets the a registry key value to its default.
        /// </summary>
        /// <param name="key">Key whose value has to be reset.</param>
        /// <param name="valueName">Name of the value to reset.</param>
        /// <returns>Default value the key was set to.</returns>
        private object ResetRegistryKeyValue(IRegistryWrapper key, string valueName)
        {
            RegistryValueKind valueKind = key.GetValueKind(valueName);
            object defaultValue = null;

            switch (valueKind)
            {
                // NOTICE: we assume that an unknown type is treated as
                // the same as a binary blob
                case RegistryValueKind.Binary:
                case RegistryValueKind.Unknown:
                    {
                        defaultValue = Array.Empty<byte>();
                    }

                    break;
                case RegistryValueKind.DWord:
                    {
                        defaultValue = (int)0;
                    }

                    break;
                case RegistryValueKind.ExpandString:
                case RegistryValueKind.String:
                    {
                        defaultValue = string.Empty;
                    }

                    break;
                case RegistryValueKind.MultiString:
                    {
                        defaultValue = Array.Empty<string>();
                    }

                    break;
                case RegistryValueKind.QWord:
                    {
                        defaultValue = (long)0;
                    }

                    break;
            }

            try
            {
                key.SetValue(valueName, defaultValue, valueKind);
            }
            catch (System.IO.IOException ioException)
            {
                // An exception occurred while trying to set the value. Write
                // out the error.

                WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.WriteError, valueName));
            }
            catch (System.Security.SecurityException securityException)
            {
                // An exception occurred while trying to set the value. Write
                // out the error.

                WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, valueName));
            }
            catch (System.UnauthorizedAccessException unauthorizedAccessException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, valueName));
            }

            return defaultValue;
        }

        /// <summary>
        /// Checks if the given path is the top container path (the one containing the hives)
        /// </summary>
        /// <param name="path">
        /// path to check
        /// </param>
        /// <returns>
        /// true if the path is empty, a \ or a /, else false
        /// </returns>
        private static bool IsHiveContainer(string path)
        {
            bool result = false;
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (string.IsNullOrEmpty(path) ||
                string.Equals(path, "\\", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "/", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Checks the container. if the container is the hive container (Registry::\)
        /// it throws an exception.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>False if the operation is not allowed.</returns>
        private bool CheckOperationNotAllowedOnHiveContainer(string path)
        {
            if (IsHiveContainer(path))
            {
                string message = RegistryProviderStrings.ContainerInvalidOperationTemplate;

                InvalidOperationException ex = new InvalidOperationException(message);
                WriteError(new ErrorRecord(ex, "InvalidContainer", ErrorCategory.InvalidArgument, path));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks the container. if the container is the hive container (Registry::\)
        /// it throws an exception.
        /// </summary>
        /// <param name="sourcePath">Source path to check.</param>
        /// <param name="destinationPath">Destination path to check.</param>
        private bool CheckOperationNotAllowedOnHiveContainer(string sourcePath, string destinationPath)
        {
            if (IsHiveContainer(sourcePath))
            {
                string message = RegistryProviderStrings.SourceContainerInvalidOperationTemplate;
                InvalidOperationException ex = new InvalidOperationException(message);
                WriteError(new ErrorRecord(ex, "InvalidContainer", ErrorCategory.InvalidArgument, sourcePath));
                return false;
            }

            if (IsHiveContainer(destinationPath))
            {
                string message =
                RegistryProviderStrings.DestinationContainerInvalidOperationTemplate;
                InvalidOperationException ex = new InvalidOperationException(message);
                WriteError(new ErrorRecord(ex, "InvalidContainer", ErrorCategory.InvalidArgument, destinationPath));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the appropriate hive root name for the specified path.
        /// </summary>
        /// <param name="path">
        /// The path to get the hive root name from.
        /// </param>
        /// <returns>
        /// A registry key for the hive root specified by the path.
        /// </returns>
        private IRegistryWrapper GetHiveRoot(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            if (TransactionAvailable())
            {
                for (int k = 0; k < s_wellKnownHivesTx.Length; k++)
                {
                    if (string.Equals(path, s_hiveNames[k], StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(path, s_hiveShortNames[k], StringComparison.OrdinalIgnoreCase))
                    {
                        using (CurrentPSTransaction)
                        {
                            return new TransactedRegistryWrapper(s_wellKnownHivesTx[k], this);
                        }
                    }
                }
            }
            else
            {
                for (int k = 0; k < s_wellKnownHives.Length; k++)
                {
                    if (string.Equals(path, s_hiveNames[k], StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(path, s_hiveShortNames[k], StringComparison.OrdinalIgnoreCase))
                        return new RegistryWrapper(s_wellKnownHives[k]);
                }
            }

            return null;
        }

        /// <summary>
        /// Creates the parent for the keypath specified by <paramref name="path"/>.
        /// </summary>
        /// <param name="path">RegistryKey path.</param>
        /// <returns>
        /// True if key is created or already exist,False otherwise.
        /// </returns>
        /// <remarks>
        /// This method wont call ShouldProcess. Callers should do this before
        /// calling this method.
        /// </remarks>
        private bool CreateIntermediateKeys(string path)
        {
            bool result = false;

            // Check input.
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            try
            {
                // 1. Normalize path ( for "//","." etc )
                // 2. Open the root
                // 3. Create subkey

                path = NormalizePath(path);

                int index = path.IndexOf('\\');
                if (index == 0)
                {
                    // The user may precede a path with \
                    path = path.Substring(1);
                    index = path.IndexOf('\\');
                }

                if (index == -1)
                {
                    // we are at root..there is no subkey to create
                    // just return

                    return true;
                }

                string keyRoot = path.Substring(0, index);

                // NormalizePath will trim "\" at the end. So there is always something
                // after index. Asserting just in case..
                Dbg.Diagnostics.Assert(index + 1 < path.Length, "Bad path");
                string remainingPath = path.Substring(index + 1);

                IRegistryWrapper rootKey = GetHiveRoot(keyRoot);

                if (remainingPath.Length == 0 || rootKey == null)
                {
                    throw PSTraceSource.NewArgumentException(nameof(path));
                }

                // Create new subkey..and close
                IRegistryWrapper subKey = rootKey.CreateSubKey(remainingPath);

                if (subKey != null)
                {
                    subKey.Close();
                }
                else
                {
                    // SubKey is null
                    // Unable to create intermediate keys
                    throw PSTraceSource.NewArgumentException(nameof(path));
                }

                result = true;
            }
            catch (ArgumentException argumentException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.
                WriteError(new ErrorRecord(argumentException, argumentException.GetType().FullName, ErrorCategory.OpenError, path));
                return result;
            }
            catch (System.IO.IOException ioException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.OpenError, path));
                return result;
            }
            catch (System.Security.SecurityException securityException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                return result;
            }
            catch (System.UnauthorizedAccessException unauthorizedAccessException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                return result;
            }
            catch (NotSupportedException notSupportedException)
            {
                WriteError(new ErrorRecord(notSupportedException, notSupportedException.GetType().FullName, ErrorCategory.InvalidOperation, path));
            }

            return result;
        }

        /// <summary>
        /// A private helper method that retrieves a RegistryKey for the specified
        /// path and if an exception is thrown retrieving the key, an error is written
        /// and null is returned.
        /// </summary>
        /// <param name="path">
        /// The path to the registry key to retrieve.
        /// </param>
        /// <param name="writeAccess">
        /// If write access is required the key then this should be true. If false,
        /// the key will be opened with read access only.
        /// </param>
        /// <returns>
        /// The RegistryKey associated with the specified path.
        /// </returns>
        private IRegistryWrapper GetRegkeyForPathWriteIfError(string path, bool writeAccess)
        {
            IRegistryWrapper result = null;
            try
            {
                result = GetRegkeyForPath(path, writeAccess);

                if (result == null)
                {
                    // The key was not found, write out an error.

                    ArgumentException exception =
                        new ArgumentException(
                        RegistryProviderStrings.KeyDoesNotExist);
                    WriteError(new ErrorRecord(exception, exception.GetType().FullName, ErrorCategory.InvalidArgument, path));

                    return null;
                }
            }
            catch (ArgumentException argumentException)
            {
                WriteError(new ErrorRecord(argumentException, argumentException.GetType().FullName, ErrorCategory.OpenError, path));
                return result;
            }
            catch (System.IO.IOException ioException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(ioException, ioException.GetType().FullName, ErrorCategory.OpenError, path));
                return result;
            }
            catch (System.Security.SecurityException securityException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(securityException, securityException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                return result;
            }
            catch (System.UnauthorizedAccessException unauthorizedAccessException)
            {
                // An exception occurred while trying to get the key. Write
                // out the error.

                WriteError(new ErrorRecord(unauthorizedAccessException, unauthorizedAccessException.GetType().FullName, ErrorCategory.PermissionDenied, path));
                return result;
            }

            return result;
        }

        /// <summary>
        /// A private helper method that retrieves a RegistryKey for the specified
        /// path.
        /// </summary>
        /// <param name="path">
        /// The path to the registry key to retrieve.
        /// </param>
        /// <param name="writeAccess">
        /// If write access is required the key then this should be true. If false,
        /// the key will be opened with read access only.
        /// </param>
        /// <returns>
        /// The RegistryKey associated with the specified path.
        /// </returns>
        private IRegistryWrapper GetRegkeyForPath(string path, bool writeAccess)
        {
            if (string.IsNullOrEmpty(path))
            {
                // The key was not found, write out an error.

                ArgumentException exception =
                    new ArgumentException(
                    RegistryProviderStrings.KeyDoesNotExist);
                throw exception;
            }

            // Making sure to obey the StopProcessing.
            if (Stopping)
            {
                return null;
            }

            s_tracer.WriteLine("writeAccess = {0}", writeAccess);

            IRegistryWrapper result = null;

            do // false loop
            {
                int index = path.IndexOf('\\');

                if (index == 0)
                {
                    // The user may proceed a path with \

                    path = path.Substring(1);
                    index = path.IndexOf('\\');
                }

                if (index == -1)
                {
                    result = GetHiveRoot(path);
                    break;
                }

                string keyRoot = path.Substring(0, index);
                string remainingPath = path.Substring(index + 1);

                IRegistryWrapper resultRoot = GetHiveRoot(keyRoot);

                if (remainingPath.Length == 0 || resultRoot == null)
                {
                    result = resultRoot;
                    break;
                }

                try
                {
                    result = resultRoot.OpenSubKey(remainingPath, writeAccess);
                }
                catch (NotSupportedException e)
                {
                    WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.InvalidOperation, path));
                }

                // If we could not open the key, see if we can find the subkey that matches.
                if (result == null)
                {
                    IRegistryWrapper currentKey = resultRoot;
                    IRegistryWrapper tempKey = null;

                    // While there is still more to process
                    while (!string.IsNullOrEmpty(remainingPath))
                    {
                        bool foundSubkey = false;

                        foreach (string subKey in currentKey.GetSubKeyNames())
                        {
                            string normalizedSubkey = subKey;

                            // Check if the remaining path starts with the subkey name
                            if (!remainingPath.Equals(subKey, StringComparison.OrdinalIgnoreCase) &&
                                !remainingPath.StartsWith(subKey + StringLiterals.DefaultPathSeparator, StringComparison.OrdinalIgnoreCase))
                            {
                                // Actually normalize the subkey and then check again
                                normalizedSubkey = NormalizePath(subKey);

                                if (!remainingPath.Equals(normalizedSubkey, StringComparison.OrdinalIgnoreCase) &&
                                    !remainingPath.StartsWith(normalizedSubkey + StringLiterals.DefaultPathSeparator, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                            }

                            tempKey = currentKey.OpenSubKey(subKey, writeAccess);
                            currentKey.Close();
                            currentKey = tempKey;

                            foundSubkey = true;

                            remainingPath = remainingPath.Equals(normalizedSubkey, StringComparison.OrdinalIgnoreCase)
                                                ? string.Empty
                                                : remainingPath.Substring((normalizedSubkey + StringLiterals.DefaultPathSeparator).Length);

                            break;
                        }

                        if (!foundSubkey)
                        {
                            return null;
                        }
                    }

                    return currentKey;
                }
            } while (false);

            return result;
        }

        // NB: The HKEY_DYN_DATA hive is left out of the following lists because
        // it is only available on Win98/ME and we do not support that platform.

        private static readonly string[] s_hiveNames = new string[] {
            "HKEY_LOCAL_MACHINE",
            "HKEY_CURRENT_USER",
            "HKEY_CLASSES_ROOT",
            "HKEY_CURRENT_CONFIG",
            "HKEY_USERS",
            "HKEY_PERFORMANCE_DATA"
        };

        private static readonly string[] s_hiveShortNames = new string[] {
            "HKLM",
            "HKCU",
            "HKCR",
            "HKCC",
            "HKU",
            "HKPD"
        };

        private static readonly RegistryKey[] s_wellKnownHives = new RegistryKey[] {
            Registry.LocalMachine,
            Registry.CurrentUser,
            Registry.ClassesRoot,
            Registry.CurrentConfig,
            Registry.Users,
            Registry.PerformanceData
        };

        private static readonly TransactedRegistryKey[] s_wellKnownHivesTx = new TransactedRegistryKey[] {
            TransactedRegistry.LocalMachine,
            TransactedRegistry.CurrentUser,
            TransactedRegistry.ClassesRoot,
            TransactedRegistry.CurrentConfig,
            TransactedRegistry.Users
        };

        /// <summary>
        /// Sets or creates a registry value on a key.
        /// </summary>
        /// <param name="key">
        /// The key to set or create the value on.
        /// </param>
        /// <param name="propertyName">
        /// The name of the value to set or create.
        /// </param>
        /// <param name="value">
        /// The new data for the value.
        /// </param>
        /// <param name="kind">
        /// The RegistryValueKind of the value.
        /// </param>
        /// <param name="path">
        /// The path to the key that the value is being set on.
        /// </param>
        private void SetRegistryValue(IRegistryWrapper key, string propertyName, object value, RegistryValueKind kind, string path)
        {
            SetRegistryValue(key, propertyName, value, kind, path, true);
        }

        /// <summary>
        /// Sets or creates a registry value on a key.
        /// </summary>
        /// <param name="key">
        /// The key to set or create the value on.
        /// </param>
        /// <param name="propertyName">
        /// The name of the value to set or create.
        /// </param>
        /// <param name="value">
        /// The new data for the value.
        /// </param>
        /// <param name="kind">
        /// The RegistryValueKind of the value.
        /// </param>
        /// <param name="path">
        /// The path to the key that the value is being set on.
        /// </param>
        /// <param name="writeResult">
        /// If true, the value that is set will be written out.
        /// </param>
        private void SetRegistryValue(
            IRegistryWrapper key,
            string propertyName,
            object value,
            RegistryValueKind kind,
            string path,
            bool writeResult)
        {
            Dbg.Diagnostics.Assert(
                key != null,
                "Caller should have verified key");

            string propertyNameToSet = GetPropertyName(propertyName);

            RegistryValueKind existingKind = RegistryValueKind.Unknown;

            // If user does not specify a kind: get the valuekind if the property
            // already exists
            if (kind == RegistryValueKind.Unknown)
            {
                existingKind = GetValueKindForProperty(key, propertyNameToSet);
            }

            // try to do a conversion based on the existing kind, if we
            // were able to retrieve one
            if (existingKind != RegistryValueKind.Unknown)
            {
                try
                {
                    value = ConvertValueToKind(value, existingKind);
                    kind = existingKind;
                }
                catch (InvalidCastException)
                {
                    // failed attempt, we reset to unknown to let the
                    // default conversion process take over
                    existingKind = RegistryValueKind.Unknown;
                }
            }

            // set the kind as defined by the user
            if (existingKind == RegistryValueKind.Unknown)
            {
                // we use to kind passed in, either because we had
                // a valid one or because we failed to retrieve an existing kind to match
                if (kind == RegistryValueKind.Unknown)
                {
                    // set the kind based on value
                    if (value != null)
                    {
                        kind = GetValueKindFromObject(value);
                    }
                    else
                    {
                        // if no value and unknown kind, then default to empty string
                        kind = RegistryValueKind.String;
                    }
                }

                value = ConvertValueToKind(value, kind);
            }

            key.SetValue(propertyNameToSet, value, kind);

            if (writeResult)
            {
                // Now write out the value
                object newValue = key.GetValue(propertyNameToSet);

                WriteWrappedPropertyObject(newValue, propertyName, path);
            }
        }

        /// <summary>
        /// Helper to wrap property values when sent to the pipeline into an PSObject;
        /// it adds the name of the property as a note.
        /// </summary>
        /// <param name="value">The property to be written.</param>
        /// <param name="propertyName">Name of the property being written.</param>
        /// <param name="path">The path of the item being written.</param>
        private void WriteWrappedPropertyObject(object value, string propertyName, string path)
        {
            PSObject result = new PSObject();

            string propertyNameToAdd = propertyName;
            if (string.IsNullOrEmpty(propertyName))
            {
                propertyNameToAdd = LocalizedDefaultToken;
            }

            result.Properties.Add(new PSNoteProperty(propertyNameToAdd, value));

            WritePropertyObject(result, path);
        }

        /// <summary>
        /// Uses LanguagePrimitives.ConvertTo to convert the value to the type that is appropriate
        /// for the specified RegistryValueKind.
        /// </summary>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <param name="kind">
        /// The RegistryValueKind type to convert the value to.
        /// </param>
        /// <returns>
        /// The converted value.
        /// </returns>
        private static object ConvertValueToKind(object value, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.Binary:
                    value = (value != null)
                        ? (byte[])LanguagePrimitives.ConvertTo(
                            value,
                            typeof(byte[]),
                            CultureInfo.CurrentCulture)
                        : Array.Empty<byte>();
                    break;

                case RegistryValueKind.DWord:
                    {
                        if (value != null)
                        {
                            try
                            {
                                value = (int)LanguagePrimitives.ConvertTo(value, typeof(int), CultureInfo.CurrentCulture);
                            }
                            catch (PSInvalidCastException)
                            {
                                value = (UInt32)LanguagePrimitives.ConvertTo(value, typeof(UInt32), CultureInfo.CurrentCulture);
                            }
                        }
                        else
                        {
                            value = 0;
                        }
                    }

                    break;

                case RegistryValueKind.ExpandString:
                    value = (value != null)
                        ? (string)LanguagePrimitives.ConvertTo(
                            value,
                            typeof(string),
                            CultureInfo.CurrentCulture)
                        : string.Empty;
                    break;

                case RegistryValueKind.MultiString:
                    value = (value != null)
                        ? (string[])LanguagePrimitives.ConvertTo(
                            value,
                            typeof(string[]),
                            CultureInfo.CurrentCulture)
                        : Array.Empty<string>();
                    break;

                case RegistryValueKind.QWord:
                    {
                        if (value != null)
                        {
                            try
                            {
                                value = (long)LanguagePrimitives.ConvertTo(value, typeof(long), CultureInfo.CurrentCulture);
                            }
                            catch (PSInvalidCastException)
                            {
                                value = (UInt64)LanguagePrimitives.ConvertTo(value, typeof(UInt64), CultureInfo.CurrentCulture);
                            }
                        }
                        else
                        {
                            value = 0;
                        }
                    }

                    break;

                case RegistryValueKind.String:
                    value = (value != null)
                        ? (string)LanguagePrimitives.ConvertTo(
                            value,
                            typeof(string),
                            CultureInfo.CurrentCulture)
                        : string.Empty;
                    break;

                    // If kind is Unknown then just leave the value as-is.
            }

            return value;
        }

        /// <summary>
        /// Helper to infer the RegistryValueKind from an object.
        /// </summary>
        /// <param name="value">Object whose RegistryValueKind has to be determined.</param>
        /// <returns>Corresponding RegistryValueKind.</returns>
        private static RegistryValueKind GetValueKindFromObject(object value)
        {
            if (value == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(value));
            }

            RegistryValueKind result = RegistryValueKind.Unknown;

            Type valueType = value.GetType();
            if (valueType == typeof(byte[]))
            {
                result = RegistryValueKind.Binary;
            }
            else if (valueType == typeof(int))
            {
                result = RegistryValueKind.DWord;
            }

            if (valueType == typeof(string))
            {
                result = RegistryValueKind.String;
            }

            if (valueType == typeof(string[]))
            {
                result = RegistryValueKind.MultiString;
            }

            if (valueType == typeof(long))
            {
                result = RegistryValueKind.QWord;
            }

            return result;
        }

        /// <summary>
        /// Helper to get RegistryValueKind for a Property.
        /// </summary>
        /// <param name="key">RegistryKey containing property.</param>
        /// <param name="valueName">Property for which RegistryValueKind is requested.</param>
        /// <returns>RegistryValueKind of the property. If the property does not exit,returns RegistryValueKind.Unknown.</returns>
        private static RegistryValueKind GetValueKindForProperty(IRegistryWrapper key, string valueName)
        {
            try
            {
                return key.GetValueKind(valueName);
            }
            catch (System.ArgumentException)
            {
                // RegistryKey that contains the specified value does not exist
            }
            catch (System.IO.IOException)
            {
            }
            catch (System.Security.SecurityException)
            {
            }
            catch (System.UnauthorizedAccessException)
            {
            }

            return RegistryValueKind.Unknown;
        }

        /// <summary>
        /// Helper to read back an existing registry key value.
        /// </summary>
        /// <param name="key">Key to read the value from.</param>
        /// <param name="valueName">Name of the value to read.</param>
        /// <returns>Value of the key, null if it could not retrieve
        /// it because known exceptions were thrown, else an exception is percolated up
        /// </returns>
        private static object ReadExistingKeyValue(IRegistryWrapper key, string valueName)
        {
            try
            {
                // Since SetValue can munge the data to a specified
                // type (RegistryValueKind), retrieve the value again
                // to output it in the correct form to the user.

                return key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            }
            catch (System.IO.IOException)
            {
            }
            catch (System.Security.SecurityException)
            {
            }
            catch (System.UnauthorizedAccessException)
            {
            }

            return null;
        }

        /// <summary>
        /// Wraps a registry item in a PSObject and sets the TreatAs to
        /// Microsoft.Win32.RegistryKey. This way values will be presented
        /// in the same format as keys.
        /// </summary>
        /// <param name="key">
        /// The registry key to be written out.
        /// </param>
        /// <param name="path">
        /// The path to the item being written out.
        /// </param>
        private void WriteRegistryItemObject(
            IRegistryWrapper key,
            string path)
        {
            if (key == null)
            {
                Dbg.Diagnostics.Assert(
                    key != null,
                    "The RegistryProvider should never attempt to write out a null value");

                // Don't error, but don't write out anything either.
                return;
            }

            // Escape any wildcard characters in the path
            path = EscapeSpecialChars(path);

            // Wrap the key in an PSObject
            PSObject outputObject = PSObject.AsPSObject(key.RegistryKey);

            // Add the registry values to the PSObject
            string[] valueNames = key.GetValueNames();

            for (int index = 0; index < valueNames.Length; ++index)
            {
                if (string.IsNullOrEmpty(valueNames[index]))
                {
                    // The first unnamed value becomes the default value
                    valueNames[index] = LocalizedDefaultToken;
                    break;
                }
            }

            outputObject.AddOrSetProperty("Property", valueNames);

            WriteItemObject(outputObject, path, true);
        }

        /// <summary>
        /// Takes a string and tries to parse it into a RegistryValueKind enum
        /// type.
        /// If the conversion fails, WriteError() is called.
        /// </summary>
        /// <param name="type">
        /// The type as specified by the user that should be parsed into a RegistryValueKind enum.
        /// </param>
        /// <param name="kind">Output for the RegistryValueKind for the string.</param>
        /// <returns>
        /// true if the conversion succeeded
        /// </returns>
        private bool ParseKind(string type, out RegistryValueKind kind)
        {
            kind = RegistryValueKind.Unknown;

            if (string.IsNullOrEmpty(type))
            {
                return true;
            }

            bool success = true;
            Exception innerException = null;
            try
            {
                // Convert the parameter to a RegistryValueKind
                kind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), type, true);
            }
            catch (InvalidCastException invalidCast)
            {
                innerException = invalidCast;
            }
            catch (ArgumentException argException)
            {
                innerException = argException;
            }

            if (innerException != null)
            {
                success = false;

                string formatString =
                    RegistryProviderStrings.TypeParameterBindingFailure;
                Exception e =
                    new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            formatString,
                            type,
                            typeof(RegistryValueKind).FullName),
                        innerException);
                WriteError(new ErrorRecord(
                    e,
                    e.GetType().FullName,
                    ErrorCategory.InvalidArgument,
                    type));
            }

            return success;
        }

        /// <summary>
        /// Gets the default value name token from the resource.
        /// In English that token is "(default)" without the quotes.
        /// </summary>
        /// <remarks>
        /// This should not be localized as it will break scripts.
        /// </remarks>
        /// <returns>
        /// A string containing the default value name.
        /// </returns>
        private static string LocalizedDefaultToken => "(default)";

        /// <summary>
        /// Converts an empty or null userEnteredPropertyName to the localized
        /// string for the default property name.
        /// </summary>
        /// <param name="userEnteredPropertyName">
        /// The property name to convert.
        /// </param>
        /// <returns>
        /// If userEnteredPropertyName is null or empty, the localized default
        /// property name is returned, else the userEnteredPropertyName is returned.
        /// </returns>
        private string GetPropertyName(string userEnteredPropertyName)
        {
            string result = userEnteredPropertyName;

            if (!string.IsNullOrEmpty(userEnteredPropertyName))
            {
                var stringComparer = Host.CurrentCulture.CompareInfo;

                if (stringComparer.Compare(
                        userEnteredPropertyName,
                        LocalizedDefaultToken,
                        CompareOptions.IgnoreCase) == 0)
                {
                    result = null;
                }
            }

            return result;
        }
        #endregion Private members
    }

    /// <summary>
    /// Defines dynamic parameters for the registry provider.
    /// </summary>
    public class RegistryProviderSetItemDynamicParameter
    {
        /// <summary>
        /// Gets or sets the Type parameter as a dynamic parameter for
        /// the registry provider's SetItem method.
        /// </summary>
        /// <remarks>
        /// The only acceptable values for this parameter are those found
        /// in the RegistryValueKind enum
        /// </remarks>
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public RegistryValueKind Type { get; set; } = RegistryValueKind.Unknown;
    }
}
#endif // !UNIX
