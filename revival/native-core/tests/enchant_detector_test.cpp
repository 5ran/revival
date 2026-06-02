#include <cstdlib>
#include <iostream>

#include "enchant_detector.hpp"

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed=0;
    auto e1 = macro_port::EnchantDetector::FindEnchant("Rod [Blessed]");
    failed += Assert(e1 == "Blessed", "detect-blessed");
    auto e2 = macro_port::EnchantDetector::FindEnchant("No enchant");
    failed += Assert(e2.empty(), "detect-none");
    if(failed==0){ std::cout<<"PASS\n"; return 0;} return 1;
}
