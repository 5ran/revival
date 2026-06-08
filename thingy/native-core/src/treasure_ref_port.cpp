#include "treasure_ref_port.hpp"

namespace macro_port
{
void TreasureAppraiser::Reset()
{
    phase_ = Phase::Setup;
    rowIndex_ = 0;
    rowProgress_ = 0;
}

TreasureStepResult TreasureAppraiser::RunStep(bool targetsReady, bool completed, int cellsAppraisedThisStep)
{
    TreasureStepResult out{};
    if (!targetsReady)
    {
        out.status = "Waiting for treasure targets.";
        return out;
    }

    if (completed)
    {
        phase_ = Phase::Done;
        out.state = TreasureStepState::Completed;
        out.status = "Treasure appraise complete.";
        return out;
    }

    switch (phase_)
    {
    case Phase::Setup:
        phase_ = Phase::AppraiseRows;
        out.status = "Setting up treasure appraise.";
        break;
    case Phase::AppraiseRows:
        rowProgress_ += cellsAppraisedThisStep <= 0 ? 1 : cellsAppraisedThisStep;
        while (rowProgress_ >= 3)
        {
            rowProgress_ -= 3;
            ++rowIndex_;
        }
        if (rowIndex_ >= 7)
        {
            phase_ = Phase::Finalize;
            out.status = "All rows appraised; finalizing.";
        }
        else
        {
            out.status = "Appraising rows.";
        }
        break;
    case Phase::Finalize:
        out.status = "Finalizing appraise.";
        break;
    case Phase::Done: out.state = TreasureStepState::Completed; out.status = "Treasure appraise complete."; break;
    }

    return out;
}
}
