#pragma once

#include <string>

#include "rod_kind.hpp"

namespace macro_port
{
class RodClassifier
{
public:
    static RodKind Classify(const std::string& displayText);
};
}

