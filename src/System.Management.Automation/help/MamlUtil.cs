// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation.Help;

namespace System.Management.Automation
{
    /// <summary>
    /// The MamlUtil class.
    /// </summary>
    internal static class MamlUtil
    {
        /// <summary>
        /// Takes Name value from maml2 and overrides it in maml1.
        /// </summary>
        /// <param name="maml1"></param>
        /// <param name="maml2"></param>
        internal static void OverrideName(PSObject maml1, PSObject maml2)
        {
            PrependPropertyValue(maml1, maml2, new string[] { "Name" }, true);
            PrependPropertyValue(maml1, maml2, new string[] { "Details", "Name" }, true);
        }

        /// <summary>
        /// Takes Name value from maml2 and overrides it in maml1.
        /// </summary>
        /// <param name="maml1"></param>
        /// <param name="maml2"></param>
        internal static void OverridePSTypeNames(PSObject maml1, PSObject maml2)
        {
            foreach (var typename in maml2.TypeNames)
            {
                if (typename.StartsWith(DefaultCommandHelpObjectBuilder.TypeNameForDefaultHelp, StringComparison.OrdinalIgnoreCase))
                {
                    // Win8: 638494 if the original help is auto-generated, let the Provider help decide the format.
                    return;
                }
            }

            maml1.TypeNames.Clear();
            // User request at the top..
            foreach (string typeName in maml2.TypeNames)
            {
                maml1.TypeNames.Add(typeName);
            }
        }

        /// <summary>
        /// Adds common properties like PSSnapIn,ModuleName from maml2 to maml1.
        /// </summary>
        /// <param name="maml1"></param>
        /// <param name="maml2"></param>
        internal static void AddCommonProperties(PSObject maml1, PSObject maml2)
        {
            if (maml1.Properties["PSSnapIn"] == null)
            {
                PSPropertyInfo snapInProperty = maml2.Properties["PSSnapIn"];
                if (snapInProperty != null)
                {
                    maml1.Properties.Add(new PSNoteProperty("PSSnapIn", snapInProperty.Value));
                }
            }

            if (maml1.Properties["ModuleName"] == null)
            {
                PSPropertyInfo moduleNameProperty = maml2.Properties["ModuleName"];
                if (moduleNameProperty != null)
                {
                    maml1.Properties.Add(new PSNoteProperty("ModuleName", moduleNameProperty.Value));
                }
            }
        }

        /// <summary>
        /// Prepend - Modify Syntax element in maml1 using the Syntax element from maml2.
        /// </summary>
        internal static void PrependSyntax(PSObject maml1, PSObject maml2)
        {
            PrependPropertyValue(maml1, maml2, new string[] { "Syntax", "SyntaxItem" }, false);
        }

        /// <summary>
        /// Prepend - Modify DetailedDescription element in maml1 using the DetailedDescription element from maml2.
        /// </summary>
        internal static void PrependDetailedDescription(PSObject maml1, PSObject maml2)
        {
            PrependPropertyValue(maml1, maml2, new string[] { "Description" }, false);
        }

        /// <summary>
        /// Override - Modify Parameters element in maml1 using the Parameters element from maml2.
        /// This will copy parameters from maml2 that are not present in maml1.
        /// </summary>
        internal static void OverrideParameters(PSObject maml1, PSObject maml2)
        {
            string[] parametersPath = new string[] { "Parameters", "Parameter" };
            // Final collection of PSObjects.
            List<object> maml2items = new List<object>();

            // Add maml2 first since we are prepending.

            // For maml2: Add as collection or single item. No-op if
            PSPropertyInfo propertyInfo2 = GetPropertyInfo(maml2, parametersPath);
            var array = propertyInfo2.Value as Array;
            if (array != null)
            {
                maml2items.AddRange(array as IEnumerable<object>);
            }
            else
            {
                maml2items.Add(PSObject.AsPSObject(propertyInfo2.Value));
            }

            // Extend maml1 to make sure the property-path exists - since we'll be modifying it soon.
            EnsurePropertyInfoPathExists(maml1, parametersPath);
            // For maml1: Add as collection or single item. Do nothing if null or some other type.
            PSPropertyInfo propertyInfo1 = GetPropertyInfo(maml1, parametersPath);
            List<object> maml1items = new List<object>();
            array = propertyInfo1.Value as Array;
            if (array != null)
            {
                maml1items.AddRange(array as IEnumerable<object>);
            }
            else
            {
                maml1items.Add(PSObject.AsPSObject(propertyInfo1.Value));
            }

            // copy parameters from maml2 that are not present in maml1
            for (int index = 0; index < maml2items.Count; index++)
            {
                PSObject m2paramObj = PSObject.AsPSObject(maml2items[index]);
                string param2Name = string.Empty;
                PSPropertyInfo m2propertyInfo = m2paramObj.Properties["Name"];

                if (m2propertyInfo != null)
                {
                    if (!LanguagePrimitives.TryConvertTo<string>(m2propertyInfo.Value, out param2Name))
                    {
                        continue;
                    }
                }

                bool isParamFoundInMaml1 = false;
                foreach (PSObject m1ParamObj in maml1items)
                {
                    string param1Name = string.Empty;
                    PSPropertyInfo m1PropertyInfo = m1ParamObj.Properties["Name"];

                    if (m1PropertyInfo != null)
                    {
                        if (!LanguagePrimitives.TryConvertTo<string>(m1PropertyInfo.Value, out param1Name))
                        {
                            continue;
                        }
                    }

                    if (param1Name.Equals(param2Name, StringComparison.OrdinalIgnoreCase))
                    {
                        isParamFoundInMaml1 = true;
                    }
                }

                if (!isParamFoundInMaml1)
                {
                    maml1items.Add(maml2items[index]);
                }
            }

            // Now replace in maml1. If items.Count == 0 do nothing since Value is already null.
            if (maml1items.Count == 1)
            {
                propertyInfo1.Value = maml1items[0];
            }
            else if (maml1items.Count >= 2)
            {
                propertyInfo1.Value = maml1items.ToArray();
            }
        }

        /// <summary>
        /// Prepend - Modify Notes element in maml1 using the Notes element from maml2.
        /// </summary>
        internal static void PrependNotes(PSObject maml1, PSObject maml2)
        {
            PrependPropertyValue(maml1, maml2, new string[] { "AlertSet", "Alert" }, false);
        }

        /// <summary>
        /// Get property info.
        /// </summary>
        internal static PSPropertyInfo GetPropertyInfo(PSObject psObject, string[] path)
        {
            if (path.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < path.Length; ++i)
            {
                string propertyName = path[i];
                PSPropertyInfo propertyInfo = psObject.Properties[propertyName];
                if (i == path.Length - 1)
                {
                    return propertyInfo;
                }

                if (propertyInfo == null || propertyInfo.Value is not PSObject)
                {
                    return null;
                }

                psObject = (PSObject)propertyInfo.Value;
            }

            // We will never reach this line but the compiler needs some reassurance.
            return null;
        }

        /// <summary>
        /// Prepend property value.
        /// </summary>
        /// <param name="maml1">
        /// </param>
        /// <param name="maml2">
        /// </param>
        /// <param name="path">
        /// </param>
        /// <param name="shouldOverride">
        /// Should Override the maml1 value from maml2 instead of prepend.
        /// </param>
        internal static void PrependPropertyValue(PSObject maml1, PSObject maml2, string[] path, bool shouldOverride)
        {
            // Final collection of PSObjects.
            List<object> items = new List<object>();

            // Add maml2 first since we are prepending.

            // For maml2: Add as collection or single item. No-op if
            PSPropertyInfo propertyInfo2 = GetPropertyInfo(maml2, path);

            if (propertyInfo2 != null)
            {
                var array = propertyInfo2.Value as Array;
                if (array != null)
                {
                    items.AddRange(propertyInfo2.Value as IEnumerable<object>);
                }
                else
                {
                    items.Add(propertyInfo2.Value);
                }
            }

            // Extend maml1 to make sure the property-path exists - since we'll be modifying it soon.
            EnsurePropertyInfoPathExists(maml1, path);
            // For maml1: Add as collection or single item. Do nothing if null or some other type.
            PSPropertyInfo propertyInfo1 = GetPropertyInfo(maml1, path);

            if (propertyInfo1 != null)
            {
                if (!shouldOverride)
                {
                    var array = propertyInfo1.Value as Array;
                    if (array != null)
                    {
                        items.AddRange(propertyInfo1.Value as IEnumerable<object>);
                    }
                    else
                    {
                        items.Add(propertyInfo1.Value);
                    }
                }

                // Now replace in maml1. If items.Count == 0 do nothing since Value is already null.
                if (items.Count == 1)
                {
                    propertyInfo1.Value = items[0];
                }
                else if (items.Count >= 2)
                {
                    propertyInfo1.Value = items.ToArray();
                }
            }
        }

        /// <summary>
        /// Ensure property info path exists.
        /// </summary>
        internal static void EnsurePropertyInfoPathExists(PSObject psObject, string[] path)
        {
            if (path.Length == 0)
            {
                return;
            }

            // Walk the path and extend it if necessary.
            for (int i = 0; i < path.Length; ++i)
            {
                string propertyName = path[i];
                PSPropertyInfo propertyInfo = psObject.Properties[propertyName];

                // Add a property info here if none was found.
                if (propertyInfo == null)
                {
                    // Add null on the last one, since we don't need to extend path further.
                    object propertyValue = (i < path.Length - 1) ? new PSObject() : null;
                    propertyInfo = new PSNoteProperty(propertyName, propertyValue);
                    psObject.Properties.Add(propertyInfo);
                }

                // If we are on the last path element, we are done. Let's not mess with modifying Value.
                if (i == path.Length - 1)
                {
                    return;
                }

                // If we are not on the last path element, let's make sure we can extend the path.
                if (propertyInfo.Value == null || propertyInfo.Value is not PSObject)
                {
                    propertyInfo.Value = new PSObject();
                }

                // Now move one step further along the path.
                psObject = (PSObject)propertyInfo.Value;
            }
        }
    }
}
