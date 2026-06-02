#include "replay_trace_io.hpp"

#include <algorithm>
#include <cctype>
#include <fstream>
#include <sstream>

namespace macro_port
{
namespace
{
std::string Trim(const std::string& s)
{
    size_t a = 0;
    while (a < s.size() && std::isspace(static_cast<unsigned char>(s[a])) != 0) ++a;
    size_t b = s.size();
    while (b > a && std::isspace(static_cast<unsigned char>(s[b - 1])) != 0) --b;
    return s.substr(a, b - a);
}

std::vector<std::string> SplitCsvLine(const std::string& line)
{
    std::vector<std::string> out;
    std::string cell;
    std::istringstream ss(line);
    while (std::getline(ss, cell, ','))
    {
        out.push_back(Trim(cell));
    }
    return out;
}

bool ParseBool(const std::string& s, bool& out)
{
    if (s == "1" || s == "true" || s == "TRUE") { out = true; return true; }
    if (s == "0" || s == "false" || s == "FALSE") { out = false; return true; }
    return false;
}

bool ParsePhase(const std::string& s, FishingPhase& out)
{
    if (s == "Off") { out = FishingPhase::Off; return true; }
    if (s == "Casting") { out = FishingPhase::Casting; return true; }
    if (s == "Casted") { out = FishingPhase::Casted; return true; }
    if (s == "Shake") { out = FishingPhase::Shake; return true; }
    if (s == "Fishing") { out = FishingPhase::Fishing; return true; }
    if (s == "Suspended") { out = FishingPhase::Suspended; return true; }
    if (s == "Error") { out = FishingPhase::Error; return true; }
    return false;
}
}

bool ReplayTraceIo::LoadFramesFromCsv(const std::string& path, std::vector<ReplayFrame>& outFrames, std::string& error)
{
    outFrames.clear();
    error.clear();

    std::ifstream in(path);
    if (!in.is_open())
    {
        error = "Could not open trace file.";
        return false;
    }

    std::string line;
    if (!std::getline(in, line))
    {
        error = "Trace file is empty.";
        return false;
    }

    const auto header = SplitCsvLine(line);
    if (header.size() < 9)
    {
        error = "Trace header has insufficient columns.";
        return false;
    }

    int row = 1;
    while (std::getline(in, line))
    {
        ++row;
        if (Trim(line).empty())
        {
            continue;
        }

        const auto cols = SplitCsvLine(line);
        if (cols.size() < 9)
        {
            error = "Row " + std::to_string(row) + " has insufficient columns.";
            return false;
        }

        ReplayFrame frame;
        frame.checkPhase = true;
        frame.checkHoldApplied = true;
        frame.checkRightHoldApplied = true;
        frame.checkAutomationGate = true;

        try
        {
            frame.input.nowMs = std::stoll(cols[0]);
            frame.expected.holdApplied = (cols[2] == "1" || cols[2] == "true" || cols[2] == "TRUE");
            frame.expected.rightHoldApplied = (cols[3] == "1" || cols[3] == "true" || cols[3] == "TRUE");
            frame.expected.automationGateOpen = (cols[4] == "1" || cols[4] == "true" || cols[4] == "TRUE");
        }
        catch (...)
        {
            error = "Row " + std::to_string(row) + " parse error.";
            return false;
        }

        if (!ParsePhase(cols[1], frame.expected.phase))
        {
            error = "Row " + std::to_string(row) + " invalid phase.";
            return false;
        }
        if (!ParseBool(cols[5], frame.input.shakeVisible) ||
            !ParseBool(cols[6], frame.input.castPowerReady) ||
            !ParseBool(cols[7], frame.input.perfectCastRelease))
        {
            error = "Row " + std::to_string(row) + " invalid bool field.";
            return false;
        }

        if (cols[8] == "null" || cols[8].empty())
        {
            frame.input.completionOverride = std::nullopt;
        }
        else
        {
            bool completion = false;
            if (!ParseBool(cols[8], completion))
            {
                error = "Row " + std::to_string(row) + " invalid completion override.";
                return false;
            }
            frame.input.completionOverride = completion;
        }

        outFrames.push_back(frame);
    }

    return true;
}
}
