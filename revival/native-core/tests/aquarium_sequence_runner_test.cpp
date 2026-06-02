#include <cstdlib>
#include <iostream>

#include "aquarium_sequence_runner.hpp"

using namespace macro_port;

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed=0;
    AquariumSequenceRunner r;

    auto a = r.Step(AquariumSequenceInput{0,250,true,false,true});
    failed += Assert(a.state == AquariumSequenceStepState::Running, "resolve-running");

    auto b = r.Step(AquariumSequenceInput{1,250,true,false,true});
    failed += Assert(b.clickAquariumButton, "open-click");

    auto c = r.Step(AquariumSequenceInput{600,250,true,false,true});
    failed += Assert(!c.clickFeedAnchor, "wait-feed-anchor");

    auto d = r.Step(AquariumSequenceInput{700,250,true,true,true});
    failed += Assert(d.clickFeedAnchor, "feed-anchor-click");

    auto e = r.Step(AquariumSequenceInput{950,250,true,true,true});
    failed += Assert(e.burstScrollUpCount == 50, "burst-scroll");

    bool sawClose = false;
    bool sawCenterComplete = false;
    for (long long t = 1200; t <= 7000; t += 100) {
        auto step = r.Step(AquariumSequenceInput{t,250,true,true,true});
        if (step.clickAquariumButton) sawClose = true;
        if (step.clickCenter && step.state == AquariumSequenceStepState::Completed) {
            sawCenterComplete = true;
            break;
        }
    }
    failed += Assert(sawClose, "close-click");
    failed += Assert(sawCenterComplete, "center-complete");

    if(failed==0){ std::cout<<"PASS\n"; return 0; }
    return 1;
}
