#include "offsets_source_provider.hpp"

namespace macro_port
{
IOffsetsSource* OffsetsSourceProvider::current_ = nullptr;

void OffsetsSourceProvider::Register(IOffsetsSource* source)
{
    if (source == nullptr)
    {
        throw std::invalid_argument("OffsetsSourceProvider::Register source is null");
    }

    current_ = source;
}

IOffsetsSource& OffsetsSourceProvider::Current()
{
    if (current_ == nullptr)
    {
        throw std::runtime_error("OffsetsSourceProvider has no registered source");
    }

    return *current_;
}

void OffsetsSourceProvider::Reset()
{
    current_ = nullptr;
}
}
