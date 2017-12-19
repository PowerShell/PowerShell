/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

#pragma once

#include "NativeMsh.h"

HINSTANCE g_hResInstance = 0;
LPCWSTR g_MAIN_BINARY_NAME = L"powershell.exe";

// Copied from JSchwart
DWORD FileType(
    IN HANDLE fp)
{
    DWORD htype = GetFileType(fp);
    htype &= ~FILE_TYPE_REMOTE;
    return htype;
}

// Copied from dnsrv\admin\dscmd>parser\varg.cpp
void MyWriteConsole(
    HANDLE  fp,
    __in_ecount(cchBuffer)  LPCWCH  lpBuffer,
    DWORD   cchBuffer
    )
{

    if (!lpBuffer || !cchBuffer)
    {
        assert(false);
        return;
    }
    //
    // Jump through hoops for output because:
    //
    //    1.  printf() family chokes on international output (stops
    //        printing when it hits an unrecognized character)
    //
    //    2.  WriteConsole() works great on international output but
    //        fails if the handle has been redirected (i.e., when the
    //        output is piped to a file)
    //
    //    3.  WriteFile() works great when output is piped to a file
    //        but only knows about bytes, so Unicode characters are
    //        printed as two Ansi characters.
    //
    DWORD cchBufferWritten = 0;
    if (FILE_TYPE_CHAR == FileType(fp))
    {
        WriteConsole(fp, lpBuffer, cchBuffer, &cchBufferWritten, NULL);
    }
    else
    {
        //Buffer bounds are passed correctly.
        WriteFile(fp, lpBuffer, cchBuffer*sizeof(WCHAR), &cchBufferWritten, NULL);
    }
}

// Copied from dnsrv\admin\dscmd>parser\varg.cpp
void WriteStandard(
    bool    bUseStdOut,
    __in_ecount(cchBuffer)   LPCWCH  lpMessage,
    DWORD   cchBuffer)
{
    static HANDLE handle =
        GetStdHandle(bUseStdOut ? STD_OUTPUT_HANDLE : STD_ERROR_HANDLE);

    //
    // Verify parameters
    //
    if (!lpMessage)
    {
        return;
    }

    //
    // Output the results
    //
    MyWriteConsole(handle,
        lpMessage,
        cchBuffer);
}

/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

class PwrshExeOutput : public NativeMsh::IPwrshCommonOutput
{
public:
    virtual ~PwrshExeOutput() {}

    virtual VOID DisplayMessage(
        bool bUseStdOut,
        DWORD dwMessageId,
        ...)
    {
        LPWSTR messageDisplayString = NULL;
        DWORD dwLength = 0;

        va_list args;
        va_start(args, dwMessageId);
        /*
        HMODULE LoadMUILibrary(LPCTSTR lpFileName, DWORD dwFlags, LANGID LangID)

        This function loads and returns the MUI resource module (e.g. foo.dll.mui) of a main binary module (e.g. foo.dll).   Developers should call this function to obtain a MUI resource module handle and use the returned handle for the purpose of resource access ONLY.

        The APIs behaves slightly different on different OS:

        For W2K, XP and WS03 systems: when the caller doesn?t pass any LangID, the MUI module is searched in this order:

        1. This function will look for the MUI module in the folder with the folder name that matches the current user's UI language.
        2. If it doesn't find any folder for that language or the file is not in the folder, it will look for the MUI module in a folder named in the language of the OS (if the OS language is different that the user's language).
        3. If the folder or the file does not exist, it will try to load the MUI module installed in the English folder (en-US).
        4. If the English (en-US) MUI module does not exist either, the function will try to load the MUI module located in the same folder as the main binary.
        5. If everything fails, the function will then load the main module and return a handle to it.

        If the caller specifies a LangID, the API will try to load the mui module for that language and if the mui files doesn?t exist the API will return an error (the same one returned by the LoadLibrary() call)

        For win9x/nt4 systems: when the caller doesn?t pass a LangID, the MUI module search order is the same that above but step (1) is not searched since that setting does not exist on those operating systems.

        The API behaves the same way than in W2K, XP and WS03 when the caller specifies a LangID.

        For Longhorn systems: regardless if the caller pass a LangID or not, the function does not do anything and will let the Longhorn resource loader handle the redirection to locate the correct MUI module for loading.


        Parameters

        lpFileName - [in] Pointer to a null-terminated string that names the executable module (either a .dll or an .exe file) whose MUI resource module (.mui file) is to be loaded. The name specified is the file name of the executable module.

        dwFlags - [in] The flag value determines the convention that the function uses to search for MUI files on down-level systems ONLY. The flag has no effect on Longhorn system. Please also refer to the MUI File installation section for more details on the location of the MUI files. dwFlags can take on the following mutually exclusive values:

        MUI_LANGUAGE_ID: The function will search for MUI files using the LCID language convention (e.g. 0411)
        MUI_LANGUAGE_NAME: (default) The function will search for MUI files using the ISO language name convention (e.g. ja-JP)

        LangID ? [in] specifies the language of the resources the caller wants to load.

        Note: the dwFlags and LangID are only use when the component is running on down-level systems.  They don?t have any effect on Longhorn systems.

        In Down-level and Longhorn system you can use MLANG function RFC1766ToLCID() and LCIDToRFC1766() to map between language name and LCIDs.

        */

        do
        {
            // if we had unlocalizable resources, we would have to try to load the resource from
            // the language-neutral module handle
            if (g_hResInstance == 0)
            {
#ifdef CORECLR
                g_hResInstance = LoadLibraryEx(g_MAIN_BINARY_NAME, 0, LOAD_LIBRARY_AS_DATAFILE | LOAD_LIBRARY_AS_IMAGE_RESOURCE);
#else
                g_hResInstance = LoadMUILibraryW(g_MAIN_BINARY_NAME, MUI_LANGUAGE_NAME, 0);
#endif
            }

            //string function
            dwLength = FormatMessageW(
                FORMAT_MESSAGE_FROM_HMODULE | FORMAT_MESSAGE_ALLOCATE_BUFFER,
                g_hResInstance,
                dwMessageId,
                0,
                (LPWSTR)&messageDisplayString,
                0,
                &args);

            if (dwLength != 0)
            {
                WriteStandard(bUseStdOut, messageDisplayString, dwLength);
                LocalFree(messageDisplayString);
            }
        } while (false);
        va_end(args);
    }

    virtual void DisplayErrorWithSystemError(
        LONG lSystemErrorCode,
        int messageId,
        LPCWSTR insertionParam)
    {
        wchar_t * wszSystemErrorMessage = NULL;
        DWORD dwLength = NativeMsh::PwrshCommon::GetSystemErrorMessage(
            lSystemErrorCode,
            &wszSystemErrorMessage);

        if (dwLength > 0)
        {
            this->DisplayMessage(false, messageId, insertionParam, wszSystemErrorMessage);
            if (wszSystemErrorMessage != NULL)
            {
                delete[] wszSystemErrorMessage;
                wszSystemErrorMessage = NULL;
            }
        }
    }
};
