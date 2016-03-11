/********************************************************************++
 * Copyright (c) Microsoft Corporation.  All rights reserved.
 * --********************************************************************/

using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using Microsoft.Management.Infrastructure;

using OTS_TypeSystemRef = System.IntPtr;
using OTS_TypeSystemConstRef =  System.IntPtr;
using OTS_SymbolRef = System.IntPtr;
using OTS_SymbolConstRef = System.IntPtr;
using OTS_TypeFactoryRef = System.IntPtr;
using OTS_TypeFactoryConstRef = System.IntPtr;
using OTS_TypeSpecRef = System.IntPtr;
using OTS_TypeSpecConstRef = System.IntPtr;
using OTS_MemberPropertyRef = System.IntPtr;
using OTS_MemberPropertyConstRef = System.IntPtr;
using OTS_ObjectRef = System.IntPtr;
using OTS_ObjectConstRef = System.IntPtr;
using OTS_TypeRef = System.IntPtr;
using OTS_TypeConstRef = System.IntPtr;
using OTS_KeyedCollectionRef = System.IntPtr;

namespace Microsoft.PowerShell.OTS.Client
{
    internal enum OTS_Result : int
    {   
        OK                                  = 0,
        FAILED                              = -1,
        UNDEFINED_PROPERTY                  = 1,
        RESULT_NULL = 2
    }

    // TODO: Validate the number assignments
    internal enum OTS_MemberKind : int
    {
        OTS_MemberKind_ByValue              = 0,
        OTS_MemberKind_ByObjectRef          = 1,
        OTS_MemberKind_ByPointerRef         = 2, /* <- Dispose */
        OTS_MemberKind_Synthetic            = 3,
    }

    /*internal static class OTS_Interop
    {
                /// <summary>
        /// Gets type name
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        /// Unicorn: dsim - this is a helper function written as inst.GetType().GetProperty("Name")
        [DllImport(OTSNativeApi.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern OTS_SymbolRef OTS_Type_GetName(OTS_TypeRef self);

        /// <summary>
        /// Sets the value of a property with the given name
        /// </summary>
        /// <param name="self">the object</param>
        /// <param name="name">the name of the property</param>
        /// <param name="value">the value of the property</param>
        /// <param name="addAsNeeded">
        /// if the parameter is TRUE and a property with this name does not exist, it will be added to the object and set to the given value. 
        /// </param>
        /// <returns></returns>
        /// Unicorn: dsim - this is a helper function over top of inst.SetProperty(key, value)
        [DllImport(OTSNativeApi.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern OTS_Result OTS_Object_SetPropertyByName(OTS_ObjectRef self,
            OTS_SymbolRef name,
            // TODO: Type spec says OTS_Object* should this be OTS_ObjectRef
            OTS_ObjectRef value, 
            // TODO: Is this just bool? defined in .h as PAL_Boolean
            bool addAsNeeded);
    }*/

    internal static class OTSInteropNative
    {
        internal const string OTSApiDll = @"omf_aos.dll";
        internal static OTS_TypeSystemRef TypeSystemRef;

        #region Type Helpers

        internal static void AddOTSType(string typeName, IDictionary<string, Type> properties)
        {
            OTS_TypeFactoryRef typeFactoryRef = OTS_TypeSystem_NewTypeFactory(TypeSystemRef);
            try
            {
                // TODO: check if the type is already created.
                OTS_SymbolRef typeNameSymbol = OTS_TypeSystem_InternAsSymbolW(TypeSystemRef, typeName);
                OTS_TypeSpecRef typeSpecRef = OTS_TypeFactory_CreateTypeSpec(typeFactoryRef, typeNameSymbol, IntPtr.Zero);

                foreach(var property in properties.Keys)
                {
                    OTS_SymbolRef propertyNameSymbol = OTS_TypeSystem_InternAsSymbolW(TypeSystemRef, property);
                    OTS_TypeSpecRef propertyTypeSpecRef = GetTypeSpecRef(properties[property]);
                    OTS_TypeSpec_CreateMemberProperty(typeSpecRef, propertyNameSymbol, OTS_MemberKind.OTS_MemberKind_ByValue, propertyTypeSpecRef, false, 1, IntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                OTS_TypeFactory_CloseAndSealTypes(typeFactoryRef);
            }
        }

        internal static CimInstance NewOTSObject(string typeName, IDictionary<string, Object> properties)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentException("typeName cannot be null or empty");
            }

            /*OTS_TypeSpecRef typeSpecRef = GetTypeSpecRef(typeName);
            if (typeSpecRef == IntPtr.Zero)
            {
                //TODO: Create TypeSpec.
                throw new ArgumentException(string.Format("typeName {0} not yet defined", typeName));
            }*/

            return null;
        }

        internal static OTS_TypeSpecRef GetTypeSpecRef(Type dotNetType)
        {
            string otsTypeName = string.Empty;
            switch (dotNetType.ToString().ToLowerInvariant())
            {
                case "int":
                case "system.int32":
                case "int32":
                    otsTypeName = "int";
                    break;
                case "float":
                case "system.single":
                    otsTypeName = "float";
                    break;
                case "system.uint32":
                    otsTypeName = "uint";
                    break;
                case "string":
                case "system.string":
                    otsTypeName = "string";
                    break;
                default:
                    throw new ArgumentException(
                        string.Format("Type {0} not supported", dotNetType.ToString()));
            }

            OTS_SymbolRef typeNameSymbol = OTS_TypeSystem_InternAsSymbolW(TypeSystemRef, otsTypeName);
            return OTS_Type_GetTypeSpec(typeNameSymbol);
        }

        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_TypeSpecRef OTS_Type_GetTypeSpec(OTS_SymbolRef typeNameSymbol);

        #endregion

        #region Instance Helpers
        #endregion
        
        #region Static Constructor

        static OTSInteropNative()
        {
            TypeSystemRef = OTS_InitializeTypeSystem(IntPtr.Zero, string.Empty, IntPtr.Zero, IntPtr.Zero);
        }

        #endregion

        #region DllImports
        /// <summary>
        /// The only static function in the type system that initializes the OTS_TypeSystem objects.
        /// </summary>
        /// <param name="rootTypeSystem">
        /// parent type system on NULL
        /// </param>
        /// <param name="configXml">
        /// An xml describing the configuration for the TypeSytem. Can be empty.
        /// </param>
        /// <param name="filterFunction"></param>
        /// <param name="filterParameter"></param>
        /// <returns>OTS_TypeSystemRef</returns>
        /// TODO: This is not present in "C" API interfaces.
        /// Unicorn: dsim - see ToDo: comment
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_TypeSystemRef OTS_InitializeTypeSystem(
          OTS_TypeSystemConstRef rootTypeSystem,
          [MarshalAs(UnmanagedType.LPWStr)] string configXml,
          [In] IntPtr filterFunction, 
          IntPtr filterParameter
          );

        /// <summary>
        /// Returns a canonical OTS_SymbolRef for the given string. Symbol is a ref-counted object.
        /// Invocation of this function adds one reference to the symbol. That reference should be
        /// released when done with the symbol. 
        /// </summary>
        /// <param name="typeSystemRef">Reference to OTS_TypeSystemRef</param>
        /// <param name="symbol">the input string</param>
        /// <returns>OTS_SymbolRef</returns>
        /// Unicorn: dsim - This is the convenience API, full API has more parameters
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_SymbolRef OTS_TypeSystem_InternAsSymbolW(
            OTS_TypeSystemConstRef typeSystemRef,
            [MarshalAs(UnmanagedType.LPWStr)] string symbol
            );

        /// <summary>
        /// Create an instance of a type factory associated with the type system. The type factory instance
        /// is specific to the type that created it. 
        /// </summary>
        /// <param name="self">the type system</param>
        /// <returns>OTS_TypeFactoryRef</returns>
        /// Unicorn: dsim - ok
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_TypeFactoryRef OTS_TypeSystem_NewTypeFactory(
            OTS_TypeSystemRef self
            );

        /// <summary>
        /// The function completes the type definitions and registers them with the type system.
        /// </summary>
        /// <param name="self">the type factory </param>
        /// <returns>OTS_Result</returns>
        /// Unicorn: dsim - ok
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_Result OTS_TypeFactory_CloseAndSealTypes(
            OTS_TypeFactoryRef self
            );

        /// <summary>
        /// The function creates a new, empty type specification definition with the given name and inheriting from the superType .
        /// </summary>
        /// <param name="self">the type factory</param>
        /// <param name="name">the name of the new type </param>
        /// <param name="superType">the parent type of the new type</param>
        /// <param name="destructorFunction">
        /// destructor function that the type system calls before releasing the storage of the objects of this type. 
        /// </param>
        /// Unicorn: dsim - remove the C api's optional destructor(s)
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_TypeSpecRef OTS_TypeFactory_CreateTypeSpec(
            OTS_TypeFactoryConstRef self,
            OTS_SymbolConstRef name,
            OTS_TypeConstRef superType
            );

        /// <summary>
        /// The function creates a member property object that can be associated with a type. 
        /// </summary>
        /// <param name="self">the type spec</param>
        /// <param name="propertyName">the name of the new property </param>
        /// <param name="propertyKind">the parent type of the new type</param>
        /// <param name="propertyType">the type of the property</param>
        /// <param name="isNullable">
        /// if the parameter value is NULL the member property will be nullable
        /// </param>
        /// <param name="elementCount">
        /// if the parameter value is > 1 the property is an array/collection
        /// </param>
        /// <param name="elementCountProperty">
        /// if this parameter is != NULL it must be a reference to an integer property in the same type. 
        /// If the above holds the property is a keyed collection of the size defined by this referenced property  
        /// </param>
        /// <returns></returns>
        /// Unicorn: dsim - not final, but should be stable for Unicorn (subject to some code integration changes)
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_MemberPropertyRef OTS_TypeSpec_CreateMemberProperty(
            OTS_TypeSpecConstRef self,
            OTS_SymbolConstRef propertyName,
            OTS_MemberKind propertyKind,
            OTS_TypeRef propertyType,
            bool isNullable,
            UInt32 elementCount,
            OTS_MemberPropertyConstRef elementCountProperty
            );

        /// <summary>
        /// Creates a new object instance on the heap with default property values. 
        /// Objects created this way are capturable.
        /// </summary>
        /// <param name="self">
        /// the type
        /// </param>
        /// <returns></returns>
        /// Unicorn: dsim - See the following list of comments
        /*
            Parameters:
            self – the type

            Unicorn: 10/2 dsim
            _Ret_maybenull_ OTS_ObjectRef           OTS_Type_NewObject  
            ( _In_ OTS_TypeRef self             //  With default object size
            );
            _Ret_maybenull_ OTS_ObjectRef           OTS_Type_NewObjectWithExtraBytes  
            ( _In_ OTS_TypeRef self
            , _In_ PAL_Uint32 propertyBytesCount//  With exact that is Sum(fixed+extraBytes)
            );
            _Ret_maybenull_ OTS_ObjectRef           OTS_Type_NewObjectWithExtraBytesAndByteInitPrototype
            ( _In_ OTS_TypeRef self
            , _In_ PAL_Uint32 propertyBytesCount//  With exact that is Sum(fixed+extraBytes)
            , _In_ PAL_Uint8* propertyBytes     //  With exact binary image to use for initializing the Sum(fixed+extraBytes)
            );
        */
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_ObjectRef OTS_Type_NewObject(OTS_TypeConstRef self);

        /// <summary>
        /// Returns OTS_Type object which is the factory for this instance. This operation cannot fail.
        /// </summary>
        /// <param name="self">
        /// the object
        /// </param>
        /// <returns></returns>
        /// Unicorn: dsim - ok
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_TypeRef OTS_Object_GetType(OTS_ObjectConstRef self);

        /// <summary>
        /// Sets the value of a property with the given name
        /// </summary>
        /// <param name="self">the object</param>
        /// <param name="name">the key for the property</param>
        /// <param name="value">the value of the property</param>
        /// <param name="addAsNeeded">
        /// if the parameter is TRUE and a property with this name does not exist, it will be added to the object and set to the given value. 
        /// </param>
        /// <returns></returns>
        /// Unicorn: dsim - ok
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_Result OTS_Object_SetValueAt(
            OTS_ObjectRef self,
            OTS_ObjectConstRef key,
            OTS_ObjectRef value, 
            bool addAsNeeded);

        /// <summary>
        /// Gets the named key within the collection stored under the given index or key. On failure, returns nullptr 
        /// </summary>
        /// <param name="self">the object</param>
        /// <param name="name">the key for the property</param>
        /// <returns></returns>
        /// Unicorn: dsim - ok
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_ObjectRef OTS_Object_GetValueAt(
            OTS_ObjectConstRef self,
            OTS_ObjectConstRef key);

        /// <summary>
        /// Sets the value of a collection element identified by the value of the key. The key of an element in a collection can be either 
        ///    -	a symbol 
        ///    -	an integer
        /// depending on the collection type. This implies that the function may fail if a wrong key type is passed to the function. 
        /// In case the key does not exist in the collection already, the addAsNeeded parameter is used to indicate if a new element should be added. 
        /// </summary>
        /// <param name="self">
        /// the collection
        /// </param>
        /// <param name="key">
        /// the key of the element in the collection
        /// </param>
        /// <param name="value">
        /// value of the element reference by the key
        /// </param>
        /// <param name="addAsNeeded">
        /// a switch indicating if a new element should be added to the collection if an element with the given key does not exist 
        /// </param>
        /// <returns></returns>
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_Result OTS_Object_SetCollectionValueAt(
            OTS_ObjectRef self,
            OTS_SymbolConstRef collectionName_sym,
            OTS_ObjectConstRef key,
            OTS_ObjectRef value, 
            bool addAsNeeded);

        /// <summary>
        /// TODO: comment needs re-work
        /// Gets the value of the collection element at the given key. On failure, returns nullptr.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_ObjectRef OTS_Object_GetCollectionValueAt(
            OTS_ObjectConstRef self,
            OTS_SymbolConstRef collectionName_sym,
            OTS_ObjectConstRef key);

        /// <summary>
        /// TODO: comment needs re-work
        /// Gets the named key within the collection stored under the given index or key. On failure, returns nullptr 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        private static extern OTS_ObjectRef OTS_Object_GetCollectionKeyAt(
            OTS_ObjectConstRef self,
            OTS_SymbolConstRef collectionName_sym,
            OTS_ObjectConstRef keyIndex);

        /// <summary>
        /// TODO: comment needs re-work
        /// Enumerates keys and valuyes. 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        [DllImport(OTSInteropNative.OTSApiDll, SetLastError = false, CharSet = CharSet.Unicode)]
        unsafe private static extern OTS_Result OTS_Object_CollectionPropertyKeysAndValuesDo(
            OTS_ObjectConstRef self,
            OTS_SymbolConstRef collectionName_sym,
            void* refCon,
            void* callbackFn    // Returns bool to continue iteration (if false, iteration stops), arguments are values
            );
        #endregion
        /*
         * callbackFn: []()->bool(OTS_ObjectConstRef key, OTS_ObjectRef value, void* refCon = null)
         * */
    }
}