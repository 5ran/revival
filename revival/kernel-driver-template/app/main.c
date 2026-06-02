#include <Windows.h>
#include <stdio.h>
#include "..\\driver\\Public.h"

int main(void)
{
    HANDLE h = CreateFileW(L"\\\\.\\KmdfHelloIoctl", GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (h == INVALID_HANDLE_VALUE)
    {
        printf("CreateFile failed: %lu\n", GetLastError());
        return 1;
    }

    HELLO_PING_REQUEST req = { 1, 0x12345678 };
    HELLO_PING_RESPONSE res = { 0 };
    DWORD bytes = 0;

    BOOL ok = DeviceIoControl(
        h,
        IOCTL_KMDF_HELLO_PING,
        &req,
        (DWORD)sizeof(req),
        &res,
        (DWORD)sizeof(res),
        &bytes,
        NULL);

    if (!ok)
    {
        printf("DeviceIoControl failed: %lu\n", GetLastError());
        CloseHandle(h);
        return 1;
    }

    printf("Ping response: Version=%u Nonce=0x%08X Status=%u Bytes=%lu\n", res.Version, res.Nonce, res.Status, bytes);
    CloseHandle(h);
    return 0;
}
