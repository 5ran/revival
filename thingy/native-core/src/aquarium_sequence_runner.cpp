#include "aquarium_sequence_runner.hpp"

namespace macro_port
{
void AquariumSequenceRunner::Reset()
{
    phase_ = Phase::Resolve;
    nextActionAt_ = 0;
    feedUntilAt_ = 0;
    nextScrollAt_ = 0;
    nextFeedClickAt_ = 0;
    didFastScrollUp_ = false;
}

AquariumSequenceResult AquariumSequenceRunner::Step(const AquariumSequenceInput& in)
{
    AquariumSequenceResult out{};
    const long long clickDelay = in.clickDelayMs < 0 ? 0 : in.clickDelayMs;
    if (in.nowMs < nextActionAt_)
    {
        return out;
    }

    if (!in.targetsAvailable)
    {
        Reset();
        return out;
    }

    switch (phase_)
    {
    case Phase::Resolve:
        phase_ = Phase::OpenAquarium;
        return out;

    case Phase::OpenAquarium:
        out.clickAquariumButton = true;
        phase_ = Phase::WaitAfterOpen;
        nextActionAt_ = in.nowMs + (clickDelay > 500 ? clickDelay : 500);
        return out;

    case Phase::WaitAfterOpen:
        if (!in.feedAnchorVisible)
        {
            nextActionAt_ = in.nowMs + 100;
            return out;
        }

        out.clickFeedAnchor = true;
        phase_ = Phase::Feed;
        feedUntilAt_ = in.nowMs + 3650;
        nextScrollAt_ = in.nowMs;
        nextFeedClickAt_ = in.nowMs;
        didFastScrollUp_ = false;
        nextActionAt_ = in.nowMs + clickDelay;
        return out;

    case Phase::Feed:
        if (in.nowMs >= feedUntilAt_)
        {
            phase_ = Phase::CloseAquarium;
            nextActionAt_ = in.nowMs;
            return out;
        }

        if (in.cursorReadable && in.nowMs >= nextFeedClickAt_)
        {
            out.feedClicks = 1;
            nextFeedClickAt_ = in.nowMs + 50;
        }

        if (in.nowMs >= nextScrollAt_)
        {
            if (!didFastScrollUp_)
            {
                out.burstScrollUpCount = 50;
                didFastScrollUp_ = true;
                nextScrollAt_ = in.nowMs + 400;
                nextActionAt_ = nextFeedClickAt_ < nextScrollAt_ ? nextFeedClickAt_ : nextScrollAt_;
                return out;
            }

            out.scrollDownCount = 1;
            long long downDelay = static_cast<long long>(clickDelay * 0.715);
            if (downDelay < 156) downDelay = 156;
            nextScrollAt_ = in.nowMs + downDelay;
        }

        nextActionAt_ = nextFeedClickAt_ < nextScrollAt_ ? nextFeedClickAt_ : nextScrollAt_;
        return out;

    case Phase::CloseAquarium:
        out.clickAquariumButton = true;
        phase_ = Phase::CenterClick;
        nextActionAt_ = in.nowMs + clickDelay;
        return out;

    case Phase::CenterClick:
        out.clickCenter = true;
        out.state = AquariumSequenceStepState::Completed;
        Reset();
        nextActionAt_ = in.nowMs + clickDelay;
        return out;
    }

    out.state = AquariumSequenceStepState::Failed;
    return out;
}
}
