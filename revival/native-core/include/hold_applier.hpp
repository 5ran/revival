#pragma once

#include "fishing_hold_gate.hpp"
#include "input_actuator.hpp"

namespace macro_port
{
// Applies desired hold state through FishingHoldGate and emits concrete input
// actions via IInputActuator. Mirrors the C# tracker pattern:
// decision -> gate(delay) -> NativeMouse LeftDown/LeftUp.
class HoldApplier
{
public:
    explicit HoldApplier(IInputActuator* actuator)
        : actuator_(actuator)
    {
    }

    void Apply(bool desiredHold, long long nowMs, int actionDelayMs);
    void Release(long long nowMs);
    void Reset();
    bool IsHolding() const { return gate_.Held(); }

private:
    IInputActuator* actuator_ = nullptr;
    FishingHoldGate gate_{};
};
}

