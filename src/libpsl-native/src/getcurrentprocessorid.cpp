// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#include "getcurrentprocessorid.h"

#include <unistd.h>

pid_t GetCurrentProcessId()
{
    return getpid();
}
