// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#pragma once

#include "pal.h"

#include <stdbool.h>

PAL_BEGIN_EXTERNC

bool IsSameFileSystemItem(const char* path_one, const char* path_two);

PAL_END_EXTERNC
