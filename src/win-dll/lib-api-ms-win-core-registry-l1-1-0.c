#include <stdint.h>

int32_t RegCloseKey(void* handle)
{
    return 0;
}

int32_t RegOpenKeyExW(void* hKey,uint16_t* lpSubKey,uint32_t ulOptions,uint32_t samDesired,void* phkResult)
{
    return 1;
}

