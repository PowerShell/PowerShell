/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

#pragma once

#include <Windows.h>

namespace NativeMsh
{
    //
    // Implement this interface to override the default no-op behaviour of the output.
    //
    class IPwrshCommonOutput
    {
    public:
        // Virtual destructor to ensure that derived destructors are called
        // during base class destruction.
        virtual ~IPwrshCommonOutput() {}

        virtual VOID DisplayMessage(
            bool bUseStdOut,
            DWORD dwMessageId,
            ...) = 0;

        virtual void DisplayErrorWithSystemError(
            LONG lSystemErrorCode,
            int messageId,
            LPCWSTR insertionParam) = 0;
    };
}