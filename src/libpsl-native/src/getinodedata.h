#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC

int32_t GetInodeData(const char* fileName, uint64_t* device, uint64_t* inode);

PAL_END_EXTERNC
