#include <cstdlib>
#include <iostream>

#include "auto_sovereign_recharge_runner.hpp"

using macro_port::AutoSovereignRechargeRunner;
using macro_port::AutoSovereignRechargeInputs;

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed = 0;
    AutoSovereignRechargeRunner r;
    AutoSovereignRechargeInputs in{};

    auto a = r.Step(0, 30, 80, 20, in);
    failed += Assert(!a.failed, "start-recharge");

    in.relicFound = false;
    auto b = r.Step(1000, 30, 80, 20, in);
    auto c = r.Step(1700, 30, 80, 20, in);
    auto d = r.Step(2400, 30, 80, 20, in);
    failed += Assert(d.failed, "relic-missing-fails");

    r.Reset();
    auto e = r.Step(3000, 30, 80, 85, in);
    failed += Assert(!e.failed, "healthy-power");

    if(failed==0){ std::cout<<"PASS\n"; return 0; }
    return 1;
}
