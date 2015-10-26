#include "terminal.h"
#include <sys/ioctl.h>

INT32 GetTerminalWidth()
{
    struct winsize ws;
    if (-1 == ioctl(0,TIOCGWINSZ,&ws))
        return -1;

    return ws.ws_col;
}

INT32 GetTerminalHeight()
{
    struct winsize ws;
    if (-1 == ioctl(0,TIOCGWINSZ,&ws))
        return -1;

    return ws.ws_row;
}

