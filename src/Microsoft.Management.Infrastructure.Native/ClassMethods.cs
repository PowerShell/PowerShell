using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Globalization;
using NativeObject;

namespace Microsoft.Management.Infrastructure.Native
{
    internal class ClassMethods
    {
        // Methods
        private ClassMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(ClassHandle ClassHandleToClone, out ClassHandle clonedClassHandle)
        {
            throw new NotImplementedException();
        }
        internal static int GetClassHashCode(ClassHandle handle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetClassName(ClassHandle handle, out string className)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetClassQualifier_Index(ClassHandle handle, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElement_GetIndex(ClassHandle handle, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetFlags(ClassHandle handle, int index, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetName(ClassHandle handle, int index, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetReferenceClass(ClassHandle handle, int index, out string referenceClass)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetType(ClassHandle handle, int index, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementAt_GetValue(ClassHandle handle, int index, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetElementCount(ClassHandle handle, out int count)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethod_GetIndex(ClassHandle handle, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodAt_GetName(ClassHandle handle, int methodIndex, int parameterIndex, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodAt_GetReferenceClass(ClassHandle handle, int methodIndex, int parameterIndex, out string referenceClass)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodAt_GetType(ClassHandle handle, int methodIndex, int parameterIndex, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodCount(ClassHandle handle, out int methodCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodElement_GetIndex(ClassHandle handle, int methodIndex, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodElementAt_GetName(ClassHandle handle, int index, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodElementAt_GetType(ClassHandle handle, int index, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodGetQualifierElement_GetIndex(ClassHandle handle, int methodIndex, int parameterIndex, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParameterGetQualifierElementAt_GetFlags(ClassHandle handle, int methodIndex, int parameterName, int index, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParameterGetQualifierElementAt_GetName(ClassHandle handle, int methodIndex, int parameterName, int index, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParameterGetQualifierElementAt_GetType(ClassHandle handle, int methodIndex, int parameterName, int index, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParameterGetQualifierElementAt_GetValue(ClassHandle handle, int methodIndex, int parameterName, int index, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParametersCount(ClassHandle handle, int index, out int parameterCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodParametersGetQualifiersCount(ClassHandle handle, int index, int parameterIndex, out int parameterCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierCount(ClassHandle handle, int methodIndex, out int parameterCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElement_GetIndex(ClassHandle handle, int methodIndex, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElementAt_GetFlags(ClassHandle handle, int methodIndex, int qualifierIndex, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElementAt_GetName(ClassHandle handle, int methodIndex, int qualifierIndex, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElementAt_GetType(ClassHandle handle, int methodIndex, int qualifierIndex, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMethodQualifierElementAt_GetValue(ClassHandle handle, int methodIndex, int qualifierIndex, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetNamespace(ClassHandle handle, out string nameSpace)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetParentClass(ClassHandle handle, out ClassHandle superClass)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetParentClassName(ClassHandle handle, out string className)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifier_Count(ClassHandle handle, string name, out int count)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifier_Index(ClassHandle handle, string propertyName, string name, out int index)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifierElementAt_GetFlags(ClassHandle handle, int index, string propertyName, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifierElementAt_GetName(ClassHandle handle, int index, string propertyName, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifierElementAt_GetType(ClassHandle handle, int index, string propertyName, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPropertyQualifierElementAt_GetValue(ClassHandle handle, int index, string propertyName, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifier_Count(ClassHandle handle, out int qualifierCount)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifierElementAt_GetFlags(ClassHandle handle, int index, out MiFlags flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifierElementAt_GetName(ClassHandle handle, int index, out string name)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifierElementAt_GetType(ClassHandle handle, int index, out MiType type)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetQualifierElementAt_GetValue(ClassHandle handle, int index, out object value)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetServerName(ClassHandle handle, out string serverName)
        {
            throw new NotImplementedException();
        }
    }
}
