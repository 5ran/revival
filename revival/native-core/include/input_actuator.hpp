#pragma once

namespace macro_port
{
class IInputActuator
{
public:
    virtual ~IInputActuator() = default;
    virtual void LeftDown() = 0;
    virtual void LeftUp() = 0;
    virtual void RightDown() {}
    virtual void RightUp() {}
};
}
