// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#include <gtest/gtest.h>
#include "getcurrentthreadid.h"
#include <pthread.h>

TEST(GetCurrentThreadId,simple)
{
	const HANDLE currentThreadId = GetCurrentThreadId();
	const pid_t tid = pthread_self();

	// first make sure that on this platform those types are of the same size
	ASSERT_TRUE(sizeof(HANDLE) >= sizeof(pid_t));
	
	// now compare the actual values
	ASSERT_EQ(currentThreadId,reinterpret_cast<HANDLE>(tid));
}

