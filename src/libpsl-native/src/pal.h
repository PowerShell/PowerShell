#pragma once

#include <stdlib.h>
#include <string.h>
#include <inttypes.h>
#include <limits.h>

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

