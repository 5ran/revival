#pragma once

#include <string>

namespace macro_port
{
enum class TreasureStepState
{
    Running,
    Completed,
    Failed,
};

struct TreasureStepResult
{
    TreasureStepState state = TreasureStepState::Running;
    std::string status;
};

class TreasureAppraiser
{
public:
    void Reset();
    TreasureStepResult RunStep(bool targetsReady, bool completed, int cellsAppraisedThisStep = 1);

private:
    enum class Phase { Setup, AppraiseRows, Finalize, Done };
    Phase phase_ = Phase::Setup;
    int rowIndex_ = 0;
    int rowProgress_ = 0;
};
}
