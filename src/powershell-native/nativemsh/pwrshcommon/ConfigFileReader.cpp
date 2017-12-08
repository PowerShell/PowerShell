#include <iostream>
#include <fstream>
#include <windef.h>

#include "NativeMsh.h"
#include "ConfigFileReader.h"

namespace NativeMsh 
{
    // The name of the PowerShell config file that identifies the PowerShell install location.
    // We use a config file to avoid writing to the registry during install
    // or hard coding paths into the binary.
    static PCWSTR powerShellConfigFileName = L"RemotePowerShellConfig.txt";

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
        std::wstring psHomeDirTag(L"PSHOMEDIR=");
        std::wstring coreClrDirTag(L"CORECLRDIR=");
        while (std::getline(psConfigFile, line))
        {
            // Search for the first actionable character in the line to
            // limit the number of passes that are made iterating through the line.
            for(std::wstring::const_iterator iter = line.begin(); iter != line.end(); iter++)
            {
                if (*iter == L'#')
                {
                    // Stop parsing the line because the rest of it is a comment
                    break;
                }
                else if (*iter == L'p' || *iter == L'P')
                {
                    std::wstring psHomeDir = this->getValueFromLine(line, psHomeDirTag);
                    HANDLE dirHandle = CreateFileW(psHomeDir.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, NULL);
                    if (INVALID_HANDLE_VALUE != dirHandle)
                    {
                        CloseHandle(dirHandle);
                        this->pathToPowerShellAssemblies = psHomeDir;
                        std::wstring::const_iterator slashIter = this->pathToPowerShellAssemblies.end();
                        slashIter--;
                        if (*slashIter != L'\\')
                        {
                            // Guarantee that there is a '\' at the end of the path
                            this->pathToPowerShellAssemblies.append(L"\\");
                        }
                    }
                    break;
                }
                else if (*iter == L'c' || *iter == L'C')
                {
                    std::wstring coreClrDir = this->getValueFromLine(line, coreClrDirTag);
                    HANDLE dirHandle = CreateFileW(coreClrDir.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, NULL);
                    if (INVALID_HANDLE_VALUE != dirHandle)
                    {
                        CloseHandle(dirHandle);
                        this->coreClrDirectory = coreClrDir;
                        std::wstring::const_iterator slashIter = this->coreClrDirectory.end();
                        slashIter--;
                        if (*slashIter != L'\\')
                        {
                            // Guarantee that there is a '\' at the end of the path
                            this->coreClrDirectory.append(L"\\");
                        }
                    }
                    break;
                }
                // Else: Do nothing to ignore unmatched characters (whitespace, etc.)
            }
        }
        if (0 == this->pathToPowerShellAssemblies.size() ||
            0 == this->coreClrDirectory.size())
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

    // Parses the specified line for the given tag. Tags are assumed to include
    // the "=" separator character.
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

