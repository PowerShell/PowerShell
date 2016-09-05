//! @file test-locale.cpp
//! @author Alex Jordan <v-alexjo@microsoft.com>
//! @brief Unit tests for linux locale

#include <gtest/gtest.h>
#include <langinfo.h>
#include <locale.h>
#include <string>
#include <stdio.h>
//! Test fixture for LocaleTest

class LocaleTest : public ::testing::Test
{
};

TEST_F(LocaleTest, Success)
{
    setlocale(LC_ALL, "");
    ASSERT_FALSE(nl_langinfo(CODESET) == NULL);
    ASSERT_STREQ(nl_langinfo(CODESET), "UTF-8");
}
