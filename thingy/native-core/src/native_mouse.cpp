#include "native_mouse.hpp"

namespace macro_port
{
namespace
{
void SendMouse(DWORD flags, LONG dx = 0, LONG dy = 0)
{
    INPUT in{};
    in.type = INPUT_MOUSE;
    in.mi.dwFlags = flags;
    in.mi.dx = dx;
    in.mi.dy = dy;
    SendInput(1, &in, sizeof(INPUT));
}
}

void NativeMouse::LeftDown() { SendMouse(MOUSEEVENTF_LEFTDOWN); }
void NativeMouse::LeftUp() { SendMouse(MOUSEEVENTF_LEFTUP); }
void NativeMouse::RightDown() { SendMouse(MOUSEEVENTF_RIGHTDOWN); }
void NativeMouse::RightUp() { SendMouse(MOUSEEVENTF_RIGHTUP); }

void NativeMouse::MoveTo(int x, int y)
{
    SetCursorPos(x, y);
}

void NativeMouse::ClickAt(int x, int y)
{
    MoveTo(x, y);
    LeftDown();
    LeftUp();
}
}
