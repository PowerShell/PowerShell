#include "terminal.h"
#include <sys/ioctl.h>

int32_t GetTerminalWidth()
{
    struct winsize ws;
    if (-1 == ioctl(0,TIOCGWINSZ,&ws))
        return -1;

    return ws.ws_col;
}

int32_t GetTerminalHeight()
{
    struct winsize ws;
    if (-1 == ioctl(0,TIOCGWINSZ,&ws))
        return -1;

    return ws.ws_row;
}

