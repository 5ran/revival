#pragma once

#include <Windows.h>

namespace macro_port
{
class NativeMouse
{
public:
    static void LeftDown();
    static void LeftUp();
    static void RightDown();
    static void RightUp();
    static void MoveTo(int x, int y);
    static void ClickAt(int x, int y);
};
}
