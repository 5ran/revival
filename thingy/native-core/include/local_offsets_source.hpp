#pragma once

#include <cstdint>
#include <filesystem>
#include <string>
#include <unordered_map>

#include "offsets_source.hpp"

namespace macro_port
{
class LocalOffsetsSource final : public IOffsetsSource
{
public:
    bool LoadFromFile(const std::filesystem::path& path, std::string* error = nullptr);
    void Clear();

    bool IsPopulated() const override;
    bool TryGetOffset(const std::string& key, std::uint64_t& value) const override;

    const std::filesystem::path& Path() const { return path_; }
    const std::string& Version() const { return version_; }
    std::size_t Count() const { return offsets_.size(); }

private:
    std::filesystem::path path_;
    std::string version_;
    std::unordered_map<std::string, std::uint64_t> offsets_;
};
}
