#pragma once

#include <cstdint>
#include <string>

namespace macro_port
{
class IOffsetsSource
{
public:
    virtual ~IOffsetsSource() = default;
    virtual bool IsPopulated() const = 0;
    virtual bool TryGetOffset(const std::string& key, std::uint64_t& value) const = 0;
};
}
