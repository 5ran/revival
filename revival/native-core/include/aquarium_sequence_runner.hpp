#pragma once

namespace macro_port
{
enum class AquariumSequenceStepState
{
    Running,
    Completed,
    Failed,
};

struct AquariumSequenceInput
{
    long long nowMs = 0;
    long long clickDelayMs = 250;
    bool targetsAvailable = false;
    bool feedAnchorVisible = false;
    bool cursorReadable = true;
};

struct AquariumSequenceResult
{
    AquariumSequenceStepState state = AquariumSequenceStepState::Running;
    bool clickAquariumButton = false;
    bool clickFeedAnchor = false;
    bool clickCenter = false;
    int burstScrollUpCount = 0;
    int scrollDownCount = 0;
    int feedClicks = 0;
};

class AquariumSequenceRunner
{
public:
    void Reset();
    AquariumSequenceResult Step(const AquariumSequenceInput& in);

private:
    enum class Phase { Resolve, OpenAquarium, WaitAfterOpen, Feed, CloseAquarium, CenterClick };
    Phase phase_ = Phase::Resolve;
    long long nextActionAt_ = 0;
    long long feedUntilAt_ = 0;
    long long nextScrollAt_ = 0;
    long long nextFeedClickAt_ = 0;
    bool didFastScrollUp_ = false;
};
}
