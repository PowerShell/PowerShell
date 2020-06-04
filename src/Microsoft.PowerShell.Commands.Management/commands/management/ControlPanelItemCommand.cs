// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;

using Microsoft.Win32;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// Represent a control panel item.
    /// </summary>
    public sealed class ControlPanelItem
    {
        /// <summary>
        /// Control panel applet name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Control panel applet canonical name.
        /// </summary>
        public string CanonicalName { get; }

        /// <summary>
        /// Control panel applet category.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Category { get; }

        /// <summary>
        /// Control panel applet description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Control panel applet path.
        /// </summary>
        internal string Path { get; }

        /// <summary>
        /// Internal constructor for ControlPanelItem.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="canonicalName"></param>
        /// <param name="category"></param>
        /// <param name="description"></param>
        /// <param name="path"></param>
        internal ControlPanelItem(string name, string canonicalName, string[] category, string description, string path)
        {
            Name = name;
            Path = path;
            CanonicalName = canonicalName;
            Category = category;
            Description = description;
        }

        /// <summary>
        /// ToString method.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Name;
        }
    }

    /// <summary>
    /// This class implements the base for ControlPanelItem commands.
    /// </summary>
    public abstract class ControlPanelItemBaseCommand : PSCmdlet
    {
        /// <summary>
        /// Locale specific verb action Open string exposed by the control panel item.
        /// </summary>
        private static string s_verbActionOpenName = null;

        /// <summary>
        /// Canonical name of the control panel item used as a reference to fetch the verb
        /// action Open string. This control panel item exists on all SKU's.
        /// </summary>
        private const string RegionCanonicalName = "Microsoft.RegionAndLanguage";

        private const string ControlPanelShellFolder = "shell:::{26EE0668-A00A-44D7-9371-BEB064C98683}";
        private static readonly string[] s_controlPanelItemFilterList = new string[] { "Folder Options", "Taskbar and Start Menu" };
        private const string TestHeadlessServerScript = @"
$result = $false
$serverManagerModule = Get-Module -ListAvailable | Where-Object {$_.Name -eq 'ServerManager'}
if ($serverManagerModule -ne $null)
{
    Import-Module ServerManager
    $Gui = (Get-WindowsFeature Server-Gui-Shell).Installed
    if ($Gui -eq $false)
    {
        $result = $true
    }
}
$result
";
        internal readonly Dictionary<string, string> CategoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal string[] CategoryNames = { "*" };
        internal string[] RegularNames = { "*" };
        internal string[] CanonicalNames = { "*" };
        internal ControlPanelItem[] ControlPanelItems = new ControlPanelItem[0];

        /// <summary>
        /// Get all executable control panel items.
        /// </summary>
        internal List<ShellFolderItem> AllControlPanelItems
        {
            get
            {
                if (_allControlPanelItems == null)
                {
                    _allControlPanelItems = new List<ShellFolderItem>();
                    string allItemFolderPath = ControlPanelShellFolder + "\\0";
                    IShellDispatch4 shell2 = (IShellDispatch4)new Shell();
                    Folder2 allItemFolder = (Folder2)shell2.NameSpace(allItemFolderPath);
                    FolderItems3 allItems = (FolderItems3)allItemFolder.Items();

                    bool applyControlPanelItemFilterList = IsServerCoreOrHeadLessServer();

                    foreach (ShellFolderItem item in allItems)
                    {
                        if (applyControlPanelItemFilterList)
                        {
                            bool match = false;
                            foreach (string name in s_controlPanelItemFilterList)
                            {
                                if (name.Equals(item.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    match = true;
                                    break;
                                }
                            }

                            if (match)
                                continue;
                        }

                        if (ContainVerbOpen(item))
                            _allControlPanelItems.Add(item);
                    }
                }

                return _allControlPanelItems;
            }
        }

        private List<ShellFolderItem> _allControlPanelItems;

        #region Cmdlet Overrides

        /// <summary>
        /// Does the preprocessing for ControlPanelItem cmdlets.
        /// </summary>
        protected override void BeginProcessing()
        {
            System.OperatingSystem osInfo = System.Environment.OSVersion;
            PlatformID platform = osInfo.Platform;
            Version version = osInfo.Version;

            if (platform.Equals(PlatformID.Win32NT) &&
                ((version.Major < 6) ||
                 ((version.Major == 6) && (version.Minor < 2))
                ))
            {
                // Below Win8, this cmdlet is not supported because of Win8:794135
                // throw terminating
                string message = string.Format(CultureInfo.InvariantCulture,
                                               ControlPanelResources.ControlPanelItemCmdletNotSupported,
                                               this.CommandInfo.Name);
                throw new PSNotSupportedException(message);
            }
        }

        #endregion

        /// <summary>
        /// Test if an item can be invoked.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ContainVerbOpen(ShellFolderItem item)
        {
            bool result = false;
            FolderItemVerbs verbs = item.Verbs();
            foreach (FolderItemVerb verb in verbs)
            {
                if (!string.IsNullOrEmpty(verb.Name) &&
                    (verb.Name.Equals(ControlPanelResources.VerbActionOpen, StringComparison.OrdinalIgnoreCase) ||
                     CompareVerbActionOpen(verb.Name)))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// CompareVerbActionOpen is a helper function used to perform locale specific
        /// comparison of the verb action Open exposed by various control panel items.
        /// </summary>
        /// <param name="verbActionName">Locale specific verb action exposed by the control panel item.</param>
        /// <returns>True if the control panel item supports verb action open or else returns false.</returns>
        private static bool CompareVerbActionOpen(string verbActionName)
        {
            if (s_verbActionOpenName == null)
            {
                const string allItemFolderPath = ControlPanelShellFolder + "\\0";
                IShellDispatch4 shell2 = (IShellDispatch4)new Shell();
                Folder2 allItemFolder = (Folder2)shell2.NameSpace(allItemFolderPath);
                FolderItems3 allItems = (FolderItems3)allItemFolder.Items();

                foreach (ShellFolderItem item in allItems)
                {
                    string canonicalName = (string)item.ExtendedProperty("System.ApplicationName");
                    canonicalName = !string.IsNullOrEmpty(canonicalName)
                                        ? canonicalName.Substring(0, canonicalName.IndexOf('\0'))
                                        : null;

                    if (canonicalName != null && canonicalName.Equals(RegionCanonicalName, StringComparison.OrdinalIgnoreCase))
                    {
                        // The 'Region' control panel item always has '&Open' (english or other locale) as the first verb name
                        s_verbActionOpenName = item.Verbs().Item(0).Name;
                        break;
                    }
                }

                Dbg.Assert(s_verbActionOpenName != null, "The 'Region' control panel item is available on all SKUs and it always "
                                                       + "has '&Open' as the first verb item, so VerbActionOpenName should never be null at this point");
            }

            return s_verbActionOpenName.Equals(verbActionName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// IsServerCoreORHeadLessServer is a helper function that checks if the current SKU is a
        /// Server Core machine or if the Server-GUI-Shell feature is removed on the machine.
        /// </summary>
        /// <returns>True if the current SKU is a Server Core machine or if the Server-GUI-Shell
        /// feature is removed on the machine or else returns false.</returns>
        private bool IsServerCoreOrHeadLessServer()
        {
            bool result = false;

            using (RegistryKey installation = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion"))
            {
                Dbg.Assert(installation != null, "the CurrentVersion subkey should exist");

                string installationType = (string)installation.GetValue("InstallationType", string.Empty);

                if (installationType.Equals("Server Core"))
                {
                    result = true;
                }
                else if (installationType.Equals("Server"))
                {
                    using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
                    {
                        ps.AddScript(TestHeadlessServerScript);
                        Collection<PSObject> psObjectCollection = ps.Invoke(Array.Empty<object>());
                        Dbg.Assert(psObjectCollection != null && psObjectCollection.Count == 1, "invoke should never return null, there should be only one return item");
                        if (LanguagePrimitives.IsTrue(PSObject.Base(psObjectCollection[0])))
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Get the category number and name map.
        /// </summary>
        internal void GetCategoryMap()
        {
            if (CategoryMap.Count != 0)
            {
                return;
            }

            IShellDispatch4 shell2 = (IShellDispatch4)new Shell();
            Folder2 categoryFolder = (Folder2)shell2.NameSpace(ControlPanelShellFolder);
            FolderItems3 catItems = (FolderItems3)categoryFolder.Items();

            foreach (ShellFolderItem category in catItems)
            {
                string path = category.Path;
                string catNum = path.Substring(path.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase) + 1);

                CategoryMap.Add(catNum, category.Name);
            }
        }

        /// <summary>
        /// Get control panel item by the category.
        /// </summary>
        /// <param name="controlPanelItems"></param>
        /// <returns></returns>
        internal List<ShellFolderItem> GetControlPanelItemByCategory(List<ShellFolderItem> controlPanelItems)
        {
            List<ShellFolderItem> list = new List<ShellFolderItem>();
            HashSet<string> itemSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string pattern in CategoryNames)
            {
                bool found = false;
                WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                foreach (ShellFolderItem item in controlPanelItems)
                {
                    string path = item.Path;
                    int[] categories = (int[])item.ExtendedProperty("System.ControlPanel.Category");
                    foreach (int cat in categories)
                    {
                        string catStr = (string)LanguagePrimitives.ConvertTo(cat, typeof(string), CultureInfo.InvariantCulture);
                        Dbg.Assert(CategoryMap.ContainsKey(catStr), "the category should be contained in _categoryMap");
                        string catName = CategoryMap[catStr];

                        if (!wildcard.IsMatch(catName))
                            continue;
                        if (itemSet.Contains(path))
                        {
                            found = true;
                            break;
                        }

                        found = true;
                        itemSet.Add(path);
                        list.Add(item);
                        break;
                    }
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    string errMsg = StringUtil.Format(ControlPanelResources.NoControlPanelItemFoundForGivenCategory, pattern);
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg),
                                                        "NoControlPanelItemFoundForGivenCategory",
                                                        ErrorCategory.InvalidArgument, pattern);
                    WriteError(error);
                }
            }

            return list;
        }

        /// <summary>
        /// Get control panel item by the regular name.
        /// </summary>
        /// <param name="controlPanelItems"></param>
        /// <param name="withCategoryFilter"></param>
        /// <returns></returns>
        internal List<ShellFolderItem> GetControlPanelItemByName(List<ShellFolderItem> controlPanelItems, bool withCategoryFilter)
        {
            List<ShellFolderItem> list = new List<ShellFolderItem>();
            HashSet<string> itemSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string pattern in RegularNames)
            {
                bool found = false;
                WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                foreach (ShellFolderItem item in controlPanelItems)
                {
                    string name = item.Name;
                    string path = item.Path;
                    if (!wildcard.IsMatch(name))
                        continue;
                    if (itemSet.Contains(path))
                    {
                        found = true;
                        continue;
                    }

                    found = true;
                    itemSet.Add(path);
                    list.Add(item);
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    string formatString = withCategoryFilter
                                              ? ControlPanelResources.NoControlPanelItemFoundForGivenNameWithCategory
                                              : ControlPanelResources.NoControlPanelItemFoundForGivenName;
                    string errMsg = StringUtil.Format(formatString, pattern);
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg),
                                                        "NoControlPanelItemFoundForGivenName",
                                                        ErrorCategory.InvalidArgument, pattern);
                    WriteError(error);
                }
            }

            return list;
        }

        /// <summary>
        /// Get control panel item by the canonical name.
        /// </summary>
        /// <param name="controlPanelItems"></param>
        /// <param name="withCategoryFilter"></param>
        /// <returns></returns>
        internal List<ShellFolderItem> GetControlPanelItemByCanonicalName(List<ShellFolderItem> controlPanelItems, bool withCategoryFilter)
        {
            List<ShellFolderItem> list = new List<ShellFolderItem>();
            HashSet<string> itemSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (CanonicalNames == null)
            {
                bool found = false;
                foreach (ShellFolderItem item in controlPanelItems)
                {
                    string canonicalName = (string)item.ExtendedProperty("System.ApplicationName");
                    if (canonicalName == null)
                    {
                        found = true;
                        list.Add(item);
                    }
                }

                if (!found)
                {
                    string errMsg = withCategoryFilter
                                        ? ControlPanelResources.NoControlPanelItemFoundWithNullCanonicalNameWithCategory
                                        : ControlPanelResources.NoControlPanelItemFoundWithNullCanonicalName;
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg), string.Empty,
                                                        ErrorCategory.InvalidArgument, CanonicalNames);
                    WriteError(error);
                }

                return list;
            }

            foreach (string pattern in CanonicalNames)
            {
                bool found = false;
                WildcardPattern wildcard = WildcardPattern.Get(pattern, WildcardOptions.IgnoreCase);
                foreach (ShellFolderItem item in controlPanelItems)
                {
                    string path = item.Path;
                    string canonicalName = (string)item.ExtendedProperty("System.ApplicationName");
                    canonicalName = canonicalName != null
                                        ? canonicalName.Substring(0, canonicalName.IndexOf('\0'))
                                        : null;

                    if (canonicalName == null)
                    {
                        if (pattern.Equals("*", StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            if (!itemSet.Contains(path))
                            {
                                itemSet.Add(path);
                                list.Add(item);
                            }
                        }
                    }
                    else
                    {
                        if (!wildcard.IsMatch(canonicalName))
                            continue;
                        if (itemSet.Contains(path))
                        {
                            found = true;
                            continue;
                        }

                        found = true;
                        itemSet.Add(path);
                        list.Add(item);
                    }
                }

                if (!found && !WildcardPattern.ContainsWildcardCharacters(pattern))
                {
                    string formatString = withCategoryFilter
                                              ? ControlPanelResources.NoControlPanelItemFoundForGivenCanonicalNameWithCategory
                                              : ControlPanelResources.NoControlPanelItemFoundForGivenCanonicalName;
                    string errMsg = StringUtil.Format(formatString, pattern);
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg),
                                                        "NoControlPanelItemFoundForGivenCanonicalName",
                                                        ErrorCategory.InvalidArgument, pattern);
                    WriteError(error);
                }
            }

            return list;
        }

        /// <summary>
        /// Get control panel item by the ControlPanelItem instances.
        /// </summary>
        /// <param name="controlPanelItems"></param>
        /// <returns></returns>
        internal List<ShellFolderItem> GetControlPanelItemsByInstance(List<ShellFolderItem> controlPanelItems)
        {
            List<ShellFolderItem> list = new List<ShellFolderItem>();
            HashSet<string> itemSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ControlPanelItem controlPanelItem in ControlPanelItems)
            {
                bool found = false;

                foreach (ShellFolderItem item in controlPanelItems)
                {
                    string path = item.Path;
                    if (!controlPanelItem.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (itemSet.Contains(path))
                    {
                        found = true;
                        break;
                    }

                    found = true;
                    itemSet.Add(path);
                    list.Add(item);
                    break;
                }

                if (!found)
                {
                    string errMsg = StringUtil.Format(ControlPanelResources.NoControlPanelItemFoundForGivenInstance,
                                                      controlPanelItem.GetType().Name);
                    ErrorRecord error = new ErrorRecord(new InvalidOperationException(errMsg),
                                                        "NoControlPanelItemFoundForGivenInstance",
                                                        ErrorCategory.InvalidArgument, controlPanelItem);
                    WriteError(error);
                }
            }

            return list;
        }
    }

    /// <summary>
    /// Get all control panel items that is available in the "All Control Panel Items" category.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ControlPanelItem", DefaultParameterSetName = RegularNameParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=219982")]
    [OutputType(typeof(ControlPanelItem))]
    public sealed class GetControlPanelItemCommand : ControlPanelItemBaseCommand
    {
        private const string RegularNameParameterSet = "RegularName";
        private const string CanonicalNameParameterSet = "CanonicalName";

        #region "Parameters"

        /// <summary>
        /// Control panel item names.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = RegularNameParameterSet, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get { return RegularNames; }

            set
            {
                RegularNames = value;
                _nameSpecified = true;
            }
        }

        private bool _nameSpecified = false;

        /// <summary>
        /// Canonical names of control panel items.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = CanonicalNameParameterSet)]
        [AllowNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CanonicalName
        {
            get { return CanonicalNames; }

            set
            {
                CanonicalNames = value;
                _canonicalNameSpecified = true;
            }
        }

        private bool _canonicalNameSpecified = false;

        /// <summary>
        /// Category of control panel items.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Category
        {
            get { return CategoryNames; }

            set
            {
                CategoryNames = value;
                _categorySpecified = true;
            }
        }

        private bool _categorySpecified = false;

        #endregion "Parameters"

        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            GetCategoryMap();
            List<ShellFolderItem> items = GetControlPanelItemByCategory(AllControlPanelItems);

            if (_nameSpecified)
            {
                items = GetControlPanelItemByName(items, _categorySpecified);
            }
            else if (_canonicalNameSpecified)
            {
                items = GetControlPanelItemByCanonicalName(items, _categorySpecified);
            }

            List<ControlPanelItem> results = new List<ControlPanelItem>();
            foreach (ShellFolderItem item in items)
            {
                string name = item.Name;
                string path = item.Path;
                string description = (string)item.ExtendedProperty("InfoTip");
                string canonicalName = (string)item.ExtendedProperty("System.ApplicationName");
                canonicalName = canonicalName != null
                                        ? canonicalName.Substring(0, canonicalName.IndexOf('\0'))
                                        : null;
                int[] categories = (int[])item.ExtendedProperty("System.ControlPanel.Category");
                string[] cateStrings = new string[categories.Length];
                for (int i = 0; i < categories.Length; i++)
                {
                    string catStr = (string)LanguagePrimitives.ConvertTo(categories[i], typeof(string), CultureInfo.InvariantCulture);
                    Dbg.Assert(CategoryMap.ContainsKey(catStr), "the category should be contained in CategoryMap");
                    cateStrings[i] = CategoryMap[catStr];
                }

                ControlPanelItem controlPanelItem = new ControlPanelItem(name, canonicalName, cateStrings, description, path);
                results.Add(controlPanelItem);
            }

            // Sort the results by Canonical Name
            results.Sort(CompareControlPanelItems);
            foreach (ControlPanelItem controlPanelItem in results)
            {
                WriteObject(controlPanelItem);
            }
        }

        #region "Private Methods"

        private static int CompareControlPanelItems(ControlPanelItem x, ControlPanelItem y)
        {
            // In the case that at least one of them is null
            if (x.CanonicalName == null && y.CanonicalName == null)
                return 0;
            if (x.CanonicalName == null)
                return 1;
            if (y.CanonicalName == null)
                return -1;

            // In the case that both are not null
            return string.Compare(x.CanonicalName, y.CanonicalName, StringComparison.OrdinalIgnoreCase);
        }

        #endregion "Private Methods"
    }

    /// <summary>
    /// Show the specified control panel applet.
    /// </summary>
    [Cmdlet(VerbsCommon.Show, "ControlPanelItem", DefaultParameterSetName = RegularNameParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=219983")]
    public sealed class ShowControlPanelItemCommand : ControlPanelItemBaseCommand
    {
        private const string RegularNameParameterSet = "RegularName";
        private const string CanonicalNameParameterSet = "CanonicalName";
        private const string ControlPanelItemParameterSet = "ControlPanelItem";

        #region "Parameters"

        /// <summary>
        /// Control panel item names.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = RegularNameParameterSet, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Name
        {
            get { return RegularNames; }

            set { RegularNames = value; }
        }

        /// <summary>
        /// Canonical names of control panel items.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = CanonicalNameParameterSet)]
        [AllowNull]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] CanonicalName
        {
            get { return CanonicalNames; }

            set { CanonicalNames = value; }
        }

        /// <summary>
        /// Control panel items returned by Get-ControlPanelItem.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = ControlPanelItemParameterSet, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ControlPanelItem[] InputObject
        {
            get { return ControlPanelItems; }

            set { ControlPanelItems = value; }
        }

        #endregion "Parameters"

        /// <summary>
        /// </summary>
        protected override void ProcessRecord()
        {
            List<ShellFolderItem> items;
            if (ParameterSetName == RegularNameParameterSet)
            {
                items = GetControlPanelItemByName(AllControlPanelItems, false);
            }
            else if (ParameterSetName == CanonicalNameParameterSet)
            {
                items = GetControlPanelItemByCanonicalName(AllControlPanelItems, false);
            }
            else
            {
                items = GetControlPanelItemsByInstance(AllControlPanelItems);
            }

            foreach (ShellFolderItem item in items)
            {
                item.InvokeVerb();
            }
        }
    }
}
