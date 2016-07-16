#pragma once

#include <sys/stat.h>
#include "pal.h"

PAL_BEGIN_EXTERNC

int32_t GetLStat(const char* path, struct stat* buf);

PAL_END_EXTERNC
