using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;

/* MI_ primitive types. Rather than define structs, this is one alternative */
using MI_Boolean = System.Byte; // unsigned char
using MI_Uint8   = System.Byte; // unsigned char
using MI_Sint8   = System.SByte; // signed char
using MI_Uint16  = System.UInt16; // unsigned short
using MI_Sint16  = System.Int16; // signed short
using MI_Uint32  = System.UInt32; // unsigned int
using MI_Sint32  = System.Int32; // signed int
using MI_Uint64  = System.UInt64; // unsigned long long
using MI_Sint64  = System.Int64; // signed long long
using MI_Real32  = System.Single; // float
using MI_Real64  = System.Double; // float
using MI_Char16  = System.UInt16; // unsigned short
using MI_Char    = System.Char;  // char (or wchar_t)
// /Primitive MI data types represented in C#

namespace Microsoft.Win32.SafeHandles
{
    internal class SafeHandleZeroOrMinusOneIsInvalid
    {
    }
}

namespace Microsoft.Management.Infrastructure.Native
{
    internal class MiNative
    {
        // Reference to the MI.h shared object library
        public const string LIBMI = "libmi.so";

#region structs
        // struct MI_SchemaDecl
        // {
        //     /* Qualifier declarations */
        //     MI_QualifierDecl const qualifierDecls;
        //     uint numQualifierDecls;

        //     /* Class declarations */
        //     MI_ClassDecl const classDecls;
        //     uint numClassDecls;
        // }

        // struct MI_ParameterDecl /* extends MI_FeatureDecl */
        // {
        //     /* Fields inherited from MI_FeatureDecl */
        //     uint flags;
        //     uint code;
        //     const string name;
        //     MI_Qualifier const qualifiers;
        //     uint numQualifiers;

        //     /* Type of this field */
        //     uint type;

        //     /* Name of reference class */
        //     const string className;

        //     /* Array subscript */
        //     uint subscript;

        //     /* Offset of this field within the structure */
        //     uint offset;
        // }

        // struct MI_MethodDecl /* extends MI_ObjectDecl */
        // {
        //     /* Fields inherited from MI_FeatureDecl */
        //     uint flags;
        //     uint code;
        //     const string* name;
        //     struct MI_Qualifier const qualifiers;
        //     uint numQualifiers;

        //     /* Fields inherited from MI_ObjectDecl */
        //     struct MI_ParameterDecl const parameters;
        //     uint numParameters;
        //     uint size;

        //     /* PostResult type of this method */
        //     uint returnType;

        //     /* Ancestor class that first defined a property with this name */
        //     const string origin;

        //     /* Ancestor class that last defined a property with this name */
        //     const string propagator;

        //     /* Pointer to scema this class belongs to */
        //     struct MI_SchemaDecl const schema;

        //     /* Pointer to extrinsic method */
        //     MI_MethodDecl_Invoke function;
        // }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_PropertyDecl /* extends MI_ParameterDecl */
        {
            /* Fields inherited from MI_FeatureDecl */
            public uint flags;
            public uint code;
            public string name;
            public IntPtr qualifiers;  //MI_Qualifier
            public uint numQualifiers;

            /* Fields inherited from MI_ParameterDecl */
            public uint type;
            public string className;
            public uint subscript;
            public uint offset;

            /* Ancestor class that first defined a property with this name */
            public string origin;

            /* Ancestor class that last defined a property with this name */
            public string propagator;

            /* Value of this property */
            public IntPtr value;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_Qualifier
        {
            /* Qualifier name */
            public string name;

            /* Qualifier type */
            public uint type;

            /* Qualifier flavor */
            public uint flavor;

            /* Pointer to value */
            IntPtr value;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_ClassDecl /* extends MI_ObjectDecl */
        {
            // /* Fields inherited from MI_FeatureDecl */
            //public uint flags;
            //public uint code;
            //public string name;
            //public MI_Qualifier qualifiers;
            //public uint numQualifiers;

            // /* Fields inherited from MI_ObjectDecl */
            //public IntPtr properties;  //MI_PropertyDecl 
            //public uint numProperties;
            // uint size;

            // /* Name of superclass */
            // const string superClass;

            // /* Superclass declaration */
            // MI_ClassDecl const superClassDecl;

            // /* The methods of this class */
            // struct MI_MethodDecl const methods;
            // uint numMethods;

            // /* Pointer to scema this class belongs to */
            // struct MI_SchemaDecl const schema;

            // /* Provider functions */
            // const MI_ProviderFT providerFT;

            // /* Owning MI_Class object, if any.  NULL if static classDecl, -1 is from a dynamic instance */
            // MI_Class owningClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_QualifierSet
        {
            UInt64 reserved1;
            IntPtr reserved2;

            readonly MI_QualifierSetFT ft;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_ParameterSet
        {
            UInt64 reserved1;
            IntPtr reserved2;

            readonly MI_ParameterSetFT ft;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_ParameterSetFT
        {
            public GetMethodReturnTypeDelegate GetMethodReturnType;
            public GetParameterCountDelegate GetParameterCount;
            public GetParameterAtDelegate GetParameterAt;
            public GetParameterDelegate GetParameter;
        }

        public delegate MiResult GetMethodReturnTypeDelegate(
                MI_ParameterSet self,
                out MiType returnType,
                out MI_QualifierSet qualifierSet);

        public delegate MiResult GetParameterCountDelegate(
                MI_ParameterSet self,
                out uint count);

        public delegate MiResult GetParameterAtDelegate(
                MI_ParameterSet self,
                uint index,
                out IntPtr name,
                MiType parameterType,
                out IntPtr referenceClass,
                out MI_QualifierSet qualifierSet);

        public delegate MiResult GetParameterDelegate(
                MI_ParameterSet self,
                string name,
                out MiType parameterType,
                out IntPtr referenceClass,
                out MI_QualifierSet qualifierSet,
                out uint index);

        [StructLayout(LayoutKind.Sequential)]
        struct MI_QualifierSetFT
        {
            public GetQualifierCountDelegate GetQualifierCount;
            public GetQualifierAtDelegate GetQualifierAt;
            public GetQualifierDelegate GetQualifier;
        }

        public delegate MiResult GetQualifierCountDelegate(
            MI_QualifierSet self,
            out uint count);

        public delegate MiResult GetQualifierAtDelegate(
            MI_QualifierSet self,
            uint index,
            out IntPtr name,  // MI_Char ** name
            out MiType qualifierType,
            out uint qualifierFlags,    /* scope information */
            out MiValue qualifierValue
            );

        public delegate MiResult GetQualifierDelegate(
            MI_QualifierSet self,
            string name,
            out MiType qualifierType,
            out uint qualifierFlags,    /* scope information */
            out MiValue qualifierValue,
            out uint index
            );

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_ClassFT
        {
            public GetClassNameDelegate GetClassName;
            public GetNameSpaceDelegate GetNameSpace;
            public GetServerNameDelegate GetServerName;
            public GetElementCountDelegate GetElementCount;
            public GetElementDelegate GetElement;
            public GetElementAtDelegate GetElementAt;
            public GetClassQualifierSetDelegate GetClassQualifierSet;
            public GetMethodCountDelegate GetMethodCount;
            public GetMethodAtDelegate GetMethodAt;
            public GetMethodDelegate GetMethod;
            public GetParentClassNameDelegate GetParentClassName;
            public GetParentClassDelegate GetParentClass;
            public DeleteDelegate Delete;
            public CloneDelegate Clone;
        }

        /* MI_ClassFT functions */
        public delegate MiResult GetClassNameDelegate(
            MI_Class self,
            out IntPtr className);

        public delegate MiResult GetNameSpaceDelegate(
            MI_Class self,
            out IntPtr nameSpace);

        public delegate MiResult GetServerNameDelegate(
            MI_Class self,
            out IntPtr serverName);

        public delegate MiResult GetElementCountDelegate(
            MI_Class self,
            out uint count);

        public delegate MiResult GetElementDelegate(
            MI_Class self,
            string name,
            out MiValue value,
            out byte valueExists,
            out MiType type,
            out IntPtr referenceClass,
            out MI_QualifierSet qualifierSet,
            out uint flags,
            out uint index);

        public delegate MiResult GetElementAtDelegate(
            MI_Class self, 
            uint index,
            out IntPtr name,
            out MiValue value,
            out byte valueExists,
            out MiType type,
            out IntPtr referenceClass,
            out MI_QualifierSet qualifierSet,
            out uint flags);

        public delegate MiResult GetClassQualifierSetDelegate(
            MI_Class self,
            out MI_QualifierSet qualifierSet
            );

        public delegate MiResult GetMethodCountDelegate(
            MI_Class self,
            out uint count);

        public delegate MiResult GetMethodAtDelegate(
            MI_Class self,
            uint index,
            out IntPtr name,
            out MI_QualifierSet qualifierSet,
            out MI_ParameterSet parameterSet  //TODO: Add MI_ParameterSet
            );

        public delegate MiResult GetMethodDelegate(
            MI_Class self,
            string name,
            out MI_QualifierSet qualifierSet,
            out MI_ParameterSet parameterSet,
            out uint index
            );

        public delegate MiResult GetParentClassNameDelegate(
            MI_Class self,
            out IntPtr name);

        public delegate MiResult GetParentClassDelegate(
            MI_Class self,
            out IntPtr parentClass);

        public delegate MiResult DeleteDelegate(
            out MI_Class self);

        public delegate MiResult CloneDelegate(
            MI_Class self,
            out IntPtr newClass);

        /** The MI_Instance function table */
        [StructLayout(LayoutKind.Sequential)]
        public struct MI_InstanceFT
        {
            public CloneDelegate Clone;
            public DestructDelegate Destruct;
            public DeleteDelegate Delete;
            public IsADelegate IsA;
            public GetClassNameDelegate GetClassName;
            public SetNameSpaceDelegate SetNameSpace;
            public GetNameSpaceDelegate GetNameSpace;
            public GetElementCountDelegate GetElementCount;
            public AddElementDelegate AddElement;
            public SetElementDelegate SetElement;
            public SetElementAtDelegate SetElementAt;
            public GetElementDelegate GetElement;
            public GetElementAtDelegate GetElementAt;
            public ClearElementDelegate ClearElement;
            public ClearElementAtDelegate ClearElementAt;
            public GetServerNameDelegate GetServerName;
            public SetServerNameDelegate SetServerName;
            public GetClassDelegate GetClass;
            // MI_InstanceFT delegate signatures
            public delegate MiResult CloneDelegate(MI_Instance self, out IntPtr newInstance);
            public delegate MiResult DestructDelegate(ref MI_Instance self);
            public delegate MiResult DeleteDelegate(ref MI_Instance self); //ref MI_Instance self);
            public delegate MiResult IsADelegate(MI_Instance self, MI_ClassDecl classDecl, out byte flag);
            public delegate MiResult GetClassNameDelegate(IntPtr self, out IntPtr className);
            public delegate MiResult GetNameSpaceDelegate(IntPtr self, out IntPtr nameSpace);
            public delegate MiResult SetNameSpaceDelegate(IntPtr self, [MarshalAs(UnmanagedType.LPStr)] string nameSpace);
            public delegate MiResult GetElementCountDelegate(IntPtr self, out uint count);
            public delegate MiResult AddElementDelegate(
                    IntPtr self,
                    IntPtr name,
                    IntPtr val, /* _In_opt_ */
                    uint type,
                    MiFlags flags);  // TODO: Implement MiFlags
            public delegate MiResult SetElementDelegate(
                    out MI_Instance self,
                    string name,
                    uint val, //TODO: Implement MiValue
                    MiType type,
                    uint flags);
            public delegate MiResult SetElementAtDelegate(
                    ref IntPtr self,
                    uint index,
                    MiValue val,
                    MiType type,
                    uint flags);
            public delegate MiResult GetElementDelegate(
                    IntPtr self,
                    string name,
                    out IntPtr val,
                    out IntPtr type,
                    out MiFlags flags,
                    out uint index);
            public delegate MiResult GetElementAtDelegate(
                     IntPtr self,
                     uint index,
                     out IntPtr name,  //IntPtr because of a `free(): invalid pointer` error using MarshalAs(UnmanagedType.LPStr)
                     out IntPtr val,
                     out MiType type,
                     out MiFlags flags);
            public delegate MiResult ClearElementDelegate(IntPtr self, string name);
            public delegate MiResult ClearElementAtDelegate(ref MI_Instance self, uint index);
            public delegate MiResult GetServerNameDelegate(IntPtr self, out IntPtr name);
            public delegate MiResult SetServerNameDelegate(IntPtr self, [MarshalAs(UnmanagedType.LPStr)] string name);
            public delegate MiResult GetClassDelegate(MI_Instance self, out IntPtr instanceClass);
            // /Signatures
        }//InstanceFT


        // MI_ApplicationFT struct mirroring the function table created in the native layer.
        [StructLayout(LayoutKind.Sequential)]
        public struct MI_ApplicationFT
        {
            public CloseDelegate Close;
            public NewSessionDelegate NewSession;
            public NewHostedProviderDelegate NewHostedProvider;
            public NewInstanceDelegate NewInstance;
            public NewDestinationOptionsDelegate NewDestinationOptions;
            public NewOperationOptionsDelegate NewOperationOptions;
            public NewSubscriptionDeliveryOptionsDelegate NewSubscriptionDeliveryOptions;
            public NewSerializerDelegate NewSerializer;
            public NewDeserializerDelegate NewDeserializer;
            public NewInstanceFromClassDelegate NewInstanceFromClass;
            // MI_ApplicationFT function table signatures
            public delegate MiResult CloseDelegate(ref MI_Application application);
            public delegate MiResult NewSessionDelegate(MI_Application application,
                                                IntPtr protocol, // _In_Opt_z_ const MI_Char *protocol
                                                IntPtr destination, // _In_Opt_z_ const MI_Char *destination
                                                IntPtr options, // _In_Opt_z_ MI_DestinationOptions *options
                                                IntPtr callbacks, // _In_Opt_z_ MI_SessionCallbacks *callbacks
                                                out IntPtr extendedError, // outptr_opt_result_maybenull_ MI_Instance **
                                                out IntPtr session); // TODO: create MI_Session struct to replace IntPtr
            public delegate MiResult NewHostedProviderDelegate(MI_Application application,
                                                string namespaceName,
                                                string providerName,
                                                IntPtr mi_Main, //MI_MainFunction mi_Main, //TODO: MI_MainFunction struct
                                                out IntPtr extendedError,
                                                out IntPtr provider); // TODO: MI_HostedProvider struct *provider
            public delegate MiResult NewInstanceDelegate(ref MI_Application application,
                                                string className,
                                                IntPtr RTTI,  //TODO: SHould be MI_ClassDecl
                                                out IntPtr instance); // MI_Instance **instance
            public delegate MiResult NewDestinationOptionsDelegate(MI_Application application,
                                                out IntPtr options); // TODO: create MI_DestinationOptions struct
            public delegate MiResult NewOperationOptionsDelegate(MI_Application application,
                                                [MarshalAs(UnmanagedType.U1)] bool mustUnderstand,
                                                out MI_OperationOptions options);
            public delegate MiResult NewSubscriptionDeliveryOptionsDelegate(MI_Application application,
                                                MiSubscriptionDeliveryType deliveryType,
                                                out IntPtr deliveryOptions); // TODO: create MI_SubscriptionDeliveryOptions struct
            public delegate MiResult NewSerializerDelegate(MI_Application application,
                                                uint flags,
                                                string format,
                                                out MI_Serializer serializer); // TODO: create MI_Serializer struct
            public delegate MiResult NewDeserializerDelegate(MI_Application application,
                                                uint flags,
                                                string format,
                                                out MI_Deserializer deserializer); // TODO: create MI_Serializer struct
            public delegate MiResult NewInstanceFromClassDelegate(MI_Application application,
                                                string className,
                                                MI_Class classObject,
                                                out IntPtr instance); // MI_Instance **instance

        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MI_Instance
        {
            public IntPtr ft; /*  the function table of MI_Instance related functions */
            public IntPtr classDecl; // TODO: Implement ClassDecl struct and assign as IntPtr
            public string serverName;
            public string nameSpace;
            public IntPtr reserved1;
            public IntPtr reserved2;
            public IntPtr reserved3;
            public IntPtr reserved4;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_Application
        {
            public uint reserved1;
            public IntPtr reserved2;
            public IntPtr ft; /* the function table of MI_Application related functions */
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MI_Operation
        {
            public uint reserved1;
            public IntPtr reserved2;
            public IntPtr ft; /* The function table of MI_Operations */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_Session
        {
            public uint reserved1;
            public IntPtr reserved2;
            public IntPtr ft; /* the function table of MI_Session related functions */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_SubscriptionDeliveryOptions
        {
            public uint reserved1;
            public IntPtr reserved2;
            public IntPtr ft; /* The function table of MI_SubscriptionDeliveryOptions related functions */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_Serializer
        {
            uint reserved1;
            IntPtr reserved2;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
        public struct MI_Deserializer
        {
            public UInt64 reserved1;
            public IntPtr reserved2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_Class
        {
            public IntPtr ft;
            public IntPtr classDecl;
            public string nameSpace;
            public string serverName;
            public IntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_ClassA
        {
            public IntPtr data;
            public uint size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_OperationOptions
        {
            public uint reserved1;
            public IntPtr reserved2;
            public IntPtr ft;   /*  the function table of MI_OperationOptions related functions */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MI_OperationOptionsFT
        {
            public delegate MiResult Delete(out MI_OperationOptions options);

            public delegate MiResult SetString(out MI_OperationOptions options,
                                               string optionName,
                                               string value,
                                               IntPtr flags);

            public delegate MiResult SetNumber(out MI_OperationOptions options,
                                               string optionName,
                                               uint value,
                                               uint flags);

            public delegate MiResult SetCustomOption(MI_OperationOptions options,
                                               string optionName,
                                               MiType valueType,
                                               IntPtr value, // MiValue *value
                                               char mustComply,
                                               uint flags);

            public delegate MiResult GetString(out MI_OperationOptions options,
                                               string optionName,
                                               IntPtr value, // MI_Char **value
                                               out uint index,
                                               out uint flags);

            public delegate MiResult GetNumber(MI_OperationOptions options,
                                               string optionName,
                                               out uint value,
                                               out uint index,
                                               out uint flags);

            public delegate MiResult GetOptionCount(MI_OperationOptions options,
                                               uint count);

            public delegate MiResult GetOptionAt(MI_OperationOptions options,
                                               string optionName,
                                               out IntPtr value, // MiValue *value
                                               out uint index,
                                               out uint flags);

            public delegate MiResult GetOption(MI_OperationOptions options,
                                               string optionName,
                                               out IntPtr value, // MiValue *value
                                               out MiType type,
                                               out uint index,
                                               out uint flags);

            public delegate MiResult GetEnabledChannels(MI_OperationOptions options,
                                               string optionName,
                                               out uint channels, // _Out_writes_to_opt_(bufferReadLength, *channelCount)
                                               out uint bufferLength,
                                               out uint channelCount,
                                               out uint flags);

            public delegate MiResult Clone(MI_OperationOptions self,
                                               out MI_OperationOptions newOperationOptions);

            public delegate MiResult SetInterval(MI_OperationOptions options,
                                               string optionName,
                                               out IntPtr value, // TODO: create MI_Interval struct
                                               out uint index, // optional
                                               out uint flags); // optional
        }

        struct MI_SessionFT
        {
            public delegate MiResult Close(
                    MI_Session session,
                    IntPtr completionContext,
                    IntPtr completionCallback); /* _In_opt_ void (MI_CALL *completionCallback)(_In_opt_ void *completionContext)) */

            public delegate MiResult GetApplication(
                    MI_Session session,
                    out MI_Application application);

            public delegate void GetInstance(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    MI_Instance inboundInstance,
                    OperationCallbacks callbacks,
                    MI_Operation operation);

            public delegate void ModifyInstance(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    MI_Instance inboundInstance,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void CreateInstance(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    string inboundInstance,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void DeleteInstance(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    MI_Instance inboundInstance,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void Invoke(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    string className,
                    string methodName,
                    MI_Instance inboundInstance,
                    MI_Instance inboundProperties,
                    OperationCallbacks callbacks,
                    MI_Operation operation);

            public delegate void EnumerateInstances(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    string className,
                    byte keysOnly,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void QueryInstances(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    string queryDialect,
                    string queryExpression,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void AssociatorInstances(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    MI_Instance instanceKeys,
                    string assocClass,
                    string resultClass,
                    string role,
                    string resultRole,
                    byte keysOnly,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void ReferenceInstances(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    MI_Instance instanceKeys,
                    string resultClass,
                    string role,
                    byte keysOnly,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void Subscribe(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    string queryDialect,
                    string queryExpression,
                    MI_SubscriptionDeliveryOptions deliverOptions,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void GetClass(
                    MI_Session session,
                    uint flags, 
                    MI_OperationOptions options,
                    string namespaceName,
                    string className,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void EnumerateClasses(
                    MI_Session session,
                    uint flags,
                    MI_OperationOptions options,
                    string namespaceName,
                    string className,
                    byte classNamesOnly,
                    OperationCallbacks callbacks,
                    out MI_Operation operation);

            public delegate void TestConnection(
                    MI_Session session,
                    uint flags,
                    OperationCallbacks callbacks,
                    out MI_Operation operation
                );
        }
#endregion //structs

#region PInvokes

        //// MI_INLINE MI_Result MI_Application_NewOperationOptions(
        ////     _In_  MI_Application *application,
        ////           MI_Boolean mustUnderstand,
        ////     _Out_ MI_OperationOptions *options)
        //[DllImport("/home/zach/git/mi/libmi.so", CharSet = CharSet.Ansi)]
        //public static extern MiResult MI_Application_NewOperationOptions(IntPtr application, //MI_Application*
        //                                                               [MarshalAs(UnmanagedType.U1)] bool mustUnderstand,
        //                                                               out MI_OperationOptions options);
        // MI_Result MI_MAIN_CALL MI_Application_InitializeV1(
        //              MI_Uint32 flags, 
        //              _In_opt_z_ const MI_Char *applicationID,
        //              _Outptr_opt_result_maybenull_ MI_Instance **extendedError,
        //              _Out_    MI_Application *application);
        [DllImport(LIBMI, CharSet = CharSet.Ansi)]
        public static extern MiResult MI_Application_InitializeV1(uint flags,
                                                                  string applicationID,
                                                                  IntPtr errInstance,
                                                                  out MI_Application application);

#endregion
    } // End MiNative

    // Creates an instance of the struct MI_Application for passing into Management Infrastructure methods.
    [StructLayout(LayoutKind.Sequential)]
    internal class ApplicationHandle// : SafeHandleZeroOrMinusOneIsInvalid
    {
        public MiNative.MI_Application miApp;

        internal ApplicationHandle()
        {
            this.miApp = new MiNative.MI_Application();
        }

        internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            MiNative.MI_ApplicationFT table = Marshal.PtrToStructure<MiNative.MI_ApplicationFT>(this.miApp.ft);
            MiResult r = table.Close(ref this.miApp);
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }

    internal class ApplicationMethods
    {
        // Fields
        internal static string protocol_DCOM;
        internal static string protocol_WSMan;

        // Methods
        static ApplicationMethods()
        {
            //throw new NotImplementedException();
        }
        private ApplicationMethods()
        {
            //throw new NotImplementedException();
        }
        internal static MiResult GetCimErrorFromMiResult(MiResult errorCode, string errorMessage, out InstanceHandle cimError)
        {
            throw new NotImplementedException();
        }

        // Creates a new MI_Application and an instance of MI_Errors
        internal static MiResult Initialize(out InstanceHandle errorDetails, out ApplicationHandle applicationHandle)
        {
            return InitializeCore(out  errorDetails, out applicationHandle);
        }

        // passes in to MI.h two pointers to structs which have been previously declared, sets them, and returns an MiResult
        internal static MiResult InitializeCore(out InstanceHandle errorDetails, out ApplicationHandle applicationHandle)
        {
            // Initialize instance
            string appID       = null;
            errorDetails       = new InstanceHandle();
            IntPtr ePtr        = IntPtr.Zero;
            applicationHandle  = new ApplicationHandle();
            MiResult result    = MiNative.MI_Application_InitializeV1(0, appID, ePtr, out applicationHandle.miApp);
            return result;
        }

        internal static MiResult NewDeserializer(ApplicationHandle applicationHandle, string format, uint flags, out DeserializerHandle deserializerHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult NewDestinationOptions(ApplicationHandle applicationHandle, out DestinationOptionsHandle destinationOptionsHandle)
        {
            throw new NotImplementedException();
        }

        internal static MiResult NewInstance(ApplicationHandle applicationHandle, string className, ClassHandle classHandle, out InstanceHandle newInstance)
        {
            IntPtr pInstance;
            IntPtr rtti                       = IntPtr.Zero;
            MiResult result                   = MiResult.OK;
            MiNative.MI_ApplicationFT table   = Marshal.PtrToStructure<MiNative.MI_ApplicationFT>(applicationHandle.miApp.ft);

            result = table.NewInstance(ref applicationHandle.miApp, className, rtti, out pInstance);

            newInstance = new InstanceHandle(pInstance);
            return result;
        }
        internal static MiResult NewOperationOptions(ApplicationHandle applicationHandle, [MarshalAs(UnmanagedType.U1)] bool mustUnderstand, out OperationOptionsHandle operationOptionsHandle)
        {
            // Console.WriteLine(">>Native/NewOperationOptions");
            // //TODO: create OperationOptionsHandle
            // operationOptionsHandle = new OperationOptionsHandle();
            // MiNative.MI_Application localApp = applicationHandle.miApp;

            // IntPtr appBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(localApp));
            // Marshal.StructureToPtr(localApp, appBuffer, false);
            // MiNative.MI_OperationOptions localOpts = operationOptionsHandle.miOperationOptions;
            // // Marshal Struct as IntPtr? We may not have to marshalAs.PtrToStructure

            // Console.WriteLine("Break> Pinvoke");
            // // perform p/invoke
            // MiResult result = MiNative.MI_Application_NewOperationOptions(appBuffer,
            //                                                               mustUnderstand,
            //                                                               out localOpts);

            // Console.WriteLine("NewOperationOptions result : {0}", result);
            // operationOptionsHandle.miOperationOptions = localOpts;
            // Marshal.FreeHGlobal(appBuffer);

            // return result;
            throw new NotImplementedException();
        }
        internal static MiResult NewSerializer(ApplicationHandle applicationHandle, string format, uint flags, out SerializerHandle serializerHandle)
        {
            throw new NotImplementedException();
        }

        internal static MiResult NewSession(ApplicationHandle applicationHandle, string protocol, string destination, DestinationOptionsHandle destinationOptionsHandle, out InstanceHandle extendedError, out SessionHandle sessionHandle)
        {
            // Console.WriteLine("In NewSession");
            // extendedError = new InstanceHandle();
            // sessionHandle = new SessionHandle();
            // Console.WriteLine("instance and sessionHandle created");

            throw new NotImplementedException();
        }
        internal static MiResult NewSubscriptionDeliveryOptions(ApplicationHandle applicationHandle, MiSubscriptionDeliveryType deliveryType, out SubscriptionDeliveryOptionsHandle subscriptionDeliveryOptionsHandle)
        {
            throw new NotImplementedException();
        }
    }

    internal class ApplicationMethodsInternal
    {
        // Methods
        private ApplicationMethodsInternal()
        {
            throw new NotImplementedException();
        }

        // Takes a newly declared DeserializerHandle, and executes a P/Invoke to create a deserialzerMOF instance.  This requires access to mofcodec.  ApplicationHandle should already be created via Native.Initialize.
        internal static MiResult NewDeserializerMOF(ApplicationHandle applicationHandle, string format, uint flags, out DeserializerHandle deserializerHandle)
        {
            throw new NotImplementedException();
            ////TODO: the applicationdHandle should already be set.  Implement guards to verify
            //// create a pointer to the app and deserializer
            //IntPtr appBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(applicationHandle.miApp)); // app size should be 24
            //Marshal.StructureToPtr(applicationHandle.miApp, appBuffer, false);

            //// Create the deserializer
            //deserializerHandle                         = new DeserializerHandle();
            //MiNative.MI_Deserializer deserializer      = deserializerHandle.miDeserializer;
            //deserializer.reserved1                     = 0;
            //deserializer.reserved2                     = IntPtr.Zero;
            //IntPtr deserializeBuffer                   = Marshal.AllocHGlobal(Marshal.SizeOf(deserializer));
            //Marshal.StructureToPtr(deserializer, deserializeBuffer, false); // map the struct to an IntPtr

            //Console.WriteLine("Creating a New DeserializerMOF");
            //// takes a pointer to the applicationHandle struct, and a pointer to the deserializerHandle struct.
            //// Sets the deserializer struct.
            //MiResult result = MiNative.MI_Application_NewDeserializer_Mof(ref applicationHandle.miApp, flags, format, out deserializer);
            //Console.WriteLine("Native>> Created new deserializer Mof Pinvoke returned {0}", result);
            //deserializerHandle = Marshal.PtrToStructure<DeserializerHandle>(deserializeBuffer);
            //deserializerHandle.miDeserializer = deserializer;
            //// TODO: if result != MiResult.OK, return result
            //// TODO: Debug.Assert that the structs are populated correctly.

            //// Free the buffers
            //Marshal.FreeHGlobal(deserializeBuffer);
            //Marshal.FreeHGlobal(appBuffer);

            //return result;
        }

        internal static MiResult NewSerializerMOF(ApplicationHandle applicationHandle, string format, uint flags, out SerializerHandle serializerHandle)
        {
            throw new NotImplementedException();
        }
    }

    internal class AuthType
    {
        // Fields
        internal static string AuthTypeBasic;
        internal static string AuthTypeClientCerts;
        internal static string AuthTypeCredSSP;
        internal static string AuthTypeDefault;
        internal static string AuthTypeDigest;
        internal static string AuthTypeIssuerCert;
        internal static string AuthTypeKerberos;
        internal static string AuthTypeNegoNoCredentials;
        internal static string AuthTypeNegoWithCredentials;
        internal static string AuthTypeNone;
        internal static string AuthTypeNTLM;

        // Methods
        static AuthType()
        {
            throw new NotImplementedException();
        }
        private AuthType()
        {
            throw new NotImplementedException();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class ClassHandle// : SafeHandleZeroOrMinusOneIsInvalid
    {
        public MiNative.MI_Class miClass;

        internal ClassHandle()
        {
            this.miClass = new MiNative.MI_Class();
        }

        internal ClassHandle(IntPtr handle)
        {
            this.miClass = Marshal.PtrToStructure<MiNative.MI_Class>(handle);
        }

         internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }

        // should be part of SafeHandleZeroOrMinusOneIsInvalid
        public bool IsInvalid { get; }
    }

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

    internal class DangerousHandleAccessor : IDisposable
    {
        // Fields
        //private bool needToCallDangerousRelease;
        //private SafeHandle safeHandle;

        // Methods
        internal DangerousHandleAccessor(SafeHandle safeHandle)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "We are calling DangerousAddRef/Release as prescribed in the docs + have to do this to call inline methods")]
        internal IntPtr DangerousGetHandle()
        {
            throw new NotImplementedException();
        }
        //public sealed override void Dispose()
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        //protected virtual void Dispose([MarshalAs(UnmanagedType.U1)] bool A_0)
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class DeserializerCallbacks
    {
        //public MiNative.MI_DeserializerCallbacks miDeserializerCallbacks;
        // Fields
        //private ClassObjectNeededCallbackDelegate<backing_store> ClassObjectNeededCallback;
        //private GetIncludedFileBufferCallbackDelegate<backing_store> GetIncludedFileBufferCallback;
        //private object <backing_store>ManagedDeserializerContext;

        // Methods
        internal DeserializerCallbacks()
        {
            // TODO: Implement
            //this.miDeserializerCallbacks = new MiNative.MI_DeserializerCallbacks();
        }
        //internal static unsafe _MI_Result ClassObjectNeededAppDomainProxy(void* context, ushort modopt(IsConst)* serverName, ushort modopt(IsConst)* namespaceName, ushort modopt(IsConst)* className, _MI_Class** requestedClassObject)
        //internal static unsafe _MI_Result GetIncludedFileBufferAppDomainProxy(void* context, ushort modopt(IsConst)* fileName, byte** fileBuffer, uint* bufferLength)
        //internal static unsafe void ReleaseDeserializerCallbacksProxy(DeserializerCallbacksProxy* pCallbacksProxy)
        //[return: MarshalAs(UnmanagedType.U1)]
        //internal bool SetMiDeserializerCallbacks(MiNative.MI_DeserializerCallbacks pmiDeserializerCallbacks)
        //{
        //    //TODO: Implement
        //    return true;
        //}
        //private static unsafe void StoreCallbackDelegate(DeserializerCallbacksProxy* pCallbacksProxy, Delegate externalCallback, Delegate appDomainProxyCallback, DeserializerCallbackId callbackId);

        // Properties
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal ClassObjectNeededCallbackDelegate ClassObjectNeededCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal GetIncludedFileBufferCallbackDelegate GetIncludedFileBufferCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp and in cs/CimOperationOptions.cs")]
        internal object ManagedDeserializerContext { get; set; }

        // Nested Types
        //internal unsafe delegate _MI_Result ClassObjectNeededAppDomainProxyDelegate(void* context, ushort modopt(IsConst)* serverName, ushort modopt(IsConst)* namespaceName, ushort modopt(IsConst)* className, _MI_Class** requestedClassObject)
        internal delegate bool ClassObjectNeededCallbackDelegate(string serverName, string namespaceName, string className, out ClassHandle classHandle);
        //internal unsafe delegate _MI_Result GetIncludedFileBufferAppDomainProxyDelegate(void* context, ushort modopt(IsConst)* fileName, byte** fileBuffer, uint* bufferLength)
        internal delegate bool GetIncludedFileBufferCallbackDelegate(string fileName, out byte[] fileBuffer);
    }

    internal class DeserializerInternalMethods
    {
        // Methods
        private DeserializerInternalMethods()
        {
            //TODO: Implement
            //throw new NotImplementedException();
        }

        /*
         * If classObjects parameter is not null, then set the contents of classArray to that, otherwise, pass in empty
         * The deserializerHandle and serializedBuffer should already be populated by now.
         */
        internal static MiResult DeserializeClassArray(DeserializerHandle deserializerHandle, OperationOptionsHandle options, DeserializerCallbacks callback, byte[] serializedBuffer, uint offset, ClassHandle[] classObjects, string serverName, string nameSpace, out ClassHandle[] deserializedClasses, out uint inputBufferUsed, out InstanceHandle cimErrorDetails)
        {
            throw new NotImplementedException();
            ////TODO: Assert that deserializeHandle exists
            ////TODO: Assert that serializedbuffer exists
            //cimErrorDetails = new InstanceHandle();
            //Console.WriteLine(">>MMI.Native/DeserializeClassArray");
            //// create locals
            //MiNative.MI_OperationOptions localOpts = options.miOperationOptions;

            ////IntPtr psb = Marshal.AllocHGlobal(serializedBuffer.Length);
            ////Marshal.Copy(serializedBuffer, 0, psb, serializedBuffer.Length);
            ////GCHandle gch = GCHandle.Alloc(serializedBuffer.Length, GCHandleType.Pinned);
            ////IntPtr psb = gch.AddrOfPinnedObject();
            //// if (serializedBuffer != null)
            //// {
            ////     int serializedBuffSize = Marshal.SizeOf(serializedBuffer[0])*serializedBuffer.Length;
            ////     sb = Marshal.AllocHGlobal(serializedBuffSize);
            ////     Marshal.Copy(serializedBuffer, 0, sb, serializedBuffer.Length);
            //// }
            //IntPtr deserializerBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(deserializerHandle.miDeserializer));
            //Marshal.StructureToPtr(deserializerHandle.miDeserializer, deserializerBuffer, false);
            //MiNative.MI_Instance ErrorIns              = cimErrorDetails.miInstance;
            //IntPtr pErr                                = Marshal.AllocHGlobal(Marshal.SizeOf(ErrorIns));
            //inputBufferUsed                            = 0;
            //MiNative.MI_DeserializerCallbacks cb       = callback.miDeserializerCallbacks;
            //IntPtr pNativeCallbacks                    = Marshal.AllocHGlobal(Marshal.SizeOf(cb));
            //uint serializedBufferRead;
            //IntPtr classObjectsBuffer;
            //int nativeClassObjectsLength;
            ////byte[] pbyNativeClassObjects;
            //// create an array of classArray structs.  If there is existing classArray information, assign that.
            ////MiNative.MI_ClassA classArray;
            ////classArray.data = IntPtr.Zero;
            ////classArray.size = 0;
            //IntPtr classArray;
            //// Populate MI_ClassA.data when an array of classObjects is provided.
            //// This will later be passed as a pointer to MI_ClassA
           ///* if ((classObjects != null) && (0 < classObjects.Length))
            //{
            //    nativeClassObjectsLength = classObjects.Length;
            //    //now populate the date with all the MI_Class instances
            //    int iSizeOfOneClassHandle = Marshal.SizeOf(classObjects[0].miClass); 
            //    classObjectsBuffer = Marshal.AllocHGlobal(iSizeOfOneClassHandle * nativeClassObjectsLength);

            //    classObjects = new ClassHandle[nativeClassObjectsLength];
            //    for( uint i = 0; i < nativeClassObjectsLength; i++)
            //    {
            //        classObjects[i] = new ClassHandle();
            //    }
            //    //pbyNativeClassObjects = (byte[])(pNativeClassObjects.ToPointer());
            //    //for ( int i = 0; i < nativeClassObjectsLength; i++, pbyNativeClassObjects += (iSizeOfOneClassHandle) )
            //    //{
            //    //    IntPtr pOneClassObject = new IntPtr(pbyNativeClassObjects);
            //    //    Marshal.StructureToPtr(classObjects[i], pOneClassObject, false);
            //    //}
            //    classArray.data = classObjectsBuffer;
            //    classArray.size = (uint)nativeClassObjectsLength;
            //}*/

            ////if (options) { nativeOptions  }
            //IntPtr pOpts = IntPtr.Zero;
            ////if (callback) { callback.SetMiDeserializerCallbacks(ref pNativeCallbacks); }
            //// TODO: this is wrong- deserializedClasses array of classHandles needs to be created differently
           //// deserializedClasses                 = new ClassHandle[1];
           //// deserializedClasses[0]              = new ClassHandle();
           //// MiNative.MI_Class dClasses          = deserializedClasses[0].miClass;

            ////var resultClassArray = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)));

            ////MiNative.MI_ClassA resultClassArray;
            ////IntPtr resultClassABuffer = (IntPtr)Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr)));
            ////// convert classObjects to byte array
            ////Console.WriteLine("the pinned address : {0}", gch.AddrOfPinnedObject());
            ////IntPtr gchPtr = gch.AddrOfPinnedObject();

            ////resultClassArray.data     = IntPtr.Zero;
            ////resultClassArray.size     = 1;
            ////IntPtr resultClassABuffer = Marshal.AllocHGlobal(Marshal.SizeOf(resultClassArray));
            ////Marshal.StructureToPtr(resultClassArray, resultClassABuffer, false);

            ////MiNative.MI_ClassA classA = new MiNative.MI_ClassA();
            ////IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(classA));
            ////Marshal.StructureToPtr(classA, ptr, false);
            //// execute p/invoke, assign to result. Note: flags= 0
            ////if ( classA == null) { Console.WriteLine("You dun goofed");}
            //IntPtr ptr;// = IntPtr.Zero;
            ////ptr = Marshal.AllocHGlobal(IntPtr.Size);
            //IntPtr bs;
            ////Marshal.StructureToPtr(callback, bs, false);//callbacks
            //MiResult result = MiNative.MI_Deserializer_DeserializeClassArray(
            //                                            ref deserializerHandle.miDeserializer,
            //                                            0,
            //                                            ref localOpts,
            //                                            out bs, // callbacks
            //                                            serializedBuffer,
            //                                            (uint)serializedBuffer.Length,
            //                                            out classArray,  /* classArray: if null, pass IntPtr.Zero, else classArray */
            //                                            serverName,
            //                                            nameSpace,
            //                                            out serializedBufferRead,
            //                                            out ptr,
            //                                            out pErr);

            //Console.WriteLine("MI_Deserializer_DeserializeClassArray pinvoke complete.  result :{0}", result);
            //Console.WriteLine("classObjects: {0}", ptr);
            ////TODO: classHandle array from resultClassArray needs to be marshalled into managed code, then assign the array of classHandles to 
            //ClassHandle[] d = new ClassHandle[1];
            //deserializedClasses = d;
            ////ClassHandle[] d = Marshal.PtrToStructure<ClassHandle>(resultClassArray);
            ////if ((result == MiResult.OK) && (0 != resultClassArray.Size))
            ////{
            ////    deserializedClasses = new ClassHandle[resultClassArray.size];
            ////    for (int i=0; i < resultClassArray.size; i++)
            ////    {
            ////        deserializedClasses[i] = Marshal.PtrToStructure<ClassHandle>(resultClassArray.data);
            ////    }
            ////}

            //return result;

            ////throw new NotImplementedException();
        }
        internal static MiResult DeserializeInstanceArray(DeserializerHandle deserializerHandle, OperationOptionsHandle options, DeserializerCallbacks callback, byte[] serializedBuffer, uint offset, ClassHandle[] classObjects, out InstanceHandle[] deserializedInstances, out uint inputBufferUsed, out InstanceHandle cimErrorDetails)
        {
            throw new NotImplementedException();
        }
    }

    // TODO: classes with sequential layout cannot inherit.  Create a Data Transfer Object
    [StructLayout(LayoutKind.Sequential)]
    internal class DeserializerHandle// : SafeHandleZeroOrMinusOneIsInvalid
    {
        public MiNative.MI_Deserializer miDeserializer;
        internal DeserializerHandle()
        {
            this.miDeserializer = new MiNative.MI_Deserializer();
        }

        // Used when setting the deserializer handle to the return value
        internal DeserializerHandle(IntPtr handle)
        {
            this.miDeserializer = Marshal.PtrToStructure<MiNative.MI_Deserializer>(handle);
        }

        internal void AssertValidInternalState()
        {
            //throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }

    internal class DeserializerMethods
    {
        // Methods
        private DeserializerMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult DeserializeClass(DeserializerHandle deserializerHandle, uint flags, byte[] serializedBuffer, uint offset, ClassHandle parentClass, string serverName, string nameSpace, out ClassHandle deserializedClass, out uint inputBufferUsed, out InstanceHandle cimErrorDetails)
        {
            throw new NotImplementedException();
        }
        internal static MiResult DeserializeInstance(DeserializerHandle deserializerHandle, uint flags, byte[] serializedBuffer, uint offset, ClassHandle[] classObjects, out InstanceHandle deserializedInstance, out uint inputBufferUsed, out InstanceHandle cimErrorDetails)
        {
            throw new NotImplementedException();
        }
    }

    internal class DestinationOptionsHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }

    internal class DestinationOptionsMethods
    {
        // Fields
        internal static string packetEncoding_Default;
        internal static string packetEncoding_UTF16;
        internal static string packetEncoding_UTF8;
        internal static string proxyType_Auto;
        internal static string proxyType_IE;
        internal static string proxyType_None;
        internal static string proxyType_WinHTTP;
        internal static string transport_Http;
        internal static string transport_Https;

        // Methods
        static DestinationOptionsMethods()
        {
            throw new NotImplementedException();
        }
        private DestinationOptionsMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult AddDestinationCredentials(DestinationOptionsHandle destinationOptionsHandle, NativeCimCredentialHandle credentials)
        {
            throw new NotImplementedException();
        }
        internal static MiResult AddProxyCredentials(DestinationOptionsHandle destinationOptionsHandle, NativeCimCredentialHandle credentials)
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(DestinationOptionsHandle destinationOptionsHandle, out DestinationOptionsHandle newDestinationOptionsHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetCertCACheck(DestinationOptionsHandle destinationOptionsHandle, out bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetCertCNCheck(DestinationOptionsHandle destinationOptionsHandle, out bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetCertRevocationCheck(DestinationOptionsHandle destinationOptionsHandle, out bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetDataLocale(DestinationOptionsHandle destinationOptionsHandle, out string locale)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetDestinationPort(DestinationOptionsHandle destinationOptionsHandle, out uint port)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetEncodePortInSPN(DestinationOptionsHandle destinationOptionsHandle, out bool encodePort)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetHttpUrlPrefix(DestinationOptionsHandle destinationOptionsHandle, out string prefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetImpersonationType(DestinationOptionsHandle destinationOptionsHandle, out MiImpersonationType impersonationType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetMaxEnvelopeSize(DestinationOptionsHandle destinationOptionsHandle, out uint sizeInKB)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPacketEncoding(DestinationOptionsHandle destinationOptionsHandle, out string encoding)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPacketIntegrity(DestinationOptionsHandle destinationOptionsHandle, out bool integrity)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPacketPrivacy(DestinationOptionsHandle destinationOptionsHandle, out bool privacy)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetProxyType(DestinationOptionsHandle destinationOptionsHandle, out string proxyType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetTimeout(DestinationOptionsHandle destinationOptionsHandle, out TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetTransport(DestinationOptionsHandle destinationOptionsHandle, out string transport)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetUILocale(DestinationOptionsHandle destinationOptionsHandle, out string locale)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCertCACheck(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCertCNCheck(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCertRevocationCheck(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool check)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCustomOption(DestinationOptionsHandle destinationOptionsHandle, string optionName, string optionValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCustomOption(DestinationOptionsHandle destinationOptionsHandle, string optionName, uint optionValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDataLocale(DestinationOptionsHandle destinationOptionsHandle, string locale)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDestinationPort(DestinationOptionsHandle destinationOptionsHandle, uint port)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetEncodePortInSPN(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool encodePort)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetHttpUrlPrefix(DestinationOptionsHandle destinationOptionsHandle, string prefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetImpersonationType(DestinationOptionsHandle destinationOptionsHandle, MiImpersonationType impersonationType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetMaxEnvelopeSize(DestinationOptionsHandle destinationOptionsHandle, uint sizeInKB)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPacketEncoding(DestinationOptionsHandle destinationOptionsHandle, string encoding)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPacketIntegrity(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool integrity)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPacketPrivacy(DestinationOptionsHandle destinationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool privacy)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetProxyType(DestinationOptionsHandle destinationOptionsHandle, string proxyType)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetTimeout(DestinationOptionsHandle destinationOptionsHandle, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetTransport(DestinationOptionsHandle destinationOptionsHandle, string transport)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetUILocale(DestinationOptionsHandle destinationOptionsHandle, string locale)
        {
            throw new NotImplementedException();
        }

        // Nested Types
        internal enum MiImpersonationType
        {
            Default,
            None,
            Identify,
            Impersonate,
            Delegate
        }
    }

    internal abstract class ExceptionSafeCallbackBase
    {
        // Fields
        internal OperationCallbackProcessingContext callbackProcessingContext;
        //private OperationCallbacks.InternalErrorCallbackDelegate internalErrorCallback;
        //internal unsafe MI_OperationWrapper* pmiOperationWrapper;

        // Methods
        //protected unsafe ExceptionSafeCallbackBase(void* callbackContext);
        protected abstract void InvokeUserCallback();
        internal void InvokeUserCallbackAndCatchInternalErrors()
        {
            throw new NotImplementedException();
        }
        //[return: MarshalAs(UnmanagedType.U1)]
        //private bool IsInternalException(Exception e);
        //private void ReportInternalError(Exception exception);
    }

    internal class ExceptionSafeClassCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe ushort modopt(IsConst)* errorString;
        //private byte moreResults;
        //private unsafe _MI_Class modopt(IsConst)* pmiClass;
        //private unsafe _MI_Instance modopt(IsConst)* pmiErrorDetails;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private _MI_Result resultCode;

        // Methods
        //internal unsafe ExceptionSafeClassCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Class modopt(IsConst)* pmiClass, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeIndicationCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe ushort modopt(IsConst)* bookmark;
        //private unsafe ushort modopt(IsConst)* errorString;
        //private unsafe ushort modopt(IsConst)* machineID;
        //private byte moreResults;
        //private unsafe _MI_Instance modopt(IsConst)* pmiErrorDetails;
        //private unsafe _MI_Instance modopt(IsConst)* pmiInstance;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private _MI_Result resultCode;

        // Methods
        //internal unsafe ExceptionSafeIndicationCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, ushort modopt(IsConst)* bookmark, ushort modopt(IsConst)* machineID, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeInstanceResultCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe ushort modopt(IsConst)* errorString;
        //private byte moreResults;
        //private unsafe _MI_Instance modopt(IsConst)* pmiErrorDetails;
        //private unsafe _MI_Instance modopt(IsConst)* pmiInstance;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private _MI_Result resultCode;

        // Methods
        //internal unsafe ExceptionSafeInstanceResultCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafePromptUserCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_PromptType promptType;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult;
        //private unsafe ushort modopt(IsConst)* wszMessage;

        // Methods
        //internal unsafe ExceptionSafePromptUserCallback(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszMessage, _MI_PromptType promptType, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeStreamedParameterResultCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private _MiType miType;
        //private unsafe _MI_Operation* pmiOperation;
        //private unsafe _MiValue modopt(IsConst)* pmiParameterValue;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement;
        //private unsafe ushort modopt(IsConst)* wszParameterName;

        // Methods
        //internal unsafe ExceptionSafeStreamedParameterResultCallback(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszParameterName, _MiType miType, _MiValue modopt(IsConst)* pmiParameterValue, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeWriteErrorCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private unsafe _MI_Instance* pmiInstance;
        //private unsafe _MI_Operation* pmiOperation;
        //private _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult;

        // Methods
        //internal unsafe ExceptionSafeWriteErrorCallback(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance* pmiInstance, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeWriteMessageCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private uint channel;
        //private unsafe _MI_Operation* pmiOperation;
        //private unsafe ushort modopt(IsConst)* wszMessage;

        // Methods
        //internal unsafe ExceptionSafeWriteMessageCallback(_MI_Operation* pmiOperation, void* callbackContext, uint channel, ushort modopt(IsConst)* wszMessage);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class ExceptionSafeWriteProgressCallback : ExceptionSafeCallbackBase
    {
        // Fields
        //private uint percentageComplete;
        //private unsafe _MI_Operation* pmiOperation;
        //private uint secondsRemaining;
        //private unsafe ushort modopt(IsConst)* wszActivity;
        //private unsafe ushort modopt(IsConst)* wszCurrentOperation;
        //private unsafe ushort modopt(IsConst)* wszStatusDescription;

        // Methods
        //internal unsafe ExceptionSafeWriteProgressCallback(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszActivity, ushort modopt(IsConst)* wszCurrentOperation, ushort modopt(IsConst)* wszStatusDescription, uint percentageComplete, uint secondsRemaining);
        protected override void InvokeUserCallback()
        {
            throw new NotImplementedException();
        }
    }

    internal class Helpers
    {
        // Methods
        private Helpers()
        {
            throw new NotImplementedException();
        }
        internal static IntPtr GetCurrentSecurityToken()
        {
            throw new NotImplementedException();
        }
        internal static IntPtr StringToHGlobalUni(string s)
        {
            throw new NotImplementedException();
        }
        internal static void ZeroFreeGlobalAllocUnicode(IntPtr s)
        {
            throw new NotImplementedException();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class InstanceHandle// : SafeHandleZeroOrMinusOneIsInvalid
    {
        public MiNative.MI_Instance miInstance;
        public MiNative.MI_InstanceFT ft;
        public IntPtr handle;
        public static WeakReference _reference;

        // construct a reference to MiNative.MI_Instance
        internal InstanceHandle()
        {
            this.miInstance = new MiNative.MI_Instance();
            InstanceHandle._reference = new WeakReference(this.miInstance);
        }

        //Stores a pointer to the instance, as well as dereferences the pointer to store the struct
        internal InstanceHandle(IntPtr handle)
        {
            this.handle = handle;
            this.miInstance = Marshal.PtrToStructure<MiNative.MI_Instance>(handle);
            this.ft = Marshal.PtrToStructure<MiNative.MI_InstanceFT>(this.miInstance.ft);
            InstanceHandle._reference = new WeakReference(this.handle);
        }

        internal void AssertValidInternalState()
        {
            System.Diagnostics.Debug.Assert(this.handle != IntPtr.Zero, "InstanceHandle.AssertValidInternalState: this.handle != IntPtr.Zero");
        }

        ~InstanceHandle()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _isDisposed = false;
        protected void Dispose(bool disposing)
        {
            MiResult r;
            try
            {
              r = this.ft.Delete(ref this.miInstance); // Release instances created on the heap
            }
            catch (NullReferenceException e)
            {
                // Object already disposed, nothing to do
                _isDisposed = true;
                return;
            }
            Debug.Assert( MiResult.OK == r, "MI Client .NET API should guarantee that calls to MI_Instance_Delete always return MI_RESULT_OK");
            _isDisposed = true;
        }

        //internal delegate void OnHandleReleasedDelegate();
        //internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        //{
        //    throw new NotImplementedException();
        //}
        internal MiResult ReleaseHandleSynchronously()
        {
              throw new NotImplementedException();
        }

        //// should be part of SafeHandleZeroOrMinusOneIsInvalid
        public bool IsInvalid { get; }
    }

    internal class InstanceMethods
    {
        // Fields
        //internal static ValueType modopt(DateTime) modopt(IsBoxed) maxValidCimTimestamp;

        // Methods
        static InstanceMethods()
        {
        }
        internal InstanceMethods()
        {
        }
        ///
        /// takes an MI Instance, and adds to it a string representing the name of the instance, the MiValue representing the data value type,
        /// a type flag, and an MI Flag
        internal static MiResult AddElement(InstanceHandle instance, string name, object managedValue, MiType type, MiFlags flags)
        {
            int size;
            IntPtr pVal = IntPtr.Zero;
            // ConvertToUnmanaged
            ConvertToMiValuePtr(type, managedValue, ref pVal);

            //TODO: does this *really* need to be a pointer?  Can we use LPStr in the function signature for AddElement with no adverse effects?
            IntPtr pName = Marshal.StringToHGlobalAnsi(name);

            MiResult r = instance.ft.AddElement(instance.handle, pName, pVal, (uint)type, flags);

            return r;
        }
        internal static MiResult ClearElementAt(InstanceHandle instance, int index)
        {
            MiResult r = instance.ft.ClearElementAt(ref instance.miInstance, (uint)index);

            return r;
        }
        internal static MiResult Clone(InstanceHandle instanceHandleToClone, out InstanceHandle clonedInstanceHandle)
        {
            throw new NotImplementedException();
        }
        //internal static unsafe object ConvertFromMiValue(MiType type, _MiValue modopt(IsConst)* pmiValue);
        internal static object ConvertFromMiValue(MiType type, Byte pValue)
        {
            switch (type.ToString())
            {
                case "Boolean":
                case "Byte":
                case "SByte":
                case "UInt16":
                case "Sint16":
                case "SInt32":
                case "UInt32":
                case "SInt64":
                case "UInt64":
                        // no work to do...
                    val = pVal;
                    break;
                case "Real32":
                    // TODO: these seem to fail for unknown reasons.  When dereferencing in the AddElement function, they appear to work fine.
                    throw new NotImplementedException();
                    break;
                case "Real64":
                    throw new NotImplementedException();
                    break;
                default:
                    Debug.Assert(false, "Unknown MiType);
                    break;
            }
        }

        internal static void ConvertToMiValuePtr(MiType type, object managedValue, ref IntPtr pVal)
        {
            int typeSize = 0;
            switch (type.ToString())
            {
                    case "Boolean":
                        typeSize = Marshal.SizeOf<Boolean>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "Byte":
                        typeSize = Marshal.SizeOf<Byte>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "SByte":
                        typeSize = Marshal.SizeOf<SByte>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "UInt16":
                        typeSize = Marshal.SizeOf<UInt16>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "SInt16":
                        typeSize = Marshal.SizeOf<Int16>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "UInt32":
                        typeSize = Marshal.SizeOf<UInt32>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "SInt32":
                        typeSize = Marshal.SizeOf<Int32>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "SInt64":
                        typeSize = Marshal.SizeOf<Int64>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "UInt64":
                        typeSize = Marshal.SizeOf<UInt64>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "Real32":
                        typeSize = Marshal.SizeOf<System.Single>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "Real64":
                        typeSize = Marshal.SizeOf<System.Double>();
                        pVal = Marshal.AllocHGlobal(typeSize);
                        Marshal.StructureToPtr(managedValue, pVal, false);
                        break;
                    case "String":
                        pVal = Marshal.StringToHGlobalAnsi(managedValue.ToString());
                        break;
                    default:
                        Debug.Assert(false, "Unrecognized MiType");
                        break;
            }
        }

        //internal static unsafe void ConvertManagedObjectToMiDateTime(object managedValue, _MI_Datetime* pmiValue);
        //internal static unsafe object ConvertMiDateTimeToManagedObject(_MI_Datetime modopt(IsConst)* pmiValue);
        //internal static unsafe IEnumerable<DangerousHandleAccessor> ConvertToMiValue(MiType type, object managedValue, _MiValue* pmiValue);

        internal static MiResult GetClass(InstanceHandle instanceHandle, out ClassHandle classHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetClassName(InstanceHandle instance, out string className)
        {
            IntPtr pName;

            MiResult r = instance.ft.GetClassName(instance.handle, out pName);
            className = Marshal.PtrToStringAnsi(pName);

            return r;
        }
        internal static MiResult GetElement_GetIndex(InstanceHandle instance, string name, out int index)
        {
            index = -1;

            uint i;
            IntPtr v;
            IntPtr t;
            MiFlags f; //flags
            MiResult r = instance.ft.GetElement(instance.handle, name, out v, out t, out f, out i);
            index = (int)i;
            return r;
        }
        internal static MiResult GetElementAt_GetFlags(InstanceHandle instance, int index, out MiFlags flags)
        {
            Debug.Assert(instance.handle != IntPtr.Zero, "Caller should verify that instance is not null");
            Debug.Assert(index >= 0, "Caller should verify that index >=0");

            IntPtr pName;
            IntPtr v = IntPtr.Zero;
            MiType t;
            MiFlags f;

            MiResult r = instance.ft.GetElementAt(instance.handle, (uint)index, out pName, out v, out t, out f);
            flags = f; //Marshal.PtrToStructure<MiFlags>(f);
            return r;
        }
        internal static MiResult GetElementAt_GetName(InstanceHandle instance, int index, out string name)
        {
            IntPtr pName;
            IntPtr val=IntPtr.Zero;
            MiType type;
            MiFlags flags;

            MiResult r = instance.ft.GetElementAt(instance.handle, (uint)index, out pName, out val, out type, out flags);
            name = Marshal.PtrToStringAnsi(pName);
            return r;
        }
        internal static MiResult GetElementAt_GetType(InstanceHandle instance, int index, out MiType type)
        {
            IntPtr pName;
            IntPtr val=IntPtr.Zero;
            MiFlags flags;
            MiType t;
            MiResult r = instance.ft.GetElementAt(instance.handle, (uint)index, out pName, out val, out t, out flags);

            type = t; //Marshal.PtrToStructure<MiType>(t);
            return r;
        }

        internal static MiResult GetElementAt_GetValue(InstanceHandle instance, int index, out object val)
        {
            Debug.Assert(0 <= index, "Caller should verify index > 0");
            IntPtr name;
            IntPtr pVal;
            MiType type;
            MiFlags flags;
            val = null;

            MiResult r = instance.ft.GetElementAt(instance.handle, (uint)index, out name, out pVal, out type, out flags);

            if (r == MiResult.OK && pVal != null)
            {
                if (!flags.HasFlag(MiFlags.NULLFLAG))
                {
                    // ConvertToManaged
                }
                else
                {
                    val = null;
                }
            }

            return r;
        }
        internal static MiResult GetElementCount(InstanceHandle instance, out int elementCount)
        {
           uint count;
           MiResult r = instance.ft.GetElementCount(instance.handle, out count);
           elementCount = Convert.ToInt32(count);
           return r;
        }
        internal static MiResult GetNamespace(InstanceHandle instance, out string nameSpace)
        {
           IntPtr str;
           MiResult r = instance.ft.GetNameSpace(instance.handle, out str);
           nameSpace = Marshal.PtrToStringAnsi(str);
           return r;
        }
        internal static MiResult GetServerName(InstanceHandle instance, out string serverName)
        {
           IntPtr str;
           MiResult r = instance.ft.GetServerName(instance.handle, out str);
           serverName = Marshal.PtrToStringAnsi(str);
           return r;
        }
        //internal static unsafe void ReleaseMiValue(MiType type, _MiValue* pmiValue, IEnumerable<DangerousHandleAccessor> dangerousHandleAccessors);
        /*
         * This function should output a cleansed MiValue struct.  Structs are non-nullable types in C#, so
         * cannot point to null
         */
        internal static void ReleaseMiValue(MiType type, ref MiValue miValue)
        {
            miValue = new MiValue();
        }

        internal static MiResult SetElementAt_SetNotModifiedFlag(InstanceHandle handle, int index, [MarshalAs(UnmanagedType.U1)] bool notModifiedFlag)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetElementAt_SetValue(InstanceHandle instance, int index, object newValue)
        {
            throw new NotImplementedException();
            //MiResult r;
            //MiValue v = default(MiValue);
            //IntPtr t;
            //IntPtr f;
            //IntPtr noUse1;
            //// GetElementAt
            //r = instance.ft.GetElementAt(instance.handle, (uint)index, out noUse1, out v, out t, out f);
            //if (MiResult.OK != r)
            //        return r;

            //// SetElementAt
            //r = instance.ft.SetElementAt(ref instance.handle, (uint)index, v, (MiType)t, 0);
            //return r;
            // No need to release MiValues here as they do in managed C++ implementation.  MiValue is a struct and thus a value type.
        }
        internal static MiResult SetNamespace(InstanceHandle instance, string nameSpace)
        {
            return instance.ft.SetNameSpace(instance.handle, nameSpace);
        }

        internal static MiResult SetServerName(InstanceHandle instance, string serverName)
        {
            return instance.ft.SetServerName(instance.handle, serverName);
        }
        internal static void ThrowIfMismatchedType(MiType type, object managedValue)
        {
        // TODO: Strings are treated similar to primitive data types in C#.  They will be cleared out the same way. Only need to deal with the
        //        complex data types

        }
    }

    internal enum MiCallbackMode
    {
        CALLBACK_REPORT,
        CALLBACK_INQUIRE,
        CALLBACK_IGNORE
    }

    internal enum MiCancellationReason
    {
        None,
        Timeout,
        Shutdown,
        ServiceStop
    }

    [Flags]
    internal enum MiFlags : uint
    {
        ABSTRACT = 0x20000,
        ADOPT = 0x80000000,
        ANY = 0x7f,
        ASSOCIATION = 0x10,
        BORROW = 0x40000000,
        CLASS = 1,
        DISABLEOVERRIDE = 0x100,
        ENABLEOVERRIDE = 0x80,
        EXPENSIVE = 0x80000,
        IN = 0x2000,
        INDICATION = 0x20,
        KEY = 0x1000,
        METHOD = 2,
        NOTMODIFIED = 0x2000000,
        NULLFLAG = 0x20000000,
        OUT = 0x4000,
        PARAMETER = 8,
        PROPERTY = 4,
        READONLY = 0x200000,
        REFERENCE = 0x40,
        REQUIRED = 0x8000,
        RESTRICTED = 0x200,
        STATIC = 0x10000,
        STREAM = 0x100000,
        TERMINAL = 0x40000,
        TOSUBCLASS = 0x400,
        TRANSLATABLE = 0x800
    }

    [Flags]
    internal enum MiOperationFlags : uint
    {
        BasicRtti = 2,
        ExpensiveProperties = 0x40,
        FullRtti = 4,
        LocalizedQualifiers = 8,
        ManualAckResults = 1,
        NoRtti = 0x400,
        PolymorphismDeepBasePropsOnly = 0x180,
        PolymorphismShallow = 0x80,
        ReportOperationStarted = 0x200,
        StandardRtti = 0x800
    }

    internal enum MiPromptType
    {
        PROMPTTYPE_NORMAL,
        PROMPTTYPE_CRITICAL
    }

    internal enum MIResponseType
    {
        MIResponseTypeNo,
        MIResponseTypeYes,
        MIResponseTypeNoToAll,
        MIResponseTypeYesToAll
    }

    internal enum MiResult
    {
        ACCESS_DENIED = 2,
        ALREADY_EXISTS = 11,
        CLASS_HAS_CHILDREN = 8,
        CLASS_HAS_INSTANCES = 9,
        CONTINUATION_ON_ERROR_NOT_SUPPORTED = 0x1a,
        FAILED = 1,
        FILTERED_ENUMERATION_NOT_SUPPORTED = 0x19,
        INVALID_CLASS = 5,
        INVALID_ENUMERATION_CONTEXT = 0x15,
        INVALID_NAMESPACE = 3,
        INVALID_OPERATION_TIMEOUT = 0x16,
        INVALID_PARAMETER = 4,
        INVALID_QUERY = 15,
        INVALID_SUPERCLASS = 10,
        METHOD_NOT_AVAILABLE = 0x10,
        METHOD_NOT_FOUND = 0x11,
        NAMESPACE_NOT_EMPTY = 20,
        NO_SUCH_PROPERTY = 12,
        NOT_FOUND = 6,
        NOT_SUPPORTED = 7,
        OK = 0,
        PULL_CANNOT_BE_ABANDONED = 0x18,
        PULL_HAS_BEEN_ABANDONED = 0x17,
        QUERY_LANGUAGE_NOT_SUPPORTED = 14,
        SERVER_IS_SHUTTING_DOWN = 0x1c,
        SERVER_LIMITS_EXCEEDED = 0x1b,
        TYPE_MISMATCH = 13
    }

    internal enum MiSubscriptionDeliveryType
    {
        SubscriptionDeliveryType_Pull = 1,
        SubscriptionDeliveryType_Push = 2
    }

    /*
    **==============================================================================
    **
    ** MI_Timestamp
    **
    **     Represents a timestamp as described in the CIM Infrastructure 
    **     specification
    **
    **     [1] MI_ee DSP0004 (http://www.dmtf.org/standards/published_documents)
    **
    **==============================================================================
    */

    struct MI_Timestamp
    {
        /* YYYYMMDDHHMMSS.MMMMMMSUTC */
        MI_Uint32 year;
        MI_Uint32 month;
        MI_Uint32 day;
        MI_Uint32 hour;
        MI_Uint32 minute;
        MI_Uint32 second;
        MI_Uint32 microseconds;
        MI_Sint32 utc;
    }

    /*
    **==============================================================================
    **
    ** struct MI_Interval
    **
    **     Represents an interval as described in the CIM Infrastructure 
    **     specification. This structure is padded to have the same length
    **     as a MI_Timestamp structure.
    **
    **     [1] MI_ee DSP0004 (http://www.dmtf.org/standards/published_documents)
    **
    **==============================================================================
    */

    struct MI_Interval
    {
        /* DDDDDDDDHHMMSS.MMMMMM:000 */
        MI_Uint32 days;
        MI_Uint32 hours;
        MI_Uint32 minutes;
        MI_Uint32 seconds;
        MI_Uint32 microseconds;
        //MI_Uint32 __padding1;
        //MI_Uint32 __padding2;
        //MI_Uint32 __padding3;
    }

    /*
    **==============================================================================
    **
    ** struct MI_Datetime
    **
    **     Represents a CIM datetime type as described in the CIM Infrastructure
    **     specification. It contains a union of MI_Timestamp and MI_Interval.
    **
    **==============================================================================
    */

    public struct MI_Datetime
    {
        MI_Uint32 isTimestamp;
        struct u
        {
            MI_Timestamp timestamp;
            MI_Interval interval;
        }
    }

    /*
    **==============================================================================
    **
    ** struct MI_<TYPE>A
    **
    **     These structure represent arrays of the types introduced above.
    **     * Porting notes:
    **        - The data 
    **==============================================================================
    */
    [StructLayout(LayoutKind.Sequential)]
    public struct MI_BooleanA
    {
        public IntPtr data;
        public MI_Uint32 size;
    }

    struct MI_Uint8A
    {
        public IntPtr data;
        public MI_Uint32 size;
    }

    public struct MI_Sint8A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Uint16A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Sint16A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Uint32A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Sint32A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Uint64A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Sint64A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Real32A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Real64A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Char16A
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_DatetimeA
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_StringA
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_ReferenceA
    {
        public IntPtr data; //struct _MI_Instance** data;
        MI_Uint32 size;
    }

    public struct MI_InstanceA
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    public struct MI_Array
    {
        public IntPtr data;
        MI_Uint32 size;
    }

    internal enum MiType:uint
    {
        Boolean,
        UInt8,
        SInt8,
        UInt16,
        SInt16,
        UInt32,
        SInt32,
        UInt64,
        SInt64,
        Real32,
        Real64,
        Char16,
        DateTime,
        String,
        Reference,
        Instance,
        BooleanArray,
        UInt8Array,
        SInt8Array,
        UInt16Array,
        SInt16Array,
        UInt32Array,
        SInt32Array,
        UInt64Array,
        SInt64Array,
        Real32Array,
        Real64Array,
        Char16Array,
        DateTimeArray,
        StringArray,
        ReferenceArray,
        InstanceArray
    }

    /*
    **==============================================================================
    **
    ** union MiValue
    **
    **     This structure defines a union of all CIM data types.
    **
    **==============================================================================
    */
    [StructLayout(LayoutKind.Explicit)]
    public struct MiValue
    {
        [FieldOffset(0)] public MI_Boolean boolean;
        [FieldOffset(0)] public System.Byte uint8;
        [FieldOffset(0)] public System.SByte sint8;
        [FieldOffset(0)] public System.UInt16 uint16;
        [FieldOffset(0)] public System.Int16 sint16;
        [FieldOffset(0)] public System.UInt32 uint32;
        [FieldOffset(0)] public System.Int32 sint32;
        [FieldOffset(0)] public System.UInt64 uint64;
        [FieldOffset(0)] public System.Int64 sint64;
        [FieldOffset(0)] public System.Single real32;
        [FieldOffset(0)] public MI_Real64 real64;
        [FieldOffset(16)] public MI_Char16 char16;
        [FieldOffset(24)] public MI_Datetime datetime;
        [FieldOffset(32)] [MarshalAs(UnmanagedType.LPStr)] public string mistring;  // MI_Char* string;
        ////[FieldOffset(40)]
        //public IntPtr instance;   // MI_Instance *instance
        ////[FieldOffset(48)]
        //public IntPtr reference;  // MI_Instance *reference
        ////[FieldOffset(48)]
        //public MI_BooleanA booleana;
        ////[FieldOffset(48)]
        //public MI_Uint8A uint8a;
        ////[FieldOffset(48)]
        //public MI_Sint8A sint8a;
        ////[FieldOffset(48)]
        //public MI_Uint16A uint16a;
        ////[FieldOffset(48)]
        //public MI_Sint16A sint16a;
        ////[FieldOffset(48)]
        //public MI_Uint32A uint32a;
        ////[FieldOffset(48)]
        //public MI_Sint32A sint32a;
        ////[FieldOffset(48)]
        //public MI_Uint64A uint64a;
        ////[FieldOffset(48)]
        //public MI_Sint64A sint64a;
        ////[FieldOffset(48)]
        //public MI_Real32A real32a;
        ////[FieldOffset(48)]
        //public MI_Real64A real64a;
        ////[FieldOffset(48)]
        //public MI_Char16A char16a;
        ////[FieldOffset(48)]
        //public MI_DatetimeA datetimea;
        ////[FieldOffset(48)]
        //public MI_StringA stringa;
        ////[FieldOffset(48)]
        //public MI_ReferenceA referencea;
        ////[FieldOffset(48)]
        //public MI_InstanceA instancea;
        ////[FieldOffset(48)]
        //public MI_Array array;
    }

    internal enum MIWriteMessageChannel
    {
        MIWriteMessageChannelWarning,
        MIWriteMessageChannelVerbose,
        MIWriteMessageChannelDebug
    }

    internal class NativeCimCredential
    {
        // Methods
        private NativeCimCredential()
        {
            throw new NotImplementedException();
        }
        internal static void CreateCimCredential(string authenticationMechanism, out NativeCimCredentialHandle credentialHandle)
        {
            throw new NotImplementedException();
        }
        internal static void CreateCimCredential(string authenticationMechanism, string certificateThumbprint, out NativeCimCredentialHandle credentialHandle)
        {
            throw new NotImplementedException();
        }
        internal static void CreateCimCredential(string authenticationMechanism, string domain, string userName, SecureString password, out NativeCimCredentialHandle credentialHandle)
        {
            throw new NotImplementedException();
        }
    }

    internal class NativeCimCredentialHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }

    internal class OperationCallbackProcessingContext
    {
        // Fields
        //private bool inUserCode;
        //private object managedOperationContext;

        // Methods
        internal OperationCallbackProcessingContext(object managedOperationContext)
        {
            throw new NotImplementedException();
        }

        // Properties
        internal bool InUserCode {[return: MarshalAs(UnmanagedType.U1)] get;[param: MarshalAs(UnmanagedType.U1)] set; }
        internal object ManagedOperationContext { get; }
    }

    internal class OperationCallbacks
    {
        // Fields
        //private ClassCallbackDelegate<backing_store> ClassCallback;
        //private IndicationResultCallbackDelegate<backing_store> IndicationResultCallback;
        //private InstanceResultCallbackDelegate<backing_store> InstanceResultCallback;
        //private InternalErrorCallbackDelegate<backing_store> InternalErrorCallback;
        //private object <backing_store>ManagedOperationContext;
        //private PromptUserCallbackDelegate<backing_store> PromptUserCallback;
        //private StreamedParameterCallbackDelegate<backing_store> StreamedParameterCallback;
        //private WriteErrorCallbackDelegate<backing_store> WriteErrorCallback;
        //private WriteMessageCallbackDelegate<backing_store> WriteMessageCallback;
        //private WriteProgressCallbackDelegate<backing_store> WriteProgressCallback;
        //private static Action<Action, Func<Exception, bool>, Action<Exception>> userFilteredExceptionHandler;

        // Methods
        static OperationCallbacks()
        {
            //TODO: Implement
            //throw new NotImplementedException();
        }
        public OperationCallbacks()
        {
            //TODO: Implement
            //throw new NotImplementedException();
        }
        internal static void InvokeWithUserFilteredExceptionHandler(Action tryBody, Func<Exception, bool> userFilter, Action<Exception> catchBody)
        {
            throw new NotImplementedException();
        }
        //[return: MarshalAs(UnmanagedType.U1)]
        //internal unsafe bool SetMiOperationCallbacks(_MI_OperationCallbacks* pmiOperationCallbacks, MI_OperationWrapper* pmiOperationWrapper);

        // Properties
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal ClassCallbackDelegate ClassCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal IndicationResultCallbackDelegate IndicationResultCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal InstanceResultCallbackDelegate InstanceResultCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal InternalErrorCallbackDelegate InternalErrorCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp and in cs/CimOperationOptions.cs")]
        internal object ManagedOperationContext { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal PromptUserCallbackDelegate PromptUserCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal StreamedParameterCallbackDelegate StreamedParameterCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal WriteErrorCallbackDelegate WriteErrorCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal WriteMessageCallbackDelegate WriteMessageCallback { get; set; }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "False positive from FxCop - this property is used in nativeOperationCallbacks.cpp")]
        internal WriteProgressCallbackDelegate WriteProgressCallback { get; set; }

        // Nested Types
        internal delegate void ClassCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, ClassHandle classHandle, [MarshalAs(UnmanagedType.U1)] bool moreResults, MiResult resultCode, string errorString, InstanceHandle errorDetails);

        internal delegate void IndicationResultCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, InstanceHandle instanceHandle, string bookmark, string machineID, [MarshalAs(UnmanagedType.U1)] bool moreResults, MiResult resultCode, string errorString, InstanceHandle errorDetails);

        internal delegate void InstanceResultCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, InstanceHandle instanceHandle, [MarshalAs(UnmanagedType.U1)] bool moreResults, MiResult resultCode, string errorString, InstanceHandle errorDetails);

        internal delegate void InternalErrorCallbackDelegate(OperationCallbackProcessingContext callbackContextWhereInternalErrorOccurred, Exception exception);

        internal delegate void PromptUserCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, string message, MiPromptType promptType, out MIResponseType response);

        internal delegate void StreamedParameterCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, string parameterName, object parameterValue, MiType parameterType);

        internal delegate void WriteErrorCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, InstanceHandle instanceHandle, out MIResponseType response);

        internal delegate void WriteMessageCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, uint channel, string message);

        internal delegate void WriteProgressCallbackDelegate(OperationCallbackProcessingContext callbackProcessingContext, OperationHandle operationHandle, string activity, string currentOperation, string statusDescription, uint percentageComplete, uint secondsRemaining);
    }

    internal class OperationCallbacksDefinitions
    {
        // Methods
        public OperationCallbacksDefinitions()
        {
            throw new NotImplementedException();
        }
        //internal static unsafe void ClassAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Class modopt(IsConst)* pmiClass, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void IndicationAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, ushort modopt(IsConst)* bookmark, ushort modopt(IsConst)* machineID, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void InstanceResultAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void PromptUserAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszMessage, _MI_PromptType promptType, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult);
        //internal static unsafe void StreamedParameterResultAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszParameterName, _MiType miType, _MiValue modopt(IsConst)* pmiParameterValue, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);
        //internal static unsafe void WriteErrorAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance* pmiInstance, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult);
        //internal static unsafe void WriteMessageAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, uint channel, ushort modopt(IsConst)* wszMessage);
        //internal static unsafe void WriteProgressAppDomainProxy(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszActivity, ushort modopt(IsConst)* wszCurrentOperation, ushort modopt(IsConst)* wszStatusDescription, uint percentageComplete, uint secondsRemaining);

        // Nested Types
        //internal unsafe delegate void ClassAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Class modopt(IsConst)* pmiClass, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void IndicationAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, ushort modopt(IsConst)* bookmark, ushort modopt(IsConst)* machineID, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void InstanceResultAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance modopt(IsConst)* pmiInstance, byte moreResults, _MI_Result resultCode, ushort modopt(IsConst)* errorString, _MI_Instance modopt(IsConst)* pmiErrorDetails, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void PromptUserAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszMessage, _MI_PromptType promptType, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) promptUserResult);

        //internal unsafe delegate void StreamedParameterResultAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszParameterName, _MiType miType, _MiValue modopt(IsConst)* pmiParameterValue, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*) resultAcknowledgement);

        //internal unsafe delegate void WriteErrorAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, _MI_Instance* pmiInstance, _MI_Result modopt(CallConvCdecl) *(_MI_Operation*, _MI_OperationCallback_ResponseType) writeErrorResult);

        //internal unsafe delegate void WriteMessageAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, uint channel, ushort modopt(IsConst)* wszMessage);

        //internal unsafe delegate void WriteProgressAppDomainProxyDelegate(_MI_Operation* pmiOperation, void* callbackContext, ushort modopt(IsConst)* wszActivity, ushort modopt(IsConst)* wszCurrentOperation, ushort modopt(IsConst)* wszStatusDescription, uint percentageComplete, uint secondsRemaining);
    }

    internal class OperationHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public MiNative.MI_OperationOptions miOperationOptions;
        internal void AssertValidInternalState()
        {
            this.miOperationOptions = new MiNative.MI_OperationOptions();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }

    internal class OperationMethods
    {
        // Methods
        private OperationMethods()
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult Cancel(OperationHandle operationHandle, MiCancellationReason cancellationReason)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult GetClass(OperationHandle operationHandle, out ClassHandle classHandle, out bool moreResults, out MiResult result, out string errorMessage, out InstanceHandle completionDetails)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult GetIndication(OperationHandle operationHandle, out InstanceHandle instanceHandle, out string bookmark, out string machineID, out bool moreResults, out MiResult result, out string errorMessage, out InstanceHandle completionDetails)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle", Justification = "C# layer internally manages the lifetime of OperationHandle + have to do this to call inline methods")]
        internal static MiResult GetInstance(OperationHandle operationHandle, out InstanceHandle instanceHandle, out bool moreResults, out MiResult result, out string errorMessage, out InstanceHandle completionDetails)
        {
            throw new NotImplementedException();
        }
    }

    internal class OperationOptionsHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public MiNative.MI_OperationOptions miOperationOptions;
        internal OperationOptionsHandle()
        {
            // 
            this.miOperationOptions = new MiNative.MI_OperationOptions();
        }

        internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }

    internal class OperationOptionsMethods
    {
        // Methods
        private OperationOptionsMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(OperationOptionsHandle operationOptionsHandle, out OperationOptionsHandle newOperationOptionsHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetPromptUserModeOption(OperationOptionsHandle operationOptionsHandle, out MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetResourceUri(OperationOptionsHandle operationOptionsHandle, out string resourceUri)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetResourceUriPrefix(OperationOptionsHandle operationOptionsHandle, out string resourceUriPrefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetTimeout(OperationOptionsHandle operationOptionsHandle, out TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetUseMachineID(OperationOptionsHandle operationOptionsHandle, out bool useMachineId)
        {
            throw new NotImplementedException();
        }
        internal static MiResult GetWriteErrorModeOption(OperationOptionsHandle operationOptionsHandle, out MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetCustomOption(OperationOptionsHandle operationOptionsHandle, string optionName, object optionValue, MiType miType, [MarshalAs(UnmanagedType.U1)] bool mustComply)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDisableChannelOption(OperationOptionsHandle operationOptionsHandle, uint channel)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetEnableChannelOption(OperationOptionsHandle operationOptionsHandle, uint channel)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetOption(OperationOptionsHandle operationOptionsHandle, string optionName, string optionValue)
        {
            //TODO: Implement
            return MiResult.OK;
            //throw new NotImplementedException();
        }
        internal static MiResult SetOption(OperationOptionsHandle operationOptionsHandle, string optionName, uint optionValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPromptUserModeOption(OperationOptionsHandle operationOptionsHandle, MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetPromptUserRegularMode(OperationOptionsHandle operationOptionsHandle, MiCallbackMode mode, [MarshalAs(UnmanagedType.U1)] bool ackValue)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetResourceUri(OperationOptionsHandle operationOptionsHandle, string resourceUri)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetResourceUriPrefix(OperationOptionsHandle operationOptionsHandle, string resourceUriPrefix)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetTimeout(OperationOptionsHandle operationOptionsHandle, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetUseMachineID(OperationOptionsHandle operationOptionsHandle, [MarshalAs(UnmanagedType.U1)] bool useMachineId)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetWriteErrorModeOption(OperationOptionsHandle operationOptionsHandle, MiCallbackMode mode)
        {
            throw new NotImplementedException();
        }
    }

    internal class SerializerHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }

    internal class SerializerMethods
    {
        // Methods
        private SerializerMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult SerializeClass(SerializerHandle serializerHandle, uint flags, ClassHandle instanceHandle, byte[] outputBuffer, uint offset, out uint outputBufferUsed)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SerializeInstance(SerializerHandle serializerHandle, uint flags, InstanceHandle instanceHandle, byte[] outputBuffer, uint offset, out uint outputBufferUsed)
        {
            throw new NotImplementedException();
        }
    }

    internal class SessionHandleCallbackDefinitions
    {
        // Methods
        public SessionHandleCallbackDefinitions()
        {
            throw new NotImplementedException();
        }
        //internal static unsafe void SessionHandle_ReleaseHandle_CallbackWrapper_Invoke_Managed(_SessionHandle_ReleaseHandle_CallbackWrapper* pCallbackWrapper);
        //internal static unsafe void SessionHandle_ReleaseHandle_CallbackWrapper_Release_Managed(_SessionHandle_ReleaseHandle_CallbackWrapper* pCallbackWrapper);

        // Nested Types
        //internal unsafe delegate void SessionHandle_ReleaseHandle_CallbackWrapper_Invoke_Managed_Delegate(_SessionHandle_ReleaseHandle_CallbackWrapper* pCallbackWrapper);

        //internal unsafe delegate void SessionHandle_ReleaseHandle_CallbackWrapper_Release_Managed_Delegate(_SessionHandle_ReleaseHandle_CallbackWrapper* pCallbackWrapper);
    }

    internal class SessionHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }

    internal class SessionMethods
    {
        // Methods
        private SessionMethods()
        {
            throw new NotImplementedException();
        }
        internal static void AssociatorInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle sourceInstance, string assocClass, string resultClass, string sourceRole, string resultRole, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void CreateInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void DeleteInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void EnumerateClasses(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, [MarshalAs(UnmanagedType.U1)] bool classNamesOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void EnumerateInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void GetClass(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void GetInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void Invoke(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string className, string methodName, InstanceHandle instanceHandleForTargetOfInvocation, InstanceHandle instanceHandleForMethodParameters, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void ModifyInstance(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle instanceHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "keysOnly")]
        internal static void QueryInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string queryDialect, string queryExpression, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void ReferenceInstances(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, InstanceHandle sourceInstance, string associationClassName, string sourceRole, [MarshalAs(UnmanagedType.U1)] bool keysOnly, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void Subscribe(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationOptionsHandle operationOptionsHandle, string namespaceName, string queryDialect, string queryExpression, SubscriptionDeliveryOptionsHandle subscriptionDeliveryOptionsHandle, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
        internal static void TestConnection(SessionHandle sessionHandle, MiOperationFlags operationFlags, OperationCallbacks operationCallbacks, out OperationHandle operationHandle)
        {
            throw new NotImplementedException();
        }
    }

    internal class SubscriptionDeliveryOptionsHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal void AssertValidInternalState()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
        internal delegate void OnHandleReleasedDelegate();
        internal MiResult ReleaseHandleAsynchronously(OnHandleReleasedDelegate completionCallback)
        {
            throw new NotImplementedException();
        }
        internal MiResult ReleaseHandleSynchronously()
        {
            throw new NotImplementedException();
        }
    }

    internal class SubscriptionDeliveryOptionsMethods
    {
        // Methods
        private SubscriptionDeliveryOptionsMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult AddCredentials(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, NativeCimCredentialHandle credentials, uint flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(SubscriptionDeliveryOptionsHandle subscriptionDeliveryOptionsHandle, out SubscriptionDeliveryOptionsHandle newSubscriptionDeliveryOptionsHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDateTime(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, object value, uint flags)
        {
            throw new NotImplementedException();
        }
        //internal static MiResult SetInterval(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, ValueType modopt(TimeSpan) modopt(IsBoxed) value, uint flags)
        internal static MiResult SetInterval(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, ValueType value, uint flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetNumber(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, uint value, uint flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetString(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, string value, uint flags)
        {
            throw new NotImplementedException();
        }
    }


}
