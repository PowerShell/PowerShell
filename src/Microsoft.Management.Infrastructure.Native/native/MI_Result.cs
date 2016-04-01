using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeObject
{
    public enum MI_Result : uint
    {
        /* The operation was successful */
        MI_RESULT_OK = 0,

        /* A general error occurred, not covered by a more specific error code. */
        MI_RESULT_FAILED = 1,

        /* Access to a CIM resource is not available to the client. */
        MI_RESULT_ACCESS_DENIED = 2,

        /* The target namespace does not exist. */
        MI_RESULT_INVALID_NAMESPACE = 3,

        /* One or more parameter values passed to the method are not valid. */
        MI_RESULT_INVALID_PARAMETER  = 4,

        /* The specified class does not exist. */
        MI_RESULT_INVALID_CLASS = 5,

        /* The requested object cannot be found. */
        MI_RESULT_NOT_FOUND = 6,

        /* The requested operation is not supported. */
        MI_RESULT_NOT_SUPPORTED = 7,

        /* The operation cannot be invoked because the class has subclasses. */
        MI_RESULT_CLASS_HAS_CHILDREN = 8,

        /* The operation cannot be invoked because the class has instances. */
        MI_RESULT_CLASS_HAS_INSTANCES = 9,

        /* The operation cannot be invoked because the superclass does not exist. */
        MI_RESULT_INVALID_SUPERCLASS = 10,

        /* The operation cannot be invoked because an object already exists. */
        MI_RESULT_ALREADY_EXISTS = 11,

        /* The specified property does not exist. */
        MI_RESULT_NO_SUCH_PROPERTY = 12,

        /* The value supplied is not compatible with the type. */
        MI_RESULT_TYPE_MISMATCH = 13,

        /* The query language is not recognized or supported. */
        MI_RESULT_QUERY_LANGUAGE_NOT_SUPPORTED = 14,

        /* The query is not valid for the specified query language. */
        MI_RESULT_INVALID_QUERY = 15,

        /* The extrinsic method cannot be invoked. */
        MI_RESULT_METHOD_NOT_AVAILABLE = 16,

        /* The specified extrinsic method does not exist. */
        MI_RESULT_METHOD_NOT_FOUND = 17,

        /* The specified namespace is not empty. */
        MI_RESULT_NAMESPACE_NOT_EMPTY = 20,

        /* The enumeration identified by the specified context is invalid. */
        MI_RESULT_INVALID_ENUMERATION_CONTEXT = 21,

        /* The specified operation timeout is not supported by the CIM Server. */
        MI_RESULT_INVALID_OPERATION_TIMEOUT = 22,

        /* The Pull operation has been abandoned. */
        MI_RESULT_PULL_HAS_BEEN_ABANDONED = 23,

        /* The attempt to abandon a concurrent Pull operation failed. */
        MI_RESULT_PULL_CANNOT_BE_ABANDONED = 24,

        /* Using a filter in the enumeration is not supported by the CIM server. */
        MI_RESULT_FILTERED_ENUMERATION_NOT_SUPPORTED = 25,

        /* The CIM server does not support continuation on error. */
        MI_RESULT_CONTINUATION_ON_ERROR_NOT_SUPPORTED = 26,

        /* The operation failed because server limits were exceeded. */
        MI_RESULT_SERVER_LIMITS_EXCEEDED = 27,

        /* The CIM server is shutting down and cannot process the operation. */
        MI_RESULT_SERVER_IS_SHUTTING_DOWN = 28
    }
}
