/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Management.Automation.Language;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using Dbg = System.Management.Automation.Diagnostics;

//
// Now define the set of commands for manipulating modules.
//

namespace Microsoft.PowerShell.Commands
{
    #region Module Specification class

    /// <summary>
    /// Represents module specification written in a module manifest (i.e. in RequiredModules member/field).
    /// 
    /// Module manifest allows 2 forms of module specification:
    /// 1. string - module name
    /// 2. hashtable - [string]ModuleName (required) + [Version]ModuleVersion/RequiredVersion (required) + [Guid]GUID (optional)
    /// 
    /// so we have a constructor that takes a string and a constructor that takes a hashtable
    /// (so that LanguagePrimitives.ConvertTo can cast a string or a hashtable to this type)
    /// </summary>
    public class ModuleSpecification
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ModuleSpecification()
        {
        }

        /// <summary>
        /// Construct a module specification from the module name.
        /// </summary>
        /// <param name="moduleName">The module name.</param>
        public ModuleSpecification(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                throw new ArgumentNullException("moduleName");
            }
            this.Name = moduleName;
            // Alias name of miniumVersion
            this.Version = null;
            this.RequiredVersion = null;
            this.MaximumVersion = null;
            this.Guid = null;
        }

        /// <summary>
        /// Construct a module specification from a hashtable.
        /// Keys can be ModuleName, ModuleVersion, and Guid.
        /// ModuleName must be convertible to <see cref="string"/>.
        /// ModuleVersion must be convertible to <see cref="Version"/>.
        /// Guid must be convertible to <see cref="Guid"/>.
        /// </summary>
        /// <param name="moduleSpecification">The module specification as a hashtable.</param>
        public ModuleSpecification(Hashtable moduleSpecification)
        {
            if (moduleSpecification == null)
            {
                throw new ArgumentNullException("moduleSpecification");
            }

            var exception = ModuleSpecificationInitHelper(this, moduleSpecification);
            if (exception != null)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Initialize moduleSpecification from hashtable. Return exception object, if hashtable cannot be converted.
        /// Return null, in the success case.
        /// </summary>
        /// <param name="moduleSpecification">object to initalize</param>
        /// <param name="hashtable">contains info about object to initialize.</param>
        /// <returns></returns>
        internal static Exception ModuleSpecificationInitHelper(ModuleSpecification moduleSpecification, Hashtable hashtable)
        {
            StringBuilder badKeys = new StringBuilder();
            try
            {
                foreach (DictionaryEntry entry in hashtable)
                {
                    if (entry.Key.ToString().Equals("ModuleName", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.Name = LanguagePrimitives.ConvertTo<string>(entry.Value);
                    }
                    else if (entry.Key.ToString().Equals("ModuleVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.Version = LanguagePrimitives.ConvertTo<Version>(entry.Value);
                    }
                    else if (entry.Key.ToString().Equals("RequiredVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.RequiredVersion = LanguagePrimitives.ConvertTo<Version>(entry.Value);
                    }
                    else if (entry.Key.ToString().Equals("MaximumVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.MaximumVersion = LanguagePrimitives.ConvertTo<String>(entry.Value);
                        ModuleCmdletBase.GetMaximumVersion(moduleSpecification.MaximumVersion);
                    }
                    else if (entry.Key.ToString().Equals("GUID", StringComparison.OrdinalIgnoreCase))
                    {
                        moduleSpecification.Guid = LanguagePrimitives.ConvertTo<Guid?>(entry.Value);
                    }
                    else
                    {
                        if (badKeys.Length > 0)
                            badKeys.Append(", ");
                        badKeys.Append("'");
                        badKeys.Append(entry.Key.ToString());
                        badKeys.Append("'");
                    }
                }
            }
            // catch all exceptions here, we are going to report them via return value.
            // Example of catched exception: one of convertions to Version failed.
            catch (Exception e)
            {
                return e;
            }

            string message;
            if (badKeys.Length != 0)
            {
                message = StringUtil.Format(Modules.InvalidModuleSpecificationMember, "ModuleName, ModuleVersion, RequiredVersion, GUID", badKeys);
                return new ArgumentException(message);
            }

            if (string.IsNullOrEmpty(moduleSpecification.Name))
            {
                message = StringUtil.Format(Modules.RequiredModuleMissingModuleName);
                return new MissingMemberException(message);
            }

            if (moduleSpecification.RequiredVersion == null && moduleSpecification.Version == null && moduleSpecification.MaximumVersion == null)
            {
                message = StringUtil.Format(Modules.RequiredModuleMissingModuleVersion);
                return new MissingMemberException(message);
            }

            if (moduleSpecification.RequiredVersion != null && moduleSpecification.Version != null)
            {
                message = StringUtil.Format(SessionStateStrings.GetContent_TailAndHeadCannotCoexist, "ModuleVersion", "RequiredVersion");
                return new ArgumentException(message);
            }

            if (moduleSpecification.RequiredVersion != null && moduleSpecification.MaximumVersion != null)
            {
                message = StringUtil.Format(SessionStateStrings.GetContent_TailAndHeadCannotCoexist, "MaxiumVersion", "RequiredVersion");
                return new ArgumentException(message);
            }
            return null;
        }

        internal ModuleSpecification(PSModuleInfo moduleInfo)
        {
            if (moduleInfo == null)
            {
                throw new ArgumentNullException("moduleInfo");
            }

            this.Name = moduleInfo.Name;
            this.Version = moduleInfo.Version;
            this.Guid = moduleInfo.Guid;
        }

        /// <summary>
        /// Implements ToString() for a module specification. If the specification
        /// just contains a Name, then that is returned as is. Otherwise, the object is
        /// formatted as a PowerSHell hashtable.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string moduleSpecString = string.Empty;
            if (Guid == null && Version == null && RequiredVersion == null && MaximumVersion == null)
            {
                moduleSpecString = Name;
            }
            else
            {
                moduleSpecString = "@{ ModuleName = '" + Name + "'";
                if (Guid != null)
                {
                    moduleSpecString += "; Guid = '{" + Guid + "}' ";
                }
                if (RequiredVersion != null)
                {
                    moduleSpecString += "; RequiredVersion = '" + RequiredVersion + "'";
                }
                else
                {
                    if (Version != null)
                    {
                        moduleSpecString += "; ModuleVersion = '" + Version + "'";
                    }
                    if (MaximumVersion != null)
                    {
                        moduleSpecString += "; MaximumVersion = '" + MaximumVersion + "'";
                    }
                }
                moduleSpecString += " }";
            }
            return moduleSpecString;
        }



        /// <summary>
        /// Parse the specified string into a ModuleSpecification object
        /// </summary>
        /// <param name="input">The module specification string</param>
        /// <param name="result">the ModuleSpecification object</param>
        /// <returns></returns>
        public static bool TryParse(string input, out ModuleSpecification result)
        {
            result = null;
            try
            {
                Hashtable hashtable;
                if (Parser.TryParseAsConstantHashtable(input, out hashtable))
                {
                    result = new ModuleSpecification(hashtable);
                    return true;
                }
            }
            catch
            {
                // Ignoring the exceptions to return false
            }

            return false;
        }

        /// <summary>
        /// The module name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The module GUID, if specified.
        /// </summary>
        public Guid? Guid { get; internal set; }

        /// <summary>
        /// The module version number if specified, otherwise null.
        /// </summary>
        public Version Version { get; internal set; }

        /// <summary>
        /// The module maxVersion number if specified, otherwise null.
        /// </summary>
        public String MaximumVersion { get; internal set; }

        /// <summary>
        /// The exact version of the module if specified, otherwise null.
        /// </summary>
        public Version RequiredVersion { get; internal set; }
    }

    internal class ModuleSpecificationComparer : IEqualityComparer<ModuleSpecification>
    {
        public bool Equals(ModuleSpecification x, ModuleSpecification y)
        {
            bool result = false;

            if (x == null && y == null)
            {
                result = true;
            }
            else if (x != null && y != null)
            {
                if (x.Name != null && y.Name != null)
                {
                    result = x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    result = true;
                }
                if (result)
                {
                    if (x.Guid.HasValue && y.Guid.HasValue)
                    {
                        result = x.Guid.Equals(y.Guid);
                    }
                }
                if (result)
                {
                    if (x.Version != null && y.Version != null)
                    {
                        result = x.Version.Equals(y.Version);
                    }
                    else if (x.Version != null || y.Version != null)
                    {
                        result = false;
                    }

                    if (x.MaximumVersion != null && y.MaximumVersion != null)
                    {
                        result = x.MaximumVersion.Equals(y.MaximumVersion);
                    }
                    else if (x.MaximumVersion != null || y.MaximumVersion != null)
                    {
                        result = false;
                    }

                    if (result && x.RequiredVersion != null && y.RequiredVersion != null)
                    {
                        result = x.RequiredVersion.Equals(y.RequiredVersion);
                    }
                    else if (result && (x.RequiredVersion != null || y.RequiredVersion != null))
                    {
                        result = false;
                    }
                }
            }

            return result;
        }

        public int GetHashCode(ModuleSpecification obj)
        {
            int result = 0;

            if (obj != null)
            {
                if (obj.Name != null)
                {
                    result = result ^ obj.Name.GetHashCode();
                }
                if (obj.Guid.HasValue)
                {
                    result = result ^ obj.Guid.GetHashCode();
                }
                if (obj.Version != null)
                {
                    result = result ^ obj.Version.GetHashCode();
                }
                if (obj.MaximumVersion != null)
                {
                    result = result ^ obj.MaximumVersion.GetHashCode();
                }
                if (obj.RequiredVersion != null)
                {
                    result = result ^ obj.RequiredVersion.GetHashCode();
                }
            }

            return result;
        }
    }

    #endregion
} // Microsoft.PowerShell.Commands
