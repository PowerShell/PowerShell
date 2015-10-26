#include <gtest/gtest.h>
#include "getcurrentprocessorid.h"

// This is a very simple test case to show how tests can be written
TEST(GetCurrentProcessId,simple)
{
	const HANDLE currentProcessId = GetCurrentProcessId();
	const pid_t pid = getpid();

	// first make sure that on this platform those types are of the same size
	ASSERT_TRUE(sizeof(HANDLE) >= sizeof(pid_t));
	
	// now compare the actual values
	ASSERT_EQ(currentProcessId,reinterpret_cast<HANDLE>(pid));
}

