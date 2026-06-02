#include <cstdlib>
#include <iostream>

#include "auto_enchant_runner.hpp"

using macro_port::AutoEnchantRunner;
using macro_port::EnchantRollMode;

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed = 0;
    AutoEnchantRunner r;

    auto a = r.Step(0, "", EnchantRollMode::Gamepass, "None", true);
    failed += Assert(a.failed, "empty-target");

    auto b = r.Step(1000, "Blessed", EnchantRollMode::Gamepass, "Blessed", true);
    failed += Assert(b.completed, "completed");

    auto c = r.Step(2000, "Blessed", EnchantRollMode::Normal, "None", false);
    failed += Assert(c.failed, "targets-fail");

    if(failed==0){ std::cout<<"PASS\n"; return 0; }
    return 1;
}
