#pragma once

#include <Windows.h>
#include <string>

namespace macro_port
{
class NativeKeyboard
{
public:
    static void PressKey(char ch);
    static void PressEnter(HWND hwnd);
    static void PressE(HWND hwnd);
    static void PressG(HWND hwnd);
    static void PressCtrlA(HWND hwnd);
    static void PressBackspace(HWND hwnd);
    static void TypeText(const std::string& text, HWND hwnd);
};
}
