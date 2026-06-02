#include "local_offsets_source.hpp"

#include <charconv>
#include <fstream>
#include <sstream>

namespace macro_port
{
namespace
{
class JsonOffsetReader
{
public:
    explicit JsonOffsetReader(std::string_view text) : text_(text) {}

    bool Parse(std::unordered_map<std::string, std::uint64_t>& offsets, std::string& version)
    {
        offsets_ = &offsets;
        version_ = &version;
        SkipWhitespace();
        return ParseObject(std::string{}, true);
    }

private:
    void SkipWhitespace()
    {
        while (pos_ < text_.size() && (text_[pos_] == ' ' || text_[pos_] == '\t' || text_[pos_] == '\r' || text_[pos_] == '\n'))
        {
            ++pos_;
        }
    }

    bool Consume(char expected)
    {
        SkipWhitespace();
        if (pos_ >= text_.size() || text_[pos_] != expected)
        {
            return false;
        }
        ++pos_;
        return true;
    }

    bool ParseObject(const std::string& prefix, bool root)
    {
        if (!Consume('{'))
        {
            return false;
        }

        SkipWhitespace();
        if (pos_ < text_.size() && text_[pos_] == '}')
        {
            ++pos_;
            return true;
        }

        while (pos_ < text_.size())
        {
            std::string key;
            if (!ParseString(key) || !Consume(':'))
            {
                return false;
            }

            SkipWhitespace();
            if (pos_ >= text_.size())
            {
                return false;
            }

            if (text_[pos_] == '{')
            {
                const std::string childPrefix = root && (key == "offsets" || key == "Offsets") ? std::string{} : Combine(prefix, key);
                if (!ParseObject(childPrefix, false))
                {
                    return false;
                }
            }
            else if (text_[pos_] == '"')
            {
                std::string value;
                if (!ParseString(value))
                {
                    return false;
                }
                StoreScalar(Combine(prefix, key), key, value);
            }
            else
            {
                std::string value;
                if (!ParseBareValue(value))
                {
                    return false;
                }
                StoreScalar(Combine(prefix, key), key, value);
            }

            SkipWhitespace();
            if (pos_ < text_.size() && text_[pos_] == ',')
            {
                ++pos_;
                continue;
            }
            if (pos_ < text_.size() && text_[pos_] == '}')
            {
                ++pos_;
                return true;
            }
            return false;
        }

        return false;
    }

    bool ParseString(std::string& value)
    {
        if (!Consume('"'))
        {
            return false;
        }

        value.clear();
        bool escaped = false;
        while (pos_ < text_.size())
        {
            const char ch = text_[pos_++];
            if (escaped)
            {
                switch (ch)
                {
                case '"': value.push_back('"'); break;
                case '\\': value.push_back('\\'); break;
                case '/': value.push_back('/'); break;
                case 'b': value.push_back('\b'); break;
                case 'f': value.push_back('\f'); break;
                case 'n': value.push_back('\n'); break;
                case 'r': value.push_back('\r'); break;
                case 't': value.push_back('\t'); break;
                default: value.push_back(ch); break;
                }
                escaped = false;
                continue;
            }
            if (ch == '\\')
            {
                escaped = true;
                continue;
            }
            if (ch == '"')
            {
                return true;
            }
            value.push_back(ch);
        }
        return false;
    }

    bool ParseBareValue(std::string& value)
    {
        SkipWhitespace();
        const auto start = pos_;
        while (pos_ < text_.size() && text_[pos_] != ',' && text_[pos_] != '}' && text_[pos_] != '\r' && text_[pos_] != '\n')
        {
            ++pos_;
        }
        value = std::string(text_.substr(start, pos_ - start));
        Trim(value);
        return !value.empty();
    }

    void StoreScalar(const std::string& fullKey, const std::string& key, const std::string& value)
    {
        if (IsVersionKey(key))
        {
            if (version_ != nullptr && key != "ByfronVersion")
            {
                *version_ = value;
            }
        }

        if (IsMetadataKey(key))
        {
            return;
        }

        std::uint64_t parsed = 0;
        if (TryParseOffset(value, parsed) && offsets_ != nullptr)
        {
            (*offsets_)[fullKey] = parsed;
        }
    }

    static bool IsVersionKey(const std::string& key)
    {
        return key == "version" || key == "RobloxVersion" || key == "Roblox Version" || key == "ByfronVersion";
    }

    static bool IsMetadataKey(const std::string& key)
    {
        return IsVersionKey(key) ||
            key == "Source" ||
            key == "Dumper Version" ||
            key == "Dumped By" ||
            key == "Dumped At" ||
            key == "Discord" ||
            key == "Total Offsets";
    }

    static std::string Combine(const std::string& prefix, const std::string& key)
    {
        return prefix.empty() ? key : prefix + "." + key;
    }

    static void Trim(std::string& value)
    {
        while (!value.empty() && (value.front() == ' ' || value.front() == '\t'))
        {
            value.erase(value.begin());
        }
        while (!value.empty() && (value.back() == ' ' || value.back() == '\t'))
        {
            value.pop_back();
        }
    }

    static bool TryParseOffset(std::string value, std::uint64_t& parsed)
    {
        Trim(value);
        if (value.empty() || value == "null" || value == "true" || value == "false" || value == "???")
        {
            return false;
        }

        int base = 10;
        const char* begin = value.data();
        const char* end = value.data() + value.size();
        if (value.size() > 2 && value[0] == '0' && (value[1] == 'x' || value[1] == 'X'))
        {
            base = 16;
            begin += 2;
        }

        std::uint64_t result = 0;
        const auto conversion = std::from_chars(begin, end, result, base);
        if (conversion.ec != std::errc() || conversion.ptr != end)
        {
            return false;
        }

        parsed = result;
        return true;
    }

    std::string_view text_;
    std::size_t pos_ = 0;
    std::unordered_map<std::string, std::uint64_t>* offsets_ = nullptr;
    std::string* version_ = nullptr;
};
}

bool LocalOffsetsSource::LoadFromFile(const std::filesystem::path& path, std::string* error)
{
    Clear();
    path_ = path;

    std::ifstream input(path, std::ios::binary);
    if (!input)
    {
        if (error != nullptr)
        {
            *error = "offsets.json not found at " + path.string();
        }
        return false;
    }

    std::ostringstream buffer;
    buffer << input.rdbuf();
    auto text = buffer.str();

    std::unordered_map<std::string, std::uint64_t> parsed;
    std::string version;
    JsonOffsetReader reader(text);
    if (!reader.Parse(parsed, version))
    {
        if (error != nullptr)
        {
            *error = "offsets.json could not be parsed.";
        }
        return false;
    }

    if (parsed.empty())
    {
        if (error != nullptr)
        {
            *error = "offsets.json contained no usable offsets.";
        }
        return false;
    }

    offsets_ = std::move(parsed);
    version_ = std::move(version);
    return true;
}

void LocalOffsetsSource::Clear()
{
    offsets_.clear();
    version_.clear();
}

bool LocalOffsetsSource::IsPopulated() const
{
    return !offsets_.empty();
}

bool LocalOffsetsSource::TryGetOffset(const std::string& key, std::uint64_t& value) const
{
    const auto it = offsets_.find(key);
    if (it == offsets_.end())
    {
        return false;
    }

    value = it->second;
    return true;
}
}
