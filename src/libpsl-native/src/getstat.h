#pragma once

#include "pal.h"

#include <sys/stat.h>

PAL_BEGIN_EXTERNC

int32_t GetStat(const char* path, struct stat* buf);

PAL_END_EXTERNC
