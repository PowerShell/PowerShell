// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (C) Microsoft Corporation, 2014.
//
//  Contents:  Source code for abstraction of CLR and worker differences between PowerShell versions. 
//  pwrshplugin is totally unmanaged.
// ----------------------------------------------------------------------

#pragma once

#include <string>

namespace NativeMsh 
{
    class ConfigFileReader
    {
    protected:
        std::wstring pathToPowerShellAssemblies;

    protected:
        std::wstring trim(const std::wstring& toTrim);

        std::wstring getValueFromLine(const std::wstring& line, std::wstring& tagToFind);

    public:
        ConfigFileReader();

        virtual unsigned int Read(std::wstring pathToConfig);

        // Information Getters
        std::wstring GetPathToPowerShell() { return pathToPowerShellAssemblies; }
    };
} // namespace NativeMsh