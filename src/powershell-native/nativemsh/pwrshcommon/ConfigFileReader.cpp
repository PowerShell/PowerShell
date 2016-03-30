// ----------------------------------------------------------------------
//
//  Microsoft Windows NT
//  Copyright (C) Microsoft Corporation, 2014.
//
//  Contents:  Source code for abstraction of CLR and worker differences between PowerShell versions. 
//  pwrshplugin is totally unmanaged.
// ----------------------------------------------------------------------

#include <iostream>
#include <fstream>
#include <windows.h>

#include "NativeMsh.h"
#include "ConfigFileReader.h"

namespace NativeMsh 
{
    // The name of the PowerShell config file that identifies the PowerShell install location.
    // We use a config file to avoid writing to the registry during install.
    static PCWSTR powerShellConfigFileName = L"PowerShellConfig.txt";

    ConfigFileReader::ConfigFileReader()
    {
    }

    unsigned int ConfigFileReader::Read(
        std::wstring pathToConfig)
    {
        std::wstring absolutePathToConfigFile(pathToConfig);
        absolutePathToConfigFile += powerShellConfigFileName;
        std::wfstream psConfigFile(absolutePathToConfigFile.c_str());

        if (!psConfigFile.is_open())
        {
            return EXIT_CODE_INIT_FAILURE;
        }

        std::wstring line;
        std::wstring psRootTag(L"PSROOTDIR=");
        while (std::getline(psConfigFile, line))
        {
            this->pathToPowerShellAssemblies = this->getValueFromLine(line, psRootTag);
            if (this->pathToPowerShellAssemblies.size() > 0) // Found a match
            {
                std::wstring::const_iterator iter = this->pathToPowerShellAssemblies.end();
                if (*iter != L'\\')
                {
                    // Guarantee that there is a '\' at the end of the path
                    this->pathToPowerShellAssemblies.append(L"\\");
                }
                break;
            }
        }

        if (0 == this->pathToPowerShellAssemblies.size())
        {
            return EXIT_CODE_INIT_FAILURE;
        }
        else
        {
            return EXIT_CODE_SUCCESS;
        }
    }

    // This trim function removes beginning and ending whitespace. It does not
    // remove internal whitespace because that is valid within paths.
    std::wstring ConfigFileReader::trim(
        const std::wstring& toTrim)
    {
        static PCWSTR WHITESPACE_CHARS = L" \n\r\t";
        std::wstring copyToTrim = toTrim;
        std::size_t first = copyToTrim.find_first_not_of(WHITESPACE_CHARS);

        if (first == std::wstring::npos) 
        {
            // No non-whitespace found
            return std::wstring(L"");
        }

        // Result not checked for std::wstring::npos because it is guaranteed to have a value if the first pass succeeded.
        copyToTrim.erase(copyToTrim.find_last_not_of(WHITESPACE_CHARS)+1);

        return copyToTrim.substr(first);
    }

    // Parses the specified line for the given tag
    std::wstring ConfigFileReader::getValueFromLine(
        const std::wstring& line, // Passed by value to preserve the original line.
        std::wstring& tagToFind)
    {
        std::wstring trimmedLine = this->trim(line);
        std::size_t index = trimmedLine.find(tagToFind);
        if (std::string::npos != index)
        {
            std::wstring value = trimmedLine.substr(index + tagToFind.size()); // Everything else after the tag
            return this->trim(value);
        }
        else
        {
            return std::wstring(L"");
        }
    }
}
