// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#pragma once

#include "pal.h"

#include <stdbool.h>

PAL_BEGIN_EXTERNC

int32_t CreateSymLink(const char *link, const char *target);

PAL_END_EXTERNC
