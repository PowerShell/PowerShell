// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Searcher class for finding PS classes on the system.
    /// </summary>
    internal class PSClassSearcher : IEnumerable<PSClassInfo>, IEnumerator<PSClassInfo>
    {
        internal PSClassSearcher(
            string className,
            bool useWildCards,
            ExecutionContext context)
        {
            Diagnostics.Assert(context != null, "caller to verify context is not null");
            _context = context;

            Diagnostics.Assert(className != null, "caller to verify className is not null");
            _className = className;
            _useWildCards = useWildCards;
            _moduleInfoCache = new Dictionary<string, PSModuleInfo>(StringComparer.OrdinalIgnoreCase);
        }

        #region private properties

        private readonly string _className = null;
        private readonly ExecutionContext _context = null;
        private PSClassInfo _currentMatch = null;
        private IEnumerator<PSClassInfo> _matchingClass = null;
        private Collection<PSClassInfo> _matchingClassList = null;
        private readonly bool _useWildCards = false;
        private readonly Dictionary<string, PSModuleInfo> _moduleInfoCache = null;
        private readonly object _lockObject = new object();

        #endregion

        #region public methods

        /// <summary>
        /// Reset the Iterator.
        /// </summary>
        public void Reset()
        {
            _currentMatch = null;
            _matchingClass = null;
        }

        /// <summary>
        /// Reset and dispose the Iterator.
        /// </summary>
        public void Dispose()
        {
            Reset();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get the Enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator<PSClassInfo> IEnumerable<PSClassInfo>.GetEnumerator()
        {
            return this;
        }

        /// <summary>
        /// Get the Enumerator.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        /// <summary>
        /// Move to the Next value in the enumerator.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            _currentMatch = GetNextClass();

            if (_currentMatch != null)
                return true;

            return false;
        }

        /// <summary>
        /// Return the current PSClassInfo.
        /// </summary>
        PSClassInfo IEnumerator<PSClassInfo>.Current
        {
            get
            {
                return _currentMatch;
            }
        }

        /// <summary>
        /// Return the current PSClassInfo as object.
        /// </summary>
        object IEnumerator.Current
        {
            get
            {
                return ((IEnumerator<PSClassInfo>)this).Current;
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Get all modules and find the matching type
        /// When found add them to the enumerator. If we have already got it, return the next resource.
        /// </summary>
        /// <returns>Next PSClassInfo object or null if none are found.</returns>
        private PSClassInfo GetNextClass()
        {
            PSClassInfo returnValue = null;
            WildcardPattern classNameMatcher = WildcardPattern.Get(_className, WildcardOptions.IgnoreCase);

            if (_matchingClassList == null)
            {
                _matchingClassList = new Collection<PSClassInfo>();

                if (FindTypeByModulePath(classNameMatcher))
                    _matchingClass = _matchingClassList.GetEnumerator();
                else
                    return null;
            }

            if (!_matchingClass.MoveNext())
            {
                _matchingClass = null;
            }
            else
            {
                returnValue = _matchingClass.Current;
            }

            return returnValue;
        }

        private bool FindTypeByModulePath(WildcardPattern classNameMatcher)
        {
            bool matchFound = false;

            var moduleList = ModuleUtils.GetDefaultAvailableModuleFiles(isForAutoDiscovery: false, _context);

            foreach (var modulePath in moduleList)
            {
                string expandedModulePath = IO.Path.GetFullPath(modulePath);
                var cachedClasses = AnalysisCache.GetExportedClasses(expandedModulePath, _context);

                if (cachedClasses != null)
                {
                    // Exact match
                    if (!_useWildCards)
                    {
                        if (cachedClasses.ContainsKey(_className))
                        {
                            var classInfo = CachedItemToPSClassInfo(classNameMatcher, modulePath);
                            if (classInfo != null)
                            {
                                _matchingClassList.Add(classInfo);
                                matchFound = true;
                            }
                        }
                    }
                    else
                    {
                        foreach (var className in cachedClasses.Keys)
                        {
                            if (classNameMatcher.IsMatch(className))
                            {
                                var classInfo = CachedItemToPSClassInfo(classNameMatcher, modulePath);
                                if (classInfo != null)
                                {
                                    _matchingClassList.Add(classInfo);
                                    matchFound = true;
                                }
                            }
                        }
                    }
                }
            }

            return matchFound;
        }

        /// <summary>
        /// Convert the cacheItem to a PSClassInfo object.
        /// For this, we call Get-Module -List with module name.
        /// </summary>
        /// <param name="classNameMatcher">Wildcard pattern matcher for comparing class name.</param>
        /// <param name="modulePath">Path to the module where the class is defined.</param>
        /// <returns>Converted PSClassInfo object.</returns>
        private PSClassInfo CachedItemToPSClassInfo(WildcardPattern classNameMatcher, string modulePath)
        {
            foreach (var module in GetPSModuleInfo(modulePath))
            {
                var exportedTypes = module.GetExportedTypeDefinitions();

                ScriptBlockAst ast = null;
                TypeDefinitionAst typeAst = null;

                if (!_useWildCards)
                {
                    if (exportedTypes.TryGetValue(_className, out typeAst))
                    {
                        ast = typeAst.Parent.Parent as ScriptBlockAst;
                        if (ast != null)
                            return ConvertToClassInfo(module, ast, typeAst);
                    }
                }
                else
                {
                    foreach (var exportedType in exportedTypes)
                    {
                        if (exportedType.Value != null &&
                            classNameMatcher.IsMatch(exportedType.Value.Name) &&
                            exportedType.Value.IsClass)
                        {
                            ast = exportedType.Value.Parent.Parent as ScriptBlockAst;
                            if (ast != null)
                                return ConvertToClassInfo(module, ast, exportedType.Value);
                        }
                    }
                }
            }

            return null;
        }

        private Collection<PSModuleInfo> GetPSModuleInfo(string modulePath)
        {
            PSModuleInfo moduleInfo = null;

            lock (_lockObject)
            {
                _moduleInfoCache.TryGetValue(modulePath, out moduleInfo);
            }

            if (moduleInfo != null)
            {
                var returnValue = new Collection<PSModuleInfo>();
                returnValue.Add(moduleInfo);
                return returnValue;
            }

            CommandInfo commandInfo = new CmdletInfo("Get-Module", typeof(Microsoft.PowerShell.Commands.GetModuleCommand), null, null, _context);
            System.Management.Automation.Runspaces.Command getModuleCommand = new System.Management.Automation.Runspaces.Command(commandInfo);

            string moduleName = Path.GetFileNameWithoutExtension(modulePath);

            var modules = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)
                .AddCommand(getModuleCommand)
                    .AddParameter("List", true)
                    .AddParameter("Name", moduleName)
                    .AddParameter("ErrorAction", ActionPreference.Ignore)
                    .AddParameter("WarningAction", ActionPreference.Ignore)
                    .AddParameter("InformationAction", ActionPreference.Ignore)
                    .AddParameter("Verbose", false)
                    .AddParameter("Debug", false)
                    .Invoke<PSModuleInfo>();

            lock (_lockObject)
            {
                foreach (var module in modules)
                {
                    _moduleInfoCache.Add(module.Path, module);
                }
            }

            return modules;
        }

        private static PSClassInfo ConvertToClassInfo(PSModuleInfo module, ScriptBlockAst ast, TypeDefinitionAst statement)
        {
            PSClassInfo classInfo = new PSClassInfo(statement.Name);
            Dbg.Assert(statement.Name != null, "statement should have a name.");
            classInfo.Module = module;
            Collection<PSClassMemberInfo> properties = new Collection<PSClassMemberInfo>();

            foreach (var member in statement.Members)
            {
                if (member is PropertyMemberAst propAst && !propAst.PropertyAttributes.HasFlag(PropertyAttributes.Hidden))
                {
                    Dbg.Assert(propAst.Name != null, "PropName cannot be null");
                    Dbg.Assert(propAst.PropertyType != null, "PropertyType cannot be null");
                    Dbg.Assert(propAst.PropertyType.TypeName != null, "Property TypeName cannot be null");
                    Dbg.Assert(propAst.Extent != null, "Property Extent cannot be null");
                    Dbg.Assert(propAst.Extent.Text != null, "Property ExtentText cannot be null");

                    PSClassMemberInfo classProperty = new PSClassMemberInfo(propAst.Name,
                                                                          propAst.PropertyType.TypeName.FullName,
                                                                          propAst.Extent.Text);
                    properties.Add(classProperty);
                }
            }

            classInfo.UpdateMembers(properties);

            string mamlHelpFile = null;
            if (ast.GetHelpContent() != null)
                mamlHelpFile = ast.GetHelpContent().MamlHelpFile;

            if (!string.IsNullOrEmpty(mamlHelpFile))
                classInfo.HelpFile = mamlHelpFile;

            return classInfo;
        }

        #endregion
    }
}
