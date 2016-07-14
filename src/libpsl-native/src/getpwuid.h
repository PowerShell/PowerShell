#pragma once

#include <sys/types.h>
#include "pal.h"

PAL_BEGIN_EXTERNC

char* GetPwUid(uid_t uid);

PAL_END_EXTERNC
