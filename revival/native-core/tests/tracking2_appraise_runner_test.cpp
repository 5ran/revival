#include <cstdlib>
#include <iostream>

#include "tracking2_appraise_runner.hpp"

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed=0;
    macro_port::Tracking2AppraiseRunner r;
    macro_port::AppraiseSettings s;
    s.baseMutations = {"Shiny"};
    s.mode = macro_port::AppraiseRunMode::Gamepass;

    auto a = r.Step(0, s, false, false);
    failed += Assert(a.failed, "enchant-required");
    auto b = r.Step(0, s, true, true);
    failed += Assert(b.completed, "complete-on-match");

    if(failed==0){ std::cout<<"PASS\n"; return 0;} return 1;
}
