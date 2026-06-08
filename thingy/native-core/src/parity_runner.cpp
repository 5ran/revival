#include "parity_runner.hpp"

namespace macro_port
{
bool ParityRunner::RunCsvTrace(
    RuntimeTrackerEngine* engine,
    const std::string& csvPath,
    std::int64_t startNowMs,
    FishingCastingMode castingMode,
    ReplayReport& outReport,
    std::string& error)
{
    std::vector<ReplayFrame> frames;
    if (!ReplayTraceIo::LoadFramesFromCsv(csvPath, frames, error))
    {
        return false;
    }

    outReport = ReplayRunner::Run(engine, startNowMs, castingMode, frames);
    return true;
}
}
