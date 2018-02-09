// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#pragma once

#include "pal.h"

PAL_BEGIN_EXTERNC

void Native_OpenLog(const char* ident, int facility);
void Native_SysLog(int32_t priority, const char* message);
void Native_CloseLog();

PAL_END_EXTERNC
