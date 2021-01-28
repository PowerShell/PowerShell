// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Security;

namespace Microsoft.PowerShell.DesiredStateConfiguration.Internal.CrossPlatform
{
    /// <summary>
    /// Class that does high level Cim schema parsing.
    /// </summary>
    internal class CimDSCParser
    {
        private readonly JsonDeserializer _jsonDeserializer;
        
        internal CimDSCParser()
        {
            _jsonDeserializer = JsonDeserializer.Create();
        }

        internal IEnumerable<PSObject> ParseSchemaJson(string filePath, bool useNewRunspace = false)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                string fileNameDefiningClass = Path.GetFileNameWithoutExtension(filePath);
                int dotIndex = fileNameDefiningClass.IndexOf(".schema", StringComparison.InvariantCultureIgnoreCase);
                if (dotIndex != -1)
                {
                    fileNameDefiningClass = fileNameDefiningClass.Substring(0, dotIndex);
                }

                IEnumerable<PSObject> result = _jsonDeserializer.DeserializeClasses(json, useNewRunspace);
                foreach (dynamic classObject in result)
                {
                    string superClassName = classObject.SuperClassName;
                    string className = classObject.ClassName;
                    if (string.Equals(superClassName, "OMI_BaseResource", StringComparison.OrdinalIgnoreCase))
                    {
                        // Get the name of the file without schema.mof/json extension
                        if (!className.Equals(fileNameDefiningClass, StringComparison.OrdinalIgnoreCase))
                        {
                            PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                                ParserStrings.ClassNameNotSameAsDefiningFile, className, fileNameDefiningClass);
                            throw e;
                        }
                    }
                }

                return result;
            }
            catch (Exception exception)
            {
                PSInvalidOperationException e = PSTraceSource.NewInvalidOperationException(
                    exception, ParserStrings.CimDeserializationError, filePath);

                e.SetErrorId("CimDeserializationError");
                throw e;
            }
        }
    }
}
