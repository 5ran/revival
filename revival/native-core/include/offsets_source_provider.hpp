#pragma once

#include <stdexcept>

#include "offsets_source.hpp"

namespace macro_port
{
class OffsetsSourceProvider
{
public:
    static void Register(IOffsetsSource* source);
    static IOffsetsSource& Current();
    static void Reset();

private:
    static IOffsetsSource* current_;
};
}
