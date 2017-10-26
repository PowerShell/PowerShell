/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

#pragma once

#include <Windows.h>

namespace NativeMsh
{
    //
    // Begin nativemsh.mc codes
    //
    const int g_MISSING_COMMAND_LINE_ARGUMENT = 1;
    const int g_INVALID_CONSOLE_FILE_PATH = 2;
    const int g_CLR_VERSION_NOT_INSTALLED = 3;
    const int g_CORBINDTORUNTIME_FAILED = 4;
    const int g_STARTING_CLR_FAILED = 5;
    const int g_GETTING_DEFAULT_DOMAIN_FAILED = 6;
    const int g_CREATING_MSH_ENTRANCE_FAILED = 7;
    const int g_GETTING_DISPATCH_ID_FAILED = 8;
    const int g_INOVKING_MSH_ENTRANCE_FAILED = 9;
    const int g_MANAGED_MSH_EXCEPTION = 10;
    const int g_READ_XML_COM_INIT_FAILED = 11;
    const int g_READ_XML_CREATE_DOMDOCUMENT_FAILED = 12;
    const int g_READ_XML_LOAD_FILE_FAILED = 13;
    const int g_EMPTY_REG_SZ_VALUE = 14;
    const int g_READ_XML_GET_CONSOLE_SCHEMA_VERSION_FAILED = 15;
    const int g_READ_XML_INVALID_CONSOLE_SCHEMA_VERSION = 16;
    const int g_INVALID_MONAD_VERSION = 17;
    const int g_READ_XML_GET_MONAD_VERSION_TEXT_FAILED = 18;
    const int g_SEARCH_LATEST_REG_KEY_FAILED_WITH = 19;
    const int g_OPEN_REG_KEY_FAILED_WITH = 20;
    const int g_CLOSE_REG_KEY_FAILED_WITH = 21;
    const int g_READ_REG_VALUE_FAILED_WITH = 22;
    const int g_CREATE_MSHENGINE_REG_KEY_PATH_FAILED_WITH = 23;
    const int g_EXPECT_REG_SZ_VALUE = 24;
    const int g_MSH_VERSION_NOT_INSTALLED = 25;
    const int g_INCORRECT_CONSOLE_FILE_EXTENSION = 26;
    const int g_INVALID_REG_MSHVERSION_VALUE = 27;
    const int g_INCOMPATIBLE_MINOR_VERSION = 28;
    const int g_NO_COMPLETELY_INSTALLED_FOUND_VERSION = 29;
    const int g_MISSING_REG_KEY = 30;
    const int g_READ_XML_LOAD_FILE_FAILED_WITH_SPECIFIC_ERROR = 31;
    const int g_READ_XML_LOAD_FILE_FAILED_WITH_SPECIFIC_POSITION_ERROR = 32;
    const int g_READ_XML_GET_EMPTY_MONAD_VERSION_TEXT = 33;
    const int g_READ_XML_GET_PSCONSOLEFILE_FAILED = 34;
    const int g_NOTSUPPORTED_MONAD_VERSION = 35;
    const int g_NONSTANDARD_CLR_VERSION = 36;
    const int g_SHELLBANNER1 = 37;
    const int g_SHELLBANNER2 = 38;
    // Win8: 622653 Creating a copy of g_MISSING_REG_KEY to address scenarios where pwszMonadVersion is NULL.
    const int g_MISSING_REG_KEY1 = 39;
    //
    // End nativemsh.mc codes
    //
    const int g_UNLOAD_APPDOMAIN_FAILED = 40;
    const int g_STOP_CLR_HOST_FAILED = 41;
    const int g_RELEASE_CLR_HOST_FAILED = 42;

    const int g_MAX_REG_KEY_LENGTH = 255 + 1;

    const int g_MAX_VERSION_FIELD_LENGTH = 10;

    const unsigned int EXIT_CODE_SUCCESS = 0x00000000;
    const unsigned int EXIT_CODE_INIT_FAILURE = 0xFFFF0000;
    const unsigned int EXIT_CODE_BAD_COMMAND_LINE_PARAMETER = 0xFFFD0000;
    const unsigned int EXIT_CODE_READ_CONSOLE_FILE_FAILURE = 0xFFFC0000;
    const unsigned int EXIT_CODE_READ_REGISTRY_FAILURE = 0xFFFB0000;
    const unsigned int EXIT_CODE_INCOMPATIBLE_MSH_VERSION = 0xFFFA0000;

    const DWORD INVALID_APPDOMAIN_ID = (DWORD)-1; // TODO: valid uninitialized value?
} // namespace NativeMsh