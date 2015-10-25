/*
**==============================================================================
**
** Open Management Infrastructure (OMI)
**
** Copyright (c) Microsoft Corporation
** 
** Licensed under the Apache License, Version 2.0 (the "License"); you may not 
** use this file except in compliance with the License. You may obtain a copy 
** of the License at 
**
**     http://www.apache.org/licenses/LICENSE-2.0 
**
** THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
** KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED 
** WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, 
** MERCHANTABLITY OR NON-INFRINGEMENT. 
**
** See the Apache 2 License for the specific language governing permissions 
** and limitations under the License.
**
**==============================================================================
*/

#pragma once

#include <stdarg.h>
#include <stddef.h>
#include <stdlib.h>
#include <string.h>
#include <wchar.h>
#include <limits.h>
#include <stdint.h>

#ifndef NAME_MAX
#  define NAME_MAX 255
#endif

/*
**==============================================================================
**
**  Windows-specific types.  Defined here for PAL on Non-Windows platforms.
**
**==============================================================================
*/

#ifdef _MSC_VER
    #ifndef WIN32_FROM_HRESULT
        #define WIN32_FROM_HRESULT(hr) (HRESULT_FACILITY(hr) == FACILITY_WIN32 ? HRESULT_CODE(hr) : hr) 
    #endif    
#else
    #define WIN32_FROM_HRESULT(hr) hr
    #define HRESULT_FROM_WIN32(error) error
    typedef unsigned long DWORD, *LPDWORD;
    typedef char BOOL;
	typedef unsigned short WCHAR_T;
	typedef unsigned int UINT32;
    typedef int INT32;
    typedef unsigned long HRESULT;
    typedef const wchar_t *PCWSTR;
    typedef wchar_t *PWSTR;
    typedef const char *PCSTR;
    typedef char *PSTR;
    typedef void *PVOID;
    typedef PVOID HANDLE;
    typedef uint32_t UINT;
    typedef char BYTE;
    #define NO_ERROR 0
    #define INFINITE 0xFFFFFFFF
    #define WINAPI
    #define S_OK 0
    #define TRUE 1
    #define FALSE 0
    #define ERROR_INVALID_PARAMETER 87
    #define ERROR_OUTOFMEMORY 14
    #define ERROR_BAD_ENVIRONMENT 0x0000000A
    #define ERROR_TOO_MANY_OPEN_FILES 0x00000004
    #define ERROR_INSUFFICIENT_BUFFER 0x0000007A
    #define ERROR_NO_ASSOCIATION 0x00000483
    #define ERROR_NO_SUCH_USER 0x00000525
    #define ERROR_INVALID_FUNCTION 0x00000001
    #define MAX_PATH 0x00000104
    #define ERROR_INVALID_ADDRESS 0x000001e7
    #define ERROR_GEN_FAILURE 0x0000001F
    #define ERROR_ACCESS_DENIED 0x00000005
    #define ERROR_INVALID_NAME 0x0000007B
    #define ERROR_STOPPED_ON_SYMLINK 0x000002A9
    #define ERROR_BUFFER_OVERFLOW 0x0000006F
    #define ERROR_FILE_NOT_FOUND 0x00000002
    #define ERROR_BAD_PATH_NAME 0x000000A1

    typedef unsigned long long uint64;
#endif

/*
**==============================================================================
**
**  __FUNCTION__
**
**==============================================================================
*/

#if !defined(CONFIG_HAVE_FUNCTION_MACRO)
# if !defined(_MSC_VER) && !defined(__FUNCTION__)
#  define __FUNCTION__ "<unknown>"
# endif
#endif

/*
**==============================================================================
**
** PAL_32BIT
** PAL_64BIT
**
**==============================================================================
*/
#if defined(__GNUC__)
# if defined(__i386__)
# define PAL_32BIT
# elif defined(__x86_64__)
# define PAL_64BIT
# elif defined(__ppc__)
# define PAL_32BIT
# elif defined(__ppc64__)
# define PAL_64BIT
# elif ((ULONG_MAX) == (UINT_MAX) && (ULONG_MAX == 0xFFFFFFFF))
# define PAL_32BIT
# else
# define PAL_64BIT
# endif
#elif defined(_MSC_VER)
# if defined(_WIN64)
# define PAL_64BIT
# else
# define PAL_32BIT
# endif
#elif ((ULONG_MAX) == (UINT_MAX) && (ULONG_MAX == 0xFFFFFFFF))
# define PAL_32BIT
#else
# define PAL_64BIT
#endif

/*
**==============================================================================
**
** PAL_CALL macro
**
**==============================================================================
*/
#ifdef _MSC_VER
# define PAL_CALL __stdcall
#else
# define PAL_CALL
#endif

/*
**==============================================================================
**
** PAL_EXPORT_API, PAL_IMPORT_API macros
**
**==============================================================================
*/

#if defined(_MSC_VER)
# define PAL_EXPORT_API __declspec(dllexport)
# define PAL_IMPORT_API __declspec(dllimport)
#elif defined(__GNUC__)
# define PAL_EXPORT_API __attribute__((visibility("default")))
# define PAL_IMPORT_API /* empty */
#elif defined(sun)
# define PAL_EXPORT_API __global 
# define PAL_IMPORT_API /* empty */
#else
# define PAL_EXPORT_API /* empty */
# define PAL_IMPORT_API /* empty */
#endif

/*
**==============================================================================
**
** PAL_BEGIN_EXTERNC
** PAL_END_EXTERNC
**
**==============================================================================
*/

#if defined(__cplusplus)
# define PAL_BEGIN_EXTERNC extern "C" {
# define PAL_END_EXTERNC }
#else
# define PAL_BEGIN_EXTERNC
# define PAL_END_EXTERNC
#endif

/*
**==============================================================================
**
** PAL_EXTERN_C
**
**==============================================================================
*/
#ifdef __cplusplus
# define PAL_EXTERN_C extern "C"
#else
# define PAL_EXTERN_C extern
#endif /* __cplusplus */

/*
**==============================================================================
**
** PAL_INLINE macro
**
**==============================================================================
*/

#if defined(_MSC_VER)
# define PAL_INLINE static __inline
#elif defined(__GNUC__)
# define PAL_INLINE static __inline
#elif defined(sun)
# define PAL_INLINE static inline
#elif defined(__PPC)
# define PAL_INLINE __inline__
#else
# define PAL_INLINE static __inline
#endif
   
/*
**==============================================================================
**
** PAL string
**
**==============================================================================
*/

#if defined(CONFIG_ENABLE_WCHAR)
typedef wchar_t PAL_Char;
#else
typedef char PAL_Char;
#endif

typedef PAL_Char TChar;

/*
**==============================================================================
**
** PAL_T()
**
**==============================================================================
*/

#if defined(CONFIG_ENABLE_WCHAR)
# define __PAL_T(STR) L ## STR
# define PAL_T(STR) __PAL_T(STR)
#else
# define PAL_T(STR) STR
#endif

/*
**==============================================================================
**
** PAL_UNUSED
**
**==============================================================================
*/

#define PAL_UNUSED(X) ((void)X)

/*
**==============================================================================
**
** PAL_HAVE_POSIX
**
**==============================================================================
*/

#if defined(linux) || defined(sun) || defined(hpux) || defined(aix)
# define PAL_HAVE_POSIX
#endif

/*
**==============================================================================
**
** PAL_HAVE_PTHREADS
**
**==============================================================================
*/

#if defined(linux) | defined(sun) | defined(hpux) | defined(aix)
# define PAL_HAVE_PTHREADS
#endif

/*
**==============================================================================
**
** Basic types
**
**==============================================================================
*/

typedef unsigned char PAL_Uint8;
#define PAL_UINT8_MAX (UCHAR_MAX)

typedef signed char PAL_Sint8;
#define PAL_SINT8_MIN (SCHAR_MIN)
#define PAL_SINT8_MAX (SCHAR_MAX)

typedef unsigned short PAL_Uint16;
#define PAL_UINT16_MAX (USHRT_MAX)

typedef signed short PAL_Sint16;
#define PAL_SINT16_MIN (SHRT_MIN)
#define PAL_SINT16_MAX (SHRT_MAX)

typedef unsigned int PAL_Uint32;
#define PAL_UINT32_MAX (UINT_MAX)

typedef signed int PAL_Sint32;
#define PAL_SINT32_MIN (INT_MIN)
#define PAL_SINT32_MAX (INT_MAX)

#if defined(_MSC_VER)
typedef unsigned __int64 PAL_Uint64;
typedef signed __int64 PAL_Sint64;
#else
typedef unsigned long long PAL_Uint64;
typedef signed long long PAL_Sint64;
#endif
#define PAL_UINT64_MIN ((PAL_Uint64)                    0ULL)
#define PAL_UINT64_MAX ((PAL_Uint64) 18446744073709551615ULL)
#define PAL_SINT64_MIN ((PAL_Sint64) (-9223372036854775807LL - 1LL))
#define PAL_SINT64_MAX ((PAL_Sint64)   9223372036854775807LL      )

typedef unsigned char PAL_Boolean;

#define PAL_TRUE ((PAL_Boolean)1)
#define PAL_FALSE ((PAL_Boolean)0)

/*
**==============================================================================
**
** Function calling conventions
**
**==============================================================================
*/

#ifdef _MSC_VER
# define PAL_CDECLAPI __cdecl
#else
# define PAL_CDECLAPI
#endif

#define ATEXIT_API PAL_CDECLAPI

/*
**==============================================================================
**
** SAL Notation for non-Windows platforms
**
**==============================================================================
*/

#if !defined(_MSC_VER)

# ifndef _In_
#  define _In_
# endif

# ifndef _Out_
#  define _Out_
# endif
 
# ifndef _Inout_
#  define _Inout_
# endif

# ifndef _Return_type_success_
#  define _Return_type_success_(x)
# endif

# ifndef _Acquires_lock_
#  define _Acquires_lock_(x)
# endif

# ifndef _Releases_lock_
#  define _Releases_lock_(x)
# endif

# ifndef _In_z_
#  define _In_z_
# endif

# ifndef _Post_z_
#  define _Post_z_
# endif

# ifndef _Out_writes_
#  define _Out_writes_(size)
# endif

# ifndef _Out_writes_z_
#  define _Out_writes_z_(size)
# endif

# ifndef _Null_terminated_
#  define _Null_terminated_
# endif

# ifndef _Use_decl_annotations_
#  define _Use_decl_annotations_
# endif

# ifndef _Out_opt_
#  define _Out_opt_
# endif

# ifndef _Deref_post_z_
#  define _Deref_post_z_
# endif

# ifndef _Inout_updates_z_
#  define _Inout_updates_z_(count)
# endif

# ifndef _Inout_opt_z_
#  define _Inout_opt_z_
# endif

# ifndef _Deref_prepost_opt_z_
#  define _Deref_prepost_opt_z_
# endif

# ifndef _In_opt_
#  define _In_opt_
# endif

# ifndef _In_opt_z_
#  define _In_opt_z_
# endif

# ifndef _Ret_maybenull_
#  define _Ret_maybenull_
# endif

# ifndef _Check_return_
#  define _Check_return_
# endif

# ifndef _Must_inspect_result_
#  define _Must_inspect_result_
# endif

# ifndef _Frees_ptr_opt_
#  define _Frees_ptr_opt_
# endif

# ifndef _Frees_ptr_
#  define _Frees_ptr_
# endif

# ifndef _Const_
#  define _Const_
# endif

# ifndef _Post_writable_byte_size
#  define _Post_writable_byte_size(size)
# endif

# ifndef _Analysis_assume_
#  define _Analysis_assume_(expr)
# endif

# ifndef _Always_
#  define _Always_(expr)
# endif

# ifndef _Outptr_
#  define _Outptr_
# endif

# ifndef _Outptr_result_buffer_
#  define _Outptr_result_buffer_(size)
# endif

# ifndef _Outptr_result_nullonfailure_ 
#  define _Outptr_result_nullonfailure_
# endif

# ifndef  _Maybenull_
#  define _Maybenull_
# endif

# ifndef _Notnull_
#  define _Notnull_
# endif

# ifndef _Valid_
#  define _Valid_
# endif

# ifndef _Analysis_noreturn_ 
#  define _Analysis_noreturn_
# endif

# ifndef _Ret_writes_maybenull_z_
#  define _Ret_writes_maybenull_z_(count)
# endif

# ifndef _String_length_
#  define _String_length_(str)
# endif

# ifndef _Success_
#  define _Success_
# endif

# ifndef _Field_range_
#  define _Field_range_(min, max)
# endif

# ifndef _In_range_
#  define _In_range_(min, max)
# endif

# ifndef _Field_size_
#  define _Field_size_(count)
# endif

# ifndef _Field_size_opt_
#  define _Field_size_opt_(count)
# endif

# ifndef _Field_size_full_opt_
#  define _Field_size_full_opt_(count)
# endif

# ifndef _Field_size_bytes_opt_
#  define _Field_size_bytes_opt_(size)
# endif

# ifndef _Readable_elements_
#  define _Readable_elements_(count)
# endif

# ifndef _Writable_elements_
#  define _Writable_elements_(count)
# endif

# ifndef _Struct_size_bytes_
#  define _Struct_size_bytes_(size)
# endif

# ifndef _At_
#  define _At_(target, annotation)
# endif

# ifndef _Pre_satisfies_
#  define _Pre_satisfies_(expr)
# endif

# ifndef _On_failure_
#  define _On_failure_(expr)
# endif

# ifndef _In_bytecount_
#  define _In_bytecount_(size)
# endif

# ifndef _Out_writes_bytes_to_opt_
#  define _Out_writes_bytes_to_opt_(buffLen, bufferNeeded)
# endif

# ifndef _When_
#  define _When_(expr, annotation)
# endif

# ifndef _Analysis_assume_nullterminated_
#  define _Analysis_assume_nullterminated_(expr)
# endif


#endif /* !defined(_MSC_VER) */

/*
**==============================================================================
**
** PAL_MAX_PATH_SIZE
**
**==============================================================================
*/

#define PAL_MAX_PATH_SIZE 1024

/*
**==============================================================================
**
** PAL_COUNT
**
**==============================================================================
*/

#ifdef _MSC_VER
# define PAL_COUNT(ARR) _countof(ARR)
#else
# define PAL_COUNT(ARR) (sizeof(ARR) / sizeof(ARR[0]))
#endif
