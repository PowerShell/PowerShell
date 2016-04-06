#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC

int32_t CreateHardLink(const char *link, const char *target);

PAL_END_EXTERNC
