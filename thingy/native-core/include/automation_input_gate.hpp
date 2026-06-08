#pragma once

#include <string>

namespace macro_port
{
class IAutomationInputGate
{
public:
    virtual ~IAutomationInputGate() = default;
    virtual bool TryEnter(const std::string& owner, int priority) = 0;
    virtual void Exit(const std::string& owner) = 0;
    virtual bool IsHeldByOther(const std::string& owner) const = 0;
    virtual void Reset() = 0;
};

class InMemoryAutomationInputGate final : public IAutomationInputGate
{
public:
    bool TryEnter(const std::string& owner, int priority) override;
    void Exit(const std::string& owner) override;
    bool IsHeldByOther(const std::string& owner) const override;
    void Reset() override;

private:
    std::string owner_;
    int ownerPriority_ = 0;
};
}
