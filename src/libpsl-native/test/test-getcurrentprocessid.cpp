// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#include <gtest/gtest.h>
#include "getcurrentprocessorid.h"

// This is a very simple test case to show how tests can be written
TEST(GetCurrentProcessId,simple)
{
	const int32_t currentProcessId = GetCurrentProcessId();
	const pid_t pid = getpid();

	// first make sure that on this platform those types are of the same size
	ASSERT_TRUE(sizeof(int32_t) >= sizeof(pid_t));
	
	// now compare the actual values
	ASSERT_EQ(currentProcessId,static_cast<int32_t>(pid));
}

