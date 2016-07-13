//! @file followSymLink.cpp
//! @author George FLeming <v-geflem@microsoft.com>
//! @brief returns whether a path is a symbolic link

#include <errno.h>
#include <unistd.h>
#include <string>
#include <iostream>
#include "followsymlink.h"
#include "issymlink.h"

//! @brief Followsymlink determines target path of a sym link
//!
//! Followsymlink
//!
//! @param[in] fileName
//! @parblock
//! A pointer to the buffer that contains the file name
//!
//! char* is marshaled as an LPStr, which on Linux is UTF-8.
//! @endparblock
//!
//! @exception errno Passes these errors via errno to GetLastError:
//! - ERROR_INVALID_PARAMETER: parameter is not valid
//! - ERROR_FILE_NOT_FOUND: file does not exist
//! - ERROR_ACCESS_DENIED: access is denied
//! - ERROR_INVALID_ADDRESS: attempt to access invalid address
//! - ERROR_STOPPED_ON_SYMLINK: too many symbolic links
//! - ERROR_GEN_FAILURE: I/O error occurred
//! - ERROR_INVALID_NAME: file provided is not a symbolic link
//! - ERROR_INVALID_FUNCTION: incorrect function
//! - ERROR_BAD_PATH_NAME: pathname is too long
//! - ERROR_OUTOFMEMORY insufficient kernal memory
//!
//! @retval target path, or NULL if unsuccessful
//!

char* FollowSymLink(const char* fileName)
{
    errno = 0;  

    // if filename is null, return null value
    if (!fileName)
    {
        errno = ERROR_INVALID_PARAMETER;
        return NULL;
    }

    //if lstat in IsSymLink returns -1, path does not exist
    if (IsSymLink(fileName) == -1)
    {
        return NULL;
    }

    /*If the path is a symlink, the function return the absolute filepath if valid. if not valid, return null*/     
    if (IsSymLink(fileName))
    {
        //Attempt to resolve with the absolute filepath
        char actualpath[PATH_MAX+1];
        char* realPath = realpath(fileName, actualpath);
    
        //if realPath is null, go onto the readlink implementation
        if (realPath == NULL)
        {       
            char buffer[PATH_MAX];
            ssize_t sz = readlink(fileName, buffer, PATH_MAX);
    
            if  (sz == -1)
            {
                switch(errno)
                {
                    case EACCES:
                        errno = ERROR_ACCESS_DENIED;
                        break;
                    case EFAULT:
                        errno = ERROR_INVALID_ADDRESS;   /*If the path is a symlink, the function return the absolute filepath if valid. if not valid, return null*/     
                        break;
                    case EINVAL:
                        errno = ERROR_INVALID_NAME;
                    case EIO:
                        errno = ERROR_GEN_FAILURE;
                        break;
                    case ELOOP:
                        errno = ERROR_STOPPED_ON_SYMLINK;
                        break;
                    case ENAMETOOLONG:
                        errno = ERROR_BAD_PATH_NAME;
                        break;
                    case ENOENT:
                        errno = ERROR_FILE_NOT_FOUND;
                        break;
                    case ENOMEM:
                        errno = ERROR_OUTOFMEMORY;
                        break;
                    case ENOTDIR:
                        errno = ERROR_BAD_PATH_NAME;
                        break;
                    default:
                        errno = ERROR_INVALID_FUNCTION;
                }

                return NULL;
           }

            buffer[sz] = '\0';
            return strndup(buffer, sz + 1);
        }
        else
        {
            return strndup(realPath, strlen(realPath) + 1 );
        }
    }
    
    else
    {
          char buffer[PATH_MAX];
            ssize_t sz = readlink(fileName, buffer, PATH_MAX);
    
            if  (sz == -1)
            {
                switch(errno)
                {
                    case EACCES:
                        errno = ERROR_ACCESS_DENIED;
                        break;
                    case EFAULT:
                        errno = ERROR_INVALID_ADDRESS;   /*If the path is a symlink, the function return the absolute filepath if valid. if not valid, return null*/     
                        break;
                    case EINVAL:
                        errno = ERROR_INVALID_NAME;
                    case EIO:
                        errno = ERROR_GEN_FAILURE;
                        break;
                    case ELOOP:
                        errno = ERROR_STOPPED_ON_SYMLINK;
                        break;
                    case ENAMETOOLONG:
                        errno = ERROR_BAD_PATH_NAME;
                        break;
                    case ENOENT:
                        errno = ERROR_FILE_NOT_FOUND;
                        break;
                    case ENOMEM:
                        errno = ERROR_OUTOFMEMORY;
                        break;
                    case ENOTDIR:
                        errno = ERROR_BAD_PATH_NAME;
                        break;
                    default:
                        errno = ERROR_INVALID_FUNCTION;
                }

                return NULL;
           }

            buffer[sz] = '\0';
            return strndup(buffer, sz + 1);

    }
}
