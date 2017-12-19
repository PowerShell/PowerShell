#pragma once

#include "pal.h"
#include <sys/types.h>

PAL_BEGIN_EXTERNC

int32_t ForkAndExecProcess(
    const char* filename,           // filename argument to execve
    char* const argv[],             // argv argument to execve
    char* const envp[],             // envp argument to execve
    const char* cwd,                // path passed to chdir in child process
    int32_t redirectStdin,          // whether to redirect standard input from the parent
    int32_t redirectStdout,         // whether to redirect standard output to the parent
    int32_t redirectStderr,         // whether to redirect standard error to the parent
    int32_t creationFlags,          // creation flags
    int32_t* childPid,              // [out] the child process' id
    int32_t* stdinFd,               // [out] if redirectStdin, the parent's fd for the child's stdin
    int32_t* stdoutFd,              // [out] if redirectStdout, the parent's fd for the child's stdout
    int32_t* stderrFd);             // [out] if redirectStderr, the parent's fd for the child's stderr 

PAL_END_EXTERNC
