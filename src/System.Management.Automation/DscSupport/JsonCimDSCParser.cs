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

namespace Microsoft.PowerShell.DesiredStateConfiguration.Json
{
    /// <summary>
    /// Class that does high level Cim schema parsing.
    /// </summary>
    internal class CimDSCParser
    {
        private readonly JsonDeserializer _json_deserializer;
        
        internal CimDSCParser()
        {
            _json_deserializer = JsonDeserializer.Create();
        }

        internal IEnumerable<PSObject> ParseSchemaJson(string filePath, bool useNewRunspace = false)
        {
            string json = File.ReadAllText(filePath);
            try
            {
                string fileNameDefiningClass = Path.GetFileNameWithoutExtension(filePath);
                int dotIndex = fileNameDefiningClass.IndexOf(".schema", StringComparison.InvariantCultureIgnoreCase);
                if (dotIndex != -1)
                {
                    fileNameDefiningClass = fileNameDefiningClass.Substring(0, dotIndex);
                }

                var result = _json_deserializer.DeserializeClasses(json, useNewRunspace);
                foreach (dynamic classObject in result)
                {
                    string superClassName = classObject.SuperClassName;
                    string className = classObject.ClassName;
                    if (superClassName?.Equals("OMI_BaseResource", StringComparison.OrdinalIgnoreCase) ?? false)
                    {
                        // Get the name of the file without schema.mof/json extension
                        if (!(className.Equals(fileNameDefiningClass, StringComparison.OrdinalIgnoreCase)))
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
