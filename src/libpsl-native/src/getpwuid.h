// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#pragma once

#include "pal.h"

#include <sys/types.h>

PAL_BEGIN_EXTERNC

char* GetPwUid(uid_t uid);

PAL_END_EXTERNC
