// ---------------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (c) Microsoft Corporation. All rights reserved.
//
//  Contents:  A class that extracts the path to $PSHOME and the path to its
//             CoreCLR from a configuration file.
// ---------------------------------------------------------------------------

#pragma once

#include <string>

namespace NativeMsh
{
    class ConfigFileReader
    {
    private:
        std::wstring pathToPowerShellAssemblies;
        std::wstring coreClrDirectory;

        std::wstring trim(const std::wstring& toTrim);
        std::wstring getValueFromLine(const std::wstring& line, std::wstring& tagToFind);

    public:
        ConfigFileReader();

        // Initiates a Read operation of the specified configuration file.
        unsigned int Read(std::wstring pathToConfig);

        // Information Getters
        std::wstring GetPathToPowerShell() { return pathToPowerShellAssemblies; }
        std::wstring GetPathToCoreClr() { return coreClrDirectory; }
    };
} // namespace NativeMsh

