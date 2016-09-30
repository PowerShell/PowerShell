#pragma once

#include "config.h"

#include <stdlib.h>
#include <string.h>
#include <inttypes.h>
#include <limits.h>

#define ERROR_INVALID_PARAMETER 87
#define ERROR_OUTOFMEMORY 14
#define ERROR_BAD_ENVIRONMENT 0x0000000A
#define ERROR_TOO_MANY_OPEN_FILES 0x00000004
#define ERROR_INSUFFICIENT_BUFFER 0x0000007A
#define ERROR_NO_ASSOCIATION 0x00000483
#define ERROR_NO_SUCH_USER 0x00000525
#define ERROR_INVALID_FUNCTION 0x00000001
#define ERROR_INVALID_ADDRESS 0x000001e7
#define ERROR_GEN_FAILURE 0x0000001F
#define ERROR_ACCESS_DENIED 0x00000005
#define ERROR_INVALID_NAME 0x0000007B
#define ERROR_STOPPED_ON_SYMLINK 0x000002A9
#define ERROR_BUFFER_OVERFLOW 0x0000006F
#define ERROR_FILE_NOT_FOUND 0x00000002
#define ERROR_BAD_PATH_NAME 0x000000A1
#define ERROR_BAD_NET_NAME 0x00000043
#define ERROR_DISK_FULL 0x00000070
#define ERROR_FILE_EXISTS 0x00000050
#define ERROR_TOO_MANY_LINKS 0x00000476

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

