#include "native_keyboard.hpp"

namespace macro_port
{
namespace
{
void SendVk(WORD vk, bool keyUp)
{
    INPUT in{};
    in.type = INPUT_KEYBOARD;
    in.ki.wVk = vk;
    in.ki.dwFlags = keyUp ? KEYEVENTF_KEYUP : 0;
    SendInput(1, &in, sizeof(INPUT));
}

void PressVk(WORD vk)
{
    SendVk(vk, false);
    SendVk(vk, true);
}
}

void NativeKeyboard::PressKey(char ch)
{
    SHORT vk = VkKeyScanA(ch);
    if (vk == -1)
    {
        return;
    }

    BYTE vkCode = static_cast<BYTE>(vk & 0xFF);
    BYTE shiftState = static_cast<BYTE>((vk >> 8) & 0xFF);
    if ((shiftState & 1) != 0)
    {
        SendVk(VK_SHIFT, false);
    }

    PressVk(vkCode);

    if ((shiftState & 1) != 0)
    {
        SendVk(VK_SHIFT, true);
    }
}

void NativeKeyboard::PressEnter(HWND) { PressVk(VK_RETURN); }
void NativeKeyboard::PressE(HWND) { PressKey('e'); }
void NativeKeyboard::PressG(HWND) { PressKey('g'); }

void NativeKeyboard::PressCtrlA(HWND)
{
    SendVk(VK_CONTROL, false);
    PressKey('a');
    SendVk(VK_CONTROL, true);
}

void NativeKeyboard::PressBackspace(HWND) { PressVk(VK_BACK); }

void NativeKeyboard::TypeText(const std::string& text, HWND)
{
    for (char ch : text)
    {
        PressKey(ch);
    }
}
}
