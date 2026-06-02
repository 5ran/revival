#include <cstdlib>
#include <iostream>

#include "auto_angler_runner.hpp"

using macro_port::AutoAnglerRunner;
using macro_port::AutoAnglerSettings;

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed = 0;
    AutoAnglerRunner r;
    AutoAnglerSettings s{100,100};

    auto a = r.Step(0, s, "Carp", true, true);
    failed += Assert(!a.failed, "step1");
    auto b = r.Step(2500, s, "Carp", true, true);
    failed += Assert(!b.failed, "step2");
    (void)r.Step(5000, s, "Carp", true, true);
    auto c = r.Step(5800, s, "Carp", false, true);
    failed += Assert(c.failed, "inventory-fail");

    if(failed==0){ std::cout<<"PASS\n"; return 0; }
    return 1;
}
