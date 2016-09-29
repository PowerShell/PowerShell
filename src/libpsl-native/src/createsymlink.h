#pragma once

#include "pal.h"

#include <stdbool.h>

PAL_BEGIN_EXTERNC

bool CreateSymLink(const char *link, const char *target);

PAL_END_EXTERNC
