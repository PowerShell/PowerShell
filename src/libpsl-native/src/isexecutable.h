// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#pragma once

#include "pal.h"

#include <stdbool.h>

PAL_BEGIN_EXTERNC

bool IsExecutable(const char* path);

PAL_END_EXTERNC
