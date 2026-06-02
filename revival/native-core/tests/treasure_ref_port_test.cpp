#include <cstdlib>
#include <iostream>

#include "treasure_ref_port.hpp"

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed=0;
    macro_port::TreasureAppraiser t;
    t.Reset();
    for(int i=0;i<7;i++){
        auto r = t.RunStep(true, false, 3);
        (void)r;
    }
    auto f = t.RunStep(true, false, 1);
    failed += Assert(f.state == macro_port::TreasureStepState::Running, "running-phase");
    auto c = t.RunStep(true, true, 1);
    failed += Assert(c.state == macro_port::TreasureStepState::Completed, "completed");
    if(failed==0){ std::cout<<"PASS\n"; return 0;} return 1;
}
